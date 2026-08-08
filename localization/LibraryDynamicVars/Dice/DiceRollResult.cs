
namespace LibraryLib.Localization.Dice;
public class DiceRollResult
{
    public int MinValue = 0;
    public int MaxValue = 0;
    public int CurrentValue = 0;
    public bool IsMinValue => CurrentValue == MinValue;
    public bool IsMaxValue => CurrentValue == MaxValue;
}