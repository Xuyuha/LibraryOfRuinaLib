#nullable enable
using System.Reflection;
using HarmonyLib;
using LibraryLib.Combat;
using LibraryLib.Commands;
using LibraryLib.Entities.Creatures;
using LibraryLib.Hooks;
using LibraryLib.Models;
using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Patches;

internal static class AttackExecuteContext
{
    internal static readonly AsyncLocal<bool> IsInAttackExecute = new();
    internal static readonly AsyncLocal<LibraryDamageType> DamageType = new();

    internal readonly record struct Scope(
        bool WasInAttackExecute,
        LibraryDamageType PreviousDamageType);
    
    public static LibraryDamageType CurrentDamageType =>
        IsInAttackExecute.Value && DamageType.Value != LibraryDamageType.None
            ? DamageType.Value
            : LibraryDamageType.None;

    internal static Scope Enter(object? attackCommand)
    {
        var scope = new Scope(IsInAttackExecute.Value, DamageType.Value);
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

        return scope;
    }

    internal static void Restore(Scope scope)
    {
        IsInAttackExecute.Value = scope.WasInAttackExecute;
        DamageType.Value = scope.PreviousDamageType;
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
///     在 AttackCommand.Execute 建立异步状态机时设置上下文，原方法返回 Task 后
///     立即恢复调用方上下文；AttackCommand 自己捕获的 ExecutionContext 保留攻击类型。
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
    private static void Prefix(object __instance, out AttackExecuteContext.Scope __state)
    {
        __state = AttackExecuteContext.Enter(__instance);
    }

    [HarmonyFinalizer]
    private static Exception? Finalizer(
        Exception? __exception,
        AttackExecuteContext.Scope __state)
    {
        AttackExecuteContext.Restore(__state);
        return __exception;
    }
}

/// <summary>
///     Only append chaos damage after CreatureCmd.Damage when running inside
///     the AttackCommand.Execute flow.
/// </summary>
/// <remarks>
///     Multiplayer patch-order contract: this Prefix both rewrites
///     <c>ref targets</c> and can short-circuit __result, so it must run after
///     every other target-filtering Prefix on this overload
///     (LibraryOfRuina's friendly-ally filter uses Priority.First).
///     Priority.Last makes the relative order deterministic on every peer.
///     The targets parameter is declared <c>ref</c> so that the short-circuit
///     path receives the filtered target list instead of the original one —
///     otherwise friendly allies removed by earlier prefixes would still take
///     damage on some peers, which was observed as multiplayer state divergence.
/// </remarks>
[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Damage),
    new[] { typeof(PlayerChoiceContext), typeof(IEnumerable<Creature>), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel), typeof(CardPlay) })]
internal static class LibraryAttackChaoDamagePatch
{
    private sealed record DamagePatchState(
        IReadOnlyList<Creature> Targets,
        IReadOnlyList<int> PreDamageBlocks,
        LibraryDamageType DamageType,
        bool HasLibraryTarget);

    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    private static bool Prefix(
        PlayerChoiceContext choiceContext,
        ref IEnumerable<Creature> targets,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay,
        out DamagePatchState? __state,
        ref Task<IEnumerable<DamageResult>> __result)
    {
        __state = null;
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
        if (targetList.Count == 0)
            return true;

        targets = targetList;

        ICombatState? combatState = targetList
            .Select(static target => target.CombatState)
            .FirstOrDefault(static state => state != null);
        if (combatState == null)
            return true;

        IRunState runState =
            IRunState.GetFrom(targetList.Append(dealer).OfType<Creature>());
        bool needsLibraryDamage =
            targetList.Any(static target =>
                target is LibraryCreature { IsPlayer: false });
        bool hasInterceptor =
            !LibraryIncomingDamageInterception.IsSuppressed
            && LibraryHooks.HasIncomingDamageInterceptor(
                runState,
                combatState);

        LibraryDamageType damageType = ResolveExecutionDamageType(
            cardSource,
            targetList);
        IReadOnlyList<int> preDamageBlocks = targetList
            .Select(static target => target.Block)
            .ToArray();
        __state = new DamagePatchState(
            targetList,
            preDamageBlocks,
            damageType,
            needsLibraryDamage);

        if (!needsLibraryDamage && !hasInterceptor)
            return true;

        __result = LibraryCreatureCmd.Damage(
            choiceContext: choiceContext,
            targets: targetList,
            damageAmount: amount,
            props: props,
            dealer: dealer,
            cardSource: cardSource,
            cardPlay: cardPlay,
            type: damageType);

        return false;
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(
        PlayerChoiceContext choiceContext,
        ref IEnumerable<Creature> targets,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay,
        DamagePatchState? __state,
        ref Task<IEnumerable<DamageResult>> __result)
    {
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

        if (__state is not { HasLibraryTarget: true })
            return;

        __result = WrapWithChaoDamage(
            __result,
            choiceContext,
            amount,
            props,
            dealer,
            cardSource,
            cardPlay,
            __state);
    }

    private static async Task<IEnumerable<DamageResult>> WrapWithChaoDamage(
        Task<IEnumerable<DamageResult>> prior,
        PlayerChoiceContext choiceContext,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay,
        DamagePatchState state)
    {
        IEnumerable<DamageResult> results = await prior;

        if (state.PreDamageBlocks.Count == 0)
            return results;

        for (int i = 0;
             i < state.Targets.Count && i < state.PreDamageBlocks.Count;
             i++)
        {
            decimal chaoDamage = Math.Max(
                amount - state.PreDamageBlocks[i],
                0m);
            await LibraryCreatureCmd.ChaoDamage(
                damageAmount: chaoDamage,
                choiceContext: choiceContext,
                targets: [state.Targets[i]],
                props: props,
                dealer: dealer,
                cardSource: cardSource,
                cardPlay: cardPlay,
                damageResults: results,
                type: state.DamageType);
        }
        return results;
    }

    private static LibraryDamageType ResolveExecutionDamageType(
        CardModel? cardSource,
        IReadOnlyList<Creature> targets)
    {
        if (cardSource != null)
        {
            Creature? target = targets.Count == 1 ? targets[0] : null;
            return LibraryDamagePreviewFeedback.ResolveVanillaPreviewDamageType(
                cardSource,
                target);
        }

        return targets.Count > 1
            ? LibraryDamageType.Slash
            : LibraryDamageType.Blunt;
    }
}
