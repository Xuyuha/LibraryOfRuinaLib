using HarmonyLib;
using LibraryLib.Models;
using MegaCrit.Sts2.Core.Models;

namespace LibraryLib.Patches;

[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.PackedIconPath), MethodType.Getter)]
internal static class PackedPowerIconPathGetterPatch
{
    private static void Postfix(PowerModel __instance, ref string __result)
    {
        if (__instance is LibraryPowerModel powerModel
            && powerModel.ShouldOverrideBaseIcon)
        {
            __result = powerModel.PackedIconPath;
        }
    }
}

[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.ResolvedBigIconPath), MethodType.Getter)]
internal static class ResolvedBigPowerIconPathGetterPatch
{
    private static void Postfix(PowerModel __instance, ref string __result)
    {
        if (__instance is LibraryPowerModel powerModel
            && powerModel.ShouldOverrideBaseIcon)
        {
            __result = powerModel.ResolvedBigIconPath;
        }
    }
}
