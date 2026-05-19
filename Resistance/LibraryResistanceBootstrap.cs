#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Library.Resistance.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Library.Resistance;

internal static class LibraryResistanceBootstrap
{
    internal static async Task EnsureOnEnemy(Creature enemy, PlayerChoiceContext? choiceContext = null)
    {
        if (!enemy.IsEnemy || enemy.CombatState == null || !enemy.IsAlive)
        {
            return;
        }

        if (!LibraryResistance.CombatHasRelevantParticipant(enemy.CombatState))
        {
            return;
        }

        PlayerChoiceContext ctx = LibraryCombatChoiceContexts.Resolve(choiceContext, enemy.CombatState);

        if (!enemy.HasPower<LibrarySlashResistancePower>())
        {
            await PowerCmd.Apply<LibrarySlashResistancePower>(ctx, enemy, 100m, enemy, null, silent: true);
        }

        if (!enemy.HasPower<LibraryBluntResistancePower>())
        {
            await PowerCmd.Apply<LibraryBluntResistancePower>(ctx, enemy, 100m, enemy, null, silent: true);
        }

        if (!enemy.HasPower<LibraryPierceResistancePower>())
        {
            await PowerCmd.Apply<LibraryPierceResistancePower>(ctx, enemy, 100m, enemy, null, silent: true);
        }
    }
}

internal static class LibraryResistanceBundle
{
    internal static void ApplyDeltaAllKinds(Creature enemy, decimal delta)
    {
        if (!enemy.IsEnemy || !enemy.IsAlive)
        {
            return;
        }

        enemy.GetPower<LibrarySlashResistancePower>()?.ApplyCoefficientDelta(delta);
        enemy.GetPower<LibraryBluntResistancePower>()?.ApplyCoefficientDelta(delta);
        enemy.GetPower<LibraryPierceResistancePower>()?.ApplyCoefficientDelta(delta);
        if (enemy.CombatState != null)
        {
            LibraryResistance.NotifyCoefficientsChanged(enemy.CombatState);
        }
    }

    internal static void SetAllKindsAbsolute(Creature enemy, decimal coefficient)
    {
        if (!enemy.IsEnemy || !enemy.IsAlive)
        {
            return;
        }

        enemy.GetPower<LibrarySlashResistancePower>()?.SetCoefficientAbsolute(coefficient);
        enemy.GetPower<LibraryBluntResistancePower>()?.SetCoefficientAbsolute(coefficient);
        enemy.GetPower<LibraryPierceResistancePower>()?.SetCoefficientAbsolute(coefficient);
    }
}
