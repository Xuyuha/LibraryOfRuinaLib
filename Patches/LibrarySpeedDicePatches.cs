using Godot;
using HarmonyLib;
using Library.SpeedDice;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
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

[HarmonyPatch(typeof(CombatManager), "FlushPlayerHand")]
internal static class LibrarySpeedDiceTurnEndCleanupPatch
{
    private static void Prefix(
        Player player,
        out HashSet<CardModel>? __state)
    {
        __state = null;
        if (!LibrarySpeedDiceService.TryGetState(
                player,
                out LibrarySpeedDiceCombatState? state)
            || state == null
            || player.Creature.CombatState == null)
        {
            return;
        }

        bool shouldFlush = Hook.ShouldFlush(
            player.Creature.CombatState,
            player);
        __state = state.Slots
            .Where(slot =>
                slot.Card != null
                && (!shouldFlush || slot.Card.ShouldRetainThisTurn))
            .Select(slot => slot.Card!)
            .ToHashSet();
    }

    private static void Postfix(
        Player player,
        HashSet<CardModel>? __state,
        ref Task __result)
    {
        if (__state != null)
        {
            __result = FinishPlayerTurnAsync(
                __result,
                player,
                __state);
        }
    }

    private static async Task FinishPlayerTurnAsync(
        Task original,
        Player player,
        IReadOnlySet<CardModel> retainedCards)
    {
        await original;
        await LibrarySpeedDiceService.FinishPlayerTurnAsync(
            player,
            retainedCards);
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

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterDeath))]
internal static class LibraryEmotionAllyDeathPatch
{
    private static void Prefix(
        ICombatState? combatState,
        Creature creature,
        bool wasRemovalPrevented)
    {
        LibrarySpeedDiceService.RecordAllyDeath(
            combatState,
            creature,
            wasRemovalPrevented);
    }
}

[HarmonyPatch(typeof(NInputManager), nameof(NInputManager._UnhandledKeyInput))]
internal static class LibrarySpeedDiceHotkeyPatch
{
    private static bool Prefix(NInputManager __instance, InputEvent inputEvent)
    {
        if (!NGame.IsGameFocusedWindow()
            || NDevConsole.Instance?.Visible == true
            || NCombatRoom.Instance == null
            || inputEvent is not InputEventKey keyEvent
            || !keyEvent.Pressed
            || inputEvent.IsEcho()
            || keyEvent.Keycode != Key.Space
            && keyEvent.PhysicalKeycode != Key.Space)
        {
            return true;
        }

        if (!LibrarySpeedDiceService.CanConsumeAdvanceInput())
            return true;

        TaskHelper.RunSafely(LibrarySpeedDiceService.AdvanceLocalAsync());
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
        LibrarySpeedDiceUiMount.TryMount(__instance);
        Callable.From(() =>
            LibrarySpeedDiceUiMount.TryMount(__instance)).CallDeferred();
    }
}

[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom._Ready))]
internal static class LibrarySpeedDiceCombatRoomUiPatch
{
    private static void Postfix(NCombatRoom __instance)
    {
        Callable.From(() =>
        {
            foreach (NCreature creatureNode in __instance.CreatureNodes)
                LibrarySpeedDiceUiMount.TryMount(creatureNode);
        }).CallDeferred();
    }
}

[HarmonyPatch(typeof(NHealthBar), "RefreshForeground")]
internal static class LibrarySpeedDiceHealthBarUiPatch
{
    private static readonly System.Reflection.FieldInfo? CreatureField =
        AccessTools.Field(typeof(NHealthBar), "_creature");

    private static void Postfix(NHealthBar __instance)
    {
        if (CreatureField?.GetValue(__instance) is Creature creature)
        {
            LibrarySpeedDiceUiMount.TryMount(
                creature.GetCreatureNode(),
                __instance);
        }
    }
}

internal static class LibrarySpeedDiceUiMount
{
    private const string UiNodeName = "LibrarySpeedDiceUi";

    public static void TryMount(
        NCreature? creatureNode,
        NHealthBar? healthBar = null)
    {
        if (creatureNode?.Entity.Player == null
            || !GodotObject.IsInstanceValid(creatureNode)
            || !LibrarySpeedDiceService.TryGetState(
                creatureNode.Entity.Player,
                out LibrarySpeedDiceCombatState? state)
            || state == null
            || (healthBar ??= creatureNode.GetNodeOrNull<NHealthBar>(
                "HealthBar/HealthBar")) == null)
        {
            return;
        }

        if (healthBar.GetNodeOrNull<LibrarySpeedDiceUi>(UiNodeName) != null)
            return;

        var ui = new LibrarySpeedDiceUi
        {
            Name = UiNodeName,
        };
        healthBar.AddChild(ui);
        Control? blockContainer =
            healthBar.GetNodeOrNull<Control>("BlockContainer");
        if (blockContainer != null)
        {
            healthBar.MoveChild(
                ui,
                Math.Min(
                    blockContainer.GetIndex() + 1,
                    healthBar.GetChildCount() - 1));
        }
        ui.Initialize(creatureNode, state);
        creatureNode.TreeExiting += ui.QueueFree;
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
