using LibraryLib.Combat;
using LibraryLib.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Powers;

// Adapted from LibraryOfRuina.framework.powers.NullifyPower (see THIRD_PARTY_NOTICES).
// Use a library-owned name/ID to coexist with the content mod's NullifyPower.
public sealed class LibraryNullifyPower : DynamicPowerModel, ILibraryCombatValueResolutionPolicy
{
    private bool _skipNextEnemyTurnEndRemoval;

    public override PowerType Type => PowerType.None;
    public override PowerStackType StackType => PowerStackType.Single;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _skipNextEnemyTurnEndRemoval = Owner.CombatState?.CurrentSide == CombatSide.Enemy;
        return Task.CompletedTask;
    }

    public LibraryCombatValueResolution GetCombatValueResolution(in LibraryCombatValueContext context)
    {
        bool affectsOwner = context.Kind switch
        {
            LibraryCombatValueKind.PhysicalDamage or LibraryCombatValueKind.ChaoDamage => context.Dealer == Owner,
            LibraryCombatValueKind.Block => context.Target == Owner,
            _ => false,
        };

        // Printed card/move values and resistance remain; independent unpowered effects remain.
        return affectsOwner && context.Props.IsPoweredAttack()
            ? LibraryCombatValueResolution.BaseValueAndResistanceOnly
            : LibraryCombatValueResolution.Default;
    }

    public override async Task AfterSideTurnEndLate(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Enemy)
            return;
        if (_skipNextEnemyTurnEndRemoval)
        {
            _skipNextEnemyTurnEndRemoval = false;
            return;
        }
        await PowerCmd.Remove(this);
    }
}
