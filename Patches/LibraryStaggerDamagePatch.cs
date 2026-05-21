#nullable enable
using System.Threading.Tasks;
using HarmonyLib;
using Library.Entities.Creatures;
using Library.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.ValueProps;

namespace Library.Patches;

/// <summary>
///     在原版 CreatureCmd.Damage 核心实现后追加 stagger 扣减，
///     使所有伤害来源（原版卡牌、Library 卡牌、充能球等）都能触发混乱抗性扣减。
/// </summary>
[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Damage),
    new[] { typeof(PlayerChoiceContext), typeof(IEnumerable<Creature>), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel) })]
internal static class LibraryStaggerDamagePatch
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
        __result = WrapWithStagger(__result, choiceContext, props, dealer, cardSource);
    }

    private static async Task<IEnumerable<DamageResult>> WrapWithStagger(
        Task<IEnumerable<DamageResult>> prior,
        PlayerChoiceContext choiceContext,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        IEnumerable<DamageResult> results = await prior;
        await LibraryCreatureCmd.ApplyStaggerReduction(choiceContext, results, props, dealer, cardSource);
        return results;
    }
}

/// <summary>
///     在原版 Hook.ModifyDamage 计算完所有加算/乘算后，对陷入混乱的目标追加 x2 乘算。
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyDamage))]
internal static class LibraryChaosDoubleDamagePatch
{
    [HarmonyPostfix]
    private static void Postfix(
        ref decimal __result,
        Creature? target,
        Creature? dealer,
        ValueProp props,
        CardModel? cardSource)
    {
        if (target is not LibraryCreature lc || !lc.IsStunPending)
            return;
        if (cardSource?.Type != CardType.Attack)
            return;
        if (!props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered))
            return;

        Log.Info($"[LibraryChaosDoubleDamage] 混乱x2: target={lc.Monster?.Id} before={__result} after={__result * 2}");
        __result *= 2;
    }
}
