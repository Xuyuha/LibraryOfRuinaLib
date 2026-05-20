#nullable enable
using System.Threading.Tasks;
using Library.Models;
using Library.Resistance.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Library.Resistance;

internal static class LibraryResistanceBootstrap
{
    internal static async Task EnsureOnEnemy(Creature enemy, PlayerChoiceContext? choiceContext = null)
    {
        if (!enemy.IsEnemy || enemy.CombatState == null || !enemy.IsAlive)
        {
            return;
        }

        if (enemy.Monster is not LibraryMonsterModel)
        {
            return;
        }

        PlayerChoiceContext ctx = LibraryCombatChoiceContexts.Resolve(choiceContext, enemy.CombatState);
        await EnsureStaggerResistance(enemy, ctx);
    }

    private static async Task EnsureStaggerResistance(Creature enemy, PlayerChoiceContext ctx)
    {
        if (enemy.HasPower<LibraryStaggerResistancePower>())
        {
            return;
        }

        if (enemy.Monster is not LibraryMonsterModel model)
        {
            return;
        }

        int? stagger = model.DefaultStaggerResistance;
        if (stagger is not { } amount || amount <= 0)
        {
            return;
        }

        await PowerCmd.Apply<LibraryStaggerResistancePower>(ctx, enemy, amount, enemy, null, silent: true);

        LibraryCreatureResistanceData? data = model.DefaultStaggerResistanceData;
        if (data == null)
        {
            return;
        }

        var power = enemy.GetPower<LibraryStaggerResistancePower>();
        if (power != null)
        {
            power.ResistanceData.SlashChaos = data.SlashChaos;
            power.ResistanceData.PierceChaos = data.PierceChaos;
            power.ResistanceData.BluntChaos = data.BluntChaos;
            power.ResistanceData.SlashPhysical = data.SlashPhysical;
            power.ResistanceData.PiercePhysical = data.PiercePhysical;
            power.ResistanceData.BluntPhysical = data.BluntPhysical;
        }
    }
}
