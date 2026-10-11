using HarmonyLib;
using LibraryLib.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Runs;

[HarmonyPatch]
public static class CombatStateHookSubscribersPatch
{
    [HarmonyPatch(typeof(ModHelper), nameof(ModHelper.IterateAllCombatStateSubscribers))]
    [HarmonyPostfix]
    public static void IterateAllCombatStateSubscribers_Postfix(
        CombatState combatState,
        ref IEnumerable<AbstractModel> __result)
    {
        __result = __result.Concat(LibraryHookSubscribers.IterateAllCombatStateSubscribers(combatState));
    }
    [HarmonyPatch(typeof(ModHelper), nameof(ModHelper.IterateAllRunStateSubscribers))]
    [HarmonyPostfix]
    public static void IterateAllRunStateSubscribers_Postfix(
        RunState runState,
        ref IEnumerable<AbstractModel> __result)
    {
        __result = __result.Concat(LibraryHookSubscribers.IterateAllRunStateSubscribers(runState));
    }
}
