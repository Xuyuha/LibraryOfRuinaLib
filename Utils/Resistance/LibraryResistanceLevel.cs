#nullable enable

namespace Library.Resistance;

/// <summary>
///     抗性等级枚举，定义生物对伤害的抵抗程度。
///     从免疫到致命共六个等级，各有不同的伤害倍率。
/// </summary>
public enum LibraryResistanceLevel
{
    Immune = 0,
    Resist = 1,
    Endure = 2,
    Normal = 3,
    Vulnerable = 4,
    Fatal = 5
}

/// <summary>
///     <see cref="LibraryResistanceLevel"/> 的扩展方法。
///     提供倍率转换和本地化键后缀。
/// </summary>
public static class LibraryResistanceLevelExtensions
{
    public static decimal GetMultiplier(this LibraryResistanceLevel level) => level switch
    {
        LibraryResistanceLevel.Immune => 0.25m,
        LibraryResistanceLevel.Resist => 0.5m,
        LibraryResistanceLevel.Endure => 0.75m,
        LibraryResistanceLevel.Normal => 1m,
        LibraryResistanceLevel.Vulnerable => 1.25m,
        LibraryResistanceLevel.Fatal => 1.5m,
        _ => 1m
    };

    public static string GetLocKeySuffix(this LibraryResistanceLevel level) => level switch
    {
        LibraryResistanceLevel.Immune => "immune",
        LibraryResistanceLevel.Resist => "resist",
        LibraryResistanceLevel.Endure => "endure",
        LibraryResistanceLevel.Normal => "normal",
        LibraryResistanceLevel.Vulnerable => "vulnerable",
        LibraryResistanceLevel.Fatal => "fatal",
        _ => "normal"
    };
}
