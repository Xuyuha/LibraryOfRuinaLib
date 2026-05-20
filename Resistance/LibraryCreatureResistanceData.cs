#nullable enable

namespace Library.Resistance;

/// <summary>
///     存储单个生物的物理抗性和混乱抗性数据。
///     包含斩击/打击/穿刺三种伤害类型的物理与混乱抗性等级。
/// </summary>
public sealed class LibraryCreatureResistanceData
{
    public LibraryResistanceLevel SlashPhysical { get; set; } = LibraryResistanceLevel.Normal;
    public LibraryResistanceLevel BluntPhysical { get; set; } = LibraryResistanceLevel.Normal;
    public LibraryResistanceLevel PiercePhysical { get; set; } = LibraryResistanceLevel.Normal;

    public LibraryResistanceLevel SlashChaos { get; set; } = LibraryResistanceLevel.Normal;
    public LibraryResistanceLevel BluntChaos { get; set; } = LibraryResistanceLevel.Normal;
    public LibraryResistanceLevel PierceChaos { get; set; } = LibraryResistanceLevel.Normal;

    public LibraryResistanceLevel GetPhysicalResistance(LibraryDamageKind kind) => kind switch
    {
        LibraryDamageKind.Slash => SlashPhysical,
        LibraryDamageKind.Blunt => BluntPhysical,
        LibraryDamageKind.Pierce => PiercePhysical,
        _ => LibraryResistanceLevel.Normal
    };

    public LibraryResistanceLevel GetChaosResistance(LibraryDamageKind kind) => kind switch
    {
        LibraryDamageKind.Slash => SlashChaos,
        LibraryDamageKind.Blunt => BluntChaos,
        LibraryDamageKind.Pierce => PierceChaos,
        _ => LibraryResistanceLevel.Normal
    };
}
