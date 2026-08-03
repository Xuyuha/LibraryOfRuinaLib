using LibraryLib.SpeedDice;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace LibraryLib.Light;

public sealed record LibraryLightStateSnapshot(
    int Current,
    int PermanentMaximumModifier,
    int TemporaryMaximumModifier,
    int LastEmotionLevel,
    int LastRecoveredTurn,
    IReadOnlyDictionary<string, int> Reservations);

public sealed class LibraryLightState
{
    private readonly record struct StoreMutationOutcome(
        long Revision,
        LibraryLightStoreMutationResult Result);

    private readonly Dictionary<string, int> _reservations =
        new(StringComparer.Ordinal);
    private readonly HashSet<string> _committingReservations =
        new(StringComparer.Ordinal);
    private readonly HashSet<string> _restoringReservations =
        new(StringComparer.Ordinal);
    private readonly object _sync = new();
    private readonly LibrarySpeedDiceCombatState _speedState;
    private readonly ILibraryLightStore _store;
    private int _current;
    private int _permanentMaximumModifier;
    private int _temporaryMaximumModifier;
    private int _lastEmotionLevel;
    private int _lastRecoveredTurn = -1;
    private int _knownMaximum;
    private long _latestStoreMutationRevision;

    internal LibraryLightState(
        LibrarySpeedDiceCombatState speedState,
        LibraryLightOptions options,
        ILibraryLightStore store)
    {
        _speedState = speedState;
        Options = options;
        _store = store;
        _current = store.TryRead(out LibraryLightStoreSnapshot snapshot)
            ? Math.Max(0, snapshot.Current)
            : options.Starting;
        _lastEmotionLevel = speedState.Emotion.Level;
        _knownMaximum = Maximum;
        _store.Changed += HandleStoreChanged;
    }

    public Player Player => _speedState.Player;

    public LibraryLightOptions Options { get; }

    public int Current => Volatile.Read(ref _current);

    public int Maximum
    {
        get
        {
            int maximum = LibraryLightRules.CalculateBaseMaximum(
                Options,
                _speedState.Emotion.Level,
                _permanentMaximumModifier,
                _temporaryMaximumModifier);
            return Math.Max(
                0,
                _speedState.Registration.Dispatcher
                    .ModifyMaximumLight(this, maximum));
        }
    }

    public int Reserved
    {
        get
        {
            lock (_sync)
                return GetReservedLocked();
        }
    }

    public int Available
    {
        get
        {
            lock (_sync)
                return Math.Max(0, _current - GetReservedLocked());
        }
    }

    public string ReservationResourceId =>
        _store is ILibraryLightStoreIdentity identity
            ? identity.ResourceId
            : LibraryLight.DefaultResourceId;

    public event Action? Changed;

    public event Action<int, int>? CurrentChanged;

    public event Action<int, int>? MaximumChanged;

    public event Action<int, int>? ReservationChanged;

    public bool HasEnoughAvailable(int amount) =>
        Math.Max(0, amount) <= Available;

    public Task Gain(int amount, AbstractModel? source = null) =>
        ChangeCurrent(
            Math.Max(0, amount),
            LibraryLightStoreMutationKind.Gain,
            source);

    public Task Lose(int amount, AbstractModel? source = null) =>
        ChangeCurrent(
            Math.Max(0, amount),
            LibraryLightStoreMutationKind.Lose,
            source);

    public async Task Set(int value, AbstractModel? source = null)
    {
        SynchronizeFromStore();
        int target;
        lock (_sync)
        {
            int reservationFloor = Math.Min(
                _current,
                GetReservedLocked());
            int ceiling = _speedState.Registration.Dispatcher
                .AllowLightOverflow(this)
                    ? int.MaxValue
                    : Math.Max(Maximum, reservationFloor);
            target =
                Math.Max(
                    reservationFloor,
                    Math.Min(ceiling, Math.Max(0, value)));
        }

        await MutateCore(
            new LibraryLightStoreMutation(
                LibraryLightStoreMutationKind.Set,
                target),
            target,
            source);
    }

