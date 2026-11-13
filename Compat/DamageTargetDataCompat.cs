#if STS2_0_113_0
#nullable enable
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace LibraryLib.Compat;

/// <summary>
///     0.113.0 起所有伤害最终汇入以 <see cref="DamageTargetData" /> 为目标的
///     <c>CreatureCmd.Damage</c> 重载，<c>AttackCommand</c> 直接调用它，不再经过以生物列表为目标的重载。
///     改写目标的补丁在这一重载上工作：先取出生物列表按原逻辑处理，再把结果写回目标数据，
///     保留下来的目标沿用原有的命中特效设置。
/// </summary>
public static class DamageTargetDataCompat
{
    /// <summary>取出目标生物列表；<paramref name="snapshot" /> 是原始目标数据的快照，供 <see cref="Rebuild" /> 使用。</summary>
    public static IEnumerable<Creature>? ToCreatures(
        IEnumerable<DamageTargetData>? targetData,
        out List<DamageTargetData>? snapshot)
    {
        if (targetData == null)
        {
            snapshot = null;
            return null;
        }

        snapshot = targetData.ToList();
        List<Creature> creatures = new(snapshot.Count);
        foreach (DamageTargetData data in snapshot)
        {
            creatures.Add(data.target);
        }

        return creatures;
    }

    /// <summary>按处理后的生物列表重建目标数据；原本就在列表中的目标沿用原数据，新加入的目标使用默认数据。</summary>
    public static IEnumerable<DamageTargetData>? Rebuild(
        List<DamageTargetData>? snapshot,
        IEnumerable<Creature>? targets)
    {
        if (targets == null)
        {
            return null;
        }

        List<DamageTargetData> remaining = snapshot == null ? [] : [.. snapshot];
        List<DamageTargetData> rebuilt = [];
        foreach (Creature target in targets)
        {
            int index = remaining.FindIndex(data => ReferenceEquals(data.target, target));
            if (index >= 0)
            {
                rebuilt.Add(remaining[index]);
                remaining.RemoveAt(index);
            }
            else
            {
                rebuilt.Add(new DamageTargetData(target));
            }
        }

        return rebuilt;
    }
}
#endif
