using Godot;
using HarmonyLib;
using LibraryLib.Models;
using LibraryLib.Utils;
using MegaCrit.Sts2.Core.Models;

namespace LibraryLib.Patches;

[HarmonyPatch(typeof(PowerModel), "BigIcon", MethodType.Getter)]
public static class BigIconGetterPatch//为了实现动态power展示的Patch
{
    static void Postfix(PowerModel __instance, ref Texture2D? __result)
    {
        if (__instance is LibraryPowerModel powerModel
            && powerModel.ShouldOverrideBaseIcon)
        {
            Texture2D? icon = powerModel.BigIcon;
            if (LibraryTextureSafety.IsValid(icon))
            {
                __result = icon;
            }
        }
    }
}
