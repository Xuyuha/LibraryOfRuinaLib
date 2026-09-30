using Godot;
using LibraryLib.Combat.HealthBars;
using LibraryLib.Models;
using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Powers;

/// <summary>Rules shared by the powers in this folder.</summary>
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

    /// <summary>The damage a dot power deals on its next trigger, drawn on its owner's health bar.</summary>
    internal static IEnumerable<LibraryHealthBarDamageForecast> NextTriggerForecast(
        LibraryBasePowerModel power,
        LibraryHealthBarForecastContext context,
        Color color) =>
        power.Amount <= 0 || context.CombatState == null
            ? []
            : [LibraryHealthBarDamageForecast.FromLibraryPower(power, color)];
}
