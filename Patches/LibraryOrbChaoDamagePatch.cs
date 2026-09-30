#nullable enable
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
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
    internal const decimal ChaoDamageMultiplier = 0.75m;

    internal static readonly AsyncLocal<bool> IsInOrbDamage = new();

    internal static int CalculateChaoDamage(decimal orbDamage)
        => (int)decimal.Floor(orbDamage * ChaoDamageMultiplier);

    public static void Set()
    {
        IsInOrbDamage.Value = true;
    }

    public static void Clear()
    {
        IsInOrbDamage.Value = false;
    }
}

/// <summary>
///     在充能球的被动/激发期间设置 <see cref="OrbDamageContext"/>。
/// </summary>
[HarmonyPatch]
internal static class OrbDamageFlagPatch
{
    private static IEnumerable<MethodBase> TargetMethods() =>
    [
        AccessTools.Method(typeof(LightningOrb), nameof(LightningOrb.Passive)),
        AccessTools.Method(typeof(LightningOrb), nameof(LightningOrb.Evoke)),
        AccessTools.Method(typeof(DarkOrb), nameof(DarkOrb.Evoke)),
        AccessTools.Method(typeof(GlassOrb), nameof(GlassOrb.Passive)),
        AccessTools.Method(typeof(GlassOrb), nameof(GlassOrb.Evoke)),
    ];

    [HarmonyPrefix]
    private static void Prefix()
    {
        OrbDamageContext.Set();
    }

    /// <summary>
    ///     在 async 方法真正完成（SetResult/SetException）前清除充能球标记，
    ///     即使方法内部没有实际造成伤害也不会泄漏标记。
    /// </summary>
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        MethodInfo clearMethod = AccessTools.Method(typeof(OrbDamageContext), nameof(OrbDamageContext.Clear));
        for (int i = 0; i < codes.Count; i++)
        {
            if (IsAsyncCompletion(codes[i]))
            {
                codes.Insert(i, new CodeInstruction(OpCodes.Call, clearMethod));
                i++;
            }
        }
        return codes;
    }

    private static bool IsAsyncCompletion(CodeInstruction instruction)
    {
        if ((instruction.opcode != OpCodes.Call && instruction.opcode != OpCodes.Callvirt)
            || instruction.operand is not MethodInfo { Name: "SetResult" or "SetException", DeclaringType: { } declaringType })
        {
            return false;
        }

        return declaringType == typeof(AsyncTaskMethodBuilder)
            || (declaringType.IsGenericType && declaringType.GetGenericTypeDefinition() == typeof(AsyncTaskMethodBuilder<>));
    }
}

/// <summary>
///     充能球伤害结算后，按“充能球自身应造成伤害的 75%”追加混乱伤害；混乱伤害无视抗性
///     （传 Unpowered + None 类型，LibraryDamageCalculate 会跳过混乱抗性乘区）。
///     充能球经由 CreatureCmd.Damage 的单目标与多目标两个 5 参数重载造成伤害。
/// </summary>
[HarmonyPatch]
internal static class LibraryOrbChaoDamagePatch
{
    private static IEnumerable<MethodBase> TargetMethods() =>
    [
        AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.Damage),
            [typeof(PlayerChoiceContext), typeof(IEnumerable<Creature>), typeof(decimal), typeof(ValueProp), typeof(Creature)]),
        AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.Damage),
            [typeof(PlayerChoiceContext), typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(Creature)]),
    ];

    [HarmonyPostfix]
    private static void Postfix(
        PlayerChoiceContext choiceContext,
        decimal amount,
        Creature dealer,
        ref Task<IEnumerable<DamageResult>> __result)
    {
        if (OrbDamageContext.IsInOrbDamage.Value)
            __result = WrapWithChaoDamage(__result, choiceContext, amount, dealer);
    }

    private static async Task<IEnumerable<DamageResult>> WrapWithChaoDamage(
        Task<IEnumerable<DamageResult>> prior,
        PlayerChoiceContext choiceContext,
        decimal orbDamage,
        Creature dealer)
    {
        IEnumerable<DamageResult> results = await prior;
        int chaoDamage = OrbDamageContext.CalculateChaoDamage(orbDamage);
        if (chaoDamage <= 0)
            return results;

        foreach (DamageResult result in results)
        {
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
