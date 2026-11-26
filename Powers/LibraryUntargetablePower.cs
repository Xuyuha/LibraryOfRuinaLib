#nullable enable
using LibraryLib.Combat;
using LibraryLib.Models;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace LibraryLib.Powers;

/// <summary>
/// Prevents the owner from being selected or hit. The applying mechanic owns removal timing.
/// Adapted from LibraryOfRuina.framework.powers.UntargetablePower; see THIRD_PARTY_NOTICES.
/// </summary>
public class LibraryUntargetablePower : DynamicPowerModel, ILibraryUntargetablePower
{
    protected override string LegacyPowerId => "LIBRARY_UNTARGETABLE_POWER";

    public override string PackedIconPath =>
        "res://LibraryOfRuinaLib/images/powers/library_untargetable_power.png";

    public override string ResolvedBigIconPath => PackedIconPath;

    public override PowerType Type => PowerType.None;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldAllowHitting(Creature creature) => creature != Owner;

    public override bool ShouldAllowTargeting(Creature target) => target != Owner;
}
