using LibraryLib.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Powers;
public sealed class LibraryProtectionPower : LibraryTurnsPowerModel//保护，受到生命值伤害-1
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override CombatSide DecaySide => OppositeSideOf(Owner);

    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource){
        if(Owner == target)
            return Math.Max(0m, amount - Amount);
        return amount;
    }
}
