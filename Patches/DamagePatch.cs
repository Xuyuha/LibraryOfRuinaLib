using HarmonyLib;
using LibraryLib.Hooks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Patches;

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Damage))]
public static class DamageTargetPatch
{
#if STS2_0_113_0
    // 0.113.0 的伤害统一汇入以 DamageTargetData 为目标的重载。
    [HarmonyPatch(typeof(CreatureCmd), "Damage", new Type[]
    {
        typeof(PlayerChoiceContext),
        typeof(IEnumerable<DamageTargetData>),
        typeof(decimal),
        typeof(ValueProp),
        typeof(Creature),
        typeof(CardModel),
        typeof(CardPlay),
        typeof(MegaCrit.Sts2.Core.Hooks.ModifyDamageHookType)
    })]
    private static void Prefix(
        PlayerChoiceContext choiceContext,
        ref IEnumerable<DamageTargetData> targetData,
        decimal amount,
        ValueProp props,
        Creature dealer)
    {
        IEnumerable<Creature> targets = LibraryLib.Compat.DamageTargetDataCompat.ToCreatures(targetData, out var snapshot);
        RedirectTargets(choiceContext, ref targets, amount, props, dealer);
        targetData = LibraryLib.Compat.DamageTargetDataCompat.Rebuild(snapshot, targets);
    }

    private static void RedirectTargets(
#elif STS2_0_111_0
[HarmonyPatch(typeof(CreatureCmd), "Damage", new Type[]
    {
        typeof(PlayerChoiceContext),
        typeof(IEnumerable<Creature>),
        typeof(decimal),
        typeof(ValueProp),
        typeof(Creature),
        typeof(CardModel),
        typeof(CardPlay)
    })]
#else
[HarmonyPatch(typeof(CreatureCmd), "Damage", new Type[]
    {
        typeof(PlayerChoiceContext),
        typeof(IEnumerable<Creature>),
        typeof(decimal),
        typeof(ValueProp),
        typeof(Creature),
        typeof(CardModel)
    })]
#endif
#if !STS2_0_113_0
    private static void Prefix(
#endif
        PlayerChoiceContext choiceContext,
        ref IEnumerable<Creature> targets,
        decimal amount,
        ValueProp props,
        Creature dealer)
    {
        if (targets == null) return;

        var targetList = targets.ToList();
        if (targetList.Count == 0) return;

        var combatState = targets.First()?.CombatState ?? targetList[0].CombatState;
        if (combatState == null) return;

        targets = LibraryHooks.ModifyDamageTarget(combatState, targetList, amount, props, dealer);
    }
}
