using LibraryLib.Models;
using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Powers;

/// <summary>威力增强：本方造成的伤害与混乱伤害 +层数。</summary>
public sealed class LibraryStrongPower : LibraryTurnsPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 玩家侧延到敌方回合结束衰减，反击攻击骰仍享受加成；敌人保持自身回合结束衰减。
    protected override CombatSide DecaySide => Owner.IsPlayer ? OppositeSideOf(Owner) : Owner.Side;

    public override decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        LibraryPowerRules.OwnerAttackBonus(this, dealer, props, Amount);

    public override decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) =>
        LibraryPowerRules.OwnerAttackBonus(this, dealer, props, Amount);
}
