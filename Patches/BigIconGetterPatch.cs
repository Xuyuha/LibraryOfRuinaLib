using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Models;
using Library.Models;

[HarmonyPatch(typeof(PowerModel), "BigIcon", MethodType.Getter)]
public static class BigIconGetterPatch
{
    static void Postfix(PowerModel __instance, ref Texture2D __result)
    {
        if (__instance is LibraryPowerModel powerModel)
        {
            __result = powerModel.BigIcon;
        }
    }
}