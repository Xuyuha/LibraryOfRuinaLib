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
    private static void Prefix(
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