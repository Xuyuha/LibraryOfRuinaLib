using Library.Models;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace Library.Powers;
public sealed class LibraryStrongSlashPower : LibraryDurationPowerModel//穿刺威力增强，若本次伤害是穿刺伤害，则所造成的伤害与混乱伤害+1
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type){
		if (base.Owner != dealer)
		{
			return 0m;
		}
		if (!props.IsPoweredAttack() || type != LibraryDamageType.Slash)
		{
			return 0m;
		}
		return base.Amount;
	}
    public override decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type)
    {
		if (base.Owner != dealer)
		{
			return 0m;
		}
		if (!props.IsPoweredAttack() || type != LibraryDamageType.Slash)
		{
			return 0m;
		}
		return base.Amount;
	}
}
