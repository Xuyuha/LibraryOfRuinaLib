using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Library.Light;

public readonly record struct LibraryLightStoreSnapshot(int Current);

public enum LibraryLightStoreMutationKind
{
    Set,
    Gain,
    Lose,
    Spend,
    ResetToMaximum,
}

public readonly record struct LibraryLightStoreMutation(
    LibraryLightStoreMutationKind Kind,
    int Amount,
    CardModel? Card = null);

public readonly record struct LibraryLightStoreMutationResult(
    bool Succeeded,
    LibraryLightStoreSnapshot Snapshot,
    Exception? NotificationError = null);

public delegate ILibraryLightStore LibraryLightStoreFactory(
    MegaCrit.Sts2.Core.Entities.Players.Player player,
    LibraryLightOptions options);

public interface ILibraryLightStore
{
    event Action? Changed;

    bool TryRead(out LibraryLightStoreSnapshot snapshot);

    Task WriteAsync(
        LibraryLightStoreSnapshot snapshot,
        AbstractModel? source = null);

    void Restore(LibraryLightStoreSnapshot snapshot);
}

/// <summary>
/// 让外部存储声明其兼容预留投影使用的稳定资源 ID。
/// </summary>
public interface ILibraryLightStoreIdentity
{
    string ResourceId { get; }
}

/// <summary>
/// Optional command-aware Light store. Implement this when an external
/// resource framework owns mutation hooks, history, clamping, or networking.
/// The returned snapshot is authoritative.
/// </summary>
public interface ILibraryLightCommandStore
{
    Task<LibraryLightStoreMutationResult> MutateAsync(
        LibraryLightStoreMutation mutation,
        AbstractModel? source = null);
}

public sealed class LibraryInMemoryLightStore(int starting) :
    ILibraryLightStore,
    ILibraryLightStoreIdentity,
    ILibraryLightCommandStore
{
    private readonly object _sync = new();
    private LibraryLightStoreSnapshot _snapshot = new(Math.Max(0, starting));

    public string ResourceId => LibraryLight.DefaultResourceId;

    public event Action? Changed;

    public bool TryRead(out LibraryLightStoreSnapshot snapshot)
    {
        lock (_sync)
            snapshot = _snapshot;
        return true;
    }

    public Task WriteAsync(
        LibraryLightStoreSnapshot snapshot,
        AbstractModel? source = null)
    {
        snapshot = snapshot with
        {
            Current = Math.Max(0, snapshot.Current),
        };
        lock (_sync)
        {
            if (_snapshot == snapshot)
                return Task.CompletedTask;
            _snapshot = snapshot;
        }

        Exception? notificationError = NotifyChangedSafely();
        if (notificationError != null)
            return Task.FromException(notificationError);
        return Task.CompletedTask;
    }

    public Task<LibraryLightStoreMutationResult> MutateAsync(
        LibraryLightStoreMutation mutation,
        AbstractModel? source = null)
    {
        bool succeeded;
        bool changed;
        LibraryLightStoreSnapshot resultSnapshot;
        lock (_sync)
        {
            int current = _snapshot.Current;
            int amount = Math.Max(0, mutation.Amount);
            succeeded = true;
            int target;
            switch (mutation.Kind)
            {
                case LibraryLightStoreMutationKind.Set:
                case LibraryLightStoreMutationKind.ResetToMaximum:
                    target = amount;
                    break;
                case LibraryLightStoreMutationKind.Gain:
                    target = checked(current + amount);
                    break;
                case LibraryLightStoreMutationKind.Lose:
                    target = Math.Max(0, current - amount);
                    break;
                case LibraryLightStoreMutationKind.Spend:
                    succeeded = current >= amount;
                    target = succeeded ? current - amount : current;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(mutation),
                        mutation.Kind,
                        null);
            }

            changed = succeeded && target != current;
            if (changed)
                _snapshot = new LibraryLightStoreSnapshot(target);
            resultSnapshot = _snapshot;
        }

        Exception? notificationError = null;
        if (changed)
            notificationError = NotifyChangedSafely();

        return Task.FromResult(
            new LibraryLightStoreMutationResult(
                succeeded,
                resultSnapshot,
                notificationError));
    }

    public void Restore(LibraryLightStoreSnapshot snapshot)
    {
        lock (_sync)
        {
            _snapshot = snapshot with
            {
                Current = Math.Max(0, snapshot.Current),
            };
        }

        Exception? notificationError = NotifyChangedSafely();
        if (notificationError != null)
            throw notificationError;
    }

    private Exception? NotifyChangedSafely()
    {
        Delegate[] handlers = Changed?.GetInvocationList() ?? [];
        List<Exception>? errors = null;
        foreach (Delegate handler in handlers)
        {
            try
            {
                ((Action)handler)();
            }
            catch (Exception exception)
            {
                (errors ??= []).Add(exception);
            }
        }

        return errors?.Count switch
        {
            null or 0 => null,
            1 => errors[0],
            _ => new AggregateException(errors),
        };
    }
}
