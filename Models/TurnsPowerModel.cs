using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace LibraryLib.Models;
//和LibraryDurationPowerModel的区别在于可以在同一个power上显示其回合层数变化
public abstract class LibraryTurnsPowerModel : LibraryPowerModel, ISecondaryDisplayAmountPower
{
    public bool ShowSecondaryDisplayAmount => AmountPlan.Count != 0;
    public int SecondaryDisplayAmount => TurnsRemaining;
    public abstract Color SecondaryDisplayAmountLabelColor { get; }
    //表示该power的每个子power会在第几回合减少
    public SortedDictionary<int, int> AmountPlan = [];
    //持续回合数
    public int TurnsRemaining => AmountPlan.Count > 0 ? AmountPlan.Keys.Max() : 0;
    private bool ShouldDecayThisTurn => AmountPlan.Count > 0 && AmountPlan.First().Key == CombatState.RoundNumber;
    public void AddPlan(int amount,int turns)
    {
        int decayRound = CombatState.RoundNumber + turns;
        AmountPlan.TryGetValue(decayRound,out int existing);
        AmountPlan[decayRound] = existing + amount;
    }
    //回合结束时改变层数
    public sealed override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    { 
        if(side == Owner.Side && AmountPlan.Count != 0 && ShouldDecayThisTurn)
        {
            SetAmount(Amount - AmountPlan.First().Value);
            AmountPlan.Remove(AmountPlan.First().Key);
        }
        await AfterSideTurnEnd(choiceContext,side,participants);
    }
    //防止子类继承时覆盖
    public virtual Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants,object? _=null)
    {
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
            LocString loc = new LocString("turn_power_prompt","PROPMT");
            loc.Add("Amount",k.Value);
            loc.Add("Turns",k.Key - CombatState.RoundNumber + 1);
            s+=loc.GetFormattedText() + "\n";
        }
        return s;
    }
    //turns代表持续回合数，IsPermanent代表是否永久性改变
	public static async Task<int> ModifyAmount(PlayerChoiceContext choiceContext, LibraryTurnsPowerModel power, decimal offset,int turns,bool IsPermanent, Creature? applier, CardModel? cardSource, bool silent = false)
	{
		if (CombatManager.Instance.IsEnding)
		{
			return 0;
		}
		Creature owner = power.Owner;
		ICombatState combatState = owner.CombatState;
		if (combatState == null)
		{
			return 0;
		}
		await Hook.BeforePowerAmountChanged(combatState, power, offset, owner, applier, cardSource);
		decimal modifiedOffset = offset;
		IEnumerable<AbstractModel> modifiers = null;
		if (applier != null && combatState.ContainsCreature(applier))
		{
			modifiedOffset = Hook.ModifyPowerAmountGiven(combatState, power, applier, modifiedOffset, owner, cardSource, out modifiers);
		}
		modifiedOffset = Hook.ModifyPowerAmountReceived(combatState, power, owner, modifiedOffset, applier, out IEnumerable<AbstractModel> receivedModifiers);
		CombatManager.Instance.History.PowerReceived(combatState, power, modifiedOffset, applier);
		int newAmount = power.Amount + (int)modifiedOffset;
		power.SetAmount(newAmount, silent);
        if (!IsPermanent)
            power.AddPlan((int)modifiedOffset,turns);
		if (modifiers != null)
		{
			await Hook.AfterModifyingPowerAmountGiven(combatState, modifiers, power);
		}
		await Hook.AfterModifyingPowerAmountReceived(combatState, receivedModifiers, power);
		if ((int)modifiedOffset != 0)
		{
			await Hook.AfterPowerAmountChanged(combatState, choiceContext, power, modifiedOffset, applier, cardSource);
		}
		if (power.ShouldRemoveDueToAmount())
		{
			await PowerCmd.Remove(power);
		}
		if (CombatManager.Instance.IsInProgress && owner != null && owner.IsMonster && owner.IsAlive)
		{
			NCreature nCreature = NCombatRoom.Instance?.GetCreatureNode(owner);
			if (nCreature != null)
			{
				try
				{
					await nCreature.UpdateIntent(combatState.Allies);
				}
				catch (ObjectDisposedException ex)
				{
					Log.Error(ex.ToString());
				}
			}
		}
		if (power.IsVisible && CombatManager.Instance.IsInProgress)
		{
			await Cmd.CustomScaledWait(0.1f, 0.25f);
		}
		return newAmount;
	}
	public static async Task Apply(PlayerChoiceContext choiceContext, LibraryTurnsPowerModel power, Creature target, decimal amount,int turns,bool IsPermanent, Creature? applier, CardModel? cardSource, bool silent = false)
	{
		if (CombatManager.Instance.IsEnding || amount == 0m || !target.CanReceivePowers)
		{
			return;
		}
		ICombatState combatState = target.CombatState;
		if (combatState == null)
		{
			return;
		}
		LibraryTurnsPowerModel? powerModel = PowerCmd.FindExistingInstanceForStacking(power, target, applier) as LibraryTurnsPowerModel;
		if (powerModel != null)
		{
			await ModifyAmount(choiceContext, powerModel, amount,turns,IsPermanent, applier, cardSource);
			return;
		}
		power.AssertMutable();
		power.Applier = applier;
		await Hook.BeforePowerAmountChanged(combatState, power, amount, target, applier, cardSource);
		decimal modifiedAmount = amount;
		IEnumerable<AbstractModel> givenModifiers = null;
		if (applier != null && combatState.ContainsCreature(applier))
		{
			modifiedAmount = Hook.ModifyPowerAmountGiven(combatState, power, applier, modifiedAmount, target, cardSource, out givenModifiers);
		}
		modifiedAmount = Hook.ModifyPowerAmountReceived(combatState, power, target, modifiedAmount, applier, out IEnumerable<AbstractModel> receivedModifiers);
		if (combatState.Players.Count > 1 && (target.IsPrimaryEnemy || target.IsSecondaryEnemy) && power.ShouldScaleInMultiplayer)
		{
			modifiedAmount = power.GetScaledAmountForMultiplayer(combatState, applier, modifiedAmount, target, cardSource);
		}
		await power.BeforeApplied(target, modifiedAmount, applier, cardSource);
		if (target.CanReceivePowers)
		{
			power.ApplyInternal(target, modifiedAmount, silent);
			if (modifiedAmount != 0m)
			{
				CombatManager.Instance.History.PowerReceived(combatState, power, modifiedAmount, applier);
			}
			if (power.IsVisible && CombatManager.Instance.IsInProgress)
			{
				await Cmd.CustomScaledWait(0.1f, 0.25f);
			}
			if (target.Side == CombatSide.Player && power.Type == PowerType.Debuff)
			{
				power.SkipNextDurationTick = true;
			}
			if (givenModifiers != null)
			{
				await Hook.AfterModifyingPowerAmountGiven(combatState, givenModifiers, power);
			}
            if(!IsPermanent)
                power.AddPlan((int)modifiedAmount,turns);
			await Hook.AfterModifyingPowerAmountReceived(combatState, receivedModifiers, power);
			if (modifiedAmount != 0m)
			{
				await power.AfterApplied(applier, cardSource);
				await Hook.AfterPowerAmountChanged(combatState, choiceContext, power, modifiedAmount, applier, cardSource);
			}
		}
	}
	public static async Task<IReadOnlyList<T>> Apply<T>(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, decimal amount,int turns,bool IsPermanent, Creature? applier, CardModel? cardSource, bool silent = false) where T : LibraryTurnsPowerModel
	{
		List<T> powers = new List<T>();
		if (targets == null)
		{
			return powers;
		}
		foreach (Creature target in targets)
		{
			T val = await Apply<T>(choiceContext, target, amount, turns,IsPermanent, applier, cardSource, silent);
			if (val != null)
			{
				powers.Add(val);
			}
		}
		return powers;
	}
	public static async Task<T?> Apply<T>(PlayerChoiceContext choiceContext, Creature target, decimal amount,int turns,bool IsPermanent, Creature? applier, CardModel? cardSource, bool silent = false) where T : LibraryTurnsPowerModel
	{
		if (CombatManager.Instance.IsEnding)
		{
			return null;
		}
		if (!target.CanReceivePowers)
		{
			return null;
		}
		LibraryTurnsPowerModel powerModel = ModelDb.Power<T>();
		LibraryTurnsPowerModel power = PowerCmd.FindExistingInstanceForStacking(powerModel, target, applier) as LibraryTurnsPowerModel;
		if (power == null)
		{
			power = powerModel.ToMutable() as LibraryTurnsPowerModel;
			await Apply(choiceContext, power, target, amount, turns, IsPermanent, applier, cardSource, silent);
		}
		else if (await ModifyAmount(choiceContext, power, amount, turns, IsPermanent, applier, cardSource, silent) == 0)
		{
			power = null;
		}
		return power as T;
	}
}
