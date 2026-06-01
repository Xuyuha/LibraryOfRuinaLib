#nullable enable
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Library.Entities.Creatures;
using Library.Resistance;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace Library.Patches;

internal static class AttackExecuteContext
{
    internal static readonly AsyncLocal<bool> IsInAttackExecute = new();
    internal static readonly AsyncLocal<LibraryDamageType> DamageType = new();

    public static LibraryDamageType CurrentDamageType =>
        IsInAttackExecute.Value && DamageType.Value != LibraryDamageType.None
            ? DamageType.Value
            : LibraryDamageType.None;

    public static void SetFlag()
    {
        IsInAttackExecute.Value = true;
        DamageType.Value = LibraryDamageType.Blunt;
    }

    public static void ClearFlag()
    {
        IsInAttackExecute.Value = false;
        DamageType.Value = LibraryDamageType.None;
    }
}

/// <summary>
///     在 AttackCommand.Execute 整个 async 执行流开始时设标记，
///     在 async 状态机真正完成时（SetResult/SetException 前）清除标记。
/// </summary>
[HarmonyPatch]
internal static class LibraryAttackExecuteFlagPatch
{
    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        var type = AccessTools.TypeByName("AttackCommand")
                   ?? AccessTools.TypeByName("MegaCrit.Sts2.Core.Commands.AttackCommand");
        return AccessTools.Method(type, "Execute");
    }

    [HarmonyPrefix]
    private static void Prefix()
    {
        AttackExecuteContext.SetFlag();
    }

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        var clearMethod = AccessTools.Method(typeof(AttackExecuteContext), nameof(AttackExecuteContext.ClearFlag));

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode != OpCodes.Call && codes[i].opcode != OpCodes.Callvirt)
                continue;

            if (codes[i].operand is not MethodInfo method)
                continue;

            if (method.Name != "SetResult" && method.Name != "SetException")
                continue;

            var declaringType = method.DeclaringType;
            if (declaringType == null)
                continue;

            if (!declaringType.IsGenericType)
                continue;

            var genericDef = declaringType.GetGenericTypeDefinition();
            if (genericDef != typeof(AsyncTaskMethodBuilder<>))
                continue;

            codes.Insert(i, new CodeInstruction(OpCodes.Call, clearMethod));
            break;
        }

        return codes;
    }
}

/// <summary>
///     只当在 AttackCommand.Execute 执行流中时，在 CreatureCmd.Damage 后追加混乱伤害。
/// </summary>
[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Damage),
    new[] { typeof(PlayerChoiceContext), typeof(IEnumerable<Creature>), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel) })]
internal static class LibraryAttackChaoDamagePatch
{
    [HarmonyPostfix]
    private static void Postfix(
        PlayerChoiceContext choiceContext,
        IEnumerable<Creature> targets,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        ref Task<IEnumerable<DamageResult>> __result)
    {
        if (!AttackExecuteContext.IsInAttackExecute.Value)
            return;

        __result = WrapWithChaoDamage(__result, choiceContext, targets, amount, props, dealer, cardSource);
    }

    private static async Task<IEnumerable<DamageResult>> WrapWithChaoDamage(
        Task<IEnumerable<DamageResult>> prior,
        PlayerChoiceContext choiceContext,
        IEnumerable<Creature> targets,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        IEnumerable<DamageResult> results = await prior;
        await LibraryCreatureCmd.ChaoDamage(
            damageAmount: amount,
            choiceContext: choiceContext,
            targets: targets,
            props: props,
            dealer: dealer,
            cardSource: cardSource,
            damageResults: results,
            type: AttackExecuteContext.CurrentDamageType);
        return results;
    }
}
