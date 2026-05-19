#nullable enable
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Library.Resistance;

/// <summary>联机：挂 power / 改系数时优先沿用调用方上下文，否则为参与方玩家的 Hook 上下文。</summary>
public static class LibraryCombatChoiceContexts
{
    public static PlayerChoiceContext ForCombatHook(Player player)
    {
        if (!LocalContext.NetId.HasValue)
        {
            return new BlockingPlayerChoiceContext();
        }

        return new HookPlayerChoiceContext(player, player.NetId, GameActionType.Combat);
    }

    /// <summary>已在 PlayCard / 伤害栈内静默改层时用，避免嵌套 Hook 入队。</summary>
    public static PlayerChoiceContext ForSilentModify() => new BlockingPlayerChoiceContext();

    public static PlayerChoiceContext Resolve(PlayerChoiceContext? choiceContext, ICombatState? combatState)
    {
        if (choiceContext != null)
        {
            return choiceContext;
        }

        if (combatState != null)
        {
            foreach (Player player in combatState.Players)
            {
                if (LibraryResistance.IsRelevantPlayer(player))
                {
                    return ForCombatHook(player);
                }
            }
        }
        return new BlockingPlayerChoiceContext();
    }
}
