#nullable enable
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Library.Entities.Creatures;
using Library.Models;
using Library.Resistance;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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
    internal static readonly AsyncLocal<List<int>?> PreDamageBlocks = new();
    
    public static LibraryDamageType CurrentDamageType =>
        IsInAttackExecute.Value && DamageType.Value != LibraryDamageType.None
            ? DamageType.Value
            : LibraryDamageType.None;

    public static void SetFlag(object? attackCommand)
    {
        try
        {
            IsInAttackExecute.Value = true;
            DamageType.Value = SafeResolveVanillaDamageType(attackCommand);
        }
        catch
        {
            IsInAttackExecute.Value = true;
            DamageType.Value = LibraryDamageType.Blunt;
        }
    }

    public static void ClearFlag()
    {
        IsInAttackExecute.Value = false;
        DamageType.Value = LibraryDamageType.None;
    }

    internal static LibraryDamageType ResolveVanillaDamageType(object? attackCommand)
    {
        return SafeResolveVanillaDamageType(attackCommand);
    }

    private static LibraryDamageType SafeResolveVanillaDamageType(object? attackCommand)
    {
        try
        {
            return ResolveVanillaDamageTypeUnsafe(attackCommand);
        }
        catch
        {
            return LibraryDamageType.Blunt;
        }
    }

    private static LibraryDamageType ResolveVanillaDamageTypeUnsafe(object? attackCommand)
    {
        if (attackCommand == null)
        {
            return LibraryDamageType.Blunt;
        }

        // 原版 AttackCommand 区分点来自 beta 源码：
        // WithHitCount 修改 _hitCount；TargetingAllOpponents/TargetingRandomOpponents 设置 IsMultiTargeted。
        if (GetHitCount(attackCommand) > 1)
        {
            return LibraryDamageType.Pierce;
        }

        if (GetObjectProperty(attackCommand, "ModelSource") is CardModel &&
            GetBoolProperty(attackCommand, "IsMultiTargeted") &&
            !GetBoolProperty(attackCommand, "IsRandomlyTargeted"))
        {
            return LibraryDamageType.Slash;
        }

        return LibraryDamageType.Blunt;
    }

    private static int GetHitCount(object attackCommand)
    {
        FieldInfo? field = AccessTools.Field(attackCommand.GetType(), "_hitCount");
        return field?.GetValue(attackCommand) is int hitCount ? hitCount : 1;
    }

    private static bool GetBoolProperty(object source, string propertyName)
    {
        MethodInfo? getter = AccessTools.PropertyGetter(source.GetType(), propertyName);
        return getter?.Invoke(source, null) is bool value && value;
    }

    private static object? GetObjectProperty(object source, string propertyName)
    {
        MethodInfo? getter = AccessTools.PropertyGetter(source.GetType(), propertyName);
        return getter?.Invoke(source, null);
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
                   ?? AccessTools.TypeByName("MegaCrit.Sts2.Core.Commands.Builders.AttackCommand")
                   ?? AccessTools.TypeByName("MegaCrit.Sts2.Core.Commands.AttackCommand");
        return AccessTools.Method(type, "Execute");
    }

    [HarmonyPrefix]
    private static void Prefix(object __instance)
    {
        AttackExecuteContext.SetFlag(__instance);
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
    new[] { typeof(PlayerChoiceContext), typeof(IEnumerable<Creature>), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel), typeof(CardPlay) })]
internal static class LibraryAttackChaoDamagePatch
{
    [HarmonyPrefix]
    private static bool Prefix(
        PlayerChoiceContext choiceContext,
        IEnumerable<Creature> targets,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay,
        ref Task<IEnumerable<DamageResult>> __result)
    {
        if (!AttackExecuteContext.IsInAttackExecute.Value)
            return true;

        if (props.HasFlag(ValueProp.Unpowered))
            return true;

        if (!props.HasFlag(ValueProp.Move))
            return true;

        if (cardSource is LibraryCardModel)
            return true;

        if (dealer == null)
            return true;

        if (!dealer.IsPlayer && !dealer.IsMonster)
            return true;

        var targetList = targets as IReadOnlyList<Creature> ?? new List<Creature>(targets);
        if (!targetList.Any(static target => target is LibraryCreature { IsPlayer: false }))
            return true;
        AttackExecuteContext.PreDamageBlocks.Value = targetList.Select(c => c.Block).ToList();
        __result = LibraryCreatureCmd.Damage(
            choiceContext: choiceContext,
            targets: targetList,
            damageAmount: amount,
            props: props,
            dealer: dealer,
            cardSource: cardSource,
            type: AttackExecuteContext.CurrentDamageType);

        return false;
    }

    [HarmonyPostfix]
    private static void Postfix(
        PlayerChoiceContext choiceContext,
        IEnumerable<Creature> targets,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay,
        ref Task<IEnumerable<DamageResult>> __result)
    {
        if (!AttackExecuteContext.IsInAttackExecute.Value)
            return;

        if (props.HasFlag(ValueProp.Unpowered))
            return;

        // 只对主攻击伤害生效（带有Move标记），排除Power等附加效果的伤害
        if (!props.HasFlag(ValueProp.Move))
            return;

        // Library系统的卡牌已在LibraryAttackCommand中自行处理混乱伤害，不重复触发
        if (cardSource is LibraryCardModel)
            return;

        // 只对原版玩家攻击牌（dealer是玩家且有cardSource）和怪物意图伤害（dealer是怪物）生效
        if (dealer == null)
            return;

        if (!dealer.IsPlayer && !dealer.IsMonster)
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

        List<int>? blocks = AttackExecuteContext.PreDamageBlocks.Value;
        AttackExecuteContext.PreDamageBlocks.Value = null;

        if (blocks == null || blocks.Count == 0)
            return results;

        var targetList = targets as IReadOnlyList<Creature> ?? new List<Creature>(targets);
        for (int i = 0; i < targetList.Count && i < blocks.Count; i++)
        {
            decimal chaoDamage = Math.Max(amount - blocks[i], 0m);
            await LibraryCreatureCmd.ChaoDamage(
                damageAmount: chaoDamage,
                choiceContext: choiceContext,
                targets: [targetList[i]],
                props: props,
                dealer: dealer,
                cardSource: cardSource,
                damageResults: results,
                type: AttackExecuteContext.CurrentDamageType);
        }
        return results;
    }
}
