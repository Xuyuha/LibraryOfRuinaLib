namespace LibraryLib.SpeedDice;

public sealed class LibraryEmotionState
{
    private int _units;

    public int Level { get; private set; }

    public int Units => _units;

    public event Action<int, int>? LevelChanged;

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

        int previousLevel = Level;
        Level++;
        _units = 0;
        LevelChanged?.Invoke(previousLevel, Level);
        return true;
    }

    internal void Restore(int level, int units, LibraryEmotionConfig config)
    {
        Level = Math.Clamp(level, 0, config.UnitThresholds.Count);
        _units = Level >= config.UnitThresholds.Count
            ? 0
            : Math.Clamp(units, 0, config.UnitThresholds[Level]);
    }

    internal bool ForceLevelUp(LibraryEmotionConfig config)
    {
        if (Level >= config.UnitThresholds.Count)
            return false;

        _units = config.UnitThresholds[Level];
        return TryLevelUp(config);
    }
}
