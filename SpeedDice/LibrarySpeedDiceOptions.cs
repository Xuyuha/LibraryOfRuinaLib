namespace LibraryLib.SpeedDice;

/// <summary>
/// 速度骰子系统的基础数值。角色专属规则应通过模块扩展。
/// </summary>
public sealed record LibrarySpeedDiceOptions(
    int BaseCount = 1,
    int MinRoll = 1,
    int MaxRoll = 4)
{
    internal void Validate()
    {
        if (BaseCount < 0)
            throw new ArgumentOutOfRangeException(nameof(BaseCount));
        if (MinRoll < 1)
            throw new ArgumentOutOfRangeException(nameof(MinRoll));
        if (MaxRoll < MinRoll)
            throw new ArgumentOutOfRangeException(nameof(MaxRoll));
    }
}
