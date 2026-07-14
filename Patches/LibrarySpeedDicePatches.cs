using Godot;
using HarmonyLib;
using Library.SpeedDice;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Debug;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Library.Patches;

[HarmonyPatch(typeof(Creature), nameof(Creature.BeforeTurnStart))]
internal static class LibrarySpeedDiceTurnStartPatch
{
    private static void Postfix(Creature __instance, CombatSide side)
    {
        LibrarySpeedDiceService.BeginPlayerTurn(__instance, side);
    }
}

[HarmonyPatch(typeof(PlayerCombatState), "get_MaxEnergy")]
internal static class LibraryEmotionMaxEnergyPatch
{
    private static void Postfix(PlayerCombatState __instance, ref int __result)
    {
        CardModel? anyCard = __instance.AllCards.FirstOrDefault();
        Player? player = anyCard?.Owner;
        if (player == null)
        {
            var playerField = AccessTools.Field(typeof(PlayerCombatState), "_player");
            player = playerField?.GetValue(__instance) as Player;
        }

        if (player != null)
            __result += LibrarySpeedDiceService.GetMaxEnergyBonus(player);
    }
}

[HarmonyPatch(
    typeof(PlayerCombatState),
    nameof(PlayerCombatState.HasEnoughResourcesFor))]
internal static class LibraryReservedResourcePatch
{
    private static void Postfix(
        CardModel card,
        ref UnplayableReason reason,
        ref bool __result)
    {
        LibrarySpeedDiceService.ApplyReservedResourceRestriction(
            card,
            ref reason,
            ref __result);
    }
}

[HarmonyPatch(
    typeof(CardPileCmd),
    nameof(CardPileCmd.Draw),
    [
        typeof(PlayerChoiceContext),
        typeof(decimal),
        typeof(Player),
        typeof(bool),
    ])]
internal static class LibraryEmotionHandDrawPatch
{
    private static void Prefix(
        Player player,
        bool fromHandDraw,
        ref decimal count)
    {
        LibrarySpeedDiceService.AddInitialHandDrawBonus(
            player,
            fromHandDraw,
            ref count);
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterDamageGiven))]
internal static class LibraryEmotionDamageGivenPatch
{
    private static void Prefix(
        Creature? dealer,
        DamageResult results,
        Creature target)
    {
        LibrarySpeedDiceService.RecordDamageGiven(dealer, results, target);
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterDamageReceived))]
internal static class LibraryEmotionDamageReceivedPatch
{
    private static void Prefix(Creature target, DamageResult result)
    {
        LibrarySpeedDiceService.RecordDamageReceived(target, result);
    }
}

[HarmonyPatch(typeof(NHotkeyManager), nameof(NHotkeyManager._UnhandledInput))]
internal static class LibrarySpeedDiceHotkeyPatch
{
    private static bool Prefix(NHotkeyManager __instance, InputEvent inputEvent)
    {
        if (!NGame.IsGameFocusedWindow()
            || NDevConsole.Instance?.Visible == true
            || NCombatRoom.Instance == null
            || NControllerManager.Instance.IsUsingController
            || inputEvent.IsEcho()
            || !inputEvent.IsActionPressed(MegaInput.accept))
        {
            return true;
        }

        if (LibrarySpeedDiceService.HasMissingRequiredTargetsLocal())
        {
            __instance.GetViewport()?.SetInputAsHandled();
            return false;
        }

        if (!LibrarySpeedDiceService.CanConsumeLockInput())
            return true;

        TaskHelper.RunSafely(LibrarySpeedDiceService.LockAndResolveLocalAsync());
        __instance.GetViewport()?.SetInputAsHandled();
        return false;
    }
}

[HarmonyPatch(typeof(NPlayerHand), "AreCardActionsAllowed")]
internal static class LibrarySpeedDiceHandInputPatch
{
    private static void Postfix(ref bool __result)
    {
        if (LibrarySpeedDiceService.TryGetLocalState(
                out LibrarySpeedDiceCombatState? state)
            && state?.IsResolving == true)
        {
            __result = false;
        }
    }
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
internal static class LibrarySpeedDiceCreatureUiPatch
{
    private static void Postfix(NCreature __instance)
    {
        if (__instance.Entity.Player == null
            || !LibrarySpeedDiceService.TryGetState(
                __instance.Entity.Player,
                out LibrarySpeedDiceCombatState? state)
            || state == null
            || NCombatRoom.Instance?.Ui == null)
        {
            return;
        }

        string nodeName =
            $"LibrarySpeedDiceUi_{__instance.Entity.CombatId?.ToString() ?? "Player"}";
        if (NCombatRoom.Instance.Ui.GetNodeOrNull(nodeName) != null)
            return;

        var ui = new LibrarySpeedDiceUi
        {
            Name = nodeName,
        };
        NCombatRoom.Instance.Ui.AddChild(ui);
        ui.Initialize(__instance, state);
        __instance.TreeExiting += ui.QueueFree;
    }
}

[HarmonyPatch(typeof(NHealthBar), "RefreshForeground")]
internal static class LibraryEmotionBarRefreshPatch
{
    private static void Postfix(NHealthBar __instance)
    {
        try
        {
            LibraryEmotionBarUi.Refresh(__instance);
        }
        catch
        {
        }
    }
}
