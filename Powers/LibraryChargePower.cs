using Library.Models;
using MegaCrit.Sts2.Core.Entities.Powers;
using Library.Powers.Mode;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Library.Powers;
public sealed class LibraryChargePower : LibraryBasePowerModel
{
    protected override LibraryPowerMode DefaultMode => new LibraryChargeModeDefault(this);
    public LibraryChargeMode CurrentMode => Mode as LibraryChargeMode;
    public override bool IsDynamic => true;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public int MaxAmount => CurrentMode.MaxAmount;
    protected override async Task Effect(PlayerChoiceContext choiceContext, decimal effectiveAmount)
    {
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