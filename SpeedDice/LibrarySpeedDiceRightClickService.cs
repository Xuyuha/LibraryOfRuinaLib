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
        NPlayerHand? hand = NPlayerHand.Instance;
        if (card is not ILibrarySpeedDiceCard
            {
                EnableSpeedDiceRightClickSelection: true,
            }
            || card.IsCanonical
            || card.Pile?.Type != PileType.Hand
            || hand == null
            || hand.IsInCardSelection
            || !LibrarySpeedDiceService.CanEquipCard(card))
        {
            return false;
        }

        return FindAvailableDiceControls(card).Count > 0;
    }

    public static async Task BeginSelectionAsync(
        CardModel card,
        bool usingController)
    {
        if (!CanBeginSelection(card)
            || !LibrarySpeedDiceService.TryBeginEquipSelection(card)
            || card.Owner == null
            || !LibrarySpeedDiceService.TryGetState(
                card.Owner,
                out LibrarySpeedDiceCombatState? state)
            || state == null)
        {
            return;
        }

        NTargetManager targetManager = NTargetManager.Instance;
        bool dieSelectionStarted = false;
        bool useControllerTargeting =
            LibrarySpeedDiceInputMode.ShouldUseControllerTargeting(
                usingController);
        LibrarySpeedDiceRightClickTargetLine? targetLine = null;
        NotifyEquipSelectionChanged(state, card, isSelecting: true);
        try
        {
            NPlayerHand? hand = NPlayerHand.Instance;
            NHandCardHolder? holder =
                hand?.GetCardHolder(card) as NHandCardHolder;
            Control? source =
                (Control?)holder?.CardNode
                ?? holder;
            Dictionary<Control, int> diceControls =
                FindAvailableDiceControls(card);
            if (source == null || diceControls.Count == 0)
                return;

            NDebugAudioManager.Instance?.Play("card_select.mp3", 0.5f);
            if (holder != null)
                NHoverTipSet.Remove(holder);
            holder?.CardNode?.CardHighlight.AnimFlash();

            SetActiveSelection(card, diceControls);
            await source.ToSignal(
                source.GetTree(),
                SceneTree.SignalName.ProcessFrame);
            if (!GodotObject.IsInstanceValid(source)
                || card.Pile?.Type != PileType.Hand
                || targetManager.IsInSelection)
            {
                return;
            }

            TargetMode targetMode =
                LibrarySpeedDiceInputMode.ResolveTargetMode(
                    usingController);
            targetManager.StartTargeting(
                TargetType.AnyEnemy,
                source,
                targetMode,
                () =>
                    card.Pile?.Type != PileType.Hand
                    || !LibrarySpeedDiceService.CanEquipCard(card),
                node =>
                    node is Control control
                    && diceControls.ContainsKey(control));
            targetLine = LibrarySpeedDiceRightClickTargetLine.Begin(
                targetManager,
                source,
                useControllerTargeting);
            _activeTargetLine = targetLine;
            dieSelectionStarted = true;

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
                    out int slotIndex)
                && card.Pile?.Type == PileType.Hand)
            {
                await LibrarySpeedDiceService.EquipCardAsync(
                    card,
                    slotIndex,
                    selectedControl,
                    usingController);
            }
        }
        catch (Exception exception)
        {
            Log.Error(
                "[LibraryOfRuinaLib] Speed-dice right-click selection failed: "
                + exception);
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
            LibrarySpeedDiceService.EndEquipSelection(card);
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
        LibrarySpeedDiceService.EndEquipSelection(card);
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
        Dictionary<Control, int> diceControls)
    {
        _activeCard = card;
        _activeDiceControls = diceControls;
    }

    private static void ClearActiveSelection(CardModel card)
    {
        if (!ReferenceEquals(_activeCard, card))
            return;

        _activeCard = null;
        _activeDiceControls = [];
    }

    private static bool IsActiveDiceControl(Control control)
    {
        return _activeCard != null
            && _activeDiceControls.ContainsKey(control);
    }
}
