using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Commands;
using Library.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;

namespace Library.Powers;
public sealed class LibraryQuicknessPower : LibraryPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        foreach(var c in CombatState.Enemies.ToList())
        {
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(),c,Amount,ValueProp.Unpowered,Owner);
        }
        await PowerCmd.Decrement(this);
    }
}
