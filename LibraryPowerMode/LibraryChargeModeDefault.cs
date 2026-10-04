using LibraryLib.Models;

namespace LibraryLib.Powers.LibraryPowerMode;
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
