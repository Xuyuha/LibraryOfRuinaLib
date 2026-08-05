#nullable enable

namespace LibraryLib.Utils.Resistance;

/// <summary>
///     全局抗性模式，由上层模组（BetterExtensionMod）设置。
/// </summary>
public enum LibraryResistanceMode
{
    Normal = 0,
    Weak = 1,
    Ignore = 2
}

/// <summary>
///     当前生效的抗性模式。默认保持原版（Normal）行为。
/// </summary>
public static class LibraryResistanceModeState
{
    public static LibraryResistanceMode Current { get; set; } = LibraryResistanceMode.Normal;
}
