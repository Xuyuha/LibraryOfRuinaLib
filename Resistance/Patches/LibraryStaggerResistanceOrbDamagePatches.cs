#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;

namespace Library.Resistance.Patches;

/// <summary>
///     AsyncLocal 作用域上下文，用于标记当前伤害来源为充能球（Orb），
///     从而在混乱抗性系统中区分 Orb 伤害与普通攻击伤害。
/// </summary>
internal static class LibraryStaggerResistanceOrbDamageContext
{
    private sealed class Scope : IDisposable
    {
        private readonly Scope? previous;

        public Scope(Creature dealer)
        {
            Dealer = dealer;
            previous = Current.Value;
            Current.Value = this;
        }

        public Creature Dealer { get; }

        public void Dispose()
        {
            if (Current.Value == this)
            {
                Current.Value = previous;
            }
        }
    }

    private static readonly AsyncLocal<Scope?> Current = new();

    public static IDisposable Begin(OrbModel orb)
    {
        return new Scope(orb.Owner.Creature);
    }

    public static bool IsOrbDamage(Creature? dealer)
    {
        Scope? scope = Current.Value;
        return scope != null && dealer == scope.Dealer;
    }

    public static async Task Wrap(Task task, IDisposable scope)
    {
        try
        {
            await task;
        }
        finally
        {
            scope.Dispose();
        }
    }

    public static async Task<T> Wrap<T>(Task<T> task, IDisposable scope)
    {
        try
        {
            return await task;
        }
        finally
        {
            scope.Dispose();
        }
    }
}

/// <summary>
///     标记闪电球的伤害为 Orb 伤害，使混乱抗性正确识别并减免。
/// </summary>
[HarmonyPatch(typeof(LightningOrb), "ApplyLightningDamage")]
internal static class LibraryStaggerResistanceLightningOrbDamagePatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void Prefix(LightningOrb __instance, out IDisposable __state)
    {
        __state = LibraryStaggerResistanceOrbDamageContext.Begin(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(ref Task<IEnumerable<Creature>> __result, IDisposable __state)
    {
        __result = LibraryStaggerResistanceOrbDamageContext.Wrap(__result, __state);
    }
}

/// <summary>
///     标记黑暗球的伤害为 Orb 伤害，使混乱抗性正确识别并减免。
/// </summary>
[HarmonyPatch(typeof(DarkOrb), nameof(DarkOrb.Evoke))]
internal static class LibraryStaggerResistanceDarkOrbDamagePatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void Prefix(DarkOrb __instance, out IDisposable __state)
    {
        __state = LibraryStaggerResistanceOrbDamageContext.Begin(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(ref Task<IEnumerable<Creature>> __result, IDisposable __state)
    {
        __result = LibraryStaggerResistanceOrbDamageContext.Wrap(__result, __state);
    }
}

/// <summary>
///     标记玻璃球的被动伤害为 Orb 伤害，使混乱抗性正确识别并减免。
/// </summary>
[HarmonyPatch(typeof(GlassOrb), nameof(GlassOrb.Passive))]
internal static class LibraryStaggerResistanceGlassOrbPassiveDamagePatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void Prefix(GlassOrb __instance, out IDisposable __state)
    {
        __state = LibraryStaggerResistanceOrbDamageContext.Begin(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(ref Task __result, IDisposable __state)
    {
        __result = LibraryStaggerResistanceOrbDamageContext.Wrap(__result, __state);
    }
}

/// <summary>
///     标记玻璃球的 Evoke 伤害为 Orb 伤害，使混乱抗性正确识别并减免。
/// </summary>
[HarmonyPatch(typeof(GlassOrb), nameof(GlassOrb.Evoke))]
internal static class LibraryStaggerResistanceGlassOrbEvokeDamagePatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void Prefix(GlassOrb __instance, out IDisposable __state)
    {
        __state = LibraryStaggerResistanceOrbDamageContext.Begin(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(ref Task<IEnumerable<Creature>> __result, IDisposable __state)
    {
        __result = LibraryStaggerResistanceOrbDamageContext.Wrap(__result, __state);
    }
}
