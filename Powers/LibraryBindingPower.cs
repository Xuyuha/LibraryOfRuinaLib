using LibraryLib.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Powers;
public sealed class LibraryBindingPower : LibraryTurnsPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override bool AllowNegative => true;
    protected override CombatSide DecaySide => OppositeSideOf(Owner);

	public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target != Owner)
		{
			return amount;
		}
		if (!props.IsPoweredAttack())
		{
			return amount;
		}
		if (amount < 1m)
		{
			return amount;
		}
		if (amount >= Amount)
		{
			return amount;
		}
		return Amount;
	}
}
