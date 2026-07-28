using System.Runtime.CompilerServices;
using Godot;
using Library.Light;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace Library.SpeedDice;

internal static class LibrarySpeedDiceService
{
    private enum AdvanceAction
    {
        Roll,
        Resolve,
    }

    private static readonly Lock Sync = new();
    private static readonly List<LibrarySpeedDiceRegistration> Registrations =
        [];
    private static ConditionalWeakTable<Player, LibrarySpeedDiceCombatState> States = new();
    private static WeakReference<LibrarySpeedDiceCombatState>? _localState;
    private static CardModel? _explicitlySelectedCard;

    public static void RegisterParticipant(LibrarySpeedDiceParticipant participant)
    {
        ArgumentNullException.ThrowIfNull(participant);
        participant.Validate();

        var registration = new LibrarySpeedDiceRegistration(
            participant.Id,
            participant.IsEnabledForPlayer,
            new LibrarySpeedDiceOptions(
                participant.BaseSpeedDiceCount,
                participant.MinSpeed,
                participant.MaxSpeed),
            participant.Emotion,
            light: null,
            lightStoreFactory: null,
            [new LegacyParticipantAdapter(participant)],
            participant,
            participant);
        RegisterRegistration(registration, replaceExisting: true);
    }

    internal static void RegisterRegistration(
        LibrarySpeedDiceRegistration registration,
        bool replaceExisting)
    {
        ArgumentNullException.ThrowIfNull(registration);
        lock (Sync)
        {
            int existingIndex = Registrations.FindIndex(candidate =>
                string.Equals(
                    candidate.Id,
                    registration.Id,
                    StringComparison.Ordinal));
            if (existingIndex >= 0 && !replaceExisting)
            {
                throw new InvalidOperationException(
                    $"Speed-dice registration '{registration.Id}' already exists.");
            }

            if (existingIndex >= 0)
                Registrations.RemoveAt(existingIndex);
            Registrations.Add(registration);
        }
    }

    public static bool TryGetState(
        Player player,
        out LibrarySpeedDiceCombatState? state)
    {
        state = null;
        if (player.PlayerCombatState == null)
            return false;

        LibrarySpeedDiceRegistration? registration =
            FindRegistration(player);
        if (registration == null)
            return false;

        if (!States.TryGetValue(player, out state))
        {
            state = new LibrarySpeedDiceCombatState(
                player,
                registration);
            state.ReplaceSlots(GetDiceCount(state));
            States.Add(player, state);
            registration.Dispatcher.OnStateCreated(state);
        }

        if (LocalContext.IsMe(player))
            _localState = new WeakReference<LibrarySpeedDiceCombatState>(state);

        return true;
    }

    public static bool TryGetLocalState(out LibrarySpeedDiceCombatState? state)
    {
        state = null;
        return _localState != null
            && _localState.TryGetTarget(out state)
            && IsStateUsable(state);
    }

    public static bool TryGetEquippedSlot(
        CardModel card,
        out LibrarySpeedDiceSlot? slot)
    {
        slot = null;
        var owner = card.Owner;
        if (owner == null
            || !TryGetState(owner, out var state)
            || state == null)
        {
            return false;
        }

        slot = state.Slots.FirstOrDefault(candidate =>
            ReferenceEquals(candidate.Card, card));
        return slot != null;
    }

    public static bool TryGetResolvingSlot(
        CardModel card,
        out LibrarySpeedDiceSlot? slot)
    {
        slot = null;
        var owner = card.Owner;
        if (owner == null
            || !TryGetState(owner, out var state)
            || state?.ResolvingSlot == null
            || !ReferenceEquals(state.ResolvingSlot.Card, card))
        {
            return false;
        }

        slot = state.ResolvingSlot;
        return true;
    }

    public static LibrarySpeedDiceStateSnapshot CreateSnapshot(
        LibrarySpeedDiceCombatState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return new LibrarySpeedDiceStateSnapshot(
            state.Player.PlayerCombatState?.TurnNumber ?? -1,
            state.Revision,
            state.HasRolled,
            state.IsLocked,
            state.CurrentTurnTriggeredCards,
            state.PreviousTurnTriggeredCards,
            state.BonusDrawPending,
            state.DamageGivenAccumulator,
            state.DamageReceivedAccumulator,
            state.Emotion.Level,
            state.Emotion.Units,
            state.Slots.Select(slot =>
            {
                LibrarySpeedDiceCardLease? lease = slot.Lease;
                return new LibrarySpeedDiceSlotSnapshot(
                    slot.Index,
                    slot.DisplayValue,
                    slot.FinalValue,
                    slot.IsLocked,
                    slot.IsSpent,
                    slot.Card,
                    slot.Target,
                    slot.ReservedEnergy,
                    slot.ReservedStars,
                    slot.ReservedSecondaryResources.ToDictionary(
                        pair => pair.Key,
                        pair => pair.Value,
                        StringComparer.Ordinal))
                {
                    Lease = lease == null
                        ? null
                        : new LibrarySpeedDiceLeaseSnapshot(
                            lease.Id,
                            lease.ReservationPlan.Resources.ToArray(),
                            lease.IsUseTriggered,
                            lease.IsTargetedUseTriggered,
                            lease.PreventUnequip,
                            lease.IsCommitted),
                };
            }).ToArray())
        {
            Extension = new LibrarySpeedDiceSnapshotExtension
            {
                LeaseSequence = state.LeaseSequence,
                PendingEmotionPreviousLevel =
                    state.PendingEmotionPreviousLevel,
                PendingEmotionCurrentLevel =
                    state.PendingEmotionCurrentLevel,
                DamageGivenAccumulatorThreshold =
                    state.DamageGivenAccumulatorThreshold,
                DamageReceivedAccumulatorThreshold =
                    state.DamageReceivedAccumulatorThreshold,
                Light = state.Light?.CreateSnapshot(),
            },
        };
    }

    public static bool TryRestoreSnapshot(
        Player player,
        LibrarySpeedDiceStateSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!TryGetState(player, out LibrarySpeedDiceCombatState? state)
            || state == null
            || player.PlayerCombatState?.TurnNumber != snapshot.TurnNumber
            || state.IsLifecycleBusy
            || !state.Gate.Wait(0))
        {
            return false;
        }

