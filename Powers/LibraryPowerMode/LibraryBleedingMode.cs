using Library.Models;
using Library.Powers.Mode;
public abstract class LibraryBleedingMode : LibraryPowerMode
{
    public LibraryBleedingMode(LibraryMultipleModePowerModel sourcePower) : base(sourcePower)
    {
    }
}