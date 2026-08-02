using HarmonyLib;
using Library.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;

namespace Library.Patches;

/// <summary>在全部玩家回合（包括连续额外回合）结束后恢复混乱前的抗性。</summary>
[HarmonyPatch(typeof(CombatManager), "SwitchSides")]
internal static class LibraryStunResistanceRecoveryPatch
{
    [HarmonyPostfix]
    private static void Postfix(CombatManager __instance)
    {
        CombatState? combatState = __instance.DebugOnlyGetState();

        // 侧切换后仍是玩家侧，说明即将进入额外回合，不能提前恢复抗性。
        if (combatState?.CurrentSide != CombatSide.Enemy)
            return;

        foreach (LibraryCreature creature in combatState.Creatures.OfType<LibraryCreature>())
        {
            if (creature.Side != CombatSide.Enemy || !creature.IsStunPending)
                continue;

            creature.RestorePreStunResistance();
            creature.RestoreChaoOnNextOwnerTurn = true;
        }
    }
}
