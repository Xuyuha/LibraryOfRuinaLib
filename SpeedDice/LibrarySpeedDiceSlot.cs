using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace Library.SpeedDice;

public sealed class LibrarySpeedDiceSlot
{
    private readonly Dictionary<string, int> _reservedSecondaryResources =
        new(StringComparer.Ordinal);
    private LibrarySpeedDiceCardLease? _lease;

    internal LibrarySpeedDiceSlot(int index, int initialDisplayValue)
    {
        Index = index;
        DisplayValue = initialDisplayValue;
    }

    public int Index { get; }

    public int DisplayValue { get; internal set; }

    public int FinalValue { get; internal set; }

    public bool IsLocked { get; internal set; }

    public bool IsSpent { get; internal set; }

    public CardModel? Card { get; internal set; }

    public Creature? Target { get; internal set; }

    public bool RequiresTarget =>
        Card?.GetSpeedDiceTargetType() is TargetType.AnyEnemy or TargetType.AnyAlly;

    public bool HasValidTarget =>
        !RequiresTarget
        || Target is { IsAlive: true }
        && Card?.IsValidSpeedDiceTarget(Target) == true;

    public LibrarySpeedDiceCardLease? Lease => _lease;

    public int ReservedEnergy =>
        _lease?.ReservationPlan.ReservedEnergy ?? 0;

    public int ReservedStars =>
        _lease?.ReservationPlan.ReservedStars ?? 0;

    public IReadOnlyDictionary<string, int> ReservedSecondaryResources =>
        _lease?.ReservationPlan.ReservedSecondaryResources
        ?? _reservedSecondaryResources;

    public void SetSecondaryResourceReservation(string resourceId, int amount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        if (_lease != null)
        {
            throw new InvalidOperationException(
                "An equipped slot has an immutable reservation plan.");
        }

        if (amount <= 0)
            _reservedSecondaryResources.Remove(resourceId);
        else
            _reservedSecondaryResources[resourceId] = amount;
    }

    public int GetSecondaryResourceReservation(string resourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        return _reservedSecondaryResources.GetValueOrDefault(resourceId);
    }

    public void ClearSecondaryResourceReservations()
    {
        _reservedSecondaryResources.Clear();
    }

    internal void ClearReservation()
    {
        _lease?.Transaction.Release();
        if (_lease != null)
        {
            _lease.IsReleased = true;
            _lease = null;
        }
        ClearSecondaryResourceReservations();
    }

    internal void SetLease(LibrarySpeedDiceCardLease lease)
    {
        ArgumentNullException.ThrowIfNull(lease);
        if (_lease != null)
        {
            throw new InvalidOperationException(
                "The speed-dice slot already owns a lease.");
        }

        _reservedSecondaryResources.Clear();
        _lease = lease;
    }

    internal void ClearCard()
    {
        Card = null;
        Target = null;
        ClearReservation();
    }
}
