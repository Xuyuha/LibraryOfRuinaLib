using Godot;
using HarmonyLib;
using LibraryLib.Models;
using MegaCrit.Sts2.Core.Models;

namespace LibraryLib.Patches;

[HarmonyPatch(typeof(PowerModel), "Icon", MethodType.Getter)]
public static class IconGetterPatch//为了实现动态power展示的Patch
{
    static void Postfix(PowerModel __instance, ref Texture2D __result)
    {
        if (__instance is LibraryPowerModel powerModel && powerModel.IsDynamic)
        {
            __result = powerModel.Icon;
        }
    }
}