using LibraryLib.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Powers;
public sealed class LibraryVulnerablePower : LibraryTurnsPowerModel//易损，受到生命值伤害+1
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override CombatSide DecaySide => OppositeSideOf(Owner);

    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 与最终生命值结算的截断规则一致，未满一点的残余伤害不触发易损。
        if (Owner == target && decimal.Truncate(amount) > 0m)
        {
            return amount + Amount;
        }

        return amount;
    }
}
