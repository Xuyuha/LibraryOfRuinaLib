using HarmonyLib;
using LibraryLib.Hooks;
using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Patches;

/// <summary>
///     Chooses the targets of vanilla damage through <see cref="LibraryHooks.ModifyDamageTarget(MegaCrit.Sts2.Core.Combat.ICombatState, List{Creature}, decimal, ValueProp, Creature?, LibraryDamageType)"/>.
///     Runs at normal priority so that later target filters (LibraryOfRuina's Priority.Last ally filter) see the result.
///     This is the only target pass of a vanilla hit: when <see cref="LibraryAttackChaoDamagePatch"/> reroutes the hit
///     into <c>LibraryCreatureCmd.Damage</c>, the chosen targets are handed over and not modified again.
/// </summary>
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
        ref IEnumerable<Creature> targets,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        DamageTargetHandoff.Pending? handoff = DamageTargetHandoff.Take();
        if (targets == null || handoff is { TargetsChosen: true }) return;

        var targetList = targets.ToList();
        if (targetList.Count == 0) return;

        var combatState = targetList[0].CombatState;
        if (combatState == null) return;

        LibraryDamageType type = handoff?.Type
            ?? (LibraryAttackChaoDamagePatch.AppliesTo(props, dealer, cardSource)
                ? LibraryAttackChaoDamagePatch.ResolveExecutionDamageType(cardSource, dealer, targetList)
                : LibraryDamageType.None);
        targets = LibraryHooks.ModifyDamageTarget(combatState, targetList, amount, props, dealer, type);
    }
}

/// <summary>
///     Passes what the next damage command needs to know about its targets when vanilla and Library damage hand a hit
///     to each other, so that every hit runs <c>ModifyDamageTarget</c> exactly once. The receiving command reads it
///     at its synchronous start; <see cref="Run"/> clears it in any case once the call returns.
/// </summary>
internal static class DamageTargetHandoff
{
    /// <param name="TargetsChosen">The targets already went through <c>ModifyDamageTarget</c>.</param>
    /// <param name="Type">The damage type of the Library command handing a player target to vanilla damage.</param>
    internal readonly record struct Pending(bool TargetsChosen, LibraryDamageType Type);

    [ThreadStatic] private static Pending? _next;

    internal static Task<IEnumerable<DamageResult>> Run(Pending pending, Func<Task<IEnumerable<DamageResult>>> damage)
    {
        _next = pending;
        try
        {
            return damage();
        }
        finally
        {
            _next = null;
        }
    }

    internal static Pending? Take()
    {
        Pending? pending = _next;
        _next = null;
        return pending;
    }
}
