using Library.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Library.Powers;
public sealed class LibraryStrongPiercePower : LibraryDurationPowerModel//穿透威力增强，若本次伤害是穿刺伤害，则所造成的伤害与混乱伤害+1
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override CombatSide GetDecaySide(Creature owner)
    {
        return owner.IsPlayer ? OppositeSideOf(owner) : owner.Side;
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type){
		if (base.Owner != dealer)
		{
			return 0m;
		}
		if (!props.IsPoweredAttack() || type != LibraryDamageType.Pierce)
		{
			return 0m;
		}
		return base.Amount;
	}
    public override decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
		if (base.Owner != dealer)
		{
			return 0m;
		}
		if (!props.IsPoweredAttack() || type != LibraryDamageType.Pierce)
		{
			return 0m;
		}
		return base.Amount;
	}
}
