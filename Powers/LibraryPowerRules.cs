using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Powers;

/// <summary>Shared conditions for the stat powers in this folder.</summary>
internal static class LibraryPowerRules
{
    /// <summary>
    ///     <paramref name="bonus"/> when the power's owner deals a powered attack (of <paramref name="onlyType"/>
    ///     when given), otherwise 0.
    /// </summary>
    internal static decimal OwnerAttackBonus(
        PowerModel power,
        Creature? dealer,
        ValueProp props,
        decimal bonus,
        LibraryDamageType type = LibraryDamageType.None,
        LibraryDamageType? onlyType = null) =>
        power.Owner == dealer && props.IsPoweredAttack() && (onlyType == null || type == onlyType) ? bonus : 0m;
}
