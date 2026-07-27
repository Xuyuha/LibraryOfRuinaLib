using Library.Models;
namespace Library.Powers.Mode;
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
