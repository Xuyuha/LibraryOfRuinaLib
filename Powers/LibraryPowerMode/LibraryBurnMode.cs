using LibraryLib.Models;

namespace LibraryLib.Powers.LibraryPowerMode;

public abstract class LibraryBurnMode : LibraryPowerMode
{
    public LibraryBurnMode(LibraryMultipleModePowerModel sourcePower) : base(sourcePower)
    {
    }
    public LibraryBurnMode()
    {
    }
}