using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using Library.Entities.Creatures;
using Library.Resistance;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Library.Models;
public abstract class LibraryMonsterModel : MonsterModel, LibraryAbstractModel
{
    public virtual int MaxInitialChao => DefaultStaggerResistance ?? 0;
    public virtual decimal[] DefaultChaoResistance => [1m, 1m, 1m, 1m];
    public virtual decimal[] DefaultDamageResistance => [1m, 1m, 1m, 1m];

    /// <summary>混乱抗性值。null = 无混乱抗性条。</summary>
    public virtual int? DefaultStaggerResistance => null;

    /// <summary>混乱抗性等级数据（斩/刺/打）。null = 全部 Normal。</summary>
    public virtual LibraryCreatureResistanceData? DefaultStaggerResistanceData => null;

    // TODO: 恢复时机改为下一个玩家回合结束（不确定要不要这样做）
    public sealed override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        await AfterTurnEnd(choiceContext, side, null);
        if (side != CombatSide.Player) return;
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
    protected virtual Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,object? _ = null) 
    {
        return Task.CompletedTask;
    }
}
