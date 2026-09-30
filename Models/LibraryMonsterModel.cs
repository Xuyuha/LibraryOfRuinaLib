using LibraryLib.Combat;
using LibraryLib.Entities.Creatures;
using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace LibraryLib.Models;

/// <summary>
///     接入抗性与混乱系统的怪物基类：战斗中由 <see cref="LibraryCreature"/> 承载。
///     库钩子的默认实现见 LibraryModelHookDefaults.cs。
/// </summary>
public abstract partial class LibraryMonsterModel : MonsterModel, ILibraryAbstractModel,
    ILibraryCombatValueResolutionPolicy
{
    /// <summary>
    /// Fraction of maximum Chao restored when this monster leaves stagger.
    /// Existing monsters retain the original full recovery behavior.
    /// </summary>
    public virtual decimal ChaoRecoveryRatio => 1m;

    /// <summary>
    /// Computes the post-stagger Chao value. Override this method when the
    /// recovery rule cannot be represented by <see cref="ChaoRecoveryRatio"/>.
    /// </summary>
    public virtual decimal GetChaoRecoveryValue(int maxChaoValue)
    {
        decimal ratio = Math.Clamp(ChaoRecoveryRatio, 0m, 1m);
        return Math.Floor(maxChaoValue * ratio);
    }

    /// <summary>
    /// 混乱恢复后进入的状态 Id；调用方未显式指定恢复招式时优先使用。
    /// 默认 null 沿用原版规则（回到最近一次抽取的招式）。按周期动态选招的怪物
    /// 应返回自己的路由状态：混乱锁生效期间无法再改写恢复招式，只有在恢复时
    /// 重新经过路由，才能按最新周期选招，而不是重放触发混乱前的那一招。
    /// </summary>
    public virtual string? StunRecoveryStateId => null;

    /// <summary>
    /// Default numerical-resolution policy. Encounter monsters can override
    /// this once and affect vanilla and LibraryLib preview/live paths alike.
    /// </summary>
    public virtual LibraryCombatValueResolution GetCombatValueResolution(
        in LibraryCombatValueContext context) =>
        LibraryCombatValueResolution.Default;

    public virtual bool ShowResistanceUi => true;

    public virtual LibraryCreatureResistanceData.Resistance? DefaultPhysicalResistanceData => null;

    /// <summary>混乱抗性值。不大于 0 表示没有混乱抗性条。</summary>
    public virtual int DefaultChaoResistance => -1;

    public bool HasChaoResistance => DefaultChaoResistance > 0;

    /// <summary>混乱抗性等级数据（斩/刺/打）。null = 全部 Normal。</summary>
    public virtual LibraryCreatureResistanceData.Resistance? DefaultChaoResistanceData => null;

    /// <summary>
    ///     敌方回合结束时推进本怪物自己的混乱恢复。子类请重写 <see cref="AfterSideTurnEndInternal"/>。
    /// </summary>
    public sealed override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        await AfterSideTurnEndInternal(choiceContext, side, participants);
        if (side != CombatSide.Enemy
            || Creature is not LibraryCreature creature
            || creature.Side != CombatSide.Enemy
            || creature.CombatState == null
            || !creature.IsChaoed
            || !creature.RestoreChaoOnNextOwnerTurn)
        {
            return;
        }

        // Every monster receives this hook; each one advances only its own stun.
        if (creature.StunPlayerTurnsRemaining > 1)
        {
            creature.DecrementStunTurns();
            return;
        }

        creature.RestoreChaoOnNextOwnerTurn = false;
        creature.RestorePreStunResistance();
        creature.SetCurrentChaoValueInternal(GetChaoRecoveryValue(creature.MaxChaoValue));
    }

    protected virtual Task AfterSideTurnEndInternal(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants) =>
        Task.CompletedTask;
}
