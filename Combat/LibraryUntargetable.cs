#nullable enable
using LibraryLib.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace LibraryLib.Combat;

/// <summary>
/// Opts an existing PowerModel into the shared command guards without changing its model ID or base
/// class. Its inherited PowerModel hook methods satisfy this contract. Both hooks must be read-only.
/// </summary>
public interface ILibraryUntargetablePower
{
    bool ShouldAllowHitting(Creature creature);
    bool ShouldAllowTargeting(Creature target);
}

/// <summary>
/// Additional command guards for LibraryUntargetablePower and opt-in compatibility models.
/// Deliberately does not turn every third-party ShouldAllowHitting veto into damage/stun immunity:
/// the content mod has source/side-specific exceptions which remain its responsibility.
/// </summary>
public static class LibraryUntargetable
{
    public static bool BlocksHit(Creature? target) =>
        target != null && target.Powers.OfType<ILibraryUntargetablePower>()
            .Any(power => !power.ShouldAllowHitting(target));

    public static bool BlocksSelection(Creature? target) =>
        BlocksHit(target)
        || (target != null && target.Powers.OfType<ILibraryUntargetablePower>()
            .Any(power => !power.ShouldAllowTargeting(target)));

    internal static IReadOnlyList<Creature> FilterHittable(IEnumerable<Creature> targets)
    {
        IReadOnlyList<Creature> snapshot = targets as IReadOnlyList<Creature> ?? targets.ToArray();
        return snapshot.All(target => !BlocksHit(target))
            ? snapshot
            : snapshot.Where(target => !BlocksHit(target)).ToArray();
    }
}
