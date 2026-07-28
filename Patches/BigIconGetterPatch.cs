using Godot;
using HarmonyLib;
using Library.Models;
using MegaCrit.Sts2.Core.Models;

[HarmonyPatch(typeof(PowerModel), "BigIcon", MethodType.Getter)]
public static class BigIconGetterPatch//为了实现动态power展示的Patch
{
    static void Postfix(PowerModel __instance, ref Texture2D __result)
    {
        if (__instance is LibraryPowerModel powerModel && powerModel.IsDynamic)
        {
            __result = powerModel.BigIcon;
        }
    }
}