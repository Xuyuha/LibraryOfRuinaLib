using System.Collections.ObjectModel;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace Library.SpeedDice;

public enum LibrarySpeedDiceResourceKind
{
    Energy,
    Stars,
    Light,
    LegacySecondary,
}

public sealed record LibrarySpeedDiceResourceReservation(
    string ResourceId,
    int Amount,
    LibrarySpeedDiceResourceKind Kind);

public sealed class LibrarySpeedDiceReservationPlan
{
    public static LibrarySpeedDiceReservationPlan Empty { get; } =
        new([]);

    private readonly IReadOnlyDictionary<string, int> _secondaryResources;

    public LibrarySpeedDiceReservationPlan(
        IEnumerable<LibrarySpeedDiceResourceReservation> resources)
    {
        ArgumentNullException.ThrowIfNull(resources);
        LibrarySpeedDiceResourceReservation[] ordered = resources
            .Select(resource =>
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(
                    resource.ResourceId);
                if (resource.Amount < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(resources),
                        "Reservation amounts cannot be negative.");
                }

                return resource;
            })
            .Where(resource => resource.Amount > 0)
            .OrderBy(resource => resource.ResourceId, StringComparer.Ordinal)
            .ToArray();

        string? duplicate = ordered
            .GroupBy(
                resource => resource.ResourceId,
                StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicate != null)
        {
            throw new InvalidOperationException(
                $"Duplicate reservation resource id '{duplicate}'.");
        }

        Resources = Array.AsReadOnly(ordered);
        _secondaryResources =
            new ReadOnlyDictionary<string, int>(
                ordered
                    .Where(resource =>
                        resource.Kind
                            is LibrarySpeedDiceResourceKind.Light
                            or LibrarySpeedDiceResourceKind.LegacySecondary)
                    .ToDictionary(
                        resource => resource.ResourceId,
                        resource => resource.Amount,
                        StringComparer.Ordinal));
    }

    public IReadOnlyList<LibrarySpeedDiceResourceReservation> Resources
    {
        get;
    }

    public int ReservedEnergy => GetAmount(
        LibrarySpeedDiceResourceKind.Energy);

    public int ReservedStars => GetAmount(
        LibrarySpeedDiceResourceKind.Stars);

    public IReadOnlyDictionary<string, int> ReservedSecondaryResources =>
        _secondaryResources;

    public int GetAmount(string resourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        return Resources
            .FirstOrDefault(resource =>
                string.Equals(
                    resource.ResourceId,
                    resourceId,
                    StringComparison.Ordinal))
            ?.Amount
            ?? 0;
    }

    public int GetAmount(LibrarySpeedDiceResourceKind kind) =>
        Resources
            .Where(resource => resource.Kind == kind)
            .Sum(resource => resource.Amount);
}

public sealed class LibrarySpeedDiceCardLease
{
    internal LibrarySpeedDiceCardLease(
        string id,
        CardModel card,
        LibrarySpeedDiceReservationPlan reservationPlan,
        LibrarySpeedDiceReservationTransaction transaction)
    {
        Id = id;
        Card = card;
        ReservationPlan = reservationPlan;
        Transaction = transaction;
    }

    public string Id { get; }

    public CardModel Card { get; }

    public LibrarySpeedDiceReservationPlan ReservationPlan { get; }

    public bool IsUseTriggered { get; internal set; }

    public bool IsTargetedUseTriggered { get; internal set; }

    public bool PreventUnequip { get; private set; }

    public bool IsCommitted { get; internal set; }

    public bool IsReleased { get; internal set; }

    internal LibrarySpeedDiceReservationTransaction Transaction { get; }

    public void LockUnequip()
    {
        if (IsReleased)
        {
            throw new InvalidOperationException(
                "A released speed-dice lease cannot be locked.");
        }

        PreventUnequip = true;
    }
}

internal sealed class LibrarySpeedDiceReservationCommitment
{
    public required string ResourceId { get; init; }

    public required Func<Task<bool>> PreflightAsync { get; init; }

    public required Func<Task<bool>> CommitAsync { get; init; }

    public required Func<Task> RollbackAsync { get; init; }

    public Func<Task>? FinalizeAsync { get; init; }

    public Action? Release { get; init; }

    public bool IsCommitted { get; private set; }

    public bool IsRolledBack { get; private set; }

    public async Task<bool> TryCommitAsync()
    {
        if (IsCommitted)
            return true;
        if (IsRolledBack)
            return false;

        if (!await CommitAsync())
            return false;

        IsCommitted = true;
        return true;
    }

    public async Task RollbackOnceAsync()
    {
        if (!IsCommitted || IsRolledBack)
            return;

        // Mark first so an exception cannot cause a second, duplicate refund.
        IsRolledBack = true;
        await RollbackAsync();
        IsCommitted = false;
    }
}

internal sealed class LibrarySpeedDiceReservationTransaction
{
    private readonly LibrarySpeedDiceReservationCommitment[] _commitments;
    private bool _released;

    public LibrarySpeedDiceReservationTransaction(
        IEnumerable<LibrarySpeedDiceReservationCommitment> commitments)
    {
        _commitments = commitments
            .OrderBy(
                commitment => commitment.ResourceId,
                StringComparer.Ordinal)
            .ToArray();

        string? duplicate = _commitments
            .GroupBy(
                commitment => commitment.ResourceId,
                StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicate != null)
        {
            throw new InvalidOperationException(
                $"Duplicate reservation commitment id '{duplicate}'.");
        }
    }

    public async Task<bool> CommitAsync()
    {
        if (_released)
            return false;

        foreach (LibrarySpeedDiceReservationCommitment commitment
                 in _commitments)
        {
            if (!await commitment.PreflightAsync())
                return false;
        }

        var committed = new List<LibrarySpeedDiceReservationCommitment>();
        try
        {
            foreach (LibrarySpeedDiceReservationCommitment commitment
                     in _commitments)
            {
                if (!await commitment.TryCommitAsync())
                {
                    await RollbackBestEffortAsync(committed);
                    return false;
                }

                committed.Add(commitment);
            }
        }
        catch (Exception exception)
        {
            Log.Error(
                "[LibraryOfRuinaLib] Reservation commit failed: "
                + exception);
            await RollbackBestEffortAsync(committed);
            throw;
        }

        // Resource writes are now committed. Finalizers notify history/hooks
        // and may be irreversible, so a notification failure must never refund
        // already-spent resources or block the card that paid them.
        _released = true;
        foreach (LibrarySpeedDiceReservationCommitment commitment
                 in _commitments)
        {
            if (commitment.FinalizeAsync == null)
                continue;

            try
            {
                await commitment.FinalizeAsync();
            }
            catch (Exception exception)
            {
                Log.Error(
                    "[LibraryOfRuinaLib] Reservation finalizer failed for "
                    + $"'{commitment.ResourceId}': {exception}");
            }
        }

        return true;
    }

    public void Release()
    {
        if (_released)
            return;

        _released = true;
        foreach (LibrarySpeedDiceReservationCommitment commitment
                 in _commitments)
        {
            commitment.Release?.Invoke();
        }
    }

    private static async Task RollbackBestEffortAsync(
        IReadOnlyList<LibrarySpeedDiceReservationCommitment> committed)
    {
        for (int index = committed.Count - 1; index >= 0; index--)
        {
            try
            {
                await committed[index].RollbackOnceAsync();
            }
            catch (Exception exception)
            {
                Log.Error(
                    "[LibraryOfRuinaLib] Reservation rollback failed for "
                    + $"'{committed[index].ResourceId}': {exception}");
            }
        }
    }
}
