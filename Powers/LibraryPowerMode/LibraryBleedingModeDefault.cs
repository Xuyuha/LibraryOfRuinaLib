using LibraryLib.Models;

namespace LibraryLib.Powers.LibraryPowerMode;
public sealed class LibraryBleedingModeDefault : LibraryBleedingMode
{
    public LibraryBleedingModeDefault(LibraryMultipleModePowerModel sourcePower) : base(sourcePower)
    {
    }
    public LibraryBleedingModeDefault()
    {
    }
    public const string name = "default";
    public override string Name => name;
}
