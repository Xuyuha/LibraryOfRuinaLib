using Library.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;


namespace Library.Powers;
public sealed class LibraryBreakProtectionPower : LibraryDurationPowerModel//振奋，受到混乱值伤害-1
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override CombatSide GetDecaySide(Creature owner)
    {
        return OppositeSideOf(owner);
    }

    public override decimal ModifyChaoDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type)
    {
        if(Owner != target)
            return 0m;
        return -Amount;
    }
}
