using Library.Models;
using MegaCrit.Sts2.Core.Entities.Powers;
using Library.Powers.Mode;

namespace Library.Powers;
public sealed class LibraryChargePower : LibraryBasePowerModel
{
    protected override LibraryPowerMode DefaultMode => new LibraryChargeModeDefault();
    public LibraryChargeMode CurrentMode => Mode as LibraryChargeMode;
    public override bool IsDynamic => true;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}