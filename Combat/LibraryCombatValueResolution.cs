using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Combat;

/// <summary>
/// Selects how a combat number is resolved. More restrictive values win when
/// several active listeners provide a policy.
/// </summary>
public enum LibraryCombatValueResolution
{
    Default = 0,

    /// <summary>
    /// Keep the value supplied by the card or monster move, skip every
    /// additive, multiplicative, cap, enchantment and HP-loss modifier, then
    /// continue through core block absorption and Library resistance.
    /// </summary>
    BaseValueAndResistanceOnly = 1,

    /// <summary>Resolve this contribution as zero.</summary>
    PreventValue = 2,
}

public enum LibraryCombatValueKind
{
    PhysicalDamage,
    ChaoDamage,
    HpLoss,
    Block,
}

/// <summary>
/// Immutable input supplied to combat-value policy listeners. Policies may
/// inspect ValueProp to distinguish base card/move values from indirect or
/// extra contributions.
/// </summary>
public readonly record struct LibraryCombatValueContext(
    ICombatState CombatState,
    LibraryCombatValueKind Kind,
    decimal BaseValue,
    Creature? Target,
    Creature? Dealer,
    ValueProp Props,
    CardModel? CardSource,
    CardPlay? CardPlay,
    LibraryDamageType DamageType,
    CardPreviewMode PreviewMode)
{
    public bool IsPreview => PreviewMode != CardPreviewMode.None;
}

/// <summary>
/// Implement on any combat hook listener that needs to control numerical
/// resolution for both previews and live command execution.
/// </summary>
public interface ILibraryCombatValueResolutionPolicy
{
    LibraryCombatValueResolution GetCombatValueResolution(
        in LibraryCombatValueContext context);
}

/// <summary>
/// Shared query point used by the vanilla Harmony bridge and LibraryLib's own
/// damage/chao pipelines.
/// </summary>
public static class LibraryCombatValueResolver
{
    public static LibraryCombatValueResolution Resolve(
        ICombatState? combatState,
        LibraryCombatValueKind kind,
        decimal baseValue,
        Creature? target,
        Creature? dealer,
        ValueProp props,
        CardModel? cardSource = null,
        CardPlay? cardPlay = null,
        LibraryDamageType damageType = LibraryDamageType.None,
        CardPreviewMode previewMode = CardPreviewMode.None)
    {
        if (combatState == null)
        {
            return LibraryCombatValueResolution.Default;
        }

        var context = new LibraryCombatValueContext(
            combatState,
            kind,
            baseValue,
            target,
            dealer,
            props,
            cardSource,
            cardPlay,
            damageType,
            previewMode);
        LibraryCombatValueResolution result =
            LibraryCombatValueResolution.Default;

        foreach (AbstractModel listener in combatState.IterateHookListeners())
        {
            if (listener is not ILibraryCombatValueResolutionPolicy policy)
            {
                continue;
            }

            result = MostRestrictive(
                result,
                policy.GetCombatValueResolution(in context));
            if (result == LibraryCombatValueResolution.PreventValue)
            {
                break;
            }
        }

        return result;
    }

    public static LibraryCombatValueResolution MostRestrictive(
        LibraryCombatValueResolution first,
        LibraryCombatValueResolution second) =>
        (LibraryCombatValueResolution)Math.Max((int)first, (int)second);

    public static decimal ResolveBaseValue(
        LibraryCombatValueResolution resolution,
        decimal baseValue) => resolution switch
    {
        LibraryCombatValueResolution.PreventValue => 0m,
        LibraryCombatValueResolution.BaseValueAndResistanceOnly =>
            Math.Max(0m, baseValue),
        _ => baseValue,
    };
}
