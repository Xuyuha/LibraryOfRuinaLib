using LibraryLib.Combat;
using LibraryLib.Combat.HealthBars;
using LibraryLib.Commands;
using LibraryLib.Compat;
using LibraryLib.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Powers;

// Original library corrosion behavior. Keep this type and model ID for existing library content.
public sealed class LibraryDecayPower : PowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || !props.IsPoweredAttack()) return;
        await CreatureCmd.Damage(choiceContext, Owner, Amount, ValueProp.Unpowered, Owner);
        await LibraryCreatureCmd.ChaoDamage(choiceContext, Owner, Amount, ValueProp.Unpowered, Owner, null);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        await CreatureCmd.Damage(choiceContext, Owner, Amount, ValueProp.Unpowered, Owner);
        await PowerCmd.Decrement(this);
    }
}

// Adapted from LibraryOfRuina's CorrosionFollowUpRules / RnfmabjCorrosionPower.
// This is intentionally separate from LibraryDecayPower so old and new corrosion can coexist.
public sealed class LibraryCorrosionPower : DynamicPowerModel, ILibraryHealthBarDamageForecastSource
{
    protected override string LegacyPowerId => "LIBRARY_CORROSION_POWER";
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public IEnumerable<LibraryHealthBarDamageForecast> GetLibraryHealthBarDamageForecasts(
        LibraryHealthBarForecastContext context) =>
        Owner.IsDead || Amount <= 0 || context.CombatState == null
            ? []
            : [new(Amount, LibraryHealthBarForecastColors.Corrosion,
                ValueProp.Unpowered, Order: 10, Dealer: Applier ?? Owner)];

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // Blocked hits count, zero damage and unpowered follow-ups do not.
        if (target != Owner || Owner.IsDead || Amount <= 0
            || result.TotalDamage <= 0 || !props.IsPoweredAttack())
            return;

        Flash();
        await GameApi.Damage(choiceContext, Owner, Amount, ValueProp.Unpowered,
            Applier ?? dealer ?? Owner, null, null);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (Owner.IsDead || Amount <= 0 || !IsOwnTurn(Owner, side, participants))
            return;

        Flash();
        await GameApi.Damage(choiceContext, Owner, Amount, ValueProp.Unpowered,
            Applier ?? Owner, null, null);
        await PowerCmd.Decrement(this);
    }

    private static bool IsOwnTurn(Creature creature, CombatSide side, IEnumerable<Creature> participants)
    {
        if (creature.Side != side) return false;
        if (side != CombatSide.Player) return true;
        Creature anchor = creature.PetOwner?.Creature ?? creature;
        return anchor.IsPlayer
            ? participants.Contains(anchor)
            : CombatManager.Instance.PlayersTakingExtraTurn.Count == 0;
    }
}
