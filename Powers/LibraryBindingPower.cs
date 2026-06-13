using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using Library.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Combat;

namespace Library.Powers;
public sealed class LibraryBindingPower : LibraryDurationPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override bool AllowNegative => true;

    // 束缚的层数表示伤害下限，持续回合数单独存放在 LibraryDurationPowerModel。
    public static Task<LibraryBindingPower?> ApplyWithDuration(
        Creature target,
        decimal amount,
        int turns,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false)
    {
        return LibraryDurationPowerModel.ApplyWithDuration<LibraryBindingPower>(
            target,
            amount,
            turns,
            applier,
            cardSource,
            silent);
    }

    protected override CombatSide GetDecaySide(Creature owner)
    {
        return OppositeSideOf(owner);
    }

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
