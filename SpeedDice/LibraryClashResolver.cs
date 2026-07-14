using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace Library.SpeedDice;

public sealed class LibraryClashContext
{
    internal LibraryClashContext(
        Player player,
        LibrarySpeedDiceSlot slot,
        Creature? target)
    {
        Player = player;
        Slot = slot;
        Target = target;
    }

    public Player Player { get; }

    public LibrarySpeedDiceSlot Slot { get; }

    public Creature? Target { get; set; }

    public bool CancelCard { get; set; }
}

public interface ILibraryClashResolver
{
    Task ResolveAsync(LibraryClashContext context);
}

public sealed class LibraryEmptyClashResolver : ILibraryClashResolver
{
    public Task ResolveAsync(LibraryClashContext context)
    {
        return Task.CompletedTask;
    }
}

public static class LibraryClashResolver
{
    private static ILibraryClashResolver _current = new LibraryEmptyClashResolver();

    public static ILibraryClashResolver Current
    {
        get => _current;
        set => _current = value ?? throw new ArgumentNullException(nameof(value));
    }
}
