#nullable enable
using System;
using HarmonyLib;
using Library.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace Library.Patches;

[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.DamageReceived))]
internal static class CombatHistoryDamageReceivedSafetyPatch
{
    private static Exception? Finalizer(
        Exception? __exception,
        Creature? receiver,
        Creature? dealer,
        DamageResult? result,
        CardModel? cardSource)
    {
        if (__exception == null)
            return null;

        if (!ShouldSuppress(__exception, receiver, result, cardSource))
            return __exception;

        var damageResult = result!;
        try
        {
            Log.Warn(
                "[LibraryOfRuinaLib] Suppressed CombatHistory.DamageReceived NullReferenceException " +
                $"for non-card or cross-assembly Library damage. receiver={GetCreatureId(receiver)}, " +
                $"dealer={GetCreatureId(dealer)}, cardSource={GetCardSourceId(cardSource)}, " +
                $"damage={damageResult.UnblockedDamage}, props={damageResult.Props}");
        }
        catch
        {
            
        }

        return null;
    }

    private static bool ShouldSuppress(
        Exception exception,
        Creature? receiver,
        DamageResult? result,
        CardModel? cardSource)
    {
        return exception is NullReferenceException
               && receiver is LibraryCreature { IsMonster: true }
               && result != null
               && ReferenceEquals(result.Receiver, receiver)
               && IsNonOwnedCardSource(receiver, cardSource);
    }

    private static bool IsNonOwnedCardSource(Creature receiver, CardModel? cardSource)
    {
        if (cardSource == null)
            return true;

        return !ReferenceEquals(
            cardSource.GetType().Assembly,
            receiver.Monster?.GetType().Assembly);
    }

    private static string GetCreatureId(Creature? creature)
    {
        if (creature == null)
            return "null";

        if (creature.IsMonster)
            return creature.Monster?.Id.Entry ?? "unknown-monster";

        if (creature.IsPlayer)
            return creature.Player?.Character.Id.Entry ?? "unknown-player";

        return creature.GetType().Name;
    }

    private static string GetCardSourceId(CardModel? cardSource)
    {
        return cardSource?.Id.Entry ?? "null";
    }
}
