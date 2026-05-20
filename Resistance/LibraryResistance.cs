#nullable enable
using System.Threading.Tasks;
using Library.Resistance.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Library.Resistance;

public static class LibraryResistance
{
    public static async Task EnsureOnEnemy(Creature enemy, PlayerChoiceContext? choiceContext = null)
    {
        await LibraryResistanceBootstrap.EnsureOnEnemy(enemy, choiceContext);
    }

    public static LibraryStaggerResistancePower? GetStaggerPower(Creature creature) =>
        creature.GetPower<LibraryStaggerResistancePower>();
}
