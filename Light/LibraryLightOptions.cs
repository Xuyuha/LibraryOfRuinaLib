namespace Library.Light;

public sealed class LibraryLightOptions
{
    public LibraryLightOptions(
        int starting,
        int baseMaximum,
        int maximumPerEmotionLevel,
        int recoveryPerTurn,
        bool refillOnLevelIncrease)
    {
        Starting = starting;
        BaseMaximum = baseMaximum;
        MaximumPerEmotionLevel = maximumPerEmotionLevel;
        RecoveryPerTurn = recoveryPerTurn;
        RefillOnLevelIncrease = refillOnLevelIncrease;
        Validate();
    }

    public int Starting { get; }

    public int BaseMaximum { get; }

    public int MaximumPerEmotionLevel { get; }

    public int RecoveryPerTurn { get; }

    public bool RefillOnLevelIncrease { get; }

    internal void Validate()
    {
        if (Starting < 0)
            throw new ArgumentOutOfRangeException(nameof(Starting));
        if (BaseMaximum < 0)
            throw new ArgumentOutOfRangeException(nameof(BaseMaximum));
        if (MaximumPerEmotionLevel < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaximumPerEmotionLevel));
        }
        if (RecoveryPerTurn < 0)
            throw new ArgumentOutOfRangeException(nameof(RecoveryPerTurn));
    }
}
