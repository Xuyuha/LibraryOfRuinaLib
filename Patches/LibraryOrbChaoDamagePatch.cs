#nullable enable
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HarmonyLib;
using LibraryLib.Commands;
using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Patches;

/// <summary>
///     标记当前是否正在执行充能球伤害（闪电/黑暗/玻璃球的被动与激发）。
/// </summary>
internal static class OrbDamageContext
{
    internal static readonly AsyncLocal<bool> IsInOrbDamage = new();

    public static void Set()
    {
        IsInOrbDamage.Value = true;
    }

    public static void Clear()
    {
        IsInOrbDamage.Value = false;
    }
}

internal static class OrbDamageFlagPatchHelper
{
    /// <summary>
    ///     在 async 方法真正完成（SetResult/SetException）前清除充能球标记，
    ///     即使方法内部没有实际造成伤害也不会泄漏标记。
    /// </summary>
    internal static IEnumerable<CodeInstruction> ClearOnAsyncCompletion(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        var clearMethod = AccessTools.Method(typeof(OrbDamageContext), nameof(OrbDamageContext.Clear));
        var genericBuilder = typeof(AsyncTaskMethodBuilder<>);
        var nonGenericBuilder = typeof(AsyncTaskMethodBuilder);

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode != OpCodes.Call && codes[i].opcode != OpCodes.Callvirt)
            {
                continue;
            }

            if (codes[i].operand is not MethodInfo method)
            {
                continue;
            }

            if (method.Name != "SetResult" && method.Name != "SetException")
            {
                continue;
            }

            var declaringType = method.DeclaringType;
            if (declaringType == null)
            {
                continue;
            }

            bool isGenericBuilder =
                declaringType.IsGenericType
                && declaringType.GetGenericTypeDefinition() == genericBuilder;
            if (declaringType != nonGenericBuilder && !isGenericBuilder)
            {
                continue;
            }

            codes.Insert(i, new CodeInstruction(OpCodes.Call, clearMethod));
            i++;
        }

        return codes;
    }
}

[HarmonyPatch(typeof(LightningOrb), nameof(LightningOrb.Passive))]
internal static class LightningOrbPassiveOrbDamageFlagPatch
{
    [HarmonyPrefix]
    private static void Prefix()
    {
        OrbDamageContext.Set();
    }

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        => OrbDamageFlagPatchHelper.ClearOnAsyncCompletion(instructions);
}

[HarmonyPatch(typeof(LightningOrb), nameof(LightningOrb.Evoke))]
internal static class LightningOrbEvokeOrbDamageFlagPatch
{
    [HarmonyPrefix]
    private static void Prefix()
    {
        OrbDamageContext.Set();
    }

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        => OrbDamageFlagPatchHelper.ClearOnAsyncCompletion(instructions);
}

[HarmonyPatch(typeof(DarkOrb), nameof(DarkOrb.Evoke))]
internal static class DarkOrbEvokeOrbDamageFlagPatch
{
    [HarmonyPrefix]
    private static void Prefix()
    {
        OrbDamageContext.Set();
    }

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        => OrbDamageFlagPatchHelper.ClearOnAsyncCompletion(instructions);
}

[HarmonyPatch(typeof(GlassOrb), nameof(GlassOrb.Passive))]
internal static class GlassOrbPassiveOrbDamageFlagPatch
{
    [HarmonyPrefix]
    private static void Prefix()
    {
        OrbDamageContext.Set();
    }

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        => OrbDamageFlagPatchHelper.ClearOnAsyncCompletion(instructions);
}

[HarmonyPatch(typeof(GlassOrb), nameof(GlassOrb.Evoke))]
internal static class GlassOrbEvokeOrbDamageFlagPatch
{
    [HarmonyPrefix]
    private static void Prefix()
    {
        OrbDamageContext.Set();
    }

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        => OrbDamageFlagPatchHelper.ClearOnAsyncCompletion(instructions);
}

/// <summary>
///     充能球伤害结算后，按“实际伤害的 50%”追加混乱伤害；混乱伤害无视抗性
///     （传 Unpowered + None 类型，LibraryDamageCalculate 会跳过混乱抗性乘区）。
/// </summary>
[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Damage),
    new[] { typeof(PlayerChoiceContext), typeof(IEnumerable<Creature>), typeof(decimal), typeof(ValueProp), typeof(Creature) })]
internal static class LibraryOrbChaoDamageEnumerablePatch
{
    [HarmonyPostfix]
    private static void Postfix(
        PlayerChoiceContext choiceContext,
        Creature dealer,
        ref Task<IEnumerable<DamageResult>> __result)
    {
        if (!OrbDamageContext.IsInOrbDamage.Value)
        {
            return;
        }

        __result = WrapWithChaoDamage(__result, choiceContext, dealer);
    }

    private static async Task<IEnumerable<DamageResult>> WrapWithChaoDamage(
        Task<IEnumerable<DamageResult>> prior,
        PlayerChoiceContext choiceContext,
        Creature dealer)
    {
        IEnumerable<DamageResult> results = await prior;

        foreach (DamageResult result in results)
        {
            int chaoDamage = result.UnblockedDamage / 2;
            if (chaoDamage <= 0)
            {
                continue;
            }

            await LibraryCreatureCmd.ChaoDamage(
                choiceContext,
                [result.Receiver],
                chaoDamage,
                ValueProp.Unpowered,
                dealer,
                null,
                null,
                LibraryDamageType.None);
        }

        return results;
    }
}

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Damage),
    new[] { typeof(PlayerChoiceContext), typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(Creature) })]
internal static class LibraryOrbChaoDamageSinglePatch
{
    [HarmonyPostfix]
    private static void Postfix(
        PlayerChoiceContext choiceContext,
        Creature dealer,
        ref Task<IEnumerable<DamageResult>> __result)
    {
        if (!OrbDamageContext.IsInOrbDamage.Value)
        {
            return;
        }

        __result = WrapWithChaoDamage(__result, choiceContext, dealer);
    }

    private static async Task<IEnumerable<DamageResult>> WrapWithChaoDamage(
        Task<IEnumerable<DamageResult>> prior,
        PlayerChoiceContext choiceContext,
        Creature dealer)
    {
        IEnumerable<DamageResult> results = await prior;

        foreach (DamageResult result in results)
        {
            int chaoDamage = result.UnblockedDamage / 2;
            if (chaoDamage <= 0)
            {
                continue;
            }

            await LibraryCreatureCmd.ChaoDamage(
                choiceContext,
                [result.Receiver],
                chaoDamage,
                ValueProp.Unpowered,
                dealer,
                null,
                null,
                LibraryDamageType.None);
        }

        return results;
    }
}
