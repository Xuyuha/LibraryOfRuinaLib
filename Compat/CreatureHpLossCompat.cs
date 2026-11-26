#nullable enable
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Compat;

/// <summary>
///     扣除生命并返回伤害结果。0.114.0 起 <c>Creature.LoseHpInternal</c> 不再接收伤害属性，
///     只返回 <c>HpLossResult</c>，这里按旧版本的字段含义组装回 <see cref="DamageResult" />。
/// </summary>
internal static class CreatureHpLossCompat
{
    internal static DamageResult LoseHpWithResult(this Creature creature, decimal amount, ValueProp props)
    {
#if STS2_0_114_0
        HpLossResult loss = creature.LoseHpInternal(amount);
        return new DamageResult(creature, props)
        {
            UnblockedDamage = loss.hpLost,
            WasTargetKilled = loss.killed,
            OverkillDamage = loss.overkillAmount
        };
#else
        return creature.LoseHpInternal(amount, props);
#endif
    }
}
