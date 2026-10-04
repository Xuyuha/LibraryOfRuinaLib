using LibraryLib.Models;

namespace LibraryLib.Powers.LibraryPowerMode;

public abstract class LibraryBurnMode : LibraryPowerModeModel
{
    public LibraryBurnMode(LibraryMultipleModePowerModel sourcePower) : base(sourcePower)
    {
    }
    public LibraryBurnMode()
    {
    }
}