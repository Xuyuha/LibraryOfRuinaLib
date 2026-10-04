using LibraryLib.Commands;
using LibraryLib.Combat;
using LibraryLib.Entities.Creatures;
using LibraryLib.Localization.LibraryDynamicVars;
using LibraryLib.Powers.LibraryPowerMode;
using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using LibraryLib.Localization.Dice;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Models;
public abstract class LibraryMonsterModel : MonsterModel,
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

    public virtual decimal ModifyDiceMaxValue(LibraryDice dice, decimal maxValue)
    {
        return maxValue;
    }
    public virtual decimal ModifyDiceMinValue(LibraryDice dice, decimal minValue)
    {
        return minValue;
    }
    public virtual Creature ModifyDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer,LibraryDamageType type)
    {
        return creature;
    }
    public virtual Creature ModifyChaoDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer,LibraryDamageType type)
    {
        return creature;        
    }
    public virtual bool ShowResistanceUi => true;

    public virtual LibraryCreatureResistanceData.Resistance? DefaultPhysicalResistanceData => null;

    /// <summary>混乱抗性值。null = 无混乱抗性条。</summary>
    public virtual int DefaultChaoResistance => -1;
    public bool HasChaoResistance => DefaultChaoResistance > 0;

    /// <summary>混乱抗性等级数据（斩/刺/打）。null = 全部 Normal。</summary>
    public virtual LibraryCreatureResistanceData.Resistance? DefaultChaoResistanceData => null;

    
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
    //子类重写不会覆盖父类方法了
    protected virtual Task AfterSideTurnEndInternal(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        return Task.CompletedTask;
    }
}
