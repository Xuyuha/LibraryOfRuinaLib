using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Library.Models;

namespace Library.Powers;
public sealed class LibrarySmokePower : LibraryPowerModel
{
    public override PowerType Type => PowerType.None;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("SmokeMultiplier",1)
    ];
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? _, out decimal modifiedAmount)
    {
        if (canonicalPower is not LibrarySmokePower)
        {
            modifiedAmount = amount;
            return false;
        }
        if (Amount + amount >= 10)
        {
            modifiedAmount = 10 - Amount;
            return false;
        }
        modifiedAmount = amount;
        return false;
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner || dealer == Owner && props.IsPoweredAttack())
        {
            return DynamicVars["SmokeMultiplier"].BaseValue;
        }
        return 1m;
    }
    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power is LibrarySmokePower && Owner == power.Owner)
            DynamicVars["SmokeMultiplier"].BaseValue = 1 + 0.1m * power.Amount;
        return Task.CompletedTask;
    }
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == Owner.Side)
        {
            await PowerCmd.Decrement(this);
        }
    }

}
