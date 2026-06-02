using Library.Models;
namespace Library.Powers.Mode;
public sealed class LibraryChargeModeDefault : LibraryChargeMode
{
    public LibraryChargeModeDefault(LibraryMultipleModePowerModel sourcePower) : base(sourcePower)
    {
    }
    public override string Name => "default";
}
