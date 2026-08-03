namespace LibraryLib.Light;

[Flags]
public enum LibraryLightCostModifiers
{
    None = 0,
    Local = 1,
    Global = 2,
    All = -1,
}

public enum LibraryLightCostModifierType
{
    Absolute,
    Relative,
}

[Flags]
public enum LibraryLightCostModifierExpiration
{
    None = 0,
    EndOfTurn = 1,
    WhenPlayed = 2,
    EndOfCombat = 4,
}

public sealed class LibraryLightCostModifier(
    int amount,
    LibraryLightCostModifierType type,
    LibraryLightCostModifierExpiration expiration,
    bool reduceOnly)
{
    public int Amount { get; set; } = amount;

    public LibraryLightCostModifierType Type { get; } = type;

    public LibraryLightCostModifierExpiration Expiration { get; } = expiration;

    public bool IsReduceOnly { get; } = reduceOnly;

    public int Modify(int currentCost)
    {
        return Type switch
        {
            LibraryLightCostModifierType.Absolute =>
                IsReduceOnly
                    ? Math.Min(currentCost, Amount)
                    : Amount,
            LibraryLightCostModifierType.Relative =>
                IsReduceOnly
                    ? Math.Min(currentCost, currentCost + Amount)
                    : currentCost + Amount,
            _ => throw new ArgumentOutOfRangeException(
                nameof(Type),
                Type,
                null),
        };
    }

    public LibraryLightCostModifier Clone() =>
        new(Amount, Type, Expiration, IsReduceOnly);
}
