using Library.Models;

namespace Library.Powers.Mode;
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
