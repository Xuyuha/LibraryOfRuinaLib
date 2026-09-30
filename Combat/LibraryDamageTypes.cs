#nullable enable
using LibraryLib.Commands;
using LibraryLib.Patches;
using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace LibraryLib.Combat;

/// <summary>
/// What is being attacked with when a damage type is decided. Exactly one source is usually set:
/// a card (preview, or a non-Library card dealing damage), a vanilla <see cref="AttackCommand"/>,
/// or a <see cref="LibraryAttackCommand"/>.
/// </summary>
public readonly record struct LibraryDamageTypeContext(
    CardModel? Card,
    Creature? Attacker,
    Creature? Target,
    AttackCommand? VanillaCommand,
    LibraryAttackCommand? LibraryCommand,
    bool IsPreview);

/// <summary>
/// Lets another mod change the damage type the library decided, for example an enchantment that turns
/// a card's attack into Pierce. Register once with <see cref="LibraryDamageTypes.RegisterModifier"/>.
/// </summary>
public interface ILibraryDamageTypeModifier
{
    /// <summary>
    /// Called after the library's own inference, in registration order; each modifier sees the result of
    /// the previous one. Return <paramref name="current"/> to leave it unchanged. The result must depend
    /// only on synchronized combat state, so every peer decides the same type.
    /// </summary>
    LibraryDamageType ModifyDamageType(in LibraryDamageTypeContext context, LibraryDamageType current);
}

/// <summary>
/// Public entry points for the damage type of attacks. Every place the library decides a type goes through
/// <see cref="Modify"/>: card previews and non-Library card damage (<see cref="ResolveForCard"/>), vanilla
/// <see cref="AttackCommand"/> execution, and <see cref="LibraryAttackCommand"/> execution.
/// </summary>
public static class LibraryDamageTypes
{
    private static readonly object RegistrationLock = new();
    private static ILibraryDamageTypeModifier[] _modifiers = [];

    public static void RegisterModifier(ILibraryDamageTypeModifier modifier)
    {
        ArgumentNullException.ThrowIfNull(modifier);
        lock (RegistrationLock)
        {
            if (Array.IndexOf(_modifiers, modifier) < 0)
            {
                _modifiers = [.. _modifiers, modifier];
            }
        }
    }

    /// <summary>
    /// Damage type of an attack card that is not a <c>LibraryCardModel</c> (Library cards carry their own
    /// type): the library's inference (all-enemies/all-allies is Slash for every hit, other multi-hit
    /// attacks are Pierce, otherwise Blunt), then
    /// registered modifiers. <paramref name="target"/> is the single target when known.
    /// </summary>
    public static LibraryDamageType ResolveForCard(CardModel card, Creature? target, bool isPreview)
    {
        LibraryDamageType inferred = LibraryDamagePreviewFeedback.ResolveVanillaPreviewDamageType(card, target);
        return Modify(new LibraryDamageTypeContext(card, TryGetOwner(card), target, null, null, isPreview), inferred);
    }

    /// <summary>Damage type the library uses while <paramref name="command"/> executes.</summary>
    public static LibraryDamageType ResolveForAttack(AttackCommand command) =>
        AttackExecuteContext.ResolveVanillaDamageType(command);

    /// <summary>Applies the registered modifiers to <paramref name="current"/>.</summary>
    public static LibraryDamageType Modify(in LibraryDamageTypeContext context, LibraryDamageType current)
    {
        foreach (ILibraryDamageTypeModifier modifier in _modifiers)
        {
            try
            {
                current = modifier.ModifyDamageType(context, current);
            }
            catch (Exception exception)
            {
                Log.Error($"[LibraryOfRuinaLib] Damage type modifier {modifier.GetType().FullName} failed: {exception}");
            }
        }

        return current;
    }

    private static Creature? TryGetOwner(CardModel card)
    {
        try
        {
            return card.Owner?.Creature;
        }
        catch
        {
            return null;
        }
    }
}
