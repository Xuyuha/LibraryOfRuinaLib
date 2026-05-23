using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using Library.Entities.Creatures;
using Library.Resistance;
using MegaCrit.Sts2.Core.Models.Cards;
using Godot;

namespace Library.Models;
public abstract class LibraryMonsterModel : MonsterModel, ILibraryAbstractModel
{
    public virtual LibraryCreatureResistanceData.Resistance? DefaultPhysicalResistanceData => null;

    /// <summary>混乱抗性值。null = 无混乱抗性条。</summary>
    public virtual int DefaultChaoResistance => -1;
    public bool HasChaoResistance => DefaultChaoResistance > 0;

    /// <summary>混乱抗性等级数据（斩/刺/打）。null = 全部 Normal。</summary>
    public virtual LibraryCreatureResistanceData.Resistance? DefaultChaoResistanceData => null;

    // TODO: 恢复时机改为下一个玩家回合结束（不确定要不要这样做）
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,IEnumerable<Creature> participants)
    {
        await AfterTurnEnd(choiceContext, side, participants, null);
        if (side == CombatSide.Player) return;
        foreach (Creature creature in CombatState.Creatures)
        {
            if (creature is not LibraryCreature lc || lc.Side != CombatSide.Enemy || !lc.RestoreChaoOnNextOwnerTurn)
                continue;
            if (lc.StunPlayerTurnsRemaining > 1)
            {
                lc.DecrementStunTurns();
                continue;
            }
            lc.RestoreChaoOnNextOwnerTurn = false;
            lc.RestorePreStunResistance();
            lc.SetCurrentChaoValueInternal(lc.MaxChaoValue);
        }
    }
    //子类重写不会覆盖父类方法了
    protected virtual Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,IEnumerable<Creature> participants,object? _ = null) 
    {
        return Task.CompletedTask;
    }
}
