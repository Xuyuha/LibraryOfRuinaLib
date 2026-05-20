#nullable enable
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Library.Resistance;

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
                return ForCombatHook(player);
            }
        }
        return new BlockingPlayerChoiceContext();
    }
}
