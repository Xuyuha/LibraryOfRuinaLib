using LibraryLib.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Powers;
public sealed class LibrarySmokePower : LibraryPowerModel
{
    public override PowerType Type => PowerType.None;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    public decimal SmokeMultiplier => Amount * 0.05m + 1;
    public override void AddVariablesToDescription(LocString description, int? amountOverride = null)
    {
        description.Add("Multiplier",SmokeMultiplier);
    }
    private const int MaxAmount = 10;

    /// <summary>烟雾最多叠到 <see cref="MaxAmount"/> 层。</summary>
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? _, out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (canonicalPower != this || Amount + amount <= MaxAmount)
            return false;
        // Returning true is what makes Hook.ModifyPowerAmountReceived apply the capped amount.
        modifiedAmount = MaxAmount - Amount;
        return true;
    }
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if(target != Owner && dealer != Owner)return 1m;
        if(target != null && target.Side == Owner.Side)return 1m;
        if (props.IsPoweredAttack())
        {
            return SmokeMultiplier;
        }
        return 1m;
    }
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == Owner.Side)
        {
            await PowerCmd.Decrement(this);
        }
    }

}
