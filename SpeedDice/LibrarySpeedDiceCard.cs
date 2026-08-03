using MegaCrit.Sts2.Core.Entities.Cards;

namespace LibraryLib.SpeedDice;

public readonly record struct LibrarySpeedDiceResourceCost(int Energy, int Stars);

public interface ILibrarySpeedDiceCard
{
    LibrarySpeedDiceResourceCost SpeedDiceResourceCost { get; }
    
    TargetType SpeedDiceTargetType { get; }

    /// <summary>
    /// 是否由基础库自动接管战斗手牌的右键速度骰子装备流程。
    /// 已有下游实现默认启用；存在其他右键用途的卡可以显式关闭。
    /// </summary>
    bool EnableSpeedDiceRightClickSelection => true;
}
