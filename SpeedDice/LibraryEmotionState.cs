namespace Library.SpeedDice;

public sealed class LibraryEmotionState
{
    private int _units;

    public int Level { get; private set; }

    public int Value { get; private set; }

    public bool IsReadyToLevelUp => Value >= 100 && Level < 5;

    internal void AddUnits(int amount, LibraryEmotionConfig config)
    {
        if (amount <= 0 || Level >= 5)
            return;

        int threshold = config.UnitThresholds[Level];
        _units = Math.Min(threshold, _units + amount);
        Value = Math.Min(100, _units * 100 / threshold);
    }

    internal bool TryLevelUp()
    {
        if (!IsReadyToLevelUp)
            return false;

        Level++;
        _units = 0;
        Value = 0;
        return true;
    }
}
