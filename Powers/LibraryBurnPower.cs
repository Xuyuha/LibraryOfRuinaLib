using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Entities.Creatures;
using Library.Models;
using Library.Powers.Mode;

namespace Library.Powers;
public sealed class LibraryBurnPower : LibraryBasePowerModel
{
    protected override LibraryPowerMode DefaultMode => new LibraryBurnModeDefault(this);
    public LibraryBurnMode CurrentMode => Mode as LibraryBurnMode;
    public override bool IsDynamic => true;
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    protected override async Task Reduce(PlayerChoiceContext choiceContext)
    {
        if (Owner.IsDead) return;
        await PowerCmd.ModifyAmount(choiceContext, this, -CalculateStackDecayByThird(Amount), null, null);
        if (Amount <= 0m)
            await PowerCmd.Remove(this);
    }
    protected override async Task Effect(PlayerChoiceContext choiceContext, decimal effectiveAmount)
    {
        if (Owner.IsDead) return;
        Flash();
        await CreatureCmd.Damage(choiceContext, Owner, effectiveAmount, ValueProp.Unpowered, Owner, null);
    }
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants,object?_ = null)
    {
        if (side != Owner.Side) return;
        await TriggerEffect(choiceContext, Owner, null);
        await TriggerReduce(choiceContext, Owner, null);
    }
}
