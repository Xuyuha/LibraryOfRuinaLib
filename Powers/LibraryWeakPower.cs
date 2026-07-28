using Library.Models;
using Library.Resistance;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Library.Powers;
public sealed class LibraryWeakPower : LibraryDurationPowerModel//虚弱，造成的伤害与混乱伤害-1
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay){
		if (target is not { IsPlayer: true })
		{
			return 0m;
		}
		return GetDamageReduction(props, dealer);
	}
    private decimal GetDamageReduction(ValueProp props, Creature? dealer)
    {
		if (base.Owner != dealer)
		{
			return 0m;
		}
		if (!props.IsPoweredAttack())
		{
			return 0m;
		}
		return -base.Amount;
	}
    public override decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type)
    {
		if (base.Owner != dealer)
		{
			return 0m;
		}
		if (!props.IsPoweredAttack())
		{
			return 0m;
		}
		return -base.Amount;
	}
}
