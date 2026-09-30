using LibraryLib.Models;
using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Powers;

/// <summary>斩击威力增强：本方造成的斩击伤害与混乱伤害 +层数。</summary>
public sealed class LibraryStrongSlashPower : LibraryTurnsPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override CombatSide DecaySide => Owner.IsPlayer ? OppositeSideOf(Owner) : Owner.Side;

    public override decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) =>
        LibraryPowerRules.OwnerAttackBonus(this, dealer, props, Amount, type, LibraryDamageType.Slash);

    public override decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) =>
        LibraryPowerRules.OwnerAttackBonus(this, dealer, props, Amount, type, LibraryDamageType.Slash);
}
