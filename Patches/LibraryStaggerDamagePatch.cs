#nullable enable
using System.Threading.Tasks;
using HarmonyLib;
using Library.Entities.Creatures;
using Library.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
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
    // 临时方法：陷入混乱时原版攻击x2，后续删除
    [HarmonyPrefix]
    private static void Prefix(
        IEnumerable<Creature> targets,
        ref decimal amount,
        ValueProp props,
        CardModel? cardSource)
    {
        if (cardSource?.Type != CardType.Attack || !props.IsPoweredAttack())
            return;

        foreach (var target in targets)
        {
            if (target is LibraryCreature lc && lc.IsStunPending)
            {
                amount *= 2;
                return;
            }
        }
    }

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
