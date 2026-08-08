#nullable enable
using LibraryLib.Models;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace LibraryLib.Powers;

/// <summary>
///     混乱抗性能力——仅用于图标和本地化描述的展示壳。
///     计算逻辑已移至 LibraryCreature / LibraryCreatureCmd / LibraryDamageCalculate。
/// </summary>
public sealed class LibraryStaggerResistancePower : LibraryPowerModel
{
    private const string StaggerIconPath =
        "res://LibraryOfRuinaLib/images/powers/library_stagger_resistance.png";

    protected override string LegacyPowerId => "LIBRARY_OF_RUINA_STAGGER_RESISTANCE_POWER";

    public override PowerType Type => PowerType.None;

    protected override bool IsVisibleInternal => true;

    public override string PackedIconPath => StaggerIconPath;

    public override string ResolvedBigIconPath => StaggerIconPath;

    public override PowerStackType StackType => PowerStackType.Counter;
}
