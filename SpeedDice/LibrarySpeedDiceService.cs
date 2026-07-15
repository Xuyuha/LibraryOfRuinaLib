using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
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
    private static readonly object Sync = new();
    private static readonly List<LibrarySpeedDiceParticipant> Participants = [];
    private static readonly ConditionalWeakTable<Player, LibrarySpeedDiceCombatState> States = new();
    private static readonly FieldInfo? CurrentCardPlayField =
        AccessTools.Field(typeof(NPlayerHand), "_currentCardPlay");

    private static WeakReference<LibrarySpeedDiceCombatState>? _localState;

    public static void RegisterParticipant(LibrarySpeedDiceParticipant participant)
    {
        ArgumentNullException.ThrowIfNull(participant);
        participant.Validate();

        lock (Sync)
        {
            Participants.RemoveAll(x => x.Id == participant.Id);
            Participants.Add(participant);
        }
    }

    public static bool TryGetState(
        Player player,
        out LibrarySpeedDiceCombatState? state)
    {
        state = null;
        if (!IsSingleplayerEnabled() || player.PlayerCombatState == null)
            return false;

        LibrarySpeedDiceParticipant? participant = FindParticipant(player);
        if (participant == null)
            return false;

        if (!States.TryGetValue(player, out state))
        {
            state = new LibrarySpeedDiceCombatState(player, participant);
            state.ReplaceSlots(GetDiceCount(state));
            States.Add(player, state);
        }

        if (MegaCrit.Sts2.Core.Context.LocalContext.IsMe(player))
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
        Player? owner = card.Owner;
        if (owner == null
            || !TryGetState(owner, out LibrarySpeedDiceCombatState? state)
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
        Player? owner = card.Owner;
        if (owner == null
            || !TryGetState(owner, out LibrarySpeedDiceCombatState? state)
            || state?.ResolvingSlot == null
            || !ReferenceEquals(state.ResolvingSlot.Card, card))
        {
            return false;
        }

        slot = state.ResolvingSlot;
        return true;
    }

    public static void ClearCombat()
    {
        if (_localState != null
            && _localState.TryGetTarget(out LibrarySpeedDiceCombatState? state))
        {
            States.Remove(state.Player);
        }
        _localState = null;
    }

    public static void BeginPlayerTurn(Creature creature, CombatSide side)
    {
        if (side != CombatSide.Player
            || creature.Player == null
            || !TryGetState(creature.Player, out LibrarySpeedDiceCombatState? state)
            || state == null)
        {
            return;
        }

        state.Emotion.TryLevelUp();
        state.PreviousTurnTriggeredCards = state.CurrentTurnTriggeredCards;
        state.CurrentTurnTriggeredCards = 0;
        state.BonusDrawPending =
            state.Emotion.Level >= state.Participant.Emotion.BonusDrawLevel
            && state.PreviousTurnTriggeredCards
            >= state.Participant.Emotion.BonusDrawRequiredTriggeredCards;

        uint turnMixin = unchecked(
            (uint)(state.Player.PlayerCombatState!.TurnNumber * 0x45D9F3B)
            ^ (uint)(state.Player.RunState.TotalFloor * 0x119DE1F3));
        state.GameplayRng = new Rng(
            state.Player.RunState.Rng.Seed ^ turnMixin,
            "library_speed_dice");
        state.ReplaceSlots(GetDiceCount(state));
    }

    public static async Task FinishPlayerTurnAsync(
        Player player,
        IReadOnlySet<CardModel> retainedCards)
    {
        if (!States.TryGetValue(
                player,
                out LibrarySpeedDiceCombatState? state))
        {
            return;
        }

        await state.Gate.WaitAsync();
        try
        {
            List<LibrarySpeedDiceSlot> equippedSlots = state.Slots
                .Where(slot => slot.Card != null)
                .ToList();
            if (equippedSlots.Count == 0)
                return;

            var cardsToRetain = new List<CardModel>();
            var cardsToDiscard = new List<CardModel>();
            foreach (LibrarySpeedDiceSlot slot in equippedSlots)
            {
                CardModel card = slot.Card!;
                if (card.Pile?.Type != PileType.Play)
                {
                    slot.ClearCard();
                    continue;
                }

                if (retainedCards.Contains(card))
                    cardsToRetain.Add(card);
                else
                    cardsToDiscard.Add(card);
            }

            await MoveEquippedCardsAsync(
                equippedSlots,
                cardsToRetain,
                PileType.Hand);
            await MoveEquippedCardsAsync(
                equippedSlots,
                cardsToDiscard,
                PileType.Discard);
            state.NotifyChanged();
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
        if (!TryGetLocalState(out LibrarySpeedDiceCombatState? state)
            || state == null
            || state.IsLocked
            || state.IsResolving
            || state.IsSelectingTarget
            || HasMissingRequiredTargets(state)
            || state.Player.PlayerCombatState!.Phase != PlayerTurnPhase.Play
            || CombatManager.Instance.PlayerActionsDisabled
            || CombatManager.Instance.IsOverOrEnding)
        {
            return false;
        }

        return RunManager.Instance.ActionExecutor.CurrentlyRunningAction == null;
    }

    public static Task AdvanceLocalAsync()
    {
        return TryGetLocalState(out LibrarySpeedDiceCombatState? state) && state != null
            ? AdvanceAsync(state)
            : Task.CompletedTask;
    }

    public static bool HasMissingRequiredTargetsLocal()
    {
        return TryGetLocalState(out LibrarySpeedDiceCombatState? state)
            && state != null
            && HasMissingRequiredTargets(state);
    }

    internal static bool CanInteractWithSlot(
        LibrarySpeedDiceCombatState state,
        int slotIndex,
        out bool canAcceptSelectedCard)
    {
        canAcceptSelectedCard = false;
        if (!IsStateUsable(state)
            || !state.HasRolled
            || state.IsLocked
            || state.IsResolving
            || state.IsSelectingTarget
            || slotIndex < 0
            || slotIndex >= state.Slots.Count)
        {
            return false;
        }

        LibrarySpeedDiceSlot slot = state.Slots[slotIndex];
        if (slot.IsSpent)
            return false;
        if (slot.Card != null)
            return true;

        CardModel? card = GetSelectedCard();
        canAcceptSelectedCard =
            card != null
            && card.Owner == state.Player
            && card.Pile?.Type == PileType.Hand
            && !card.EnergyCost.CostsX
            && !card.HasStarCostX
            && CanEquipCard(state, card)
            && TryCalculateReservation(
                state,
                card,
                out _,
                out _);
        return canAcceptSelectedCard;
    }

    public static async Task ActivateSlotAsync(int slotIndex, Control targetingOrigin)
    {
        if (!TryGetLocalState(out LibrarySpeedDiceCombatState? state) || state == null)
            return;

        CardModel? cardToTarget = null;
        await state.Gate.WaitAsync();
        try
        {
            if (!IsStateUsable(state)
                || !state.HasRolled
                || state.IsLocked
                || state.IsResolving
                || slotIndex < 0
                || slotIndex >= state.Slots.Count)
            {
                return;
            }

            LibrarySpeedDiceSlot slot = state.Slots[slotIndex];
            if (slot.Card != null)
            {
                cardToTarget = slot.Card.RequiresSpeedDiceTarget()
                    ? slot.Card
                    : null;
            }
            else
            {
                CardModel? card = GetSelectedCard();
                if (card == null
                    || card.Owner != state.Player
                    || card.Pile?.Type != PileType.Hand
                    || card.EnergyCost.CostsX
                    || card.HasStarCostX
                    || !CanEquipCard(state, card))
                {
                    return;
                }

                if (!TryCalculateReservation(state, card, out int energy, out int stars))
                    return;

                NPlayerHand? hand = NPlayerHand.Instance;
                if (hand != null)
                {
                    hand.CancelAllCardPlay();
                    if (hand.GetCardHolder(card) != null)
                        hand.Remove(card);
                }

                CardPileAddResult result = await CardPileCmd.Add(
                    card,
                    PileType.Play,
                    skipVisuals: true);
                if (!result.success)
                {
                    await CardPileCmd.Add(card, PileType.Hand);
                    return;
                }

                slot.Card = card;
                slot.Target = null;
                slot.ReservedEnergy = energy;
                slot.ReservedStars = stars;
                cardToTarget = card.RequiresSpeedDiceTarget() ? card : null;
                state.NotifyChanged();
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

        if (cardToTarget != null)
            await SelectTargetAsync(state, slotIndex, cardToTarget, targetingOrigin);
    }

    public static async Task UnequipCardAsync(int slotIndex)
    {
        if (!TryGetLocalState(out LibrarySpeedDiceCombatState? state) || state == null)
            return;

        await state.Gate.WaitAsync();
        try
        {
            if (!IsStateUsable(state)
                || !state.HasRolled
                || state.IsLocked
                || state.IsResolving
                || slotIndex < 0
                || slotIndex >= state.Slots.Count)
            {
                return;
            }

            LibrarySpeedDiceSlot slot = state.Slots[slotIndex];
            CardModel? card = slot.Card;
            if (card == null)
                return;

            CardPileAddResult result = await CardPileCmd.Add(card, PileType.Hand);
            if (!result.success)
                return;

            slot.ClearCard();
            state.NotifyChanged();
        }
        catch (Exception exception)
        {
            Log.Error("[LibraryOfRuinaLib] Failed to unequip a speed-die card: " + exception);
        }
        finally
        {
            state.Gate.Release();
        }
    }

    public static void ApplyReservedResourceRestriction(
        CardModel card,
        ref UnplayableReason reason,
        ref bool result)
    {
        if (!TryGetState(card.Owner, out LibrarySpeedDiceCombatState? state)
            || state == null
            || state.ReservedEnergy <= 0 && state.ReservedStars <= 0)
        {
            return;
        }

        PlayerCombatState? resources = card.Owner?.PlayerCombatState;
        if (resources == null)
            return;
        int energyAvailable = Math.Max(0, resources.Energy - state.ReservedEnergy);
        int starsAvailable = Math.Max(0, resources.Stars - state.ReservedStars);
        int energyCost = Math.Max(0, card.EnergyCost.GetWithModifiers(CostModifiers.All));
        int starCost = Math.Max(0, card.GetStarCostWithModifiers());

        if (energyCost > energyAvailable
            && card.CombatState != null
            && Hook.ShouldPayExcessEnergyCostWithStars(card.CombatState, card.Owner))
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
        return TryGetState(player, out LibrarySpeedDiceCombatState? state) && state != null
            ? state.Emotion.Level * state.Participant.Emotion.MaxEnergyPerLevel
            : 0;
    }

    public static void AddInitialHandDrawBonus(
        Player player,
        bool fromHandDraw,
        ref decimal count)
    {
        if (!fromHandDraw
            || !TryGetState(player, out LibrarySpeedDiceCombatState? state)
            || state == null
            || !state.BonusDrawPending)
        {
            return;
        }

        state.BonusDrawPending = false;
        count += state.Participant.Emotion.BonusDrawAmount;
        state.NotifyChanged();
    }

    public static void RecordDamageGiven(
        Creature? dealer,
        DamageResult result,
        Creature target)
    {
        if (dealer?.Player == null
            || target.Side == dealer.Side
            || !TryGetState(dealer.Player, out LibrarySpeedDiceCombatState? state)
            || state == null)
        {
            return;
        }

        if (state.Participant.Emotion.GainEmotionFromDamage)
        {
            AddDamageEmotion(
                state,
                Math.Max(0, result.UnblockedDamage - result.OverkillDamage),
                isDamageGiven: true);
        }
        if (result.WasTargetKilled)
            AddEmotionUnits(state, state.Participant.Emotion.KillEmotionUnits);
    }

    public static void RecordDamageReceived(Creature target, DamageResult result)
    {
        if (target.Player == null)
        {
            return;
        }

        if (TryGetState(
                target.Player,
                out LibrarySpeedDiceCombatState? targetState)
            && targetState != null
            && targetState.Participant.Emotion.GainEmotionFromDamage)
        {
            AddDamageEmotion(
                targetState,
                Math.Max(0, result.UnblockedDamage - result.OverkillDamage),
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

        foreach (Player ally in combatState.Players)
        {
            if (ally == creature.Player
                || ally.Creature.IsDead
                || !TryGetState(
                    ally,
                    out LibrarySpeedDiceCombatState? allyState)
                || allyState == null)
            {
                continue;
            }

            AddEmotionUnits(
                allyState,
                allyState.Participant.Emotion.AllyDeathEmotionUnits);
        }
    }

    private static async Task AdvanceAsync(LibrarySpeedDiceCombatState state)
    {
        await state.Gate.WaitAsync();
        try
        {
            if (!CanConsumeAdvanceInput()
                || state.IsLocked
                || state.IsResolving)
            {
                return;
            }

            LibrarySpeedDiceAudio.PlayAdvance();
            NPlayerHand.Instance?.CancelAllCardPlay();
            if (!state.HasRolled)
            {
                int emotionUnits = 0;
                foreach (LibrarySpeedDiceSlot slot in state.Slots)
                {
                    slot.FinalValue = state.GameplayRng.NextInt(
                        state.Participant.MinSpeed,
                        state.Participant.MaxSpeed + 1);
                    slot.DisplayValue = slot.FinalValue;
                    if (slot.FinalValue == state.Participant.MinSpeed
                        || slot.FinalValue == state.Participant.MaxSpeed)
                    {
                        emotionUnits += state.Participant.Emotion
                            .ExtremeRollEmotionUnits;
                    }
                }

                AddEmotionUnits(state, emotionUnits);
                state.HasRolled = true;
                state.NotifyChanged();
                return;
            }

            foreach (LibrarySpeedDiceSlot slot in state.Slots)
            {
                slot.IsLocked = true;
            }

            state.IsLocked = true;
            state.IsResolving = true;
            state.NotifyChanged();

            IEnumerable<LibrarySpeedDiceSlot> orderedSlots = state.Slots
                .OrderByDescending(x => x.FinalValue)
                .ThenBy(x => x.Index);
            foreach (LibrarySpeedDiceSlot slot in orderedSlots)
            {
                CardModel? card = slot.Card;
                if (card == null)
                    continue;

                int reservedEnergy = slot.ReservedEnergy;
                int reservedStars = slot.ReservedStars;
                slot.ClearReservation();
                state.ResolvingSlot = slot;
                state.NotifyChanged();
                try
                {
                    bool triggered = await ResolveCardAsync(
                        state,
                        slot,
                        card,
                        reservedEnergy,
                        reservedStars);
                    if (triggered)
                        state.CurrentTurnTriggeredCards++;
                }
                finally
                {
                    state.ResolvingSlot = null;
                    slot.IsSpent = true;
                    slot.ClearCard();
                    state.NotifyChanged();
                }
            }
        }
        catch (Exception exception)
        {
            Log.Error("[LibraryOfRuinaLib] Speed-dice advance failed: " + exception);
        }
        finally
        {
            state.ResolvingSlot = null;
            state.IsResolving = false;
            state.NotifyChanged();
            state.Gate.Release();
        }
    }

    private static async Task<bool> ResolveCardAsync(
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot,
        CardModel card,
        int reservedEnergy,
        int reservedStars)
    {
        var choiceContext = new BlockingPlayerChoiceContext();
        try
        {
            Creature? target = slot.Target;
            var clashContext = new LibraryClashContext(state.Player, slot, target);
            await LibraryClashResolver.Current.ResolveAsync(clashContext);
            target = clashContext.Target;

            card.CanPlay(out UnplayableReason reason, out _);
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

            await SpendReservedResourcesAsync(
                card,
                reservedEnergy,
                reservedStars);
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
                $"[LibraryOfRuinaLib] Speed-die card {card.Id.Entry} failed: {exception}");
            await DiscardFailedCardAsync(card);
            return false;
        }
    }

    private static async Task SpendReservedResourcesAsync(
        CardModel card,
        int energy,
        int stars)
    {
        energy = Math.Max(0, energy);
        stars = Math.Max(0, stars);

        if (energy > 0)
        {
            CombatManager.Instance.History.EnergySpent(
                card.CombatState,
                energy,
                card.Owner);
            card.Owner.PlayerCombatState!.LoseEnergy(energy);
        }
        await Hook.AfterEnergySpent(
            card.CombatState,
            card,
            energy);

        card.LastStarsSpent = stars;
        if (stars > 0)
        {
            card.Owner.PlayerCombatState!.LoseStars(stars);
            await Hook.AfterStarsSpent(
                card.Owner.Creature.CombatState,
                stars,
                card.Owner);
        }
    }

    private static async Task SelectTargetAsync(
        LibrarySpeedDiceCombatState state,
        int slotIndex,
        CardModel card,
        Control targetingOrigin)
    {
        if (!GodotObject.IsInstanceValid(targetingOrigin)
            || !state.HasRolled
            || state.IsSelectingTarget)
        {
            return;
        }

        await targetingOrigin.ToSignal(
            targetingOrigin.GetTree(),
            SceneTree.SignalName.ProcessFrame);

        if (!IsStateUsable(state)
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
                    || !state.HasRolled
                    || state.IsLocked
                    || state.IsResolving
                    || slotIndex < 0
                    || slotIndex >= state.Slots.Count
                    || !ReferenceEquals(state.Slots[slotIndex].Card, card),
                node =>
                {
                    Creature? target = GetCreatureFromTargetNode(node);
                    return target != null && card.IsValidSpeedDiceTarget(target);
                });
            targetLine = LibrarySpeedDiceTargetLine.Begin(
                targetManager,
                targetingOrigin,
                targetMode == TargetMode.Controller);

            Node? selectedNode = await targetManager.SelectionFinished();
            Creature? selectedTarget = GetCreatureFromTargetNode(selectedNode);
            if (selectedTarget == null)
                return;

            await state.Gate.WaitAsync();
            try
            {
                if (IsStateUsable(state)
                    && state.HasRolled
                    && !state.IsLocked
                    && !state.IsResolving
                    && slotIndex >= 0
                    && slotIndex < state.Slots.Count
                    && ReferenceEquals(state.Slots[slotIndex].Card, card)
                    && card.IsValidSpeedDiceTarget(selectedTarget))
                {
                    state.Slots[slotIndex].Target = selectedTarget;
                    state.NotifyChanged();
                }
            }
            finally
            {
                state.Gate.Release();
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
        NPlayerHand? hand = NPlayerHand.Instance;
        if (hand == null)
            return null;

        return (CurrentCardPlayField?.GetValue(hand) as NCardPlay)?.Holder.CardModel;
    }

    private static async Task MoveEquippedCardsAsync(
        IReadOnlyList<LibrarySpeedDiceSlot> equippedSlots,
        IReadOnlyList<CardModel> cards,
        PileType destination)
    {
        if (cards.Count == 0)
            return;

        IReadOnlyList<CardPileAddResult> results = await CardPileCmd.Add(
            cards,
            destination,
            skipVisuals: destination != PileType.Hand);
        foreach (CardPileAddResult result in results)
        {
            if (!result.success)
            {
                Log.Error(
                    $"[LibraryOfRuinaLib] Failed to move unused speed-dice card "
                    + $"{result.cardAdded.Id.Entry} to {destination}.");
                continue;
            }

            LibrarySpeedDiceSlot? slot = equippedSlots.FirstOrDefault(
                candidate => ReferenceEquals(
                    candidate.Card,
                    result.cardAdded));
            slot?.ClearCard();
        }
    }

    private static bool CanEquipCard(
        LibrarySpeedDiceCombatState state,
        CardModel card)
    {
        try
        {
            return state.Participant.CanEquipCard(card);
        }
        catch (Exception exception)
        {
            Log.Error(
                $"[LibraryOfRuinaLib] Participant {state.Participant.Id} card predicate failed: {exception}");
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

    private static bool TryCalculateReservation(
        LibrarySpeedDiceCombatState state,
        CardModel card,
        out int energy,
        out int stars)
    {
        bool hasCustomCost = card is ILibrarySpeedDiceCard;
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

        int energyAvailable = Math.Max(
            0,
            state.Player.PlayerCombatState!.Energy - state.ReservedEnergy);
        int starsAvailable = Math.Max(
            0,
            state.Player.PlayerCombatState!.Stars - state.ReservedStars);

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
        bool isDamageGiven)
    {
        if (damage <= 0)
            return;

        int threshold = Math.Max(
            1,
            (int)Math.Ceiling(
                state.Player.Creature.MaxHp
                * state.Participant.Emotion.DamageUnitFractionOfMaxHp));
        int total = damage + (isDamageGiven
            ? state.DamageGivenAccumulator
            : state.DamageReceivedAccumulator);
        int units = total / threshold;
        int remainder = total % threshold;
        if (isDamageGiven)
            state.DamageGivenAccumulator = remainder;
        else
            state.DamageReceivedAccumulator = remainder;

        AddEmotionUnits(state, units);
    }

    private static void AddEmotionUnits(
        LibrarySpeedDiceCombatState state,
        int units)
    {
        if (units <= 0)
            return;

        state.Emotion.AddUnits(units, state.Participant.Emotion);
        state.NotifyChanged();
    }

    private static int GetDiceCount(LibrarySpeedDiceCombatState state)
    {
        int extra = state.Emotion.Level
            >= state.Participant.Emotion.ExtraSpeedDieLevel
            ? state.Participant.Emotion.ExtraSpeedDice
            : 0;
        return Math.Max(0, state.Participant.BaseSpeedDiceCount + extra);
    }

    private static LibrarySpeedDiceParticipant? FindParticipant(Player player)
    {
        lock (Sync)
        {
            return Participants.FirstOrDefault(x =>
            {
                try
                {
                    return x.IsEnabledForPlayer(player);
                }
                catch (Exception exception)
                {
                    Log.Error(
                        $"[LibraryOfRuinaLib] Participant {x.Id} predicate failed: {exception}");
                    return false;
                }
            });
        }
    }

    private static bool IsSingleplayerEnabled()
    {
        try
        {
            return RunManager.Instance.NetService.Type == NetGameType.Singleplayer;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsStateUsable(LibrarySpeedDiceCombatState state)
    {
        return IsSingleplayerEnabled()
            && state.Player.PlayerCombatState != null
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
                and not TargetType.AnyAlly;
        }

        if (!target.IsAlive)
            return false;

        return targetType switch
        {
            TargetType.AnyEnemy => target.Side != card.Owner.Creature.Side,
            TargetType.AnyAlly => target.Side == card.Owner.Creature.Side,
            _ => false,
        };
    }
}
