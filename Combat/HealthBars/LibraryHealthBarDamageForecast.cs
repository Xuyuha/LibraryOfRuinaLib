using Godot;
using LibraryLib.Hooks;
using LibraryLib.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Combat.HealthBars;

/// <summary>
/// Runtime context supplied to a status-damage health-bar forecast source.
/// </summary>
public readonly record struct LibraryHealthBarForecastContext(
    Creature Creature)
{
    public ICombatState? CombatState => Creature.CombatState;
}

/// <summary>
/// One future damage contribution. The renderer resolves all contributions in
/// order against one shared block budget, then runs the normal damage and
/// HP-loss hooks before drawing the resulting HP loss.
/// </summary>
public readonly record struct LibraryHealthBarDamageForecast(
    decimal Damage,
    Color Color,
    ValueProp Props,
    int Order = 0,
    Creature? Dealer = null,
    CardModel? CardSource = null,
    CardPlay? CardPlay = null)
{
    public static LibraryHealthBarDamageForecast FromLibraryPower(
        LibraryBasePowerModel power,
        Color color,
        decimal? amount = null,
        int order = 0)
    {
        ArgumentNullException.ThrowIfNull(power);

        decimal damage = amount ?? power.Amount;
        if (power.Owner?.CombatState is { } combatState)
        {
            damage = LibraryHooks.ModifyEffectiveAmount(
                combatState,
                power,
                power.Owner,
                damage,
                null,
                out _);
        }

        return new(
            damage,
            color,
            ValueProp.Unpowered,
            order);
    }
}

/// <summary>
/// Implement on a PowerModel whose next damage contribution should appear on
/// the creature's health bar and share block consumption with other sources.
/// </summary>
public interface ILibraryHealthBarDamageForecastSource
{
    IEnumerable<LibraryHealthBarDamageForecast>
        GetLibraryHealthBarDamageForecasts(
            LibraryHealthBarForecastContext context);
}

public static class LibraryHealthBarForecastColors
{
    public static readonly Color Burn = new("F2A52B");
    public static readonly Color Bleeding = new("761C24");
}
