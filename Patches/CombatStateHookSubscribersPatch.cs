using HarmonyLib;
using LibraryLib.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Modding;

[HarmonyPatch(typeof(ModHelper), nameof(ModHelper.IterateAllCombatStateSubscribers))]
public static class CombatStateHookSubscribersPatch
{
    [HarmonyPostfix]
    public static void IterateAllCombatStateSubscribers_Postfix(
        CombatState combatState,
        ref IEnumerable<AbstractModel> __result)
    {
        __result = __result.Concat(LibraryHookSubscribers.IterateAllCombatStateSubscribers()
            .Where(model => model is not LibraryPowerModeModel mode
                || mode.SourcePower?.Owner?.CombatState == combatState));
    }
}
