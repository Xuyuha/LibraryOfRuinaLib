using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace LibraryLib.Combat;

// Adapted from LibraryOfRuina.framework.combat.TurnParticipants (see THIRD_PARTY_NOTICES).
internal static class LibraryTurnParticipants
{
    // Player pets follow their owner. A teammate's extra turn must not tick this power.
    internal static bool IsOwnTurn(Creature creature, CombatSide side, IEnumerable<Creature> participants)
    {
        if (creature.Side != side)
            return false;
        if (side != CombatSide.Player)
            return true;

        Creature anchor = creature.PetOwner?.Creature ?? creature;
        return anchor.IsPlayer
            ? participants.Contains(anchor)
            : CombatManager.Instance.PlayersTakingExtraTurn.Count == 0;
    }
}
