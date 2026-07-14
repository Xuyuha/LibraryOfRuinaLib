namespace Library.SpeedDice;

public sealed class LibraryEmotionConfig
{
    public IReadOnlyList<int> UnitThresholds { get; init; } = [3, 3, 5, 7, 9];

    public decimal DamageUnitFractionOfMaxHp { get; init; } = 0.10m;

    public int KillEmotionUnits { get; init; } = 3;

    public int MaxEnergyPerLevel { get; init; } = 1;

    public int ExtraSpeedDieLevel { get; init; } = 4;

    public int ExtraSpeedDice { get; init; } = 1;

    public int BonusDrawLevel { get; init; } = 5;

    public int BonusDrawRequiredTriggeredCards { get; init; } = 2;

    public int BonusDrawAmount { get; init; } = 1;
}
