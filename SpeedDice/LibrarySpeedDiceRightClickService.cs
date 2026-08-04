using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace LibraryLib.SpeedDice;

internal static class LibrarySpeedDiceRightClickService
{
    private static CardModel? _activeCard;
    private static string? _activeSourceId;
    private static Dictionary<Control, int> _activeDiceControls = [];
    private static LibrarySpeedDiceRightClickTargetLine? _activeTargetLine;

    public static bool TryHandle(
        CardModel card,
        bool usingController)
    {
        if (card is not ILibrarySpeedDiceCard
            {
                EnableSpeedDiceRightClickSelection: true,
            })
        {
            return false;
        }

        if (CanBeginSelection(card))
        {
            TaskHelper.RunSafely(
                BeginSelectionAsync(card, usingController));
        }

        // 实现该接口且未关闭自动入口的卡，其右键事件归速度骰子系统所有。
        return true;
    }

    public static bool CanBeginSelection(CardModel card)
    {
        NPlayerHand? hand = NPlayerHand.Instance;
        return CanRequestSelection(card)
            && hand != null
            && !hand.InCardPlay
            && !NTargetManager.Instance.IsInSelection;
    }

    public static bool CanRequestSelection(CardModel card)
    {
        return CanRequestSelection(
            card,
            LibrarySpeedDiceSelectionSourceIds.Hand);
    }

    internal static bool CanRequestSelection(
        CardModel card,
        string sourceId)
    {
        NPlayerHand? hand = NPlayerHand.Instance;
        if (string.IsNullOrWhiteSpace(sourceId)
            || card is not ILibrarySpeedDiceCard
            {
                EnableSpeedDiceRightClickSelection: true,
            }
            || card.IsCanonical
            || string.Equals(
                sourceId,
                LibrarySpeedDiceSelectionSourceIds.Hand,
                StringComparison.Ordinal)
            && card.Pile?.Type != PileType.Hand
            || hand == null
            || hand.IsInCardSelection
            || !LibrarySpeedDiceService.CanEquipCard(
                card,
                sourceId))
        {
            return false;
        }

        return FindAvailableDiceControls(card).Count > 0;
    }

    public static Task<LibrarySpeedDiceSelectionResult> BeginSelectionAsync(
        CardModel card,
        bool usingController)
    {
        return BeginSlotSelectionAsync(
            card,
            usingController,
            LibrarySpeedDiceSelectionSourceIds.Hand);
    }

    public static async Task<LibrarySpeedDiceSelectionResult>
        BeginSlotSelectionAsync(
            CardModel card,
            bool usingController,
            string? requestedSourceId)
    {
        if (card.IsCanonical
            || card.Owner == null
            || !LibrarySpeedDiceService.TryGetState(
                card.Owner,
                out LibrarySpeedDiceCombatState? state)
            || state == null
            || !LibrarySpeedDiceService.TryResolveSelectionSource(
                state,
                card,
                requestedSourceId,
                out string sourceId)
            || NPlayerHand.Instance is { InCardPlay: true }
            || NPlayerHand.Instance?.IsInCardSelection == true
            || NTargetManager.Instance.IsInSelection
            || !LibrarySpeedDiceService.CanEquipCard(card, sourceId)
            || FindAvailableDiceControls(card).Count == 0
            || !LibrarySpeedDiceService.TryBeginEquipSelection(
                card,
                sourceId))
        {
            return LibrarySpeedDiceSelectionResult.Rejected;
        }

        NTargetManager targetManager = NTargetManager.Instance;
        bool dieSelectionStarted = false;
        bool useControllerTargeting =
            LibrarySpeedDiceInputMode.ShouldUseControllerTargeting(
                usingController);
        LibrarySpeedDiceRightClickTargetLine? targetLine = null;
        LibrarySpeedDiceSelectionResult selectionResult =
            LibrarySpeedDiceSelectionResult.Rejected;
        NotifyEquipSelectionChanged(state, card, isSelecting: true);
        try
        {
            NHandCardHolder? holder = null;
            Control? source;
            if (string.Equals(
                    sourceId,
                    LibrarySpeedDiceSelectionSourceIds.Hand,
                    StringComparison.Ordinal))
            {
                holder = NPlayerHand.Instance?.GetCardHolder(card)
                    as NHandCardHolder;
                source = (Control?)holder?.CardNode ?? holder;
            }
            else
            {
                source = LibrarySpeedDiceService
                    .GetSelectionTargetingOrigin(
                        state,
                        card,
                        sourceId);
            }

            Dictionary<Control, int> diceControls =
                FindAvailableDiceControls(card);
            if (source == null || diceControls.Count == 0)
                return LibrarySpeedDiceSelectionResult.Rejected;

            NDebugAudioManager.Instance?.Play("card_select.mp3", 0.5f);
            if (holder != null)
                NHoverTipSet.Remove(holder);
            holder?.CardNode?.CardHighlight.AnimFlash();

            SetActiveSelection(card, sourceId, diceControls);
            await source.ToSignal(
                source.GetTree(),
                SceneTree.SignalName.ProcessFrame);
            if (!GodotObject.IsInstanceValid(source)
                || !LibrarySpeedDiceService.CanEquipCard(card, sourceId)
                || targetManager.IsInSelection)
            {
                return LibrarySpeedDiceSelectionResult.Rejected;
            }

            TargetMode targetMode =
                LibrarySpeedDiceInputMode.ResolveTargetMode(
                    usingController);
            targetManager.StartTargeting(
                TargetType.AnyEnemy,
                source,
                targetMode,
                () =>
                    !LibrarySpeedDiceService.CanEquipCard(
                        card,
                        sourceId),
                node =>
                    node is Control control
                    && diceControls.ContainsKey(control));
            targetLine = LibrarySpeedDiceRightClickTargetLine.Begin(
                targetManager,
                source,
                useControllerTargeting);
            _activeTargetLine = targetLine;
            dieSelectionStarted = true;
            selectionResult = LibrarySpeedDiceSelectionResult.Canceled;

            if (useControllerTargeting)
            {
                foreach (Control control in diceControls.Keys)
                {
                    control.FocusMode = Control.FocusModeEnum.All;
                    control.SetFocusBehaviorRecursive(
                        Control.FocusBehaviorRecursiveEnum.Enabled);
                }

                NCombatRoom.Instance?.RestrictControllerNavigation(
                    diceControls.Keys);
                diceControls.Keys.First().TryGrabFocus();
            }

            Node? selectedNode = await targetManager.SelectionFinished();
            dieSelectionStarted = false;
            if (selectedNode is Control selectedControl
                && diceControls.TryGetValue(
                    selectedControl,
                    out int slotIndex))
            {
                bool submitted = await LibrarySpeedDiceService
                    .SubmitEquipCardAsync(
                        card,
                        slotIndex,
                        selectedControl,
                        usingController,
                        sourceId);
                selectionResult = submitted
                    ? LibrarySpeedDiceSelectionResult.Submitted
                    : LibrarySpeedDiceSelectionResult.Rejected;
            }

            return selectionResult;
        }
        catch (Exception exception)
        {
            Log.Error(
                "[LibraryOfRuinaLib] Speed-dice right-click selection failed: "
                + exception);
            return LibrarySpeedDiceSelectionResult.Rejected;
        }
        finally
        {
            targetLine?.Stop();
            if (ReferenceEquals(_activeTargetLine, targetLine))
                _activeTargetLine = null;

            if (dieSelectionStarted && targetManager.IsInSelection)
                targetManager.CancelTargeting();

            if (useControllerTargeting)
                NCombatRoom.Instance?.EnableControllerNavigation();

            ClearActiveSelection(card);
            LibrarySpeedDiceService.EndEquipSelection(card, sourceId);
            NotifyEquipSelectionChanged(state, card, isSelecting: false);
        }
    }

