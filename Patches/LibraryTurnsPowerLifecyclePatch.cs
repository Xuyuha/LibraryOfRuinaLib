#nullable enable
using HarmonyLib;
using LibraryLib.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Hooks;

namespace LibraryLib.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterSideTurnEnd))]
internal static class LibraryTurnsPowerLifecyclePatch
{
    [HarmonyPostfix]
    private static void Postfix(
        ICombatState combatState,
        CombatSide side,
        ref Task __result)
    {
        __result = ExpireFilteredPowersAsync(__result, combatState, side);
    }

    private static async Task ExpireFilteredPowersAsync(
        Task original,
        ICombatState combatState,
        CombatSide side)
    {
        await original;

        var powers = combatState.Creatures
            .SelectMany(static creature => creature.Powers)
            .OfType<LibraryTurnsPowerModel>()
            .ToArray();
        foreach (var power in powers)
        {
            await power.ExpireDuePlans(side, combatState.RoundNumber);
        }
    }
}