        try
        {
            state.Restore(snapshot);
            if (snapshot.Extension?.Light != null)
                state.Light?.Restore(snapshot.Extension.Light);

            foreach (LibrarySpeedDiceSlotSnapshot savedSlot
                     in snapshot.Slots.OrderBy(slot => slot.Index))
            {
                LibrarySpeedDiceSlot? slot = state.Slots.FirstOrDefault(
                    candidate => candidate.Index == savedSlot.Index);
                if (slot?.Card != null)
                    RestoreLease(state, slot, savedSlot);
            }

            state.Revision = Math.Max(0, snapshot.Revision);
            state.NotifyChanged();
            return true;
        }
        finally
        {
            state.Gate.Release();
        }
    }

    public static void ClearCombat()
    {
        _explicitlySelectedCard = null;
        States = new ConditionalWeakTable<Player, LibrarySpeedDiceCombatState>();
        _localState = null;
        LibraryLight.ClearCombatCosts();
    }

    public static void BeginPlayerTurn(Creature creature, CombatSide side)
    {
        if (side != CombatSide.Player
            || creature.Player == null
            || !TryGetState(creature.Player, out var state)
            || state == null
            || state.IsLocked
            || state.IsResolving
            || state.IsLifecycleBusy)
        {
            return;
        }

        state.Registration.Dispatcher.BeforePlayerTurn(state);

        state.DeferEmotionLevelChangedLifecycle = true;
        try
        {
            state.Emotion.TryLevelUp(state.Registration.Emotion);
        }
        finally
        {
            state.DeferEmotionLevelChangedLifecycle = false;
        }
        state.PreviousTurnTriggeredCards = state.CurrentTurnTriggeredCards;
        state.CurrentTurnTriggeredCards = 0;
        state.BonusDrawPending =
            state.Emotion.Level >= state.Registration.Emotion.BonusDrawLevel
            && state.PreviousTurnTriggeredCards
            >= state.Registration.Emotion.BonusDrawRequiredTriggeredCards;

        var turnMixin = unchecked(
            (uint)(state.Player.PlayerCombatState!.TurnNumber * 0x45D9F3B)
            ^ (uint)(state.Player.RunState.TotalFloor * 0x119DE1F3));
        state.GameplayRng =
            state.Registration.Dispatcher.CreateGameplayRng(state.Player)
            ?? new Rng(
                state.Player.RunState.Rng.Seed ^ turnMixin,
                "library_speed_dice");
        state.TargetRepairRng =
            state.Registration.Dispatcher.CreateTargetRepairRng(
                state.Player)
            ?? new Rng(
                state.Player.RunState.Rng.Seed ^ turnMixin,
                "library_speed_target_repair");
        state.ReplaceSlots(GetDiceCount(state));
    }

    public static async Task FinishPlayerTurnAsync(
        Player player,
        IReadOnlySet<CardModel> retainedCards)
    {
        if (!States.TryGetValue(
                player,
                out var state)
            || state.IsLocked
            || state.IsResolving
            || state.IsLifecycleBusy)
        {
            return;
        }

        await state.Gate.WaitAsync();
        try
        {
            var equippedSlots = state.Slots
                .Where(slot => slot.Card != null)
                .ToList();
            if (equippedSlots.Count == 0)
                return;

            var cardsToRetain = new List<CardModel>();
            var cardsToDiscard = new List<CardModel>();
            foreach (var slot in equippedSlots)
            {
                var card = slot.Card!;
                if (card.Pile?.Type != PileType.Play)
                {
                    ReleaseSlotCard(state, slot);
                    continue;
                }

                if (retainedCards.Contains(card))
                    cardsToRetain.Add(card);
                else
                    cardsToDiscard.Add(card);
            }

            await MoveEquippedCardsAsync(
                state,
                equippedSlots,
                cardsToRetain,
                PileType.Hand);
            await MoveEquippedCardsAsync(
                state,
                equippedSlots,
                cardsToDiscard,
                PileType.Discard);
            state.NotifyGameplayChanged();
        }
        catch (Exception exception)
        {
            Log.Error(
                "[LibraryOfRuinaLib] Failed to finish speed-dice turn cleanup: "
                + exception);
        }
        finally
        {
            state.Gate.Release();
        }
    }

    public static bool CanConsumeAdvanceInput()
    {
        if (!TryGetLocalState(out var state)
            || state == null
            || state.IsLocked
            || state.IsResolving
            || state.IsLifecycleBusy
            || state.IsSelectingTarget
            || state.Player.PlayerCombatState!.Phase != PlayerTurnPhase.Play
            || CombatManager.Instance.PlayerActionsDisabled
            || CombatManager.Instance.IsOverOrEnding)
        {
            return false;
        }

        if (state.Registration.Dispatcher.HasInputRouter
            && RunManager.Instance.NetService.Type != NetGameType.Singleplayer)
        {
            return false;
        }

        return RunManager.Instance.ActionExecutor.CurrentlyRunningAction == null;
    }

    public static async Task AdvanceLocalAsync()
    {
        if (!TryGetLocalState(out var state) || state == null)
            return;

        var choiceContext = new BlockingPlayerChoiceContext();
        if (state.HasRolled)
            await ResolveForPlayerAsync(choiceContext, state.Player);
        else
            await RollForPlayerAsync(choiceContext, state.Player);
    }

    public static async Task RollForPlayerAsync(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        ArgumentNullException.ThrowIfNull(choiceContext);
        ArgumentNullException.ThrowIfNull(player);
        if (!TryGetState(player, out LibrarySpeedDiceCombatState? state)
            || state == null
            || state.HasRolled
            || state.IsLocked
            || state.IsResolving
            || !state.TryBeginLifecycle())
        {
            return;
        }

        try
        {
            bool rolled = await AdvanceAsync(
                state,
                choiceContext,
                AdvanceAction.Roll);
            if (rolled)
            {
                int currentLevel = state.Emotion.Level;
                int previousLevel = currentLevel;
                bool emotionLevelChanged =
                    state.ConsumePendingEmotionChange(
                        out previousLevel,
                        out currentLevel);
                if (!emotionLevelChanged)
                {
                    previousLevel = state.Emotion.Level;
                    currentLevel = previousLevel;
                }
                else
                {
                    state.DispatchEmotionLevelChanged(
                        previousLevel,
                        currentLevel);
                }

                if (state.Light != null)
                {
                    await state.Light.Recover(
                        previousLevel,
                        currentLevel);
                }

                await state.Registration.Dispatcher.AfterRollAsync(
                    choiceContext,
                    state);
            }
        }
        finally
        {
            state.EndLifecycle();
        }
    }

    public static async Task ResolveForPlayerAsync(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        ArgumentNullException.ThrowIfNull(choiceContext);
        ArgumentNullException.ThrowIfNull(player);
        if (!TryGetState(player, out LibrarySpeedDiceCombatState? state)
            || state == null)
        {
            return;
        }

        if (state.IsLifecycleBusy)
            return;

        if (!state.HasRolled
            || state.IsLocked
            || state.IsResolving
            || !state.Slots.Any(slot => slot.Card != null && !slot.IsSpent)
            || !state.TryBeginLifecycle())
        {
            return;
        }

        try
        {
            bool resolved = await AdvanceAsync(
                state,
                choiceContext,
                AdvanceAction.Resolve);
            if (!resolved)
                return;

            if (state.Registration.LegacyParticipant?
                    .AfterSpeedResolutionAsync != null)
            {
                await state.Registration.LegacyParticipant
                    .AfterSpeedResolutionAsync(
                    choiceContext,
                    state);
            }
        }
        finally
        {
            state.EndLifecycle();
        }
    }

    public static async Task ResolveBatchAsync(
        PlayerChoiceContext choiceContext,
        IReadOnlyList<LibrarySpeedDiceCombatState> states)
    {
        ArgumentNullException.ThrowIfNull(choiceContext);
        ArgumentNullException.ThrowIfNull(states);
        if (states.Count == 0)
            return;

        LibrarySpeedDiceCombatState[] orderedStates = states
            .Distinct()
            .Where(state =>
                IsStateUsable(state)
                && !state.IsLocked
                && !state.IsResolving
                && !state.IsLifecycleBusy)
            .OrderBy(state => state.Player.NetId)
            .ToArray();
        if (orderedStates.Length == 0)
            return;
        LibrarySpeedDiceCombatState[] lifecycleStates = orderedStates
            .Where(state => state.TryBeginLifecycle())
            .ToArray();
        if (lifecycleStates.Length == 0)
            return;
        IReadOnlyList<LibrarySpeedDiceCombatState> resolvedStates = [];
        var acquiredStates = new List<LibrarySpeedDiceCombatState>(
            lifecycleStates.Length);
        var resolvingStates = new List<LibrarySpeedDiceCombatState>(
            lifecycleStates.Length);
        try
        {
            foreach (LibrarySpeedDiceCombatState state in lifecycleStates)
            {
                await state.Gate.WaitAsync();
                acquiredStates.Add(state);
            }

            resolvedStates = await ResolveBatchCoreAsync(
                choiceContext,
                lifecycleStates,
                resolvingStates,
                playAdvanceFeedback: true);
        }
        catch (Exception exception)
        {
            Log.Error(
                "[LibraryOfRuinaLib] Speed dice batch resolution failed: "
                + exception);
        }
        finally
        {
            foreach (LibrarySpeedDiceCombatState state in resolvingStates)
            {
                state.ResolvingSlot = null;
                state.IsResolving = false;
                state.NotifyGameplayChanged();
            }

            for (int index = acquiredStates.Count - 1; index >= 0; index--)
                acquiredStates[index].Gate.Release();
        }

        try
        {
            foreach (LibrarySpeedDiceCombatState state in resolvedStates)
            {
                if (state.Registration.LegacyParticipant?
                        .AfterSpeedResolutionAsync != null)
                {
                    await state.Registration.LegacyParticipant
                        .AfterSpeedResolutionAsync(
                        choiceContext,
                        state);
                }
            }
        }
        finally
        {
            foreach (LibrarySpeedDiceCombatState state in lifecycleStates)
                state.EndLifecycle();
        }
    }

    internal static bool CanInteractWithSlot(
        LibrarySpeedDiceCombatState state,
        int slotIndex,
        out bool canAcceptSelectedCard)
    {
        canAcceptSelectedCard = false;
        if (!LocalContext.IsMe(state.Player)
            || !IsStateUsable(state)
            || state.Player.PlayerCombatState?.Phase != PlayerTurnPhase.Play
            || !state.HasRolled
            || state.IsLocked
            || state.IsResolving
            || state.IsLifecycleBusy
            || state.IsSelectingTarget
            || slotIndex < 0
            || slotIndex >= state.Slots.Count)
        {
            return false;
        }

        var slot = state.Slots[slotIndex];
        if (slot.IsSpent)
            return false;
        if (slot.Card != null)
            return true;

        var card = GetSelectedCard();
        canAcceptSelectedCard =
            card != null
            && card.Owner == state.Player
            && card.Pile?.Type == PileType.Hand
            && !card.EnergyCost.CostsX
            && !card.HasStarCostX
            && CanEquipCard(state, card)
            && CanReserveCard(state, card);
        return canAcceptSelectedCard;
    }

    public static bool CanEquipCard(CardModel card)
    {
        return card.Owner != null
            && TryGetState(
                card.Owner,
                out var state)
            && state != null
            && !state.IsLifecycleBusy
            && CanEquipCard(state, card)
            && state.Slots.Any(slot =>
                !slot.IsSpent
                && !slot.IsLocked
                && slot.Card == null);
    }

    public static bool TryBeginEquipSelection(CardModel card)
    {
        if (_explicitlySelectedCard != null
            || !CanEquipCard(card))
        {
            return false;
        }

        _explicitlySelectedCard = card;
        if (TryGetState(
                card.Owner,
                out var state)
            && state != null)
        {
            state.NotifyChanged();
        }

        return true;
    }

    public static void EndEquipSelection(CardModel card)
    {
        if (!ReferenceEquals(_explicitlySelectedCard, card))
            return;

        _explicitlySelectedCard = null;
        if (card.Owner != null
            && TryGetState(
                card.Owner,
                out var state)
            && state != null)
        {
            state.NotifyChanged();
        }
    }

    public static async Task ActivateSlotAsync(int slotIndex, Control targetingOrigin)
    {
        if (!TryGetLocalState(out var state) || state == null)
            return;

        if (_explicitlySelectedCard != null)
            return;

        CardModel? selectedCard = GetSelectedCard();
        if (selectedCard != null
            && state.Registration.Dispatcher.HasInputRouter
            && RunManager.Instance.NetService.Type != NetGameType.Singleplayer)
        {
            await EquipCardAsync(selectedCard, slotIndex, targetingOrigin);
            return;
        }

        await ActivateSlotWithLifecycleAsync(
            state,
            slotIndex,
            targetingOrigin,
            selectedCard,
            allowRetargetExisting: true);
    }

    public static async Task EquipCardAsync(
        CardModel card,
        int slotIndex,
        Control targetingOrigin)
    {
        if (card.Owner == null
            || !TryGetState(
                card.Owner,
                out var state)
            || state == null)
        {
            return;
        }

        if (state.IsLifecycleBusy)
            return;

        if (state.Registration.Dispatcher.HasInputRouter
            && RunManager.Instance.NetService.Type != NetGameType.Singleplayer)
        {
            Creature? target = null;
            if (card.RequiresSpeedDiceTarget())
            {
                target = await SelectUnequippedCardTargetAsync(
                    state,
                    card,
                    targetingOrigin);
                if (target == null)
                    return;
            }

            if (!IsStateUsable(state)
                || state.Player.PlayerCombatState?.Phase
                != PlayerTurnPhase.Play
                || !state.HasRolled
                || state.IsLocked
                || state.IsResolving
                || slotIndex < 0
                || slotIndex >= state.Slots.Count
                || state.Slots[slotIndex].Card != null
                || card.Owner != state.Player
                || card.Pile?.Type != PileType.Hand
                || !CanEquipCard(state, card))
            {
                return;
            }

            await state.Registration.Dispatcher.RouteInputAsync(
                new LibrarySpeedDiceInputRequest(
                    LibrarySpeedDiceInputKind.Equip,
                    state.Player,
                    slotIndex,
                    state.Player.PlayerCombatState?.TurnNumber ?? -1,
                    state.Revision,
                    card,
                    target));
            return;
        }

        await ActivateSlotWithLifecycleAsync(
            state,
            slotIndex,
            targetingOrigin,
            card,
            allowRetargetExisting: false);
    }

    private static async Task ActivateSlotWithLifecycleAsync(
        LibrarySpeedDiceCombatState state,
        int slotIndex,
        Control targetingOrigin,
        CardModel? selectedCard,
        bool allowRetargetExisting)
    {
        if (!state.TryBeginLifecycle())
            return;

        try
        {
            await ActivateSlotAsync(
                state,
                slotIndex,
                targetingOrigin,
                selectedCard,
                allowRetargetExisting);
        }
        finally
        {
            state.EndLifecycle();
        }
    }

    private static async Task ActivateSlotAsync(
        LibrarySpeedDiceCombatState state,
        int slotIndex,
        Control targetingOrigin,
        CardModel? selectedCard,
        bool allowRetargetExisting)
    {
        CardModel? cardToTarget = null;
        LibrarySpeedDiceSlot? equippedSlot = null;
        if (state.IsLocked || state.IsResolving)
            return;

        await state.Gate.WaitAsync();
        try
        {
            if (!IsStateUsable(state)
                || state.Player.PlayerCombatState?.Phase
                != PlayerTurnPhase.Play
                || !state.HasRolled
                || state.IsLocked
                || state.IsResolving
                || slotIndex < 0
                || slotIndex >= state.Slots.Count)
            {
                return;
            }

            var slot = state.Slots[slotIndex];
            if (slot.Card != null)
            {
                if (!allowRetargetExisting)
                    return;

                cardToTarget = slot.Card.RequiresSpeedDiceTarget()
                    ? slot.Card
                    : null;
            }
            else
            {
                var card = selectedCard;
                if (card == null
                    || card.Owner != state.Player
                    || card.Pile?.Type != PileType.Hand
                    || card.EnergyCost.CostsX
                    || card.HasStarCostX
                    || !CanEquipCard(state, card))
                {
                    return;
                }

                if (!TryCreateReservationLease(
                        state,
                        card,
                        slot,
                        out LibrarySpeedDiceCardLease? lease)
                    || lease == null)
                    return;

                var hand = NPlayerHand.Instance;
                if (hand != null)
                {
                    hand.CancelAllCardPlay();
                    if (hand.GetCardHolder(card) != null)
                        hand.Remove(card);
                }

                var result = await CardPileCmd.Add(
                    card,
                    PileType.Play,
                    skipVisuals: true);
                if (!result.success)
                {
                    lease.Transaction.Release();
                    await CardPileCmd.Add(card, PileType.Hand);
                    return;
                }

                slot.Card = card;
                slot.Target = null;
                slot.SetLease(lease);
                ApplyLegacyReservationProjection(state, card, slot);
                equippedSlot = slot;
                cardToTarget = card.RequiresSpeedDiceTarget() ? card : null;
                state.NotifyGameplayChanged();
            }
        }
        catch (Exception exception)
        {
            Log.Error("[LibraryOfRuinaLib] Failed to activate a speed-die slot: " + exception);
        }
        finally
        {
            state.Gate.Release();
        }

        if (equippedSlot?.Lease != null)
        {
            await TriggerUseAsync(
                new BlockingPlayerChoiceContext(),
                state,
                equippedSlot);
        }

        if (cardToTarget != null)
            await SelectTargetAsync(state, slotIndex, cardToTarget, targetingOrigin);

        if (equippedSlot?.Card != null)
        {
            await state.Registration.Dispatcher.AfterCardEquippedAsync(
                new BlockingPlayerChoiceContext(),
                state,
                equippedSlot);
        }
    }

    public static async Task UnequipCardAsync(int slotIndex)
    {
        if (!TryGetLocalState(out var state) || state == null)
            return;

        if (state.Registration.Dispatcher.HasInputRouter
            && RunManager.Instance.NetService.Type != NetGameType.Singleplayer)
        {
            await state.Registration.Dispatcher.RouteInputAsync(
                new LibrarySpeedDiceInputRequest(
                    LibrarySpeedDiceInputKind.Unequip,
                    state.Player,
                    slotIndex,
                    state.Player.PlayerCombatState?.TurnNumber ?? -1,
                    state.Revision));
            return;
        }

        await ExecuteUnequipAsync(
            state.Player,
            slotIndex,
            state.Player.PlayerCombatState?.TurnNumber ?? -1,
            state.Revision);
    }

    public static async Task<bool> ExecuteEquipAsync(
        PlayerChoiceContext choiceContext,
        Player player,
        CardModel card,
        int slotIndex,
        Creature? target,
        int expectedTurnNumber,
        int expectedRevision)
    {
        ArgumentNullException.ThrowIfNull(choiceContext);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(card);
        if (!TryGetState(player, out LibrarySpeedDiceCombatState? state)
            || state == null)
        {
            return false;
        }

        if (state.IsLocked
            || state.IsResolving
            || !state.TryBeginLifecycle())
            return false;

        try
        {
            LibrarySpeedDiceSlot? equippedSlot = null;
            await state.Gate.WaitAsync();
            try
            {
                if (!CanExecuteInput(
                        state,
                        slotIndex,
                        expectedTurnNumber,
                        expectedRevision)
                    || card.Owner != player
                    || card.Pile?.Type != PileType.Hand
                    || state.Slots[slotIndex].Card != null
                    || !CanEquipCard(state, card)
                    || card.RequiresSpeedDiceTarget()
                    && !card.IsValidSpeedDiceTarget(target))
                {
                    return false;
                }

                LibrarySpeedDiceSlot slot = state.Slots[slotIndex];
                if (!TryCreateReservationLease(
                        state,
                        card,
                        slot,
                        out LibrarySpeedDiceCardLease? lease)
                    || lease == null)
                {
                    return false;
                }

                var result = await CardPileCmd.Add(
                    card,
                    PileType.Play,
                    skipVisuals: true);
                if (!result.success)
                {
                    lease.Transaction.Release();
                    return false;
                }

                slot.Card = card;
                slot.Target = target;
                slot.SetLease(lease);
                ApplyLegacyReservationProjection(state, card, slot);
                state.NotifyGameplayChanged();
                equippedSlot = slot;
            }
            catch (Exception exception)
            {
                Log.Error(
                    "[LibraryOfRuinaLib] Synchronized speed-die equip failed: "
                    + exception);
                return false;
            }
            finally
            {
                state.Gate.Release();
            }

            if (equippedSlot?.Lease != null)
            {
                await TriggerUseAsync(
                    choiceContext,
                    state,
                    equippedSlot);
                if (target != null)
                {
                    await TriggerTargetedUseAsync(
                        choiceContext,
                        state,
                        equippedSlot,
                        target);
                }
            }

            if (equippedSlot != null)
            {
                await state.Registration.Dispatcher.AfterCardEquippedAsync(
                    choiceContext,
                    state,
                    equippedSlot);
            }

            return true;
        }
        finally
        {
            state.EndLifecycle();
        }
    }

    public static Task RequestEquipAsync(
        Player player,
        CardModel card,
        int slotIndex,
        Creature? target,
        int expectedTurnNumber,
        int expectedRevision)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(card);
        return RequestInputAsync(
            new LibrarySpeedDiceInputRequest(
                LibrarySpeedDiceInputKind.Equip,
                player,
                slotIndex,
                expectedTurnNumber,
                expectedRevision,
                card,
                target));
    }

    public static Task RequestUnequipAsync(
        Player player,
        int slotIndex,
        int expectedTurnNumber,
        int expectedRevision)
    {
        ArgumentNullException.ThrowIfNull(player);
        return RequestInputAsync(
            new LibrarySpeedDiceInputRequest(
                LibrarySpeedDiceInputKind.Unequip,
                player,
                slotIndex,
                expectedTurnNumber,
                expectedRevision));
    }

    public static Task RequestRetargetAsync(
        Player player,
        int slotIndex,
        Creature target,
        int expectedTurnNumber,
        int expectedRevision)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(target);
        return RequestInputAsync(
            new LibrarySpeedDiceInputRequest(
                LibrarySpeedDiceInputKind.Retarget,
                player,
                slotIndex,
                expectedTurnNumber,
                expectedRevision,
                Target: target));
    }

    private static async Task RequestInputAsync(
        LibrarySpeedDiceInputRequest request)
    {
        if (!LocalContext.IsMe(request.Player)
            || !TryGetState(
                request.Player,
                out LibrarySpeedDiceCombatState? state)
            || state == null)
        {
            return;
        }

        if (state.Registration.Dispatcher.HasInputRouter
            && RunManager.Instance.NetService.Type != NetGameType.Singleplayer)
        {
            if (await state.Registration.Dispatcher.RouteInputAsync(request))
                return;
        }

        switch (request.Kind)
        {
            case LibrarySpeedDiceInputKind.Equip when request.Card != null:
                await ExecuteEquipAsync(
                    new BlockingPlayerChoiceContext(),
                    request.Player,
                    request.Card,
                    request.SlotIndex,
                    request.Target,
                    request.TurnNumber,
                    request.Revision);
                break;
            case LibrarySpeedDiceInputKind.Unequip:
                await ExecuteUnequipAsync(
                    request.Player,
                    request.SlotIndex,
                    request.TurnNumber,
                    request.Revision);
                break;
            case LibrarySpeedDiceInputKind.Retarget
                when request.Target != null:
                await ExecuteRetargetAsync(
                    request.Player,
                    request.SlotIndex,
                    request.Target,
                    request.TurnNumber,
                    request.Revision);
                break;
        }
    }

    public static async Task<bool> ExecuteUnequipAsync(
        Player player,
        int slotIndex,
        int expectedTurnNumber,
        int expectedRevision)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (!TryGetState(player, out LibrarySpeedDiceCombatState? state)
            || state == null)
        {
            return false;
        }

        if (state.IsLocked
            || state.IsResolving
            || !state.TryBeginLifecycle())
            return false;

        try
        {
            await state.Gate.WaitAsync();
            try
            {
                if (!CanExecuteInput(
                        state,
                        slotIndex,
                        expectedTurnNumber,
                        expectedRevision))
                {
                    return false;
                }

                LibrarySpeedDiceSlot slot = state.Slots[slotIndex];
                CardModel? card = slot.Card;
                if (card == null
                    || slot.Lease?.PreventUnequip == true
                    || !CanParticipantUnequipCard(state, card))
                    return false;

                var result = await CardPileCmd.Add(card, PileType.Hand);
                if (!result.success)
                    return false;

                ReleaseSlotCard(state, slot);
                state.NotifyGameplayChanged();
                return true;
            }
            catch (Exception exception)
            {
                Log.Error(
                    "[LibraryOfRuinaLib] Synchronized speed-die unequip failed: "
                    + exception);
                return false;
            }
            finally
            {
                state.Gate.Release();
            }
        }
        finally
        {
            state.EndLifecycle();
        }
    }

    public static async Task<bool> ExecuteRetargetAsync(
        Player player,
        int slotIndex,
        Creature target,
        int expectedTurnNumber,
        int expectedRevision)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(target);
        if (!TryGetState(player, out LibrarySpeedDiceCombatState? state)
            || state == null)
        {
            return false;
        }

        if (state.IsLocked
            || state.IsResolving
            || !state.TryBeginLifecycle())
            return false;

        try
        {
            LibrarySpeedDiceSlot? retargetedSlot = null;
            await state.Gate.WaitAsync();
            try
            {
                if (!CanExecuteInput(
                        state,
                        slotIndex,
                        expectedTurnNumber,
                        expectedRevision))
                {
                    return false;
                }

                LibrarySpeedDiceSlot slot = state.Slots[slotIndex];
                if (slot.Card == null
                    || !slot.Card.RequiresSpeedDiceTarget()
                    || !slot.Card.IsValidSpeedDiceTarget(target))
                {
                    return false;
                }

                slot.Target = target;
                state.NotifyGameplayChanged();
                retargetedSlot = slot;
            }
            finally
            {
                state.Gate.Release();
            }

            if (retargetedSlot != null)
            {
                await TriggerTargetedUseAsync(
                    new BlockingPlayerChoiceContext(),
                    state,
                    retargetedSlot,
                    target);
                return true;
            }

            return false;
        }
        finally
        {
            state.EndLifecycle();
        }
    }

    private static bool CanExecuteInput(
        LibrarySpeedDiceCombatState state,
        int slotIndex,
        int expectedTurnNumber,
        int expectedRevision)
    {
        return IsStateUsable(state)
            && state.Player.PlayerCombatState?.Phase == PlayerTurnPhase.Play
            && state.HasRolled
            && !state.IsLocked
            && !state.IsResolving
            && slotIndex >= 0
            && slotIndex < state.Slots.Count
            && state.Player.PlayerCombatState?.TurnNumber
            == expectedTurnNumber
            && state.Revision == expectedRevision;
    }

    /// <summary>
    /// 应用速度骰子资源预定的限制：如果其他速度骰子已经预定了部分能量/光芒，
    /// 则当前卡必须用剩余资源支付。能量不足时可用光芒补足缺口（1:2比率），
    /// 设置对应的UnplayableReason。
    /// </summary>
    public static void ApplyReservedResourceRestriction(
        CardModel card,
        ref UnplayableReason reason,
        ref bool result)
    {
        Player? owner = card.Owner;
        if (owner == null
            || !TryGetState(owner, out var state)
            || state == null
            || state.ReservedEnergy <= 0 && state.ReservedStars <= 0)
        {
            return;
        }

        var resources = owner.PlayerCombatState;
        if (resources == null)
            return;
        // 可用资源 = 总资源 - 已被其他速度骰子预定的部分
        var energyAvailable = Math.Max(0, resources.Energy - state.ReservedEnergy);
        var starsAvailable = Math.Max(0, resources.Stars - state.ReservedStars);
        var energyCost = Math.Max(0, card.EnergyCost.GetWithModifiers(CostModifiers.All));
        var starCost = Math.Max(0, card.GetStarCostWithModifiers());

        // 能量不足时，用光芒补足缺口
        // 兑换比率：1点能量缺口 = 2点光芒额外消耗
        if (energyCost > energyAvailable
            && card.CombatState != null
            && Hook.ShouldPayExcessEnergyCostWithStars(
                card.CombatState,
                owner))
        {
            starCost += (energyCost - energyAvailable) * 2;
            energyCost = energyAvailable;
        }

        if (energyCost > energyAvailable)
            reason |= UnplayableReason.EnergyCostTooHigh;
        if (starCost > starsAvailable)
            reason |= UnplayableReason.StarCostTooHigh;
        result = reason == UnplayableReason.None;
    }

    public static int GetMaxEnergyBonus(Player player)
    {
        return States.TryGetValue(player, out var state)
            && IsStateUsable(state)
            ? state.Emotion.Level
              * state.Registration.Emotion.MaxEnergyPerLevel
            : 0;
    }

    public static void AddInitialHandDrawBonus(
        Player player,
        bool fromHandDraw,
        ref decimal count)
    {
        if (!fromHandDraw
            || !TryGetState(player, out var state)
            || state == null
            || !state.BonusDrawPending)
        {
            return;
        }

        state.BonusDrawPending = false;
        count += state.Registration.Emotion.BonusDrawAmount;
        state.NotifyGameplayChanged();
    }

    public static void RecordDamageGiven(
        Creature? dealer,
        DamageResult result,
        Creature target)
    {
        if (dealer?.Player == null
            || target.Side == dealer.Side
            || !TryGetState(dealer.Player, out var state)
            || state == null)
        {
            return;
        }

        if (state.Registration.Emotion.GainEmotionFromDamage)
        {
            AddDamageEmotion(
                state,
                Math.Max(0, result.UnblockedDamage - result.OverkillDamage),
                target.MaxHp,
                isDamageGiven: true);
        }
        if (result.WasTargetKilled)
            AddEmotionUnits(
                state,
                state.Registration.Emotion.KillEmotionUnits);
    }

    public static void RecordDamageReceived(Creature target, DamageResult result)
    {
        if (target.Player == null)
        {
            return;
        }

        if (TryGetState(
                target.Player,
                out var targetState)
            && targetState != null
            && targetState.Registration.Emotion.GainEmotionFromDamage)
        {
            AddDamageEmotion(
                targetState,
                Math.Max(0, result.UnblockedDamage - result.OverkillDamage),
                target.MaxHp,
                isDamageGiven: false);
        }
    }

    public static void RecordAllyDeath(
        ICombatState? combatState,
        Creature creature,
        bool wasRemovalPrevented)
    {
        if (wasRemovalPrevented
            || creature.Player == null
            || combatState == null)
        {
            return;
        }

        foreach (var ally in combatState.Players)
        {
            if (ally == creature.Player
                || ally.Creature.IsDead
                || !TryGetState(
                    ally,
                    out var allyState)
                || allyState == null)
            {
                continue;
            }

            AddEmotionUnits(
                allyState,
                allyState.Registration.Emotion.AllyDeathEmotionUnits);
        }
    }

    private static async Task<bool> AdvanceAsync(
        LibrarySpeedDiceCombatState state,
        PlayerChoiceContext choiceContext,
        AdvanceAction action)
    {
        if (state.IsLocked || state.IsResolving)
            return false;

        await state.Gate.WaitAsync();
        try
        {
            if (!IsStateUsable(state)
                || state.IsLocked
                || state.IsResolving
                || (action == AdvanceAction.Roll && state.HasRolled)
                || (action == AdvanceAction.Resolve && !state.HasRolled))
            {
                return false;
            }

            LibrarySpeedDiceAudio.PlayAdvance();
            NPlayerHand.Instance?.CancelAllCardPlay();
            if (!state.HasRolled)
            {
                var emotionUnits = 0;
                foreach (var slot in state.Slots)
                {
                    slot.FinalValue = state.GameplayRng.NextInt(
                        state.Registration.Options.MinRoll,
                        state.Registration.Options.MaxRoll + 1);
                    slot.DisplayValue = slot.FinalValue;
                    if (slot.FinalValue
                            == state.Registration.Options.MinRoll
                        || slot.FinalValue
                            == state.Registration.Options.MaxRoll)
                    {
                        emotionUnits += state.Registration.Emotion
                            .ExtremeRollEmotionUnits;
                    }
                }

                AddEmotionUnits(state, emotionUnits);
                state.HasRolled = true;
                state.NotifyGameplayChanged();
                return true;
            }

            var resolvingStates = new List<LibrarySpeedDiceCombatState>(1);
            IReadOnlyList<LibrarySpeedDiceCombatState> resolvedStates =
                await ResolveBatchCoreAsync(
                choiceContext,
                [state],
                resolvingStates,
                playAdvanceFeedback: false);
            return resolvedStates.Count > 0;
        }
        catch (Exception exception)
        {
            Log.Error("[LibraryOfRuinaLib] Speed dice advance failed: " + exception);
            return false;
        }
        finally
        {
            state.ResolvingSlot = null;
            state.IsResolving = false;
            state.NotifyGameplayChanged();
            state.Gate.Release();
        }
    }

    private static async Task<IReadOnlyList<LibrarySpeedDiceCombatState>>
        ResolveBatchCoreAsync(
        PlayerChoiceContext choiceContext,
        IReadOnlyList<LibrarySpeedDiceCombatState> candidateStates,
        ICollection<LibrarySpeedDiceCombatState> resolvingStates,
        bool playAdvanceFeedback)
    {
        var states = new List<LibrarySpeedDiceCombatState>(
            candidateStates.Count);
        foreach (LibrarySpeedDiceCombatState state in candidateStates)
        {
            if (!IsStateUsable(state)
                || !state.HasRolled
                || state.IsLocked
                || state.IsResolving
                || !await RepairInvalidTargetsBeforeResolutionAsync(state)
                || !state.Slots.Any(slot =>
                    slot.Card != null && !slot.IsSpent))
            {
                continue;
            }

            states.Add(state);
        }

        if (states.Count == 0)
            return [];

        if (playAdvanceFeedback)
        {
            LibrarySpeedDiceAudio.PlayAdvance();
            NPlayerHand.Instance?.CancelAllCardPlay();
        }

        var batchContexts = new Dictionary<
            LibrarySpeedDiceCombatState,
            LibrarySpeedDiceResolutionBatchContext>();
        foreach (LibrarySpeedDiceCombatState state in states)
        {
            resolvingStates.Add(state);
            foreach (LibrarySpeedDiceSlot slot in state.Slots)
                slot.IsLocked = true;

            state.IsLocked = true;
            state.IsResolving = true;
            state.NotifyGameplayChanged();
            LibrarySpeedDiceSlot[] stateSlots = state.Slots
                .OrderByDescending(slot => slot.FinalValue)
                .ThenBy(slot => slot.Index)
                .ToArray();
            batchContexts[state] =
                new LibrarySpeedDiceResolutionBatchContext(
                    choiceContext,
                    state,
                    stateSlots);
        }

        var startedBatches =
            new List<LibrarySpeedDiceResolutionBatchContext>(states.Count);
        try
        {
            foreach (LibrarySpeedDiceCombatState state in states)
            {
                LibrarySpeedDiceResolutionBatchContext batchContext =
                    batchContexts[state];
                await state.Registration.Dispatcher
                    .BeforeResolutionBatchAsync(batchContext);
                startedBatches.Add(batchContext);
            }

            var orderedSlots = states
                .SelectMany(state => state.Slots.Select(slot =>
                    (State: state, Slot: slot)))
                .Where(item =>
                    item.Slot.Card != null && !item.Slot.IsSpent)
                .OrderByDescending(item => item.Slot.FinalValue)
                .ThenBy(item => item.State.Player.NetId)
                .ThenBy(item => item.Slot.Index)
                .ToArray();
            foreach ((LibrarySpeedDiceCombatState state,
                     LibrarySpeedDiceSlot slot) in orderedSlots)
            {
                CardModel? card = slot.Card;
                LibrarySpeedDiceCardLease? lease = slot.Lease;
                if (card == null || lease == null || slot.IsSpent)
                    continue;

                state.ResolvingSlot = slot;
                state.NotifyGameplayChanged();
                var cardContext =
                    new LibrarySpeedDiceCardResolutionContext(
                        batchContexts[state],
                        slot,
                        card,
                        lease);
                bool triggered = false;
                try
                {
                    await state.Registration.Dispatcher
                        .BeforeCardResolutionAsync(cardContext);
                    triggered = await ResolveCardAsync(cardContext);
                    if (triggered)
                        state.CurrentTurnTriggeredCards++;
                }
                finally
                {
                    try
                    {
                        await state.Registration.Dispatcher
                            .AfterCardResolutionAsync(
                                cardContext,
                                triggered);
                    }
                    finally
                    {
                        state.ResolvingSlot = null;
                        slot.IsSpent = true;
                        ReleaseSlotCard(state, slot);
                        state.NotifyGameplayChanged();
                    }
                }
            }
        }
        finally
        {
            foreach (LibrarySpeedDiceResolutionBatchContext batchContext
                     in startedBatches)
            {
                await batchContext.State.Registration.Dispatcher
                    .AfterResolutionBatchAsync(batchContext);
            }
        }

        return states;
    }

    private static async Task<bool> ResolveCardAsync(
        LibrarySpeedDiceCardResolutionContext resolution)
    {
        LibrarySpeedDiceCombatState state = resolution.State;
        LibrarySpeedDiceSlot slot = resolution.Slot;
        CardModel card = resolution.Card;
        LibrarySpeedDiceCardLease lease = resolution.Lease;
        PlayerChoiceContext choiceContext = resolution.ChoiceContext;
        try
        {
            var target = slot.Target;
            if (!card.IsValidSpeedDiceTarget(target))
            {
                target = GetRandomValidTarget(state, card);
                slot.Target = target;
                state.NotifyGameplayChanged();
            }

            var clashContext = new LibraryClashContext(
                state.Player,
                slot,
                target,
                choiceContext);
            await LibraryClashResolver.Current.ResolveAsync(clashContext);
            target = clashContext.Target;
            if (!clashContext.CancelCard
                && !card.IsValidSpeedDiceTarget(target))
            {
                target = GetRandomValidTarget(state, card);
                slot.Target = target;
                state.NotifyGameplayChanged();
            }

            card.CanPlay(out var reason, out _);
            reason &= ~(
                UnplayableReason.EnergyCostTooHigh
                | UnplayableReason.StarCostTooHigh);
            if (clashContext.CancelCard
                || reason != UnplayableReason.None
                || !card.IsValidSpeedDiceTarget(target))
            {
                await ReturnCardToHandAsync(card);
                return false;
            }

            if (!lease.IsCommitted
                && !await lease.Transaction.CommitAsync())
            {
                await ReturnCardToHandAsync(card);
                return false;
            }

            if (!lease.IsCommitted)
            {
                lease.IsCommitted = true;
                state.NotifyGameplayChanged();
            }
            int reservedEnergy =
                lease.ReservationPlan.ReservedEnergy;
            int reservedStars =
                lease.ReservationPlan.ReservedStars;
            var resources = new ResourceInfo
            {
                EnergySpent = reservedEnergy,
                EnergyValue = reservedEnergy,
                StarsSpent = reservedStars,
                StarValue = reservedStars,
            };
            await card.OnPlayWrapper(
                choiceContext,
                target,
                isAutoPlay: false,
                resources);
            return true;
        }
        catch (Exception exception)
        {
            Log.Error(
                $"[LibraryOfRuinaLib] Speed die card {card.Id.Entry} failed: {exception}");
            await DiscardFailedCardAsync(card);
            return false;
        }
    }

    private static async Task SelectTargetAsync(
        LibrarySpeedDiceCombatState state,
        int slotIndex,
        CardModel card,
        Control targetingOrigin)
    {
        if (!GodotObject.IsInstanceValid(targetingOrigin)
            || state.Player.PlayerCombatState?.Phase
            != PlayerTurnPhase.Play
            || !state.HasRolled
            || state.IsSelectingTarget)
        {
            return;
        }

        await targetingOrigin.ToSignal(
            targetingOrigin.GetTree(),
            SceneTree.SignalName.ProcessFrame);

        if (!IsStateUsable(state)
            || state.Player.PlayerCombatState?.Phase
            != PlayerTurnPhase.Play
            || !state.HasRolled
            || state.IsLocked
            || state.IsResolving
            || state.IsSelectingTarget
            || slotIndex < 0
            || slotIndex >= state.Slots.Count
            || !ReferenceEquals(state.Slots[slotIndex].Card, card)
            || !card.RequiresSpeedDiceTarget()
            || NTargetManager.Instance.IsInSelection)
        {
            return;
        }

        state.IsSelectingTarget = true;
        state.NotifyChanged();
        LibrarySpeedDiceTargetLine? targetLine = null;
        bool targetAssigned = false;
        Creature? assignedTarget = null;
        try
        {
            var targetManager = NTargetManager.Instance;
            var targetMode =
                NControllerManager.Instance?.IsUsingController == true
                    ? TargetMode.Controller
                    : TargetMode.ClickMouseToTarget;
            targetManager.StartTargeting(
                card.GetSpeedDiceTargetType(),
                targetingOrigin,
                targetMode,
                () =>
                    !IsStateUsable(state)
                    || state.Player.PlayerCombatState?.Phase
                    != PlayerTurnPhase.Play
                    || !state.HasRolled
                    || state.IsLocked
                    || state.IsResolving
                    || slotIndex < 0
                    || slotIndex >= state.Slots.Count
                    || !ReferenceEquals(state.Slots[slotIndex].Card, card),
                node =>
                {
                    var target = GetCreatureFromTargetNode(node);
                    return target != null && card.IsValidSpeedDiceTarget(target);
                });
            targetLine = LibrarySpeedDiceTargetLine.Begin(
                targetManager,
                targetingOrigin,
                targetMode == TargetMode.Controller);

            var selectedNode = await targetManager.SelectionFinished();
            var selectedTarget = GetCreatureFromTargetNode(selectedNode);
            if (selectedTarget == null)
                return;

            if (state.Registration.Dispatcher.HasInputRouter
                && RunManager.Instance.NetService.Type
                != NetGameType.Singleplayer)
            {
                if (IsStateUsable(state)
                    && state.Player.PlayerCombatState?.Phase
                    == PlayerTurnPhase.Play
                    && state.HasRolled
                    && !state.IsLocked
                    && !state.IsResolving
                    && slotIndex >= 0
                    && slotIndex < state.Slots.Count
                    && ReferenceEquals(state.Slots[slotIndex].Card, card)
                    && card.IsValidSpeedDiceTarget(selectedTarget))
                {
                    await state.Registration.Dispatcher.RouteInputAsync(
                        new LibrarySpeedDiceInputRequest(
                            LibrarySpeedDiceInputKind.Retarget,
                            state.Player,
                            slotIndex,
                            state.Player.PlayerCombatState?.TurnNumber ?? -1,
                            state.Revision,
                            card,
                            selectedTarget));
                }
                return;
            }

            if (state.IsLocked || state.IsResolving)
                return;

            await state.Gate.WaitAsync();
            try
            {
                if (IsStateUsable(state)
                    && state.Player.PlayerCombatState?.Phase
                    == PlayerTurnPhase.Play
                    && state.HasRolled
                    && !state.IsLocked
                    && !state.IsResolving
                    && slotIndex >= 0
                    && slotIndex < state.Slots.Count
                    && ReferenceEquals(state.Slots[slotIndex].Card, card)
                    && card.IsValidSpeedDiceTarget(selectedTarget))
                {
                    state.Slots[slotIndex].Target = selectedTarget;
                    state.NotifyGameplayChanged();
                    targetAssigned = true;
                    assignedTarget = selectedTarget;
                }
            }
            finally
            {
                state.Gate.Release();
            }

            if (targetAssigned
                && assignedTarget != null
                && slotIndex >= 0
                && slotIndex < state.Slots.Count)
            {
                await TriggerTargetedUseAsync(
                    new BlockingPlayerChoiceContext(),
                    state,
                    state.Slots[slotIndex],
                    assignedTarget);
            }
        }
        catch (Exception exception)
        {
            Log.Error("[LibraryOfRuinaLib] Failed to select a speed-die target: " + exception);
        }
        finally
        {
            targetLine?.Stop();
            state.IsSelectingTarget = false;
            state.NotifyChanged();
        }
    }

    private static async Task<Creature?> SelectUnequippedCardTargetAsync(
        LibrarySpeedDiceCombatState state,
        CardModel card,
        Control targetingOrigin)
    {
        if (!GodotObject.IsInstanceValid(targetingOrigin)
            || state.Player.PlayerCombatState?.Phase
            != PlayerTurnPhase.Play
            || !state.HasRolled
            || state.IsSelectingTarget)
        {
            return null;
        }

        await targetingOrigin.ToSignal(
            targetingOrigin.GetTree(),
            SceneTree.SignalName.ProcessFrame);

        if (!IsStateUsable(state)
            || state.Player.PlayerCombatState?.Phase
            != PlayerTurnPhase.Play
            || !state.HasRolled
            || state.IsLocked
            || state.IsResolving
            || state.IsSelectingTarget
            || card.Owner != state.Player
            || card.Pile?.Type != PileType.Hand
            || NTargetManager.Instance.IsInSelection)
        {
            return null;
        }

        state.IsSelectingTarget = true;
        state.NotifyChanged();
        LibrarySpeedDiceTargetLine? targetLine = null;
        try
        {
            NTargetManager targetManager = NTargetManager.Instance;
            TargetMode targetMode =
                NControllerManager.Instance?.IsUsingController == true
                    ? TargetMode.Controller
                    : TargetMode.ClickMouseToTarget;
            targetManager.StartTargeting(
                card.GetSpeedDiceTargetType(),
                targetingOrigin,
                targetMode,
                () =>
                    !IsStateUsable(state)
                    || state.Player.PlayerCombatState?.Phase
                    != PlayerTurnPhase.Play
                    || !state.HasRolled
                    || state.IsLocked
                    || state.IsResolving
                    || card.Pile?.Type != PileType.Hand,
                node =>
                {
                    Creature? target = GetCreatureFromTargetNode(node);
                    return target != null
                        && card.IsValidSpeedDiceTarget(target);
                });
            targetLine = LibrarySpeedDiceTargetLine.Begin(
                targetManager,
                targetingOrigin,
                targetMode == TargetMode.Controller);

            Creature? selectedTarget = GetCreatureFromTargetNode(
                await targetManager.SelectionFinished());
            return selectedTarget != null
                && state.Player.PlayerCombatState?.Phase
                == PlayerTurnPhase.Play
                && card.Pile?.Type == PileType.Hand
                && card.IsValidSpeedDiceTarget(selectedTarget)
                    ? selectedTarget
                    : null;
        }
        catch (Exception exception)
        {
            Log.Error(
                "[LibraryOfRuinaLib] Failed to select a target for a synchronized speed-die equip: "
                + exception);
            return null;
        }
        finally
        {
            targetLine?.Stop();
            state.IsSelectingTarget = false;
            state.NotifyChanged();
        }
    }

    private static Creature? GetCreatureFromTargetNode(Node? node)
    {
        return node switch
        {
            NCreature creature => creature.Entity,
            NMultiplayerPlayerState playerState => playerState.Player.Creature,
            _ => null,
        };
    }

    private static async Task ReturnCardToHandAsync(CardModel card)
    {
        if (card.Pile?.Type == PileType.Play)
            await CardPileCmd.Add(card, PileType.Hand);
    }

    private static async Task DiscardFailedCardAsync(CardModel card)
    {
        if (card.Pile?.Type == PileType.Play)
            await CardPileCmd.Add(card, PileType.Discard);
    }

    private static CardModel? GetSelectedCard()
    {
        if (_explicitlySelectedCard is { } explicitCard)
        {
            if (explicitCard.Pile?.Type == PileType.Hand)
                return explicitCard;

            _explicitlySelectedCard = null;
        }
        return null;
    }

    private static async Task MoveEquippedCardsAsync(
        LibrarySpeedDiceCombatState state,
        IReadOnlyList<LibrarySpeedDiceSlot> equippedSlots,
        IReadOnlyList<CardModel> cards,
        PileType destination)
    {
        if (cards.Count == 0)
            return;

        var results = await CardPileCmd.Add(
            cards,
            destination,
            skipVisuals: destination != PileType.Hand);
        foreach (var result in results)
        {
            if (!result.success)
            {
                Log.Error(
                    $"[LibraryOfRuinaLib] Failed to move unused speed-dice card "
                    + $"{result.cardAdded.Id.Entry} to {destination}.");
                continue;
            }

            var slot = equippedSlots.FirstOrDefault(
                candidate => ReferenceEquals(
                    candidate.Card,
                    result.cardAdded));
            if (slot != null)
                ReleaseSlotCard(state, slot);
        }
    }

    private static bool CanEquipCard(
        LibrarySpeedDiceCombatState state,
        CardModel card)
    {
        if (!IsStateUsable(state)
            || state.Player.PlayerCombatState?.Phase != PlayerTurnPhase.Play
            || !state.HasRolled
            || state.IsLocked
            || state.IsResolving
            || state.IsSelectingTarget
            || card.Owner != state.Player
            || card.Pile?.Type != PileType.Hand
            || card.EnergyCost.CostsX
            || card.HasStarCostX
            || !CanParticipantEquipCard(state, card))
        {
            return false;
        }

        return CanReserveCard(state, card);
    }

    private static bool TryGetLegacySecondaryResourceReservations(
        LibrarySpeedDiceCombatState state,
        CardModel card,
        out IReadOnlyDictionary<string, int> reservations)
    {
        reservations = new Dictionary<string, int>(
            StringComparer.Ordinal);
        LibrarySpeedDiceParticipant? legacy =
            state.Registration.LegacyParticipant;
        if (legacy?.GetSecondaryResourceReservations == null)
            return true;

        try
        {
            IReadOnlyDictionary<string, int>? result =
                legacy.GetSecondaryResourceReservations(
                    state,
                    card);
            if (result == null)
                return false;

            reservations = result
                .Where(pair =>
                    !string.IsNullOrWhiteSpace(pair.Key)
                    && pair.Value > 0)
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value,
                    StringComparer.Ordinal);
            return true;
        }
        catch (Exception exception)
        {
            Log.Error(
                $"[LibraryOfRuinaLib] Participant {state.Registration.Id} secondary-resource reservation failed: {exception}");
            return false;
        }
    }

    private static bool CanReserveCard(
        LibrarySpeedDiceCombatState state,
        CardModel card)
    {
        return TryCalculateReservation(
                state,
                card,
                out _,
                out _)
            && TryGetLegacySecondaryResourceReservations(
                state,
                card,
                out _)
            && TryCalculateLightReservation(
                state,
                card,
                out _);
    }

    private static bool TryCreateReservationLease(
        LibrarySpeedDiceCombatState state,
        CardModel card,
        LibrarySpeedDiceSlot slot,
        out LibrarySpeedDiceCardLease? lease)
    {
        lease = null;
        if (!TryCalculateReservation(
                state,
                card,
                out int energy,
                out int stars)
            || !TryGetLegacySecondaryResourceReservations(
                state,
                card,
                out IReadOnlyDictionary<string, int> legacySecondary)
            || !TryCalculateLightReservation(
                state,
                card,
                out int light))
        {
            return false;
        }

        string leaseId = state.CreateLeaseId(slot.Index);
        var resources = new List<LibrarySpeedDiceResourceReservation>();
        if (energy > 0)
        {
            resources.Add(
                new LibrarySpeedDiceResourceReservation(
                    "energy",
                    energy,
                    LibrarySpeedDiceResourceKind.Energy));
        }
        if (stars > 0)
        {
            resources.Add(
                new LibrarySpeedDiceResourceReservation(
                    "stars",
                    stars,
                    LibrarySpeedDiceResourceKind.Stars));
        }

        if (card is ILibraryLightCard)
        {
            if (state.Light == null
                || !state.Light.TryReserve(leaseId, light))
            {
                return false;
            }

            if (light > 0)
            {
                resources.Add(
                    new LibrarySpeedDiceResourceReservation(
                        state.Light.ReservationResourceId,
                        light,
                        LibrarySpeedDiceResourceKind.Light));
            }
        }

        foreach ((string resourceId, int amount) in legacySecondary)
        {
            resources.Add(
                new LibrarySpeedDiceResourceReservation(
                    resourceId,
                    amount,
                    LibrarySpeedDiceResourceKind.LegacySecondary));
        }

        try
        {
            var plan = new LibrarySpeedDiceReservationPlan(resources);
            LibrarySpeedDiceReservationTransaction transaction =
                CreateReservationTransaction(
                    state,
                    card,
                    leaseId,
                    plan,
                    light,
                    legacySecondary);
            lease = new LibrarySpeedDiceCardLease(
                leaseId,
                card,
                plan,
                transaction);
            return true;
        }
        catch (Exception exception)
        {
            state.Light?.ReleaseReservation(leaseId);
            Log.Error(
                $"[LibraryOfRuinaLib] Participant {state.Registration.Id} could not freeze a reservation plan: {exception}");
            return false;
        }
    }

    private static bool TryCalculateLightReservation(
        LibrarySpeedDiceCombatState state,
        CardModel card,
        out int amount)
    {
        amount = 0;
        if (card is not ILibraryLightCard)
            return true;
        if (state.Light == null)
            return false;

        try
        {
            amount = Math.Max(
                0,
                LibraryLight.GetCost(card).GetAmountToSpend());
            return state.Light.HasEnoughAvailable(amount);
        }
        catch (Exception exception)
        {
            Log.Error(
                $"[LibraryOfRuinaLib] Participant {state.Registration.Id} Light reservation failed: {exception}");
            amount = 0;
            return false;
        }
    }

    private static LibrarySpeedDiceReservationTransaction
        CreateReservationTransaction(
            LibrarySpeedDiceCombatState state,
            CardModel card,
            string leaseId,
            LibrarySpeedDiceReservationPlan plan,
            int frozenLight,
            IReadOnlyDictionary<string, int> legacySecondary)
    {
        int energy = plan.ReservedEnergy;
        int stars = plan.ReservedStars;
        bool energyCommitted = false;
        bool starsCommitted = false;
        int previousLastStarsSpent = card.LastStarsSpent;
        var commitments =
            new List<LibrarySpeedDiceReservationCommitment>
            {
                new()
                {
                    ResourceId = "20.energy",
                    PreflightAsync = () => Task.FromResult(
                        state.Player.PlayerCombatState!.Energy >= energy),
                    CommitAsync = () =>
                    {
                        if (energy > 0)
                        {
                            if (state.Player.PlayerCombatState!.Energy
                                < energy)
                            {
                                return Task.FromResult(false);
                            }

                            state.Player.PlayerCombatState!
                                .LoseEnergy(energy);
                            energyCommitted = true;
                        }
                        return Task.FromResult(true);
                    },
                    RollbackAsync = () =>
                    {
                        if (energyCommitted)
                        {
                            state.Player.PlayerCombatState!
                                .GainEnergy(energy);
                            energyCommitted = false;
                        }
                        return Task.CompletedTask;
                    },
                    FinalizeAsync = async () =>
                    {
                        if (energy > 0)
                        {
                            CombatManager.Instance.History.EnergySpent(
                                card.CombatState!,
                                energy,
                                card.Owner);
                        }
                        await Hook.AfterEnergySpent(
                            card.CombatState!,
                            card,
                            energy);
                    },
                },
                new()
                {
                    ResourceId = "30.stars",
                    PreflightAsync = () => Task.FromResult(
                        state.Player.PlayerCombatState!.Stars >= stars),
                    CommitAsync = () =>
                    {
                        if (stars > 0
                            && state.Player.PlayerCombatState!.Stars
                            < stars)
                        {
                            return Task.FromResult(false);
                        }

                        card.LastStarsSpent = stars;
                        if (stars > 0)
                        {
                            state.Player.PlayerCombatState!
                                .LoseStars(stars);
                        }
                        starsCommitted = true;
                        return Task.FromResult(true);
                    },
                    RollbackAsync = () =>
                    {
                        if (starsCommitted)
                        {
                            if (stars > 0)
                            {
                                state.Player.PlayerCombatState!
                                    .GainStars(stars);
                            }
                            card.LastStarsSpent =
                                previousLastStarsSpent;
                            starsCommitted = false;
                        }
                        return Task.CompletedTask;
                    },
                    FinalizeAsync = () => stars > 0
                        ? Hook.AfterStarsSpent(
                            card.Owner.Creature.CombatState!,
                            stars,
                            card.Owner)
                        : Task.CompletedTask,
                },
            };

        if (card is ILibraryLightCard && state.Light != null)
        {
            LibraryLightState lightState = state.Light;
            bool lightCommitted = false;
            commitments.Add(
                new LibrarySpeedDiceReservationCommitment
                {
                    ResourceId = "80.light",
                    PreflightAsync = () => Task.FromResult(
                        lightState.HasReservation(
                            leaseId,
                            frozenLight)
                        && lightState.Current >= frozenLight),
                    CommitAsync = async () =>
                    {
                        lightCommitted =
                            await lightState.CommitReservation(
                                leaseId,
                                frozenLight,
                                card);
                        return lightCommitted;
                    },
                    RollbackAsync = async () =>
                    {
                        if (!lightCommitted)
                            return;

                        if (!await lightState.RestoreCommittedReservation(
                                leaseId,
                                frozenLight,
                                card))
                        {
                            throw new InvalidOperationException(
                                "Failed to restore a committed Light reservation.");
                        }
                        lightCommitted = false;
                    },
                    FinalizeAsync = () =>
                    {
                        LibraryLightCost cost =
                            LibraryLight.GetCost(card);
                        if (cost.CostsX)
                            cost.CapturedXValue = frozenLight;
                        return Task.CompletedTask;
                    },
                    Release = () =>
                        lightState.ReleaseReservation(leaseId),
                });
        }

        LibrarySpeedDiceParticipant? legacy =
            state.Registration.LegacyParticipant;
        if (legacy?.CommitSecondaryResourcesAsync != null)
        {
            commitments.Add(
                new LibrarySpeedDiceReservationCommitment
                {
                    ResourceId = "90.legacy-secondary",
                    PreflightAsync = () => Task.FromResult(true),
                    CommitAsync = () =>
                        legacy.CommitSecondaryResourcesAsync(
                            state,
                            card,
                            legacySecondary),
                    RollbackAsync = static () => Task.CompletedTask,
                });
        }

        return new LibrarySpeedDiceReservationTransaction(commitments);
    }

    private static void ApplyLegacyReservationProjection(
        LibrarySpeedDiceCombatState state,
        CardModel card,
        LibrarySpeedDiceSlot slot)
    {
        LibrarySpeedDiceParticipant? legacy =
            state.Registration.LegacyParticipant;
        if (legacy?.OnSecondaryResourcesReserved == null)
            return;

        IReadOnlyDictionary<string, int> reservations =
            slot.Lease?.ReservationPlan.Resources
                .Where(resource =>
                    resource.Kind
                        == LibrarySpeedDiceResourceKind.LegacySecondary)
                .ToDictionary(
                    resource => resource.ResourceId,
                    resource => resource.Amount,
                    StringComparer.Ordinal)
            ?? new Dictionary<string, int>(StringComparer.Ordinal);
        legacy.OnSecondaryResourcesReserved(card, slot, reservations);
    }

    private static void ReleaseSlotReservation(
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot)
    {
        state.Registration.LegacyParticipant?
            .OnSecondaryResourceReservationsReleased?.Invoke(slot);
        slot.ClearReservation();
    }

    private static void ReleaseSlotCard(
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot)
    {
        CardModel? card = slot.Card;
        LibrarySpeedDiceCardLease? lease = slot.Lease;
        ReleaseSlotReservation(state, slot);
        if (card != null)
        {
            state.Registration.Dispatcher.OnCardReleased(
                state,
                slot,
                card,
                lease);
        }
        slot.ClearCard();
    }

    private static void RestoreLease(
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot,
        LibrarySpeedDiceSlotSnapshot snapshot)
    {
        CardModel card = slot.Card!;
        LibrarySpeedDiceResourceReservation[] resources;
        string leaseId;
        bool useTriggered = false;
        bool targetedUseTriggered = false;
        bool preventUnequip = false;
        bool committed = false;

        if (snapshot.Lease != null)
        {
            resources = snapshot.Lease.Resources.ToArray();
            leaseId = snapshot.Lease.Id;
            useTriggered = snapshot.Lease.IsUseTriggered;
            targetedUseTriggered =
                snapshot.Lease.IsTargetedUseTriggered;
            preventUnequip = snapshot.Lease.PreventUnequip;
            committed = snapshot.Lease.IsCommitted;
        }
        else
        {
            var restored =
                new List<LibrarySpeedDiceResourceReservation>();
            if (snapshot.ReservedEnergy > 0)
            {
                restored.Add(
                    new LibrarySpeedDiceResourceReservation(
                        "energy",
                        snapshot.ReservedEnergy,
                        LibrarySpeedDiceResourceKind.Energy));
            }
            if (snapshot.ReservedStars > 0)
            {
                restored.Add(
                    new LibrarySpeedDiceResourceReservation(
                        "stars",
                        snapshot.ReservedStars,
                        LibrarySpeedDiceResourceKind.Stars));
            }
            foreach ((string resourceId, int amount)
                     in snapshot.ReservedSecondaryResources)
            {
                LibrarySpeedDiceResourceKind kind =
                    state.Light != null
                    && card is ILibraryLightCard
                    && string.Equals(
                        resourceId,
                        state.Light.ReservationResourceId,
                        StringComparison.Ordinal)
                        ? LibrarySpeedDiceResourceKind.Light
                        : LibrarySpeedDiceResourceKind.LegacySecondary;
                restored.Add(
                    new LibrarySpeedDiceResourceReservation(
                        resourceId,
                        Math.Max(0, amount),
                        kind));
            }

            resources = restored.ToArray();
            leaseId =
                $"{state.Registration.Id}:{state.Player.NetId}:"
                + $"{snapshot.Index}:restored";
        }

        var plan = new LibrarySpeedDiceReservationPlan(resources);
        int light = plan.GetAmount(
            LibrarySpeedDiceResourceKind.Light);
        if (!committed
            && card is ILibraryLightCard
            && state.Light != null
            && !state.Light.TryReserve(leaseId, light))
        {
            throw new InvalidOperationException(
                $"Unable to restore Light reservation for lease '{leaseId}'.");
        }

        IReadOnlyDictionary<string, int> legacySecondary =
            plan.Resources
                .Where(resource =>
                    resource.Kind
                        == LibrarySpeedDiceResourceKind.LegacySecondary)
                .ToDictionary(
                    resource => resource.ResourceId,
                    resource => resource.Amount,
                    StringComparer.Ordinal);
        var lease = new LibrarySpeedDiceCardLease(
            leaseId,
            card,
            plan,
            CreateReservationTransaction(
                state,
                card,
                leaseId,
                plan,
                light,
                legacySecondary))
        {
            IsUseTriggered = useTriggered,
            IsTargetedUseTriggered = targetedUseTriggered,
            IsCommitted = committed,
        };
        if (preventUnequip)
            lease.LockUnequip();
        slot.SetLease(lease);
        ApplyLegacyReservationProjection(state, card, slot);
    }

    private static async Task TriggerUseAsync(
        PlayerChoiceContext choiceContext,
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot)
    {
        LibrarySpeedDiceCardLease? lease = slot.Lease;
        if (lease == null || lease.IsUseTriggered || lease.IsReleased)
            return;

        lease.IsUseTriggered = true;
        await state.Registration.Dispatcher.OnUseAsync(
            choiceContext,
            state,
            slot,
            lease);
        state.NotifyGameplayChanged();
    }

    private static async Task TriggerTargetedUseAsync(
        PlayerChoiceContext choiceContext,
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot,
        Creature target)
    {
        LibrarySpeedDiceCardLease? lease = slot.Lease;
        CardModel? card = slot.Card;
        if (lease == null
            || card == null
            || lease.IsTargetedUseTriggered
            || lease.IsReleased
            || !card.IsValidSpeedDiceTarget(target))
        {
            return;
        }

        if (!lease.IsUseTriggered)
            await TriggerUseAsync(choiceContext, state, slot);
        lease.IsTargetedUseTriggered = true;
        await state.Registration.Dispatcher.OnTargetedUseAsync(
            choiceContext,
            state,
            slot,
            lease,
            target);
        state.NotifyGameplayChanged();
    }

    private static bool CanParticipantEquipCard(
        LibrarySpeedDiceCombatState state,
        CardModel card)
    {
        try
        {
            return state.Registration.Dispatcher.CanEquipCard(
                state,
                card);
        }
        catch (Exception exception)
        {
            Log.Error(
                $"[LibraryOfRuinaLib] Participant {state.Registration.Id} card predicate failed: {exception}");
            return false;
        }
    }

    private static bool CanParticipantUnequipCard(
        LibrarySpeedDiceCombatState state,
        CardModel card)
    {
        try
        {
            return state.Registration.Dispatcher.CanUnequipCard(
                state,
                card);
        }
        catch (Exception exception)
        {
            Log.Error(
                $"[LibraryOfRuinaLib] Participant {state.Registration.Id} unequip predicate failed: {exception}");
            return false;
        }
    }

    internal static bool IsParticipantTargetAllowed(
        CardModel card,
        Creature? target)
    {
        if (card.Owner == null
            || !TryGetState(
                card.Owner,
                out LibrarySpeedDiceCombatState? state)
            || state == null)
        {
            return true;
        }

        try
        {
            return state.Registration.Dispatcher.CanTargetCard(
                state,
                card,
                target);
        }
        catch (Exception exception)
        {
            Log.Error(
                $"[LibraryOfRuinaLib] Participant {state.Registration.Id} target predicate failed: {exception}");
            return false;
        }
    }

    private static bool HasMissingRequiredTargets(
        LibrarySpeedDiceCombatState state)
    {
        return state.Slots.Any(slot =>
            slot.Card != null
            && slot.RequiresTarget
            && !slot.HasValidTarget);
    }

    private static async Task<bool> RepairInvalidTargetsBeforeResolutionAsync(
        LibrarySpeedDiceCombatState state)
    {
        var changed = false;
        foreach (var slot in state.Slots)
        {
            var card = slot.Card;
            if (card == null || !slot.RequiresTarget || slot.HasValidTarget)
                continue;

            var target = GetRandomValidTarget(state, card);
            if (target != null)
            {
                slot.Target = target;
                changed = true;
                continue;
            }

            if (!slot.IsSpent && card.Pile?.Type == PileType.Play)
            {
                var result = await CardPileCmd.Add(
                    card,
                    PileType.Hand);
                if (result.success)
                {
                    ReleaseSlotCard(state, slot);
                    changed = true;
                }
            }
        }

        if (changed)
            state.NotifyGameplayChanged();
        return !HasMissingRequiredTargets(state);
    }

    private static Creature? GetRandomValidTarget(
        LibrarySpeedDiceCombatState state,
        CardModel card)
    {
        var combatState = state.Player.Creature.CombatState;
        if (combatState == null)
            return null;

        var owner = state.Player.Creature;
        var candidates =
            card.GetSpeedDiceTargetType() switch
            {
                TargetType.AnyEnemy => combatState
                    .GetOpponentsOf(owner)
                    .Where(candidate => candidate.IsHittable),
                TargetType.AnyAlly => combatState.PlayerCreatures
                    .Where(candidate =>
                        candidate.IsHittable
                        && !ReferenceEquals(candidate, owner)),
                _ => [],
            };
        candidates = candidates.Where(candidate =>
            card.IsValidSpeedDiceTarget(candidate)
            && Hook.ShouldAllowTargeting(
                combatState,
                candidate,
                out _));
        Creature[] orderedCandidates = candidates
            .OrderBy(
                candidate => GetStableTargetKey(state, candidate),
                StringComparer.Ordinal)
            .ToArray();
        return orderedCandidates.Length == 0
            ? null
            : state.TargetRepairRng.NextItem(orderedCandidates);
    }

    private static string GetStableTargetKey(
        LibrarySpeedDiceCombatState state,
        Creature target)
    {
        try
        {
            string? key =
                state.Registration.Dispatcher.GetStableTargetKey(target);
            if (!string.IsNullOrWhiteSpace(key))
                return key;
        }
        catch (Exception exception)
        {
            Log.Error(
                $"[LibraryOfRuinaLib] Participant {state.Registration.Id} target key failed: {exception}");
        }

        return target.Player != null
            ? $"player:{target.Player.NetId:D20}"
            : $"model:{target.Monster?.Id}:{target.SlotName}";
    }

    /// <summary>
    /// 计算一张卡在速度骰子系统中所需的能量和光芒，并返回当前资源是否足够
    /// </summary>
    /// <param name="state">当前速度骰子战斗状态</param>
    /// <param name="card">要计算的卡牌</param>
    /// <param name="energy">输出：实际需要的能量（可能被光芒补足后降低）</param>
    /// <param name="stars">输出：实际需要的光芒（可能因补足能量而增加）</param>
    /// <returns>当前可用资源是否足够</returns>
    private static bool TryCalculateReservation(
        LibrarySpeedDiceCombatState state,
        CardModel card,
        out int energy,
        out int stars)
    {
        LibrarySpeedDiceParticipant? legacy =
            state.Registration.LegacyParticipant;
        if (legacy?.GetPrimaryResourceReservation != null)
        {
            try
            {
                LibrarySpeedDicePrimaryResourceReservation? reservation =
                    legacy.GetPrimaryResourceReservation(
                        state,
                        card);
                if (reservation == null)
                {
                    energy = 0;
                    stars = 0;
                    return false;
                }

                energy = Math.Max(0, reservation.Value.Energy);
                stars = Math.Max(0, reservation.Value.Stars);
                int availableEnergy = Math.Max(
                    0,
                    state.Player.PlayerCombatState!.Energy
                    - state.ReservedEnergy);
                int availableStars = Math.Max(
                    0,
                    state.Player.PlayerCombatState!.Stars
                    - state.ReservedStars);
                return energy <= availableEnergy
                    && stars <= availableStars;
            }
            catch (Exception exception)
            {
                Log.Error(
                    $"[LibraryOfRuinaLib] Participant {state.Registration.Id} primary-resource reservation failed: {exception}");
                energy = 0;
                stars = 0;
                return false;
            }
        }

        // ReSharper disable once SuspiciousTypeConversion.Global
        var hasCustomCost = card is ILibrarySpeedDiceCard;
        // ReSharper disable once SuspiciousTypeConversion.Global
        // 获取卡的费用：如果实现了ILibrarySpeedDiceCard则用自定义速度骰子费用，否则用标准修饰符计算
        if (card is ILibrarySpeedDiceCard speedDiceCard)
        {
            energy = Math.Max(0, speedDiceCard.SpeedDiceResourceCost.Energy);
            stars = Math.Max(0, speedDiceCard.SpeedDiceResourceCost.Stars);
        }
        else
        {
            energy = Math.Max(0, card.EnergyCost.GetWithModifiers(CostModifiers.All));
            stars = Math.Max(0, card.GetStarCostWithModifiers());
        }

        // 当前可用资源 = 总资源 - 已被其他速度骰子预定的部分
        var energyAvailable = Math.Max(
            0,
            state.Player.PlayerCombatState!.Energy - state.ReservedEnergy);
        var starsAvailable = Math.Max(
            0,
            state.Player.PlayerCombatState!.Stars - state.ReservedStars);

        // 能量不足时，用星光补足缺口（仅非自定义费用卡，且钩子允许时）
        // 兑换比率：1点能量缺口 = 2点额外消耗
        if (!hasCustomCost
            && energy > energyAvailable
            && card.CombatState != null
            && Hook.ShouldPayExcessEnergyCostWithStars(card.CombatState, card.Owner))
        {
            stars += (energy - energyAvailable) * 2;
            energy = energyAvailable;
        }

        return energy <= energyAvailable && stars <= starsAvailable;
    }

    private static void AddDamageEmotion(
        LibrarySpeedDiceCombatState state,
        int damage,
        int referenceMaxHp,
        bool isDamageGiven)
    {
        if (damage <= 0)
            return;

        var threshold = Math.Max(
            1,
            (int)Math.Ceiling(
                Math.Max(1, referenceMaxHp)
                * state.Registration.Emotion
                    .DamageUnitFractionOfMaxHp));
        int accumulator = isDamageGiven
            ? state.DamageGivenAccumulator
            : state.DamageReceivedAccumulator;
        int previousThreshold = isDamageGiven
            ? state.DamageGivenAccumulatorThreshold
            : state.DamageReceivedAccumulatorThreshold;
        if (previousThreshold <= 0)
        {
            accumulator = 0;
        }
        else if (previousThreshold != threshold)
        {
            accumulator = (int)Math.Floor(
                (decimal)accumulator
                * threshold
                / previousThreshold);
        }

        var total = damage + accumulator;
        var units = total / threshold;
        var remainder = total % threshold;
        if (isDamageGiven)
        {
            state.DamageGivenAccumulator = remainder;
            state.DamageGivenAccumulatorThreshold = threshold;
        }
        else
        {
            state.DamageReceivedAccumulator = remainder;
            state.DamageReceivedAccumulatorThreshold = threshold;
        }

        AddEmotionUnits(state, units);
    }

    private static void AddEmotionUnits(
        LibrarySpeedDiceCombatState state,
        int units)
    {
        if (units <= 0)
            return;

        state.Emotion.AddUnits(
            units,
            state.Registration.Emotion);
        state.NotifyGameplayChanged();
    }

    public static void AddEmotionUnits(Player player, int units)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (TryGetState(player, out LibrarySpeedDiceCombatState? state)
            && state != null)
        {
            AddEmotionUnits(state, units);
        }
    }

    public static bool TryForceEmotionLevelUp(
        Player player,
        out int previousLevel,
        out int currentLevel)
    {
        ArgumentNullException.ThrowIfNull(player);
        previousLevel = 0;
        currentLevel = 0;
        if (!TryGetState(player, out LibrarySpeedDiceCombatState? state)
            || state == null)
        {
            return false;
        }

        previousLevel = state.Emotion.Level;
        if (!state.Emotion.ForceLevelUp(
                state.Registration.Emotion))
        {
            currentLevel = previousLevel;
            return false;
        }

        currentLevel = state.Emotion.Level;
        return true;
    }

    public static void NotifyParticipantStateChanged(
        LibrarySpeedDiceCombatState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        state.Light?.RefreshMaximum();
        state.NotifyGameplayChanged();
    }

    private static int GetDiceCount(LibrarySpeedDiceCombatState state)
    {
        var extra = state.Emotion.Level
                    >= state.Registration.Emotion.ExtraSpeedDieLevel
            ? state.Registration.Emotion.ExtraSpeedDice
            : 0;
        int count = state.Registration.Options.BaseCount + extra;
        count = state.Registration.Dispatcher.ModifySpeedDiceCount(
            state,
            count);

        return Math.Max(0, count);
    }

    public static void RefreshSlotCount(Player player, bool rollNewSlots)
    {
        if (!TryGetState(player, out LibrarySpeedDiceCombatState? state)
            || state == null
            || state.IsResolving)
        {
            return;
        }

        state.EnsureSlotCount(GetDiceCount(state), rollNewSlots);
    }

    private static LibrarySpeedDiceRegistration? FindRegistration(
        Player player)
    {
        lock (Sync)
        {
            return Registrations.FirstOrDefault(registration =>
            {
                try
                {
                    return registration.IsEnabledForPlayer(player);
                }
                catch (Exception exception)
                {
                    Log.Error(
                        $"[LibraryOfRuinaLib] Participant {registration.Id} predicate failed: {exception}");
                    return false;
                }
            });
        }
    }

    private static bool IsStateUsable(LibrarySpeedDiceCombatState state)
    {
        return state.Player.PlayerCombatState != null
            && state.Player.Creature.CombatState != null
            && CombatManager.Instance.IsInProgress;
    }
}

