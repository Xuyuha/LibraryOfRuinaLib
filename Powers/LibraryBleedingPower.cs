using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Entities.Creatures;
using Library.Models;
using Library.Powers.Mode;

namespace Library.Powers;
public sealed class LibraryBleedingPower : LibraryBasePowerModel
{
    protected override LibraryPowerMode DefaultMode => new LibraryBleedingModeDefault();
    public LibraryBleedingMode CurrentMode => Mode as LibraryBleedingMode;
    public override bool IsDynamic => true;
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    protected override async Task Reduce(PlayerChoiceContext choiceContext)
    {
        if (Owner.IsDead) return;
        await PowerCmd.Apply<LibraryBleedingPower>(choiceContext, Owner,  -Amount *  3, null, null);
    }
    protected override async Task Effect(PlayerChoiceContext choiceContext)
    {
        if (Owner.IsDead) return;
        Flash();
        await CreatureCmd.Damage(choiceContext, Owner, base.Amount, ValueProp.Unpowered, Owner, null);
    }
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants,object?_ = null)
    {
        if (side != Owner.Side) return;
        await TriggerReduce(choiceContext, Owner, null);
        if (Amount < 3) await PowerCmd.Remove(this);
    }
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource,object?_ = null)
    {
        if (!props.IsPoweredAttack()) return;
        if (dealer == Owner && target != Owner)
        {
            await TriggerEffect(choiceContext, Owner, null);
            await TriggerReduce(choiceContext, Owner, null);
        }
    }
}
