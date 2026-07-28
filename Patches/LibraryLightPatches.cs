using System.Reflection;
using HarmonyLib;
using Library.Light;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Library.Patches;

[HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.MutableClone))]
internal static class LibraryLightClonePatch
{
    private static void Postfix(
        AbstractModel __instance,
        AbstractModel __result)
    {
        if (__instance is CardModel source
            && __result is CardModel destination)
        {
            LibraryLight.CopyCost(source, destination);
        }
    }
}

internal static class LibraryLightCardEnergyCostOwner
{
    private static readonly FieldInfo CardField =
        AccessTools.Field(typeof(CardEnergyCost), "_card")
        ?? throw new MissingFieldException(
            typeof(CardEnergyCost).FullName,
            "_card");

    public static CardModel Get(CardEnergyCost cost) =>
        (CardModel)CardField.GetValue(cost)!;
}

[HarmonyPatch(
    typeof(CardEnergyCost),
    nameof(CardEnergyCost.EndOfTurnCleanup))]
internal static class LibraryLightEndOfTurnCleanupPatch
{
    private static void Postfix(CardEnergyCost __instance)
    {
        LibraryLight.EndOfTurnCleanup(
            LibraryLightCardEnergyCostOwner.Get(__instance));
    }
}

[HarmonyPatch(
    typeof(CardEnergyCost),
    nameof(CardEnergyCost.AfterCardPlayedCleanup))]
internal static class LibraryLightAfterPlayedCleanupPatch
{
    private static void Postfix(CardEnergyCost __instance)
    {
        LibraryLight.AfterCardPlayedCleanup(
            LibraryLightCardEnergyCostOwner.Get(__instance));
    }
}

[HarmonyPatch(
    typeof(CardEnergyCost),
    nameof(CardEnergyCost.FinalizeUpgrade))]
internal static class LibraryLightFinalizeUpgradePatch
{
    private static void Postfix(CardEnergyCost __instance)
    {
        LibraryLight.FinalizeUpgrade(
            LibraryLightCardEnergyCostOwner.Get(__instance));
    }
}

[HarmonyPatch(
    typeof(CardEnergyCost),
    nameof(CardEnergyCost.ResetForDowngrade))]
internal static class LibraryLightResetForDowngradePatch
{
    private static void Postfix(CardEnergyCost __instance)
    {
        LibraryLight.ResetForDowngrade(
            LibraryLightCardEnergyCostOwner.Get(__instance));
    }
}
