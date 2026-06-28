#nullable enable
using Godot;
using HarmonyLib;
using Library.Models;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace Library.Patches;

[HarmonyPatch(typeof(NRelicInventoryHolder), nameof(NRelicInventoryHolder._Ready))]
internal static class LibraryRelicRightClickPatch
{
    private const string ConnectedMeta = "library_right_click_relic_connected";

    [HarmonyPostfix]
    private static void Postfix(NRelicInventoryHolder __instance)
    {
        if (__instance.HasMeta(ConnectedMeta))
        {
            return;
        }

        __instance.SetMeta(ConnectedMeta, true);
        __instance.Connect(NClickableControl.SignalName.MouseReleased,
            Callable.From<InputEvent>(inputEvent => OnMouseReleased(__instance, inputEvent)));
        __instance.Connect(Control.SignalName.GuiInput,
            Callable.From<InputEvent>(inputEvent => OnGuiInput(__instance, inputEvent)));
    }

    private static void OnMouseReleased(NRelicInventoryHolder holder, InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Right } rightClick
            && rightClick.IsReleased())
        {
            TryHandle(holder, new LibraryRightClickTrigger(false));
        }
    }

    private static void OnGuiInput(NRelicInventoryHolder holder, InputEvent inputEvent)
    {
        if (inputEvent is InputEventAction { Action: var action } actionEvent
            && action == MegaInput.cancel
            && actionEvent.IsPressed()
            && !actionEvent.IsEcho()
            && holder.HasFocus())
        {
            TryHandle(holder, new LibraryRightClickTrigger(true));
        }
    }

    private static void TryHandle(NRelicInventoryHolder holder, LibraryRightClickTrigger trigger)
    {
        Viewport viewport = holder.GetViewport();
        if (viewport.IsInputHandled() || NTargetManager.Instance.IsInSelection)
        {
            return;
        }

        if (holder.Relic?.Model is not LibraryRelicModel relic)
        {
            return;
        }

        Player? player;
        try
        {
            player = LocalContext.GetMe(relic.Owner.RunState);
        }
        catch (Exception ex)
        {
            Log.Warn("[LibraryOfRuinaLib.RightClick] Failed to resolve local relic owner. relic="
                + relic.Id
                + " error="
                + ex.Message);
            return;
        }

        if (player == null || relic.Owner != player)
        {
            return;
        }

        var context = new LibraryRightClickContext(player, relic, trigger);
        bool shouldHandle;
        try
        {
            shouldHandle = relic.CanHandleRightClickLocal(context);
        }
        catch (Exception ex)
        {
            Log.Warn("[LibraryOfRuinaLib.RightClick] Relic local preflight failed. relic="
                + relic.Id
                + " error="
                + ex.Message);
            return;
        }

        if (!shouldHandle)
        {
            return;
        }

        if (LibraryRelicRightClickAction.TryRequest(player, relic, trigger))
        {
            viewport.SetInputAsHandled();
        }
    }
}
