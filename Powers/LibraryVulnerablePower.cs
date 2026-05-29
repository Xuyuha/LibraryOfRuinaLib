using Library.Models;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;


namespace Library.Powers;
public sealed class LibraryVulnerablePower : LibraryDurationPowerModel//易损，受到生命值伤害+1
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource){
        if(Owner == target)
            return amount + Amount;
        return amount;
    }
}
