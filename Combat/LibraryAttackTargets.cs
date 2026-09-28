#nullable enable
using LibraryLib.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;

namespace LibraryLib.Combat;

/// <summary>
/// Lets another mod narrow the targets a <see cref="LibraryAttackCommand"/> may hit, for example to keep
/// player-side attacks off friendly allies. Register once with <see cref="LibraryAttackTargets.RegisterFilter"/>.
/// </summary>
public interface ILibraryAttackTargetFilter
{
    /// <summary>
    /// Called in registration order with the targets left by the previous filter; return them unchanged to
    /// keep them. Also used for target previews, so it must not change state, and must depend only on
    /// synchronized combat state so every peer picks the same targets.
    /// </summary>
    IReadOnlyList<Creature> FilterTargets(LibraryAttackCommand command, IReadOnlyList<Creature> targets);
}

public static class LibraryAttackTargets
{
    private static readonly object RegistrationLock = new();
    private static ILibraryAttackTargetFilter[] _filters = [];

    public static void RegisterFilter(ILibraryAttackTargetFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        lock (RegistrationLock)
        {
            if (Array.IndexOf(_filters, filter) < 0)
            {
                _filters = [.. _filters, filter];
            }
        }
    }

    internal static IReadOnlyList<Creature> Filter(LibraryAttackCommand command, IReadOnlyList<Creature> targets)
    {
        foreach (ILibraryAttackTargetFilter filter in _filters)
        {
            try
            {
                targets = filter.FilterTargets(command, targets) ?? targets;
            }
            catch (Exception exception)
            {
                Log.Error($"[LibraryOfRuinaLib] Attack target filter {filter.GetType().FullName} failed: {exception}");
            }
        }

        return targets;
    }
}
