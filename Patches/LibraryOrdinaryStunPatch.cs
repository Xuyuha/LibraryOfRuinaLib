using HarmonyLib;
using LibraryLib.Entities.Creatures;
using LibraryLib.Models;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace LibraryLib.Patches;

// 原版卡牌的击晕仍走 Creature 入口，动态路由怪物的招式历史可以为空。
[HarmonyPatch(typeof(Creature), nameof(Creature.StunInternal))]
internal static class LibraryOrdinaryStunPatch
{
    [HarmonyPrefix]
    private static bool Prefix(Creature __instance, ref string? nextMoveId)
    {
        if (__instance is not LibraryCreature creature || creature.Monster == null)
        {
            return true;
        }

        // 保留混乱的恢复时机和已经安排的转阶段行动，普通击晕不覆盖它们。
        if (creature.IsChaoed || creature.Monster.NextMove is LibraryPhaseTransitionMoveState)
        {
            return false;
        }

        nextMoveId = LibraryCreature.ResolvePostStunMoveId(creature.Monster, nextMoveId);
        return nextMoveId != null;
    }
}
