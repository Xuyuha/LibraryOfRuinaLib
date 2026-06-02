using Library.Models;
using Library.Powers.Mode;

public abstract class LibraryChargeMode : LibraryPowerMode
{
    public LibraryChargeMode(LibraryMultipleModePowerModel sourcePower) : base(sourcePower)
    {
    }
}