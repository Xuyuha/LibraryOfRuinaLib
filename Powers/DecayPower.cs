using LibraryLib.Combat;
using LibraryLib.Combat.HealthBars;
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

// Adapted from LibraryOfRuina's CorrosionFollowUpRules / RnfmabjCorrosionPower.
// See THIRD_PARTY_NOTICES for the source revision and license.
// Keep the existing type and model ID so downstream mods and saves remain valid.
public sealed class LibraryDecayPower : DynamicPowerModel, ILibraryHealthBarDamageForecastSource
{
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
        if (Owner.IsDead || Amount <= 0 || !LibraryTurnParticipants.IsOwnTurn(Owner, side, participants))
            return;

        Flash();
        await GameApi.Damage(choiceContext, Owner, Amount, ValueProp.Unpowered,
            Applier ?? Owner, null, null);
        await PowerCmd.Decrement(this);
    }
}
