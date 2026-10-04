using LibraryLib.Models;

namespace LibraryLib.Powers.LibraryPowerMode;

public abstract class LibraryBleedingMode : LibraryPowerModeModel
{
    public LibraryBleedingMode(LibraryMultipleModePowerModel sourcePower) : base(sourcePower)
    {
    }
    public LibraryBleedingMode()
    {
    }
}