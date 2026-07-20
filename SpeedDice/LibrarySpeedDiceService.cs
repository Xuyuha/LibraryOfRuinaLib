using System.Runtime.CompilerServices;
using Godot;
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
using MegaCrit.Sts2.Core.Context;
namespace Library.SpeedDice;

internal static class LibrarySpeedDiceService
{
    private static readonly Lock Sync = new();
    private static readonly List<LibrarySpeedDiceParticipant> Participants = [];
    private static readonly ConditionalWeakTable<Player, LibrarySpeedDiceCombatState> States = new();
    private static WeakReference<LibrarySpeedDiceCombatState>? _localState;
    private static CardModel? _explicitlySelectedCard;

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

        var participant = FindParticipant(player);
        if (participant == null)
            return false;

        if (!States.TryGetValue(player, out state))
        {
            state = new LibrarySpeedDiceCombatState(player, participant);
            state.ReplaceSlots(GetDiceCount(state));
            States.Add(player, state);
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
        if (!TryGetState(owner, out var state)
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

    public static void ClearCombat()
    {
        _explicitlySelectedCard = null;
        if (_localState != null
            && _localState.TryGetTarget(out var state))
        {
            States.Remove(state.Player);
        }
        _localState = null;
    }

    public static void BeginPlayerTurn(Creature creature, CombatSide side)
    {
        if (side != CombatSide.Player
            || creature.Player == null
            || !TryGetState(creature.Player, out var state)
            || state == null)
        {
            return;
        }

        state.Emotion.TryLevelUp(state.Participant.Emotion);
        state.PreviousTurnTriggeredCards = state.CurrentTurnTriggeredCards;
        state.CurrentTurnTriggeredCards = 0;
        state.BonusDrawPending =
            state.Emotion.Level >= state.Participant.Emotion.BonusDrawLevel
            && state.PreviousTurnTriggeredCards
            >= state.Participant.Emotion.BonusDrawRequiredTriggeredCards;

        var turnMixin = unchecked(
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
                out var state))
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
        if (!TryGetLocalState(out var state)
            || state == null
            || state.IsLocked
            || state.IsResolving
            || state.IsSelectingTarget
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
        return TryGetLocalState(out var state) && state != null
            ? AdvanceAsync(state)
            : Task.CompletedTask;
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
            && TryCalculateReservation(
                state,
                card,
                out _,
                out _);
        return canAcceptSelectedCard;
    }

    public static bool CanEquipCard(CardModel card)
    {
        return card.Owner != null
            && TryGetState(
                card.Owner,
                out var state)
            && state != null
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

        await ActivateSlotAsync(
            state,
            slotIndex,
            targetingOrigin,
            GetSelectedCard(),
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

        await ActivateSlotAsync(
            state,
            slotIndex,
            targetingOrigin,
            card,
            allowRetargetExisting: false);
    }

    private static async Task ActivateSlotAsync(
        LibrarySpeedDiceCombatState state,
        int slotIndex,
        Control targetingOrigin,
        CardModel? selectedCard,
        bool allowRetargetExisting)
    {
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

                if (!TryCalculateReservation(state, card, out var energy, out var stars))
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
        if (!TryGetLocalState(out var state) || state == null)
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

            var slot = state.Slots[slotIndex];
            var card = slot.Card;
            if (card == null)
                return;

            var result = await CardPileCmd.Add(card, PileType.Hand);
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

    /// <summary>
    /// 应用速度骰子资源预定的限制：如果其他速度骰子已经预定了部分能量/星光，
    /// 则当前卡必须用剩余资源支付。能量不足时可用星光补足缺口（1:2比率），
    /// 设置对应的UnplayableReason。
    /// </summary>
    public static void ApplyReservedResourceRestriction(
        CardModel card,
        ref UnplayableReason reason,
        ref bool result)
    {
        if (!TryGetState(card.Owner, out var state)
            || state == null
            || state.ReservedEnergy <= 0 && state.ReservedStars <= 0)
        {
            return;
        }

        var resources = card.Owner?.PlayerCombatState;
        if (resources == null)
            return;
        // 可用资源 = 总资源 - 已被其他速度骰子预定的部分
        var energyAvailable = Math.Max(0, resources.Energy - state.ReservedEnergy);
        var starsAvailable = Math.Max(0, resources.Stars - state.ReservedStars);
        var energyCost = Math.Max(0, card.EnergyCost.GetWithModifiers(CostModifiers.All));
        var starCost = Math.Max(0, card.GetStarCostWithModifiers());

        // 能量不足时，用星光补足缺口
        // 兑换比率：1点能量缺口 = 2点星光额外消耗
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
        return TryGetState(player, out var state) && state != null
            ? state.Emotion.Level * state.Participant.Emotion.MaxEnergyPerLevel
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
            || !TryGetState(dealer.Player, out var state)
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
                out var targetState)
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
                var emotionUnits = 0;
                foreach (var slot in state.Slots)
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

            if (!await RepairInvalidTargetsBeforeResolutionAsync(state))
                return;

            foreach (var slot in state.Slots)
            {
                slot.IsLocked = true;
            }

            state.IsLocked = true;
            state.IsResolving = true;
            state.NotifyChanged();

            IEnumerable<LibrarySpeedDiceSlot> orderedSlots = state.Slots
                .OrderByDescending(x => x.FinalValue)
                .ThenBy(x => x.Index);
            foreach (var slot in orderedSlots)
            {
                var card = slot.Card;
                if (card == null)
                    continue;

                var reservedEnergy = slot.ReservedEnergy;
                var reservedStars = slot.ReservedStars;
                slot.ClearReservation();
                state.ResolvingSlot = slot;
                state.NotifyChanged();
                try
                {
                    var triggered = await ResolveCardAsync(
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
            Log.Error("[LibraryOfRuinaLib] Speed dice advance failed: " + exception);
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
            var target = slot.Target;
            if (!card.IsValidSpeedDiceTarget(target))
            {
                target = GetRandomValidTarget(state, card);
                slot.Target = target;
                state.NotifyChanged();
            }

            var clashContext = new LibraryClashContext(state.Player, slot, target);
            await LibraryClashResolver.Current.ResolveAsync(clashContext);
            target = clashContext.Target;
            if (!clashContext.CancelCard
                && !card.IsValidSpeedDiceTarget(target))
            {
                target = GetRandomValidTarget(state, card);
                slot.Target = target;
                state.NotifyChanged();
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
                $"[LibraryOfRuinaLib] Speed die card {card.Id.Entry} failed: {exception}");
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
                card.CombatState!,
                energy,
                card.Owner);
            card.Owner.PlayerCombatState!.LoseEnergy(energy);
        }
        await Hook.AfterEnergySpent(
            card.CombatState!,
            card,
            energy);

        card.LastStarsSpent = stars;
        if (stars > 0)
        {
            card.Owner.PlayerCombatState!.LoseStars(stars);
            await Hook.AfterStarsSpent(
                card.Owner.Creature.CombatState!,
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

            await state.Gate.WaitAsync();
            try
            {
                if (IsStateUsable(state)
                    && state.HasRolled
                    && !state.IsLocked
                    && !state.IsResolving
                    
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
        if (_explicitlySelectedCard is { } explicitCard)
        {
            if (explicitCard.Pile?.Type == PileType.Hand)
                return explicitCard;

            _explicitlySelectedCard = null;
        }
        return null;
    }

    private static async Task MoveEquippedCardsAsync(
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
            slot?.ClearCard();
        }
    }

    private static bool CanEquipCard(
        LibrarySpeedDiceCombatState state,
        CardModel card)
    {
        if (!IsStateUsable(state)
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

        return TryCalculateReservation(
            state,
            card,
            out _,
            out _);
    }

    private static bool CanParticipantEquipCard(
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

    private static async Task<bool> RepairInvalidTargetsBeforeResolutionAsync(
        LibrarySpeedDiceCombatState state)
    {
        var changed = false;
        foreach (var slot in state.Slots)
        {
            var card = slot.Card;
            if (card == null || !slot.RequiresTarget || slot.HasValidTarget)
                continue;

            if (!slot.IsSpent && card.Pile?.Type == PileType.Play)
            {
                var result = await CardPileCmd.Add(
                    card,
                    PileType.Hand);
                if (result.success)
                {
                    slot.ClearCard();
                    changed = true;
                    continue;
                }
            }

            var target = GetRandomValidTarget(state, card);
            if (target != null)
            {
                slot.Target = target;
                changed = true;
            }
        }

        if (changed)
            state.NotifyChanged();
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
        return state.Player.RunState.Rng.CombatTargets.NextItem(candidates);
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
        bool isDamageGiven)
    {
        if (damage <= 0)
            return;

        var threshold = Math.Max(
            1,
            (int)Math.Ceiling(
                state.Player.Creature.MaxHp
                * state.Participant.Emotion.DamageUnitFractionOfMaxHp));
        var total = damage + (isDamageGiven
            ? state.DamageGivenAccumulator
            : state.DamageReceivedAccumulator);
        var units = total / threshold;
        var remainder = total % threshold;
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
        var extra = state.Emotion.Level
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

    private static bool IsSingleplayerEnabled() //暂时屏蔽了多人模式
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
