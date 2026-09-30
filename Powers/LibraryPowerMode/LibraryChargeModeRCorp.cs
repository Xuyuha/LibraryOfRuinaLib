using LibraryLib.Models;

namespace LibraryLib.Powers.LibraryPowerMode;
public sealed class LibraryChargeModeRCorp : LibraryChargeMode
{
    public LibraryChargeModeRCorp(LibraryMultipleModePowerModel sourcePower) : base(sourcePower)
    {
    }
    public LibraryChargeModeRCorp()
    {
    }
    public override int MaxAmount => 20;
    public const string name = "r_corp";
    public override string Name => name;
}