    public static void NotifyDiceFocused(Control control)
    {
        if (IsActiveDiceControl(control))
            NTargetManager.Instance.OnNodeHovered(control);
    }

    public static void NotifyDiceUnfocused(Control control)
    {
        if (IsActiveDiceControl(control))
            NTargetManager.Instance.OnNodeUnhovered(control);
    }

    public static void CancelActiveSelection()
    {
        CardModel? card = _activeCard;
        string? sourceId = _activeSourceId;
        if (card == null)
            return;

        LibrarySpeedDiceCombatState? state = null;
        if (card.Owner != null)
        {
            LibrarySpeedDiceService.TryGetState(
                card.Owner,
                out state);
        }

        try
        {
            if (NTargetManager.Instance.IsInSelection)
                NTargetManager.Instance.CancelTargeting();
        }
        catch (Exception exception)
        {
            Log.Warn(
                "[LibraryOfRuinaLib] Failed to cancel speed-dice targeting: "
                + exception.Message);
        }

        _activeTargetLine?.Stop();
        _activeTargetLine = null;
        NCombatRoom.Instance?.EnableControllerNavigation();
        ClearActiveSelection(card);
        LibrarySpeedDiceService.EndEquipSelection(card, sourceId);
        if (state != null)
            NotifyEquipSelectionChanged(state, card, isSelecting: false);
    }

    private static void NotifyEquipSelectionChanged(
        LibrarySpeedDiceCombatState state,
        CardModel card,
        bool isSelecting)
    {
        try
        {
            state.Registration.Dispatcher.OnEquipSelectionChanged(
                state,
                card,
                isSelecting);
        }
        catch (Exception exception)
        {
            Log.Error(
                "[LibraryOfRuinaLib] Speed-dice selection presentation failed: "
                + exception);
        }
    }

    private static Dictionary<Control, int> FindAvailableDiceControls(
        CardModel card)
    {
        if (card.Owner == null
            || !LibrarySpeedDiceService.TryGetState(
                card.Owner,
                out LibrarySpeedDiceCombatState? state)
            || state == null)
        {
            return [];
        }

        Node? ownerNode =
            NCombatRoom.Instance?.GetCreatureNode(card.Owner.Creature);
        if (ownerNode == null)
            return [];

        var controls = new Dictionary<Control, int>();
        foreach (LibrarySpeedDiceSlot slot in state.Slots)
        {
            if (slot.IsSpent || slot.IsLocked || slot.Card != null)
                continue;

            if (ownerNode.FindChild(
                    $"SpeedDie{slot.Index + 1}",
                    recursive: true,
                    owned: false)
                is Control control)
            {
                controls[control] = slot.Index;
            }
        }

        return controls;
    }

    private static void SetActiveSelection(
        CardModel card,
        string sourceId,
        Dictionary<Control, int> diceControls)
    {
        _activeCard = card;
        _activeSourceId = sourceId;
        _activeDiceControls = diceControls;
    }

    private static void ClearActiveSelection(CardModel card)
    {
        if (!ReferenceEquals(_activeCard, card))
            return;

        _activeCard = null;
        _activeSourceId = null;
        _activeDiceControls = [];
    }

    private static bool IsActiveDiceControl(Control control)
    {
        return _activeCard != null
            && _activeDiceControls.ContainsKey(control);
    }
}
