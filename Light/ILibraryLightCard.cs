namespace LibraryLib.Light;

/// <summary>
/// 声明卡牌拥有独立 Light 费用。该费用不会读取或修改 Energy 费用。
/// </summary>
public interface ILibraryLightCard
{
    int BaseLightCost { get; }

    bool HasLightCostX { get; }
}
