#nullable enable
using Godot;
using HarmonyLib;
using Library.Resistance.Powers;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace Library.Patches;

/// <summary>PCK 未加载时仍尝试从 mod 资源路径解析抗性图标。</summary>
[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.Icon), MethodType.Getter)]
internal static class LibraryResistancePowerIconPatch
{
    private static readonly string SlashPath = ImageHelper.GetImagePath("powers/library_resistance_slash.png");

    private static readonly string BluntPath = ImageHelper.GetImagePath("powers/library_resistance_blunt.png");

    private static readonly string PiercePath = ImageHelper.GetImagePath("powers/library_resistance_pierce.png");

    [HarmonyPostfix]
    private static void Postfix(PowerModel __instance, ref Texture2D __result)
    {
        string? path = __instance switch
        {
            LibrarySlashResistancePower => SlashPath,
            LibraryBluntResistancePower => BluntPath,
            LibraryPierceResistancePower => PiercePath,
            _ => null
        };

        if (path == null || !ResourceLoader.Exists(path))
        {
            return;
        }

        __result = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
    }
}
