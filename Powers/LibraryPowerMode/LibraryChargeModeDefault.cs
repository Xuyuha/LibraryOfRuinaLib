using Library.Models;
namespace Library.Powers.Mode;
public sealed class LibraryChargeModeDefault : LibraryChargeMode
{
    public LibraryChargeModeDefault(LibraryMultipleModePowerModel sourcePower) : base(sourcePower)
    {
    }
    public LibraryChargeModeDefault()
    {
    }
    public const string name = "default";
    public override string Name => name;
}
