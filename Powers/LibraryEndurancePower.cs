using Library.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;


namespace Library.Powers;
public sealed class LibraryEndurancePower : LibraryDurationPowerModel//忍耐，玩家获得护盾增加，怪物受到伤害减少
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 玩家侧也按敌方回合结束衰减，保证反击阶段仍生效，并在下个玩家回合开始前消失。
    protected override CombatSide GetDecaySide(Creature owner)
    {
        return OppositeSideOf(owner);
    }

    public override decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        if(Owner != target)
            return 0m;
        if(Owner.IsMonster)
            return 0m;
        if(props.IsPoweredCardOrMonsterMoveBlock())
            return Amount;
        return 0m;
    }
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if(Owner != target)
            return 0m;
        if(Owner.IsPlayer)
            return 0m;
        if(props.IsPoweredAttack())
            return -Amount;
        return 0m;
    }
}
