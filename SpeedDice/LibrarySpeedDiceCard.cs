using MegaCrit.Sts2.Core.Entities.Cards;

namespace LibraryLib.SpeedDice;

public readonly record struct LibrarySpeedDiceResourceCost(int Energy, int Stars);

public enum LibrarySpeedDiceAssignmentMode
{
    Persistent,
    Instant,
}

public interface ILibrarySpeedDiceCard
{
    LibrarySpeedDiceResourceCost SpeedDiceResourceCost { get; }

    /// <summary>
    /// 有时候卡牌在速度骰子上的类型和直接打出不一致
    /// 这里先加一个skill - attack的标记，方便后续做一些特殊处理
    /// </summary>
    bool CountsAsAttackDuringSpeedDiceResolution => false;
    
    TargetType SpeedDiceTargetType { get; }

    /// <summary>
    /// 速度骰子装备模式，Persistent表示装备后持续存在，Instant表示使用后不会占用打出区和骰子
    /// </summary>
    LibrarySpeedDiceAssignmentMode AssignmentMode =>
        LibrarySpeedDiceAssignmentMode.Persistent;

    /// <summary>
    /// 是否由基础库自动接管战斗手牌的右键速度骰子装备流程。
    /// 已有下游实现默认启用；存在其他右键用途的卡可以显式关闭。
    /// </summary>
    bool EnableSpeedDiceRightClickSelection => true;
}
