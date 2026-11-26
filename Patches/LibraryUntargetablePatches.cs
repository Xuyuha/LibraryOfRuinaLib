#nullable enable
using HarmonyLib;
using LibraryLib.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace LibraryLib.Patches;

[HarmonyPatch(typeof(AttackCommand), "GetPossibleTargets")]
internal static class LibraryUntargetableAttackTargetsPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    internal static void Postfix(ref IReadOnlyList<Creature> __result) =>
        __result = LibraryUntargetable.FilterHittable(__result);
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.IsValidTarget), typeof(Creature))]
internal static class LibraryUntargetableCardTargetPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    internal static void Postfix(Creature? target, ref bool __result)
    {
        if (__result && LibraryUntargetable.BlocksSelection(target))
            __result = false;
    }
}

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Stun),
    typeof(Creature), typeof(Func<IReadOnlyList<Creature>, Task>), typeof(string))]
internal static class LibraryUntargetableStunPatch
{
    [HarmonyPrefix]
    internal static bool Prefix(Creature creature, ref Task __result)
    {
        if (!LibraryUntargetable.BlocksHit(creature))
            return true;

        __result = Task.CompletedTask;
        return false;
    }
}