    public async Task Reset(AbstractModel? source = null)
    {
        SynchronizeFromStore();
        int target;
        LibraryLightStoreMutationKind kind;
        lock (_sync)
        {
            target = Math.Max(
                Math.Min(_current, GetReservedLocked()),
                Maximum);
            kind = target == Maximum
                ? LibraryLightStoreMutationKind.ResetToMaximum
                : LibraryLightStoreMutationKind.Set;
        }

        await MutateCore(
            new LibraryLightStoreMutation(kind, target),
            target,
            source);
    }

    public async Task Recover(
        int previousEmotionLevel,
        int currentEmotionLevel,
        AbstractModel? source = null)
    {
        int turn = Player.PlayerCombatState?.TurnNumber ?? -1;
        if (_lastRecoveredTurn == turn)
            return;

        _lastRecoveredTurn = turn;
        LibraryLightRecoveryPlan plan =
            LibraryLightRules.CreateRecoveryPlan(
                Options,
                previousEmotionLevel,
                currentEmotionLevel,
                _lastEmotionLevel,
                _speedState.Registration.Dispatcher.ModifyTurnRecovery(
                    this,
                    Options.RecoveryPerTurn),
                _speedState.Registration.Dispatcher
                    .ShouldRecoverLightForTurn(this));
        _lastEmotionLevel = plan.LastEmotionLevel;

        if (!plan.ShouldRecover)
        {
            return;
        }

        if (plan.ShouldRefill)
            await Reset(source);
        else
            await Gain(plan.RecoveryAmount, source);
    }

    public Task ModifyMaximum(
        int amount,
        bool temporary = false,
        AbstractModel? source = null) =>
        ModifyMaximumCore(
            amount,
            temporary,
            gainCurrent: false,
            source: source);

    public Task ModifyMaximumAndGain(
        int amount,
        bool temporary = false,
        AbstractModel? source = null) =>
        ModifyMaximumCore(
            amount,
            temporary,
            gainCurrent: true,
            source: source);

    private async Task ModifyMaximumCore(
        int amount,
        bool temporary,
        bool gainCurrent,
        AbstractModel? source)
    {
        if (amount == 0)
            return;

        int previousMaximum = Maximum;
        if (temporary)
        {
            _temporaryMaximumModifier = checked(
                _temporaryMaximumModifier + amount);
        }
        else
        {
            _permanentMaximumModifier = checked(
                _permanentMaximumModifier + amount);
        }

        int currentMaximum = Maximum;
        _knownMaximum = currentMaximum;
        InvokeSafely(
            MaximumChanged,
            previousMaximum,
            currentMaximum,
            nameof(MaximumChanged));
        InvokeSafely(Changed, nameof(Changed));
        _speedState.NotifyGameplayChanged();

        int maximumIncrease = Math.Max(
            0,
            currentMaximum - previousMaximum);
        if (gainCurrent && maximumIncrease > 0)
            await Gain(maximumIncrease, source);
        else if (Current > currentMaximum && Reserved <= currentMaximum)
            await SetCore(currentMaximum, source);
    }

    public async Task ClearTemporaryMaximum(
        AbstractModel? source = null)
    {
        if (_temporaryMaximumModifier == 0)
            return;

        int previousMaximum = Maximum;
        _temporaryMaximumModifier = 0;
        int currentMaximum = Maximum;
        _knownMaximum = currentMaximum;
        InvokeSafely(
            MaximumChanged,
            previousMaximum,
            currentMaximum,
            nameof(MaximumChanged));
        InvokeSafely(Changed, nameof(Changed));
        _speedState.NotifyGameplayChanged();
        if (Current > currentMaximum && Reserved <= currentMaximum)
            await SetCore(currentMaximum, source);
    }

