using LibraryLib.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Powers;
public sealed class LibraryQuicknessPower : LibraryPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    /// <summary>主人回合结束时，对所有对手造成等同层数的伤害，然后层数 -1。</summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || side is not (CombatSide.Enemy or CombatSide.Player))
            return;

        List<Creature> opponents = side == CombatSide.Enemy
            ? CombatState.Players.Select(player => player.Creature).ToList()
            : CombatState.Enemies.ToList();
        foreach (Creature opponent in opponents)
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), opponent, Amount, ValueProp.Unpowered, Owner);
        await PowerCmd.Decrement(this);
    }
}
