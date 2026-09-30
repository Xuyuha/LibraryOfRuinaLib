using System.Runtime.CompilerServices;
using Godot;
using LibraryLib.Light;
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

namespace LibraryLib.SpeedDice;

/// <summary>回合流程：回合开始/结束、投掷与按速度结算已装备的卡牌。</summary>
internal static partial class LibrarySpeedDiceService
{
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

        int turnNumber =
            state.Player.PlayerCombatState?.TurnNumber ?? -1;
        if (state.PreparedTurnNumber == turnNumber)
            return;

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
        state.PreparedTurnNumber = turnNumber;
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

        if (RoutesInputThroughNetwork(state))
        {
            return false;
        }

        return RunManager.Instance.ActionExecutor.CurrentlyRunningAction == null;
    }

    internal static bool IsLocalPlayerInResolutionLifecycle()
    {
        return TryGetLocalState(out LibrarySpeedDiceCombatState? state)
            && state != null
            && (state.IsLifecycleBusy || state.IsResolving);
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
            try
            {
                foreach (LibrarySpeedDiceResolutionBatchContext batchContext
                         in startedBatches)
                {
                    await batchContext.State.Registration.Dispatcher
                        .AfterResolutionBatchAsync(batchContext);
                }
            }
            finally
            {
                foreach (LibrarySpeedDiceCombatState state in states)
                {
                    foreach (LibrarySpeedDiceSlot slot in state.Slots)
                        slot.IsLocked = false;

                    state.IsLocked = false;
                    state.NotifyGameplayChanged();
                }
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
                    // 卡因无有效目标而无法打出、退回手牌时，该速度骰子
                    // 必须碎裂，否则玩家可以反复装备同一张带“使用时”
                    // 效果的卡，无限触发其效果。
                    slot.IsSpent = true;
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
}
