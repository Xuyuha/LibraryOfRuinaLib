using Godot;

namespace Library.Models;

/// <summary>
///     由 <see cref="PowerModel"/> 子类实现，用于在能力图标上显示次要数字徽章
///     （例如剩余回合数、次要属性值）。
///     目前仅支持双参数。
///     UI 渲染由 <see cref="Library.Patches.PowerSecondaryCounterUi"/> 自动处理。
/// </summary>
public interface ISecondaryDisplayAmountPower
{
    /// <summary>
    ///     次要徽章是否可见。
    /// </summary>
    bool ShowSecondaryDisplayAmount { get; }

    /// <summary>
    ///     次要徽章中显示的数值。
    /// </summary>
    int SecondaryDisplayAmount { get; }

    /// <summary>
    ///     次要徽章标签的字体颜色。
    /// </summary>
    Color SecondaryDisplayAmountLabelColor { get; }
}
