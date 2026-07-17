namespace Library.SpeedDice;

public sealed class LibraryEmotionState
{
    private int _units;

    public int Level { get; private set; }

    public int Units => _units;

    internal void AddUnits(int amount, LibraryEmotionConfig config)
    {
        if (amount <= 0 || Level >= config.UnitThresholds.Count)
            return;

        int threshold = config.UnitThresholds[Level];
        _units = Math.Min(threshold, _units + amount);
    }

    internal bool TryLevelUp(LibraryEmotionConfig config)
    {
        if (Level >= config.UnitThresholds.Count
            || _units < config.UnitThresholds[Level])
        {
            return false;
        }

        Level++;
        _units = 0;
        return true;
    }
}
