using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.ValueProps;
using Library.Models;

namespace Library.Powers;
public sealed class LibraryBurnPower : LibraryBasePowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    protected override async Task Effect(PlayerChoiceContext choiceContext)
    {
        Flash();
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), base.Owner, base.Amount, ValueProp.Unpowered, Owner.IsMonster ? Owner : null, null);
    }
    protected override async Task Reduce(PlayerChoiceContext choiceContext)
    {
        await PowerCmd.Apply<LibraryBurnPower>(choiceContext, Owner, -Amount / 3, null, null);
    }
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == Owner.Side)
        {
            await TriggerEffect(choiceContext, Owner, null);
            await TriggerReduce(choiceContext, Owner, null);
        }
    }
}