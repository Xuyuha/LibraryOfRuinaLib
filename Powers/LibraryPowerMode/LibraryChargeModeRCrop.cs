using Library.Models;
namespace Library.Powers.Mode;
public sealed class LibraryChargeModeRCorp : LibraryChargeMode
{
    public LibraryChargeModeRCorp(LibraryMultipleModePowerModel sourcePower) : base(sourcePower)
    {
    }
    public override int MaxAmount => 20;
    public override string Name => "r_corp";
}
