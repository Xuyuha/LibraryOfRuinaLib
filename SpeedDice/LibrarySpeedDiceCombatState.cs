using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Random;

namespace Library.SpeedDice;

public sealed class LibrarySpeedDiceCombatState
{
    private readonly List<LibrarySpeedDiceSlot> _slots = [];

    internal LibrarySpeedDiceCombatState(Player player, LibrarySpeedDiceParticipant participant)
    {
        Player = player;
        Participant = participant;
        GameplayRng = new Rng(player.RunState.Rng.Seed, "library_speed_dice");
    }

    public Player Player { get; }

    public LibrarySpeedDiceParticipant Participant { get; }

    public LibraryEmotionState Emotion { get; } = new();

    public IReadOnlyList<LibrarySpeedDiceSlot> Slots => _slots;

    public bool IsLocked { get; internal set; }

    public bool IsResolving { get; internal set; }

    public bool IsSelectingTarget { get; internal set; }

    public LibrarySpeedDiceSlot? ResolvingSlot { get; internal set; }

    public int CurrentTurnTriggeredCards { get; internal set; }

    public int PreviousTurnTriggeredCards { get; internal set; }

    public int ReservedEnergy => _slots.Sum(x => x.ReservedEnergy);

    public int ReservedStars => _slots.Sum(x => x.ReservedStars);

    public event Action? Changed;

    internal Rng GameplayRng { get; set; }

    internal int DamageGivenAccumulator { get; set; }

    internal int DamageReceivedAccumulator { get; set; }

    internal bool BonusDrawPending { get; set; }

    internal SemaphoreSlim Gate { get; } = new(1, 1);

    internal void ReplaceSlots(int count)
    {
        _slots.Clear();
        for (int i = 0; i < count; i++)
            _slots.Add(new LibrarySpeedDiceSlot(i, Participant.MinSpeed));
        IsLocked = false;
        IsResolving = false;
        IsSelectingTarget = false;
        ResolvingSlot = null;
        NotifyChanged();
    }

    internal void NotifyChanged()
    {
        Changed?.Invoke();
    }
}
