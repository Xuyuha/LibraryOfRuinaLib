using Godot;
using HarmonyLib;
using LibraryLib.Models;
using LibraryLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
public static class CombatStateHookSubscribersPatch
{
    [HarmonyPatch(typeof(CombatState), "IterateAllCombatStateSubscribers")]
    [HarmonyPostfix]
    public static void IterateAllCombatStateSubscribers_Postfix(
        CombatState combatState,
        ref IEnumerable<AbstractModel> __result)
    {
        __result = __result.Concat(LibraryHookSubscribers.IterateAllCombatStateSubscribers());
    }
}