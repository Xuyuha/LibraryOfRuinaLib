using Library.Models;

namespace Library.Powers.Mode;
public sealed class LibraryBleedingModeDefault : LibraryBleedingMode
{
    public LibraryBleedingModeDefault(LibraryMultipleModePowerModel sourcePower) : base(sourcePower)
    {
    }
    public override string Name => "default";
}
