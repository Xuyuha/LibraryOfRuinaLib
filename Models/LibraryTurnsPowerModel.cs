using Godot;
using LibraryLib.Patches;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace LibraryLib.Models;
//和LibraryDurationPowerModel的区别在于可以在同一个power上显示其回合层数变化
/// <summary>
///     用原版方法施加相当于施加永久buff
/// </summary>
public abstract class LibraryTurnsPowerModel : LibraryPowerModel, ISecondaryDisplayAmountPower
{
    public bool ShowSecondaryDisplayAmount => AmountPlan.Count != 0;
    public int SecondaryDisplayAmount => TurnsRemaining;
    protected static CombatSide OppositeSideOf(Creature owner)
    {
        return owner.Side == CombatSide.Player ? CombatSide.Enemy : CombatSide.Player;
    }
    public virtual Color SecondaryDisplayAmountLabelColor => _normalAmountLabelColor;
    //表示该power的每个子power会在第几回合减少
    private SortedDictionary<int, int> _AmountPlan = null;
    public override bool NeedNpower => true;
    public SortedDictionary<int, int> AmountPlan
    {
        get
        {
            if(_AmountPlan == null)
                AmountPlan = [];
            return _AmountPlan;
        }
        set
        {
            _AmountPlan =value;
            if(BoundNPower != null)
                PowerSecondaryCounterUi.RefreshSecondaryLabel(BoundNPower);
        }
    }
    //持续回合数
    public int TurnsRemaining
    {
        get
        {
            if (!IsMutable)
            {
                return 0;
            }

            ICombatState? combatState = Owner?.CombatState;
            return combatState != null && AmountPlan.Count > 0
                ? Math.Max(0, AmountPlan.Keys.Max() - combatState.RoundNumber + 1)
                : 0;
        }
    }
	/// <summary>
	///     表示在谁的回合结束时消耗，默认为自己回合结束时消耗
	/// </summary>
    protected virtual CombatSide DecaySide => Owner.Side;
    public void AddPlan(int amount,int turns)
    {
        if (amount == 0)
        {
            return;
        }

        Creature? owner = Owner;
        ICombatState? combatState = owner?.CombatState;
        if (combatState == null)
        {
            return;
        }

        int decayRound = combatState.RoundNumber + turns;
        AmountPlan.TryGetValue(decayRound,out int existing);
        int merged = (int)Math.Clamp(
            (long)existing + amount,
            -999_999_999L,
            999_999_999L);
        if (merged == 0)
        {
            AmountPlan.Remove(decayRound);
            return;
        }

        AmountPlan[decayRound] = merged;
    }
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (BoundNPower != null) PowerSecondaryCounterUi.RefreshSecondaryLabel(BoundNPower);
        return Task.CompletedTask;
    }
    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if(side == CombatSide.Player)
            if (BoundNPower != null)
                PowerSecondaryCounterUi.RefreshSecondaryLabel(BoundNPower);
        return Task.CompletedTask;
    }
    //回合结束时改变层数
    public sealed override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        ICombatState? combatState = Owner?.CombatState;
        if (combatState == null)
        {
            return;
        }

        await AfterSideTurnEnd(choiceContext,side,participants,null);
        await ExpireDuePlans(side, combatState.RoundNumber);
    }
    internal async Task ExpireDuePlans(CombatSide side, int currentRound)
    {
        if (Owner?.CombatState == null)
        {
            return;
        }

        if (!AllowNegative && Amount < 0)
        {
            AmountPlan.Clear();
            SetAmount(0);
            await PowerCmd.Remove(this);
            BoundNPower = null;
            return;
        }

        if (side == DecaySide
            && AmountPlan.Count != 0
            && AmountPlan.First().Key <= currentRound)
        {
            LibraryTurnsPowerPlanResolution resolution =
                LibraryTurnsPowerPlan.ExpireDueEntries(
                    AmountPlan,
                    currentRound,
                    Amount,
                    AllowNegative);
            if (resolution.Amount != Amount)
            {
                SetAmount(resolution.Amount);
            }

            if (resolution.ShouldRemove)
            {
                await PowerCmd.Remove(this);
                BoundNPower = null;
                return;
            }
        }

        if(BoundNPower != null)
            PowerSecondaryCounterUi.RefreshSecondaryLabel(BoundNPower);
    }
    //防止子类继承时覆盖
    public virtual Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants,object? _=null)
    {
        return Task.CompletedTask;
    }    
    public virtual Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState,object? _=null)
    {
        if(side == CombatSide.Player)
            if (BoundNPower != null)
                PowerSecondaryCounterUi.RefreshSecondaryLabel(BoundNPower);
        return Task.CompletedTask;
    }
    public virtual Task AfterApplied(Creature? applier, CardModel? cardSource,object? _=null)
    {
        if (BoundNPower != null) PowerSecondaryCounterUi.RefreshSecondaryLabel(BoundNPower);
        return Task.CompletedTask;
    }
    public override void AddVariablesToDescription(LocString description, int? amountOverride = null)
    {
        // Dumb hover tips use the canonical ModelDb instance; skip turn-plan prompt formatting there.
        description.Add("Prompt", IsMutable ? Prompt() : "");
    }
    public string Prompt()
    {
        if (!IsMutable)
        {
            return "";
        }

        ICombatState? combatState = Owner?.CombatState;
        if (combatState == null)
        {
            return "";
        }

        string s = "\n";
        foreach(var k in AmountPlan)
        {
            LocString loc = new LocString("powers","LIBRARY_TURN_POWER_PROMPT");
            loc.Add("Amount",k.Value);
            loc.Add("Turns",Math.Max(0, k.Key - combatState.RoundNumber + 1));
            s+=loc.GetFormattedText() + "\n";
        }
        return s;
    }
}