internal static class LibrarySpeedDiceCardExtensions
{
    public static TargetType GetSpeedDiceTargetType(this CardModel card)
    {
        return card is ILibrarySpeedDiceCard speedDiceCard
            ? speedDiceCard.SpeedDiceTargetType
            : card.TargetType;
    }

    public static bool RequiresSpeedDiceTarget(this CardModel card)
    {
        return card.GetSpeedDiceTargetType()
            is TargetType.AnyEnemy or TargetType.AnyAlly;
    }

    public static bool IsValidSpeedDiceTarget(
        this CardModel card,
        Creature? target)
    {
        TargetType targetType = card.GetSpeedDiceTargetType();
        if (target == null)
        {
            return targetType is not TargetType.AnyEnemy
                and not TargetType.AnyAlly
                && LibrarySpeedDiceService.IsParticipantTargetAllowed(
                    card,
                    null);
        }

        if (!target.IsAlive)
            return false;

        bool isValid = targetType switch
        {
            TargetType.AnyEnemy => target.Side != card.Owner.Creature.Side,
            TargetType.AnyAlly => target.Side == card.Owner.Creature.Side,
            _ => false,
        };
        return isValid
            && LibrarySpeedDiceService.IsParticipantTargetAllowed(
                card,
                target);
    }
}
