using Library.Light;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace Library.SpeedDice;

public sealed record LibrarySpeedDiceStateSnapshot(
    int TurnNumber,
    int Revision,
    bool HasRolled,
    bool IsLocked,
    int CurrentTurnTriggeredCards,
    int PreviousTurnTriggeredCards,
    bool BonusDrawPending,
    int DamageGivenAccumulator,
    int DamageReceivedAccumulator,
    int EmotionLevel,
    int EmotionUnits,
    IReadOnlyList<LibrarySpeedDiceSlotSnapshot> Slots)
{
    public LibrarySpeedDiceSnapshotExtension Extension { get; init; } =
        new();
}

public sealed record LibrarySpeedDiceSlotSnapshot(
    int Index,
    int DisplayValue,
    int FinalValue,
    bool IsLocked,
    bool IsSpent,
    CardModel? Card,
    Creature? Target,
    int ReservedEnergy,
    int ReservedStars,
    IReadOnlyDictionary<string, int> ReservedSecondaryResources)
{
    public LibrarySpeedDiceLeaseSnapshot? Lease { get; init; }
}

public sealed record LibrarySpeedDiceSnapshotExtension
{
    public int LeaseSequence { get; init; }

    public int PendingEmotionPreviousLevel { get; init; } = -1;

    public int PendingEmotionCurrentLevel { get; init; } = -1;

    public int DamageGivenAccumulatorThreshold { get; init; }

    public int DamageReceivedAccumulatorThreshold { get; init; }

    public LibraryLightStateSnapshot? Light { get; init; }
}

public sealed record LibrarySpeedDiceLeaseSnapshot(
    string Id,
    IReadOnlyList<LibrarySpeedDiceResourceReservation> Resources,
    bool IsUseTriggered,
    bool IsTargetedUseTriggered,
    bool PreventUnequip,
    bool IsCommitted);
