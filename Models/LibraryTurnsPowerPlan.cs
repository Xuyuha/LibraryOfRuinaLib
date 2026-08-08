namespace LibraryLib.Models;

internal readonly record struct LibraryTurnsPowerPlanResolution(
    int Amount,
    bool ShouldRemove,
    int ExpiredEntryCount);

internal static class LibraryTurnsPowerPlan
{
    private const int MinPowerAmount = -999999999;
    private const int MaxPowerAmount = 999999999;

    internal static LibraryTurnsPowerPlanResolution ExpireDueEntries(
        SortedDictionary<int, int> amountPlan,
        int currentRound,
        int currentAmount,
        bool allowNegative)
    {
        ArgumentNullException.ThrowIfNull(amountPlan);

        var expiredAmount = 0;
        var expiredEntryCount = 0;
        while (amountPlan.Count > 0)
        {
            var first = amountPlan.First();
            if (first.Key > currentRound)
            {
                break;
            }

            expiredAmount += first.Value;
            amountPlan.Remove(first.Key);
            expiredEntryCount++;
        }

        var rawAmount = (long)currentAmount - expiredAmount;
        var nextAmount = (int)Math.Clamp(
            rawAmount,
            MinPowerAmount,
            MaxPowerAmount);
        if (!allowNegative && nextAmount < 0)
        {
            nextAmount = 0;
        }

        var shouldRemove = allowNegative
            ? nextAmount == 0
            : nextAmount <= 0;
        if (shouldRemove)
        {
            amountPlan.Clear();
        }

        return new LibraryTurnsPowerPlanResolution(
            nextAmount,
            shouldRemove,
            expiredEntryCount);
    }
}
