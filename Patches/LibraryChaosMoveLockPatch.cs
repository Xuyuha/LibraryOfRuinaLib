using HarmonyLib;
using LibraryLib.Entities.Creatures;
using LibraryLib.Models;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace LibraryLib.Patches;

// 混乱恢复前，强制切招也必须保留击晕状态及其恢复后的后续招式。
[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.SetMoveImmediate))]
internal static class LibraryChaosMoveLockPatch
{
    [HarmonyPrefix]
    private static bool Prefix(MonsterModel __instance, MoveState state)
    {
        return CanSetState(__instance.NextMove, state);
    }

    internal static bool CanSetState(MonsterState currentState, MonsterState state)
    {
        // 必须先放行显式转阶段，两个入口共用规则，避免 NextMove 与状态机分叉。
        if (state is LibraryPhaseTransitionMoveState)
        {
            return true;
        }

        if (currentState is LibraryPhaseTransitionMoveState
            && state is LibraryCreature.LibraryStunMoveState)
        {
            return false;
        }

        return currentState is not LibraryCreature.LibraryStunMoveState
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
    private static bool Prefix(MonsterState ____currentState, MonsterState state)
    {
        return LibraryChaosMoveLockPatch.CanSetState(____currentState, state);
    }
}