    public LibraryLightStateSnapshot CreateSnapshot()
    {
        lock (_sync)
        {
            return new LibraryLightStateSnapshot(
                _current,
                _permanentMaximumModifier,
                _temporaryMaximumModifier,
                _lastEmotionLevel,
                _lastRecoveredTurn,
                _reservations.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value,
                    StringComparer.Ordinal));
        }
    }

    public void Restore(LibraryLightStateSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        int previousCurrent;
        int previousMaximum = Maximum;
        int previousReserved;
        lock (_sync)
        {
            previousCurrent = _current;
            previousReserved = GetReservedLocked();
            _permanentMaximumModifier =
                snapshot.PermanentMaximumModifier;
            _temporaryMaximumModifier =
                snapshot.TemporaryMaximumModifier;
            _lastEmotionLevel = Math.Max(0, snapshot.LastEmotionLevel);
            _lastRecoveredTurn = snapshot.LastRecoveredTurn;
            _reservations.Clear();
            _committingReservations.Clear();
            _restoringReservations.Clear();
            foreach ((string id, int amount) in snapshot.Reservations)
            {
                if (!string.IsNullOrWhiteSpace(id) && amount > 0)
                    _reservations[id] = amount;
            }

            _current = Math.Max(0, snapshot.Current);
        }

        try
        {
            _store.Restore(new LibraryLightStoreSnapshot(Current));
        }
        catch (Exception exception)
        {
            Log.Error(
                "[LibraryOfRuinaLib] Light store restore notification failed: "
                + exception);
        }

        if (_store.TryRead(out LibraryLightStoreSnapshot authoritative))
        {
            lock (_sync)
                _current = Math.Max(0, authoritative.Current);
        }

        RaiseChanges(
            previousCurrent,
            previousMaximum,
            previousReserved);
    }

    internal bool TryReserve(string leaseId, int amount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseId);
        amount = Math.Max(0, amount);
        if (amount == 0)
            return true;

        int previous;
        int current;
        lock (_sync)
        {
            if (_committingReservations.Contains(leaseId)
                || _restoringReservations.Contains(leaseId))
            {
                return false;
            }

            previous = GetReservedLocked();
            int oldAmount = _reservations.GetValueOrDefault(leaseId);
            int delta = amount - oldAmount;
            int available = Math.Max(0, _current - previous);
            if (delta > available)
                return false;

            _reservations[leaseId] = amount;
            current = GetReservedLocked();
        }

        InvokeSafely(
            ReservationChanged,
            previous,
            current,
            nameof(ReservationChanged));
        InvokeSafely(Changed, nameof(Changed));
        _speedState.NotifyGameplayChanged();
        return true;
    }

    internal bool HasReservation(string leaseId, int amount)
    {
        lock (_sync)
        {
            return _reservations.GetValueOrDefault(leaseId)
                == Math.Max(0, amount);
        }
    }

    internal void RefreshMaximum()
    {
        int currentMaximum = Maximum;
        if (_knownMaximum == currentMaximum)
            return;

        int previousMaximum = _knownMaximum;
        _knownMaximum = currentMaximum;
        InvokeSafely(
            MaximumChanged,
            previousMaximum,
            currentMaximum,
            nameof(MaximumChanged));
        InvokeSafely(Changed, nameof(Changed));
    }

    internal void ReleaseReservation(string leaseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseId);
        int previous;
        int current;
        lock (_sync)
        {
            if (_committingReservations.Contains(leaseId)
                || _restoringReservations.Contains(leaseId))
            {
                return;
            }

            previous = GetReservedLocked();
            if (!_reservations.Remove(leaseId))
                return;
            current = GetReservedLocked();
        }

        InvokeSafely(
            ReservationChanged,
            previous,
            current,
            nameof(ReservationChanged));
        InvokeSafely(Changed, nameof(Changed));
        _speedState.NotifyGameplayChanged();
    }

    internal async Task<bool> CommitReservation(
        string leaseId,
        int amount,
        AbstractModel? source = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseId);
        amount = Math.Max(0, amount);
        if (amount == 0)
            return true;

        SynchronizeFromStore();
        int previousCurrent;
        lock (_sync)
        {
            if (_committingReservations.Contains(leaseId)
                || _restoringReservations.Contains(leaseId)
                || _reservations.GetValueOrDefault(leaseId) != amount
                || _current < amount)
            {
                return false;
            }

            _committingReservations.Add(leaseId);
            previousCurrent = _current;
        }

        try
        {
            StoreMutationOutcome outcome =
                await MutateStoreAsync(
                    new LibraryLightStoreMutation(
                        LibraryLightStoreMutationKind.Spend,
                        amount,
                        source as CardModel),
                    previousCurrent - amount,
                    source);
            LibraryLightStoreMutationResult result = outcome.Result;

            int previousProjectedCurrent;
            int previousReserved;
            int currentReserved;
            bool reservationRemoved = false;
            lock (_sync)
            {
                previousProjectedCurrent = _current;
                if (outcome.Revision == _latestStoreMutationRevision)
                    _current = Math.Max(0, result.Snapshot.Current);
                previousReserved = GetReservedLocked();
                if (result.Succeeded
                    && _reservations.GetValueOrDefault(leaseId) == amount)
                {
                    reservationRemoved = _reservations.Remove(leaseId);
                }
                currentReserved = GetReservedLocked();
            }

            if (previousProjectedCurrent != Current)
            {
                InvokeSafely(
                    CurrentChanged,
                    previousProjectedCurrent,
                    Current,
                    nameof(CurrentChanged));
            }
            if (previousReserved != currentReserved)
            {
                InvokeSafely(
                    ReservationChanged,
                    previousReserved,
                    currentReserved,
                    nameof(ReservationChanged));
            }
            if (previousProjectedCurrent != Current
                || previousReserved != currentReserved)
            {
                InvokeSafely(Changed, nameof(Changed));
                _speedState.NotifyGameplayChanged();
            }

            return result.Succeeded && reservationRemoved;
        }
        finally
        {
            lock (_sync)
                _committingReservations.Remove(leaseId);
        }
    }

    internal async Task<bool> RestoreCommittedReservation(
        string leaseId,
        int amount,
        AbstractModel? source = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseId);
        amount = Math.Max(0, amount);
        if (amount == 0)
            return true;

        SynchronizeFromStore();
        int target;
        lock (_sync)
        {
            if (_committingReservations.Contains(leaseId)
                || _restoringReservations.Contains(leaseId))
            {
                return false;
            }

            _restoringReservations.Add(leaseId);
            target = checked(_current + amount);
        }

        try
        {
            StoreMutationOutcome outcome =
                await MutateStoreAsync(
                    new LibraryLightStoreMutation(
                        LibraryLightStoreMutationKind.Set,
                        target),
                    target,
                    source);
            LibraryLightStoreMutationResult result = outcome.Result;
            int previousCurrent;
            int previousReserved;
            int currentReserved;
            bool restored;
            lock (_sync)
            {
                previousCurrent = _current;
                bool isLatest =
                    outcome.Revision == _latestStoreMutationRevision;
                if (isLatest)
                    _current = Math.Max(0, result.Snapshot.Current);
                previousReserved = GetReservedLocked();
                restored = isLatest
                    && result.Succeeded
                    && _current == target;
                if (restored && amount > 0)
                    _reservations[leaseId] = amount;
                currentReserved = GetReservedLocked();
            }

            if (previousCurrent != Current)
            {
                InvokeSafely(
                    CurrentChanged,
                    previousCurrent,
                    Current,
                    nameof(CurrentChanged));
            }
            if (previousReserved != currentReserved)
            {
                InvokeSafely(
                    ReservationChanged,
                    previousReserved,
                    currentReserved,
                    nameof(ReservationChanged));
            }
            if (previousCurrent != Current
                || previousReserved != currentReserved)
            {
                InvokeSafely(Changed, nameof(Changed));
                _speedState.NotifyGameplayChanged();
            }

            return restored;
        }
        finally
        {
            lock (_sync)
                _restoringReservations.Remove(leaseId);
        }
    }

    private async Task ChangeCurrent(
        int amount,
        LibraryLightStoreMutationKind kind,
        AbstractModel? source)
    {
        if (amount == 0)
            return;

        SynchronizeFromStore();
        int target;
        int mutationAmount;
        int? ceiling = null;
        lock (_sync)
        {
            if (kind == LibraryLightStoreMutationKind.Gain)
            {
                ceiling =
                    _speedState.Registration.Dispatcher
                         .AllowLightOverflow(this)
                         ? int.MaxValue
                         : Math.Max(
                             Maximum,
                             Math.Min(
                                 _current,
                                 GetReservedLocked()));
                target = Math.Min(
                    ceiling.Value,
                    checked(_current + amount));
                mutationAmount = Math.Max(0, target - _current);
            }
            else
            {
                int reserved = GetReservedLocked();
                target = _current < reserved
                    ? _current
                    : Math.Max(reserved, _current - amount);
                mutationAmount = Math.Max(0, _current - target);
            }
        }

        if (mutationAmount == 0)
            return;

        await MutateCore(
            new LibraryLightStoreMutation(kind, mutationAmount),
            target,
            source,
            ceiling);
    }

    private Task SetCore(int target, AbstractModel? source) =>
        MutateCore(
            new LibraryLightStoreMutation(
                LibraryLightStoreMutationKind.Set,
                target),
            target,
            source);

    private async Task MutateCore(
        LibraryLightStoreMutation mutation,
        int fallbackTarget,
        AbstractModel? source,
        int? ceiling = null)
    {
        int previous = Current;
        if (mutation.Kind == LibraryLightStoreMutationKind.Set
            && previous == fallbackTarget)
        {
            return;
        }

        StoreMutationOutcome outcome =
            await MutateStoreAsync(
                mutation,
                fallbackTarget,
                source);
        if (ceiling.HasValue
            && outcome.Result.Snapshot.Current > ceiling.Value)
        {
            outcome = await MutateStoreAsync(
                new LibraryLightStoreMutation(
                    LibraryLightStoreMutationKind.Set,
                    ceiling.Value),
                ceiling.Value,
                    source);
        }

        int projectedPrevious;
        int projectedCurrent;
        lock (_sync)
        {
            projectedPrevious = _current;
            if (outcome.Revision == _latestStoreMutationRevision)
            {
                _current = Math.Max(
                    0,
                    outcome.Result.Snapshot.Current);
            }
            projectedCurrent = _current;
        }

        if (projectedPrevious == projectedCurrent)
            return;

        InvokeSafely(
            CurrentChanged,
            projectedPrevious,
            projectedCurrent,
            nameof(CurrentChanged));
        InvokeSafely(Changed, nameof(Changed));
        _speedState.NotifyGameplayChanged();
    }

    private async Task<StoreMutationOutcome> MutateStoreAsync(
        LibraryLightStoreMutation mutation,
        int fallbackTarget,
        AbstractModel? source)
    {
        long revision;
        lock (_sync)
        {
            revision = checked(++_latestStoreMutationRevision);
        }

        int previous = Current;
        LibraryLightStoreMutationResult result;
        try
        {
            if (_store is ILibraryLightCommandStore commandStore)
            {
                result = await commandStore.MutateAsync(
                    mutation,
                    source);
            }
            else
            {
                bool succeeded = mutation.Kind
                    != LibraryLightStoreMutationKind.Spend
                    || Current >= mutation.Amount;
                if (succeeded)
                {
                    await _store.WriteAsync(
                        new LibraryLightStoreSnapshot(fallbackTarget),
                        source);
                }

                result = new LibraryLightStoreMutationResult(
                    succeeded,
                    new LibraryLightStoreSnapshot(
                        succeeded ? fallbackTarget : Current));
            }
        }
        catch (Exception exception)
        {
            LibraryLightStoreSnapshot snapshot =
                _store.TryRead(out LibraryLightStoreSnapshot authoritative)
                    ? authoritative
                    : new LibraryLightStoreSnapshot(Current);
            bool succeeded = mutation.Kind switch
            {
                LibraryLightStoreMutationKind.Set
                    or LibraryLightStoreMutationKind.ResetToMaximum =>
                    snapshot.Current == fallbackTarget,
                LibraryLightStoreMutationKind.Gain =>
                    snapshot.Current >= fallbackTarget
                    && snapshot.Current > previous,
                LibraryLightStoreMutationKind.Lose
                    or LibraryLightStoreMutationKind.Spend =>
                    snapshot.Current <= fallbackTarget
                    && snapshot.Current < previous,
                _ => false,
            };
            result = new LibraryLightStoreMutationResult(
                succeeded,
                snapshot,
                exception);
        }

        if (_store.TryRead(out LibraryLightStoreSnapshot finalSnapshot))
        {
            result = result with
            {
                Snapshot = finalSnapshot with
                {
                    Current = Math.Max(0, finalSnapshot.Current),
                },
            };
        }

        if (result.NotificationError != null)
        {
            Log.Error(
                "[LibraryOfRuinaLib] Light store notification failed "
                + $"after {mutation.Kind}: "
                + result.NotificationError);
        }

        return new StoreMutationOutcome(revision, result);
    }

    private void HandleStoreChanged()
    {
        try
        {
            SynchronizeFromStore();
        }
        catch (Exception exception)
        {
            Log.Error(
                "[LibraryOfRuinaLib] Failed to synchronize Light store change: "
                + exception);
        }
    }

    private int SynchronizeFromStore()
    {
        if (!_store.TryRead(out LibraryLightStoreSnapshot snapshot))
            return Current;

        int previous;
        int current;
        // The external store is authoritative. It may legitimately shrink
        // below an earlier reservation; preserve that value so commit
        // preflight fails instead of manufacturing spendable Light.
        lock (_sync)
        {
            previous = _current;
            _current = Math.Max(0, snapshot.Current);
            current = _current;
        }
        if (previous == current)
            return current;

        InvokeSafely(
            CurrentChanged,
            previous,
            current,
            nameof(CurrentChanged));
        InvokeSafely(Changed, nameof(Changed));
        _speedState.NotifyGameplayChanged();
        return current;
    }

    private void RaiseChanges(
        int previousCurrent,
        int previousMaximum,
        int previousReserved)
    {
        if (previousCurrent != Current)
        {
            InvokeSafely(
                CurrentChanged,
                previousCurrent,
                Current,
                nameof(CurrentChanged));
        }
        if (previousMaximum != Maximum)
        {
            InvokeSafely(
                MaximumChanged,
                previousMaximum,
                Maximum,
                nameof(MaximumChanged));
        }
        _knownMaximum = Maximum;
        if (previousReserved != Reserved)
        {
            InvokeSafely(
                ReservationChanged,
                previousReserved,
                Reserved,
                nameof(ReservationChanged));
        }
        InvokeSafely(Changed, nameof(Changed));
        _speedState.NotifyGameplayChanged();
    }

    private int GetReservedLocked() => _reservations.Values.Sum();

    private static void InvokeSafely(Action? handlers, string eventName)
    {
        foreach (Delegate handler in handlers?.GetInvocationList() ?? [])
        {
            try
            {
                ((Action)handler)();
            }
            catch (Exception exception)
            {
                Log.Error(
                    "[LibraryOfRuinaLib] Light event listener failed for "
                    + $"{eventName}: {exception}");
            }
        }
    }

    private static void InvokeSafely(
        Action<int, int>? handlers,
        int previous,
        int current,
        string eventName)
    {
        foreach (Delegate handler in handlers?.GetInvocationList() ?? [])
        {
            try
            {
                ((Action<int, int>)handler)(previous, current);
            }
            catch (Exception exception)
            {
                Log.Error(
                    "[LibraryOfRuinaLib] Light event listener failed for "
                    + $"{eventName}: {exception}");
            }
        }
    }
}
