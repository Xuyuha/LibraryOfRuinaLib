using LibraryLib.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Powers;

public sealed class LibraryDefensePowerUpPower : LibraryDurationPowerModel
{
    public override PowerType Type => PowerType.Buff;
    
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override CombatSide GetDecaySide(Creature owner)
    {
        return owner.IsPlayer ? OppositeSideOf(owner) : owner.Side;
    }

    public override decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (props == ValueProp.Unpowered || cardSource == null)
            return 0;
        return base.Amount;
    }
}