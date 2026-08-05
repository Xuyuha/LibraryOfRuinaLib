using Godot;
using LibraryLib.Patches;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

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
    public int TurnsRemaining => AmountPlan.Count > 0 ? AmountPlan.Keys.Max() - CombatState.RoundNumber + 1 : 0;
    private bool ShouldDecayThisTurn => AmountPlan.Count > 0 && AmountPlan.First().Key == CombatState.RoundNumber;
	/// <summary>
	///     表示在谁的回合结束时消耗，默认为自己回合结束时消耗
	/// </summary>
    protected virtual CombatSide DecaySide => Owner.Side;
    public void AddPlan(int amount,int turns)
    {
        int decayRound = CombatState.RoundNumber + turns;
        AmountPlan.TryGetValue(decayRound,out int existing);
        AmountPlan[decayRound] = existing + amount;
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
        await AfterSideTurnEnd(choiceContext,side,participants,null);
        if(side == DecaySide && AmountPlan.Count != 0 && ShouldDecayThisTurn)
        {
            SetAmount(Amount - AmountPlan.First().Value);
            AmountPlan.Remove(AmountPlan.First().Key);
            if(AmountPlan.Count == 0)
            {
                await PowerCmd.Remove(this);
                BoundNPower = null;
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
        description.Add("Prompt",Prompt());
    }
    public string Prompt()
    {
        if(!IsMutable)return"";
        string s = "\n";
        foreach(var k in AmountPlan)
        {
            LocString loc = new LocString("powers","LIBRARY_TURN_POWER_PROMPT");
            loc.Add("Amount",k.Value);
            loc.Add("Turns",k.Key - CombatState.RoundNumber + 1);
            s+=loc.GetFormattedText() + "\n";
        }
        return s;
    }
}
