using System.Runtime.CompilerServices;
using Library.SpeedDice;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace Library.Light;

public static class LibraryLight
{
    public const string DefaultResourceId = "library.light";

    private static readonly ConditionalWeakTable<CardModel, LibraryLightCost> _costs =
        new();

    public static LibraryLightCost GetCost(CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);
        if (card is not ILibraryLightCard lightCard)
        {
            throw new InvalidOperationException(
                $"Card '{card.GetType().FullName}' does not implement "
                + nameof(ILibraryLightCard)
                + ".");
        }

        return _costs.GetValue(
            card,
            owner => new LibraryLightCost(
                owner,
                lightCard.BaseLightCost,
                lightCard.HasLightCostX));
    }

    public static bool TryGetCost(
        CardModel? card,
        out LibraryLightCost? cost)
    {
        cost = null;
        if (card is not ILibraryLightCard)
            return false;
        cost = GetCost(card);
        return true;
    }

    public static bool TryGetState(
        Player? player,
        out LibraryLightState? state)
    {
        state = null;
        return player != null
            && LibrarySpeedDiceService.TryGetState(
                player,
                out LibrarySpeedDiceCombatState? speedState)
            && speedState?.Light != null
            && (state = speedState.Light) != null;
    }

    internal static int ModifyCost(CardModel card, int current)
    {
        if (card.Owner == null
            || !LibrarySpeedDiceService.TryGetState(
                card.Owner,
                out LibrarySpeedDiceCombatState? state)
            || state == null)
        {
            return current;
        }

        return state.Registration.Dispatcher.ModifyLightCost(
            card,
            current);
    }

    internal static void CopyCost(CardModel source, CardModel destination)
    {
        if (!TryGetCost(source, out LibraryLightCost? sourceCost)
            || sourceCost == null)
        {
            return;
        }

        _costs.Remove(destination);
        _costs.Add(destination, sourceCost.Clone(destination));
    }

    internal static void EndOfTurnCleanup(CardModel card)
    {
        if (TryGetCost(card, out LibraryLightCost? cost))
            cost!.EndOfTurnCleanup();
    }

    internal static void AfterCardPlayedCleanup(CardModel card)
    {
        if (TryGetCost(card, out LibraryLightCost? cost))
            cost!.AfterCardPlayedCleanup();
    }

    internal static void FinalizeUpgrade(CardModel card)
    {
        if (TryGetCost(card, out LibraryLightCost? cost))
            cost!.FinalizeUpgrade();
    }

    internal static void ResetForDowngrade(CardModel card)
    {
        if (TryGetCost(card, out LibraryLightCost? cost))
            cost!.ResetForDowngrade();
    }

    internal static void ClearCombatCosts()
    {
        foreach (LibraryLightCost cost in
                 _costs.Select(pair => pair.Value).ToArray())
        {
            cost.EndOfCombatCleanup();
        }
    }
}
