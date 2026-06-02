using Library.Models;
namespace Library.Powers.Mode;
public sealed class LibraryBurnModeDefault : LibraryBurnMode
{
    public LibraryBurnModeDefault(LibraryMultipleModePowerModel sourcePower) : base(sourcePower)
    {
    }
    public override string Name => "default";
}
