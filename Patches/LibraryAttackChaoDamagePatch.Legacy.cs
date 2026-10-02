#if STS2_0_107_1
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
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Patches;
internal static partial class LibraryAttackChaoDamagePatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    private static bool Prefix(
        PlayerChoiceContext choiceContext,
        ref IEnumerable<Creature> targets,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
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
            dealer,
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
            cardPlay: null,
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
            null,
            __state);
    }
}
#endif
