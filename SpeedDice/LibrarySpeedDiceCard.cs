using MegaCrit.Sts2.Core.Entities.Cards;

namespace Library.SpeedDice;

public readonly record struct LibrarySpeedDiceResourceCost(int Energy, int Stars);

public interface ILibrarySpeedDiceCard
{
    LibrarySpeedDiceResourceCost SpeedDiceResourceCost { get; }
    
    TargetType SpeedDiceTargetType { get; } 
}
