#nullable enable
using System.Threading.Tasks;
using Library.Entities.Creatures;
using Library.Models;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Library.Resistance;

internal static class LibraryResistanceBootstrap
{
    internal static Task EnsureOnEnemy(Creature enemy, PlayerChoiceContext? choiceContext = null)
    {
        if (!enemy.IsEnemy || enemy.CombatState == null || !enemy.IsAlive)
        {
            return Task.CompletedTask;
        }

        if (enemy.Monster is not LibraryMonsterModel)
        {
            return Task.CompletedTask;
        }

        // chao 值和 ResistanceData 已在 LibraryCreature 构造函数 + ChaoSyncPatch 中初始化
        return Task.CompletedTask;
    }
}
