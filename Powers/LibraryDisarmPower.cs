using Library.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;


namespace Library.Powers;
public sealed class LibraryDisarmPower : LibraryDurationPowerModel//破绽，玩家获得护盾减少，怪物受到伤害增加
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        if(Owner != target)
            return 0m;
        if(Owner.IsMonster)
            return 0m;
        if(props.IsPoweredCardOrMonsterMoveBlock())
            return -Amount;
        return 0m;
    }
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if(Owner != target)
            return 0m;
        if(Owner.IsPlayer)
            return 0m;
        if(props.IsPoweredAttack())
            return Amount;
        return 0m;
    }
}
