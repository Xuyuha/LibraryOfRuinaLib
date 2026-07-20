using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;

namespace Library.SpeedDice;

public sealed class LibrarySpeedDiceParticipant
{
    public required string Id { get; init; }

    public required Func<Player, bool> IsEnabledForPlayer { get; init; }

    public Func<CardModel, bool> CanEquipCard { get; init; } = static _ => true;

    public int BaseSpeedDiceCount { get; init; } = 1;
    // 骰子默认最小值，可以自己new的时候改
    public int MinSpeed { get; init; } = 1;

    // 骰子默认最大值，可以自己new的时候改
    public int MaxSpeed { get; init; } = 4;

    public LibraryEmotionConfig Emotion { get; init; } = new();

    internal void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Id);
        ArgumentNullException.ThrowIfNull(IsEnabledForPlayer);
        ArgumentNullException.ThrowIfNull(CanEquipCard);

        if (BaseSpeedDiceCount < 0)
            throw new ArgumentOutOfRangeException(nameof(BaseSpeedDiceCount));
        if (MinSpeed < 1)
            throw new ArgumentOutOfRangeException(nameof(MinSpeed));
        if (MaxSpeed < MinSpeed)
            throw new ArgumentOutOfRangeException(nameof(MaxSpeed));
        if (Emotion.UnitThresholds.Count != 5 || Emotion.UnitThresholds.Any(x => x <= 0))
            throw new ArgumentException("Emotion thresholds must contain five positive values.", nameof(Emotion));
    }
}
