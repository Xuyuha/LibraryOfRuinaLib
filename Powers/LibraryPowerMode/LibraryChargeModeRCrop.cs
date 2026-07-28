using Library.Models;

namespace Library.Powers.Mode;
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
