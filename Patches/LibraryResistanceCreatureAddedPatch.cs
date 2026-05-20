#nullable enable
using System.Threading.Tasks;
using HarmonyLib;
using Library.Models;
using Library.Resistance;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Library.Patches;

[HarmonyPatch(typeof(CombatManager), nameof(CombatManager.AfterCreatureAdded))]
internal static class LibraryResistanceCreatureAddedPatch
{
    [HarmonyPostfix]
    private static void Postfix(CombatManager __instance, Creature creature, ref Task __result)
    {
        _ = __instance;
        __result = ContinueAfterCreatureAdded(__result, creature);
    }

    private static async Task ContinueAfterCreatureAdded(Task prior, Creature creature)
    {
        await prior;
        if (!creature.IsEnemy || creature.Monster is not LibraryMonsterModel)
        {
            return;
        }

        if (creature.CombatState == null)
        {
            return;
        }

        await LibraryResistance.EnsureOnEnemy(creature);
    }
}
