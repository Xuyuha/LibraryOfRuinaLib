using Godot;
using HarmonyLib;
using LibraryLib.Models;
using LibraryLib.Utils;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace LibraryLib.Patches;

// Vanilla PowerModel members that LibraryPowerModel re-declares (dynamic icons, description variables)
// are not virtual, so these patches route the vanilla getters to the Library implementation.

[HarmonyPatch(typeof(PowerModel), "Icon", MethodType.Getter)]
public static class IconGetterPatch
{
    static void Postfix(PowerModel __instance, ref Texture2D? __result)
    {
        if (__instance is LibraryPowerModel { ShouldOverrideBaseIcon: true } powerModel
            && powerModel.Icon is { } icon
            && LibraryTextureSafety.IsValid(icon))
        {
            __result = icon;
        }
    }
}

[HarmonyPatch(typeof(PowerModel), "BigIcon", MethodType.Getter)]
public static class BigIconGetterPatch
{
    static void Postfix(PowerModel __instance, ref Texture2D? __result)
    {
        if (__instance is LibraryPowerModel { ShouldOverrideBaseIcon: true } powerModel
            && powerModel.BigIcon is { } icon
            && LibraryTextureSafety.IsValid(icon))
        {
            __result = icon;
        }
    }
}

[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.PackedIconPath), MethodType.Getter)]
internal static class PackedPowerIconPathGetterPatch
{
    private static void Postfix(PowerModel __instance, ref string __result)
    {
        if (__instance is LibraryPowerModel { ShouldOverrideBaseIcon: true } powerModel)
            __result = powerModel.PackedIconPath;
    }
}

[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.ResolvedBigIconPath), MethodType.Getter)]
internal static class ResolvedBigPowerIconPathGetterPatch
{
    private static void Postfix(PowerModel __instance, ref string __result)
    {
        if (__instance is LibraryPowerModel { ShouldOverrideBaseIcon: true } powerModel)
            __result = powerModel.ResolvedBigIconPath;
    }
}

/// <summary>记下显示该能力的 NPower，供动态模式切换时刷新图标与副计数。</summary>
[HarmonyPatch(typeof(NPower), "Model", MethodType.Setter)]
public static class ModelSetterPatch
{
    static void Postfix(NPower __instance)
    {
        if (__instance.Model is LibraryPowerModel { NeedNpower: true } powerModel)
            powerModel.BoundNPower = __instance;
    }
}

[HarmonyPatch(typeof(PowerModel), "AddDumbVariablesToDescription")]
public static class PowerDescriptionPatch
{
    static void Postfix(PowerModel __instance, LocString description, int? amountOverride = null)
    {
        if (__instance is LibraryPowerModel power)
            power.AddVariablesToDescription(description, amountOverride);
    }
}
