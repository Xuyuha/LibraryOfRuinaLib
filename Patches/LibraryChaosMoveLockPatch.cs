using HarmonyLib;
using LibraryLib.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace LibraryLib.Patches;

// 混乱恢复前，强制切招也必须保留击晕状态及其恢复后的后续招式。
[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.SetMoveImmediate))]
internal static class LibraryChaosMoveLockPatch
{
    [HarmonyPrefix]
    private static bool Prefix(MonsterModel __instance)
    {
        return __instance.NextMove is not LibraryCreature.LibraryStunMoveState
        {
            IsChaosLocked: true
        };
    }
}

// 同时保护直接操作状态机的入口，保持实际招式与显示意图一致。
[HarmonyPatch(typeof(MonsterMoveStateMachine), "SetCurrentState")]
internal static class LibraryChaosStateLockPatch
{
    [HarmonyPrefix]
    private static bool Prefix(MonsterState ____currentState)
    {
        return ____currentState is not LibraryCreature.LibraryStunMoveState
        {
            IsChaosLocked: true
        };
    }
}
