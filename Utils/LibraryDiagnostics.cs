using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace LibraryLib.Utils;

/// <summary>Null-safe, exception-safe descriptions for log messages.</summary>
internal static class LibraryDiagnostics
{
    internal static string Describe(AbstractModel model)
    {
        try
        {
            return model.Id.ToString();
        }
        catch
        {
            return "unknown-model-id";
        }
    }

    internal static string Describe(CardModel? card)
    {
        if (card == null)
            return "null";

        try
        {
            return card.Id.Entry;
        }
        catch
        {
            return card.GetType().FullName ?? card.GetType().Name;
        }
    }

    internal static string Describe(Creature? creature)
    {
        if (creature == null)
            return "null";

        try
        {
            if (creature.IsMonster)
                return creature.Monster?.Id.Entry ?? "unknown-monster";
            if (creature.IsPlayer)
                return creature.Player?.Character.Id.Entry ?? "unknown-player";
        }
        catch
        {
            // Fall back to the type name below.
        }

        return creature.GetType().FullName ?? creature.GetType().Name;
    }
}
