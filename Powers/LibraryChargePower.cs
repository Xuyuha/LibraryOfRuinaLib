using LibraryLib.Models;
using LibraryLib.Powers.LibraryPowerMode;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace LibraryLib.Powers;
public sealed class LibraryChargePower : LibraryBasePowerModel
{
    protected override LibraryPowerMode.LibraryPowerMode DefaultMode => new LibraryChargeModeDefault(this);
    public LibraryChargeMode CurrentMode => Mode as LibraryChargeMode;
    public override bool IsDynamic => true;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public int MaxAmount => CurrentMode.MaxAmount;
    //将充能的Effect实现为消耗Amount层
    protected override async Task Effect(PlayerChoiceContext choiceContext, decimal effectiveAmount)
    {
        SetAmount(Amount - (int)effectiveAmount);
        await CurrentMode.Effect(choiceContext, effectiveAmount);
    }
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount, object? _ = null)
    {
        if(canonicalPower != this)
        {
            modifiedAmount = amount;
            return false;
        }
        if(amount + Amount > MaxAmount)
        {
            modifiedAmount = MaxAmount - Amount;
            return true;
        }
        modifiedAmount = amount;
        return false;
    }
    
}
