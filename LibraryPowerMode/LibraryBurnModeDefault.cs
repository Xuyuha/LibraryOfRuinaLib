using LibraryLib.Models;

namespace LibraryLib.Powers.LibraryPowerMode;
public sealed class LibraryBurnModeDefault : LibraryBurnMode
{
    public LibraryBurnModeDefault(LibraryMultipleModePowerModel sourcePower) : base(sourcePower)
    {
    }
    public LibraryBurnModeDefault()
    {
    }
    public const string name = "default";
    public override string Name => name;
}
