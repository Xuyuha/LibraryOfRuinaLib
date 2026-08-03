#nullable enable
using Godot;
using HarmonyLib;
using LibraryLib.Powers;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace LibraryLib.Patches;

[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.Icon), MethodType.Getter)]
internal static class LibraryResistancePowerIconPatch
{
    private static readonly string StaggerPath = ImageHelper.GetImagePath("powers/library_stagger_resistance.png");

    [HarmonyPostfix]
    private static void Postfix(PowerModel __instance, ref Texture2D __result)
    {
        if (__instance is not LibraryStaggerResistancePower)
        {
            return;
        }

        if (!ResourceLoader.Exists(StaggerPath))
        {
            return;
        }

        __result = ResourceLoader.Load<Texture2D>(StaggerPath, null, ResourceLoader.CacheMode.Reuse);
    }
}
