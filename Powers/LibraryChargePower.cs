using Library.Models;
using MegaCrit.Sts2.Core.Entities.Powers;
using Library.Powers.Mode;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Library.Powers;
public sealed class LibraryChargePower : LibraryBasePowerModel
{
    protected override LibraryPowerMode DefaultMode => new LibraryChargeModeDefault(this);
    public LibraryChargeMode CurrentMode => Mode as LibraryChargeMode;
    public override bool IsDynamic => true;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override async Task Effect(PlayerChoiceContext choiceContext, decimal effectiveAmount)
    {
        await CurrentMode.Effect(choiceContext, effectiveAmount);
    }
}