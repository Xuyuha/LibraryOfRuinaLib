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

/// <summary>玩家输入：选择卡牌、装备/卸下/改目标，以及多人下的输入路由。</summary>
internal static partial class LibrarySpeedDiceService
{
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

        ExplicitSelection? selection = GetSelectedSelection();
        CardModel? card = selection?.Card;
        canAcceptSelectedCard =
            card != null
            && selection != null
            && card.Owner == state.Player
            && !card.EnergyCost.CostsX
            && !card.HasStarCostX
            && CanEquipCard(state, card, selection.Value.SourceId)
            && CanReserveCard(state, card);
        return canAcceptSelectedCard;
    }

    public static bool CanEquipCard(CardModel card)
    {
        return !card.IsCanonical
            && card.Owner != null
            && TryGetState(
                card.Owner,
                out var state)
            && state != null
            && !state.IsLifecycleBusy
            && TryResolveSelectionSource(
                state,
                card,
                requestedSourceId: null,
                out string sourceId)
            && CanEquipCard(state, card, sourceId)
            && state.Slots.Any(slot =>
                !slot.IsSpent
                && !slot.IsLocked
                && slot.Card == null);
    }

    internal static bool CanEquipCard(
        CardModel card,
        string sourceId)
    {
        return !card.IsCanonical
            && card.Owner != null
            && TryGetState(
                card.Owner,
                out LibrarySpeedDiceCombatState? state)
            && state != null
            && !state.IsLifecycleBusy
            && CanEquipCard(state, card, sourceId)
            && state.Slots.Any(slot =>
                !slot.IsSpent
                && !slot.IsLocked
                && slot.Card == null);
    }

    public static bool TryBeginEquipSelection(CardModel card)
    {
        return TryBeginEquipSelection(
            card,
            LibrarySpeedDiceSelectionSourceIds.Hand);
    }

    internal static bool TryBeginEquipSelection(
        CardModel card,
        string sourceId)
    {
        if (_explicitSelection != null
            || !CanEquipCard(card, sourceId))
        {
            return false;
        }

        _explicitSelection = new ExplicitSelection(card, sourceId);
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
        EndEquipSelection(card, sourceId: null);
    }

    internal static void EndEquipSelection(
        CardModel card,
        string? sourceId)
    {
        if (_explicitSelection is not { } selection
            || !ReferenceEquals(selection.Card, card)
            || sourceId != null
            && !string.Equals(
                selection.SourceId,
                sourceId,
                StringComparison.Ordinal))
        {
            return;
        }

        _explicitSelection = null;
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

        if (_explicitSelection != null)
            return;

        CardModel? selectedCard = GetSelectedSelection()?.Card;
        if (selectedCard != null
            && RoutesInputThroughNetwork(state))
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
        Control targetingOrigin,
        bool usingController = false)
    {
        await SubmitEquipCardAsync(
            card,
            slotIndex,
            targetingOrigin,
            usingController,
            LibrarySpeedDiceSelectionSourceIds.Hand);
    }

    internal static async Task<bool> SubmitEquipCardAsync(
        CardModel card,
        int slotIndex,
        Control targetingOrigin,
        bool usingController,
        string sourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        if (card.Owner == null
            || !TryGetState(
                card.Owner,
                out var state)
            || state == null)
        {
            return false;
        }

        if (state.IsLifecycleBusy)
            return false;

        if (!CanEquipCard(state, card, sourceId))
            return false;

        if (RoutesInputThroughNetwork(state))
        {
            Creature? target = null;
            if (card.RequiresSpeedDiceTarget())
            {
                target = await SelectUnequippedCardTargetAsync(
                    state,
                    card,
                    targetingOrigin,
                    usingController,
                    sourceId);
                if (target == null)
                    return false;
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
                || !CanEquipCard(state, card, sourceId))
            {
                return false;
            }

            return await state.Registration.Dispatcher.RouteInputAsync(
                new LibrarySpeedDiceInputRequest(
                    LibrarySpeedDiceInputKind.Equip,
                    state.Player,
                    slotIndex,
                    state.Player.PlayerCombatState?.TurnNumber ?? -1,
                    state.Revision,
                    card,
                    target)
                {
                    SourceId = sourceId,
                });
        }

        if (card.GetSpeedDiceAssignmentMode()
            == LibrarySpeedDiceAssignmentMode.Persistent)
        {
            await ActivateSlotWithLifecycleAsync(
                state,
                slotIndex,
                targetingOrigin,
                card,
                allowRetargetExisting: false);
            return TryGetEquippedSlot(card, out _);
        }

        Creature? instantTarget = null;
        if (card.RequiresSpeedDiceTarget())
        {
            instantTarget = await SelectUnequippedCardTargetAsync(
                state,
                card,
                targetingOrigin,
                usingController,
                sourceId);
            if (instantTarget == null)
                return false;
        }

        return await ExecuteEquipAsync(
            new BlockingPlayerChoiceContext(),
            state.Player,
            card,
            slotIndex,
            instantTarget,
            state.Player.PlayerCombatState?.TurnNumber ?? -1,
            state.Revision,
            sourceId);
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
                    || !CanEquipCard(
                        state,
                        card,
                        LibrarySpeedDiceSelectionSourceIds.Hand))
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

        if (RoutesInputThroughNetwork(state))
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

    public static Task<bool> ExecuteEquipAsync(
        PlayerChoiceContext choiceContext,
        Player player,
        CardModel card,
        int slotIndex,
        Creature? target,
        int expectedTurnNumber,
        int expectedRevision)
    {
        return ExecuteEquipAsync(
            choiceContext,
            player,
            card,
            slotIndex,
            target,
            expectedTurnNumber,
            expectedRevision,
            LibrarySpeedDiceSelectionSourceIds.Hand);
    }

    public static async Task<bool> ExecuteEquipAsync(
        PlayerChoiceContext choiceContext,
        Player player,
        CardModel card,
        int slotIndex,
        Creature? target,
        int expectedTurnNumber,
        int expectedRevision,
        string sourceId)
    {
        ArgumentNullException.ThrowIfNull(choiceContext);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        if (!TryGetState(player, out LibrarySpeedDiceCombatState? state)
            || state == null)
        {
            return false;
        }

        if (state.IsLocked || state.IsResolving)
            return false;

        bool lifecycleAcquired = state.TryBeginLifecycle();
        LibrarySpeedDiceCardLease? instantLease = null;
        LibrarySpeedDiceCombatState.GameplayNotificationBatch?
            instantNotificationBatch = null;
        bool instantResourcesCommitted = false;
        try
        {
            LibrarySpeedDiceSlot? equippedSlot = null;
            LibrarySpeedDiceInstantAssignmentContext? instantContext = null;
            await state.Gate.WaitAsync();
            try
            {
                if (!CanExecuteInput(
                        state,
                        slotIndex,
                        expectedTurnNumber,
                        expectedRevision,
                        requireExpectedRevision: false)
                    || card.Owner != player
                    || state.Slots[slotIndex].Card != null
                    || !CanEquipCard(
                        state,
                        card,
                        sourceId,
                        ignoreLocalInteractionState: true)
                    || card.RequiresSpeedDiceTarget()
                    && !card.IsValidSpeedDiceTarget(target))
                {
                    return false;
                }

                LibrarySpeedDiceSlot slot = state.Slots[slotIndex];
                LibrarySpeedDiceAssignmentMode assignmentMode =
                    card.GetSpeedDiceAssignmentMode();
                if (assignmentMode == LibrarySpeedDiceAssignmentMode.Instant
                    && (slot.IsSpent || slot.IsLocked))
                {
                    return false;
                }

                if (state.Revision != expectedRevision)
                {
                    int actualRevision = state.Revision;
                    state.Revision = expectedRevision;
                    Log.Warn(
                        "[LibraryOfRuinaLib] Reconciled synchronized speed-die equip revision; "
                        + $"player={player.NetId} slot={slotIndex} turn={expectedTurnNumber} "
                        + $"expected={expectedRevision} actual={actualRevision}.");
                }

                if (assignmentMode == LibrarySpeedDiceAssignmentMode.Instant)
                {
                    instantNotificationBatch =
                        state.BeginGameplayNotificationBatch();
                }

                if (!TryCreateReservationLease(
                        state,
                        card,
                        slot,
                        out LibrarySpeedDiceCardLease? lease)
                    || lease == null)
                {
                    return false;
                }

                if (assignmentMode == LibrarySpeedDiceAssignmentMode.Instant)
                {
                    instantLease = lease;
                    if (!await lease.Transaction.CommitAsync())
                    {
                        lease.Transaction.Release();
                        return false;
                    }

                    lease.IsCommitted = true;
                    instantResourcesCommitted = true;
                    instantContext =
                        new LibrarySpeedDiceInstantAssignmentContext(
                            choiceContext,
                            state,
                            slot,
                            card,
                            target,
                            sourceId,
                            lease.ReservationPlan);
                }
                else
                {
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
            }
            catch (Exception exception)
            {
                if (instantLease?.IsCommitted != true)
                    instantLease?.Transaction.Release();
                Log.Error(
                    "[LibraryOfRuinaLib] Synchronized speed-die equip failed: "
                    + exception);
                return instantResourcesCommitted;
            }
            finally
            {
                state.Gate.Release();
            }

            if (instantContext != null && instantLease != null)
            {
                try
                {
                    await state.Registration.Dispatcher
                        .OnInstantAssignmentAsync(instantContext);
                    return true;
                }
                catch (Exception exception)
                {
                    Log.Error(
                        "[LibraryOfRuinaLib] Instant speed-die assignment lifecycle failed: "
                        + exception);
                    return true;
                }
                finally
                {
                    instantLease.IsReleased = true;
                }
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
            try
            {
                if (instantNotificationBatch != null)
                {
                    if (instantResourcesCommitted)
                        instantNotificationBatch.Complete();
                    else
                        instantNotificationBatch.Dispose();
                }

                if (instantResourcesCommitted && instantLease != null)
                    instantLease.IsReleased = true;
            }
            finally
            {
                if (lifecycleAcquired)
                    state.EndLifecycle();
            }
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
        return RequestEquipAsync(
            player,
            card,
            slotIndex,
            target,
            expectedTurnNumber,
            expectedRevision,
            LibrarySpeedDiceSelectionSourceIds.Hand);
    }

    public static Task RequestEquipAsync(
        Player player,
        CardModel card,
        int slotIndex,
        Creature? target,
        int expectedTurnNumber,
        int expectedRevision,
        string sourceId)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        return RequestInputAsync(
            new LibrarySpeedDiceInputRequest(
                LibrarySpeedDiceInputKind.Equip,
                player,
                slotIndex,
                expectedTurnNumber,
                expectedRevision,
                card,
                target)
            {
                SourceId = sourceId,
            });
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

        if (RoutesInputThroughNetwork(state))
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
                    request.Revision,
                    string.IsNullOrWhiteSpace(request.SourceId)
                        ? LibrarySpeedDiceSelectionSourceIds.Hand
                        : request.SourceId);
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

        if (state.IsLocked || state.IsResolving)
            return false;

        bool lifecycleAcquired = state.TryBeginLifecycle();
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
            if (lifecycleAcquired)
                state.EndLifecycle();
        }
    }

    private static bool CanExecuteInput(
        LibrarySpeedDiceCombatState state,
        int slotIndex,
        int expectedTurnNumber,
        int expectedRevision,
        bool requireExpectedRevision = true)
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
            && expectedRevision >= 0
            && (!requireExpectedRevision
                || state.Revision == expectedRevision);
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
            TargetMode targetMode =
                LibrarySpeedDiceInputMode.ResolveTargetMode(false);
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

            if (RoutesInputThroughNetwork(state))
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
        Control targetingOrigin,
        bool usingController,
        string sourceId)
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
            || !CanEquipCard(state, card, sourceId)
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
                LibrarySpeedDiceInputMode.ResolveTargetMode(
                    usingController);
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
                    || !TryResolveSelectionSource(
                        state,
                        card,
                        sourceId,
                        out _),
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
                && TryResolveSelectionSource(
                    state,
                    card,
                    sourceId,
                    out _)
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

    private static ExplicitSelection? GetSelectedSelection()
    {
        if (_explicitSelection is { } selection)
        {
            CardModel card = selection.Card;
            if (!card.IsCanonical
                && card.Owner != null
                && TryGetState(
                    card.Owner,
                    out LibrarySpeedDiceCombatState? state)
                && state != null
                && TryResolveSelectionSource(
                    state,
                    card,
                    selection.SourceId,
                    out _))
            {
                return selection;
            }

            _explicitSelection = null;
        }
        return null;
    }

    private static bool CanEquipCard(
        LibrarySpeedDiceCombatState state,
        CardModel card,
        string sourceId,
        bool ignoreLocalInteractionState = false)
    {
        if (!IsStateUsable(state)
            || state.Player.PlayerCombatState?.Phase != PlayerTurnPhase.Play
            || !state.HasRolled
            || state.IsLocked
            || state.IsResolving
            || !ignoreLocalInteractionState
            && state.IsSelectingTarget
            || card.Owner != state.Player
            || card.EnergyCost.CostsX
            || card.HasStarCostX
            || card.GetSpeedDiceAssignmentMode()
                == LibrarySpeedDiceAssignmentMode.Persistent
            && !string.Equals(
                sourceId,
                LibrarySpeedDiceSelectionSourceIds.Hand,
                StringComparison.Ordinal)
            || !TryResolveSelectionSource(
                state,
                card,
                sourceId,
                out _)
            || !CanParticipantEquipCard(state, card))
        {
            return false;
        }

        return CanReserveCard(state, card);
    }

    internal static bool TryResolveSelectionSource(
        LibrarySpeedDiceCombatState state,
        CardModel card,
        string? requestedSourceId,
        out string sourceId)
    {
        sourceId = string.Empty;
        if (card.IsCanonical || card.Owner != state.Player)
            return false;

        if (!string.IsNullOrWhiteSpace(requestedSourceId))
        {
            if (string.Equals(
                    requestedSourceId,
                    LibrarySpeedDiceSelectionSourceIds.Hand,
                    StringComparison.Ordinal))
            {
                if (card.Pile?.Type != PileType.Hand)
                    return false;

                sourceId = LibrarySpeedDiceSelectionSourceIds.Hand;
                return true;
            }

            ILibrarySpeedDiceSelectionSource? requested =
                state.Registration.Dispatcher.FindSelectionSource(
                    requestedSourceId);
            if (requested == null
                || !CanSelectionSourceSelect(requested, state, card))
            {
                return false;
            }

            sourceId = requested.SourceId;
            return true;
        }

        var matches = new List<string>();
        if (card.Pile?.Type == PileType.Hand)
            matches.Add(LibrarySpeedDiceSelectionSourceIds.Hand);

        foreach (ILibrarySpeedDiceSelectionSource source in
                 state.Registration.Dispatcher.SelectionSources)
        {
            if (CanSelectionSourceSelect(source, state, card))
                matches.Add(source.SourceId);
        }

        if (matches.Count != 1)
        {
            if (matches.Count > 1)
            {
                Log.Error(
                    "[LibraryOfRuinaLib] Ambiguous speed-dice selection source "
                    + $"for card {card.Id}: {string.Join(", ", matches)}");
            }
            return false;
        }

        sourceId = matches[0];
        return true;
    }

    internal static Control? GetSelectionTargetingOrigin(
        LibrarySpeedDiceCombatState state,
        CardModel card,
        string sourceId)
    {
        ILibrarySpeedDiceSelectionSource? source =
            state.Registration.Dispatcher.FindSelectionSource(sourceId);
        if (source == null
            || !CanSelectionSourceSelect(source, state, card))
        {
            return null;
        }

        try
        {
            return source.GetTargetingOrigin(state, card);
        }
        catch (Exception exception)
        {
            Log.Error(
                $"[LibraryOfRuinaLib] Selection source '{sourceId}' targeting origin failed: "
                + exception);
            return null;
        }
    }

    private static bool CanSelectionSourceSelect(
        ILibrarySpeedDiceSelectionSource source,
        LibrarySpeedDiceCombatState state,
        CardModel card)
    {
        try
        {
            return source.CanSelect(state, card);
        }
        catch (Exception exception)
        {
            Log.Error(
                $"[LibraryOfRuinaLib] Selection source '{source.SourceId}' predicate failed: "
                + exception);
            return false;
        }
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
}
