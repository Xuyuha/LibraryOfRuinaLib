using LibraryLib.Models;
using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Powers;
public sealed class LibraryBreakVulnerablePower : LibraryTurnsPowerModel//萎靡，受到混乱伤害+1
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override CombatSide DecaySide => OppositeSideOf(Owner);

    public override decimal ModifyChaoDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type)
    {
        if(Owner != target)
            return 0m;
        return Amount;
    }
}
