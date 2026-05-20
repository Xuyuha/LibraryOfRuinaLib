#nullable enable
using System.Threading.Tasks;
using Library.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Library.Resistance;

public static class LibraryResistance
{
    public static async Task EnsureOnEnemy(Creature enemy, PlayerChoiceContext? choiceContext = null)
    {
        await LibraryResistanceBootstrap.EnsureOnEnemy(enemy, choiceContext);
    }

    public static LibraryCreatureResistanceData? GetResistanceData(Creature creature) =>
        (creature as LibraryCreature)?.ResistanceData;
}
