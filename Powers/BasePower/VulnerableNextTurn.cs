using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands;
using Library.Models;

namespace Library.Powers;
public sealed class LibraryVulnerableNextTurn : LibraryPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;
    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, ICombatState combatState)
    {
        if (side != Owner.Side)
        {
            await PowerCmd.Apply<LibraryVulnerablePower>(choiceContext, Owner, Amount, null, null);
            await PowerCmd.Remove(this);
        }
    }
}
