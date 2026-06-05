using System.Text.Json.Serialization;
using Godot;
using Library.Entities.Creatures;
using Library.Hooks;
using Library.Patches;
using Library.Resistance;
using Library.Resistance.Patches;
using Library.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.ValueProps;
public static class LibraryCreatureCmd
{
	public static async Task GainBlock(Creature creature, CardPlay? cardPlay, LibraryDice dice, bool fast = false)
	{
		dice.Roll(cardPlay.Card.Owner);
		int amount = dice.CurrentBaseValue;
		await CreatureCmd.GainBlock(creature, amount, ValueProp.Move, cardPlay, fast);
		await dice.TriggerDiceEffect(new BlockingPlayerChoiceContext(), cardPlay);
	}
	public static async Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext choiceContext, Creature target, DamageVar damageVar, CardModel cardSource ,LibraryDamageType type = LibraryDamageType.None)
	{
		return await Damage(choiceContext, target, damageVar.BaseValue, damageVar.Props, cardSource,type);
	}

	public static async Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, CardModel cardSource ,LibraryDamageType type = LibraryDamageType.None)
	{
		return await Damage(choiceContext, new List<Creature> { target }, amount, props, cardSource.Owner.Creature as Creature, cardSource,type);
	}

	public static async Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext choiceContext, IEnumerable<Creature> targets, DamageVar damageVar, Creature dealer ,LibraryDamageType type = LibraryDamageType.None)
	{
		return await Damage(choiceContext, targets, damageVar.BaseValue, damageVar.Props, dealer,type);
	}

	public static async Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext choiceContext, IEnumerable<Creature> targets, decimal amount, ValueProp props, Creature dealer ,LibraryDamageType type = LibraryDamageType.None)
	{
		return await Damage(choiceContext, targets, amount, props, dealer, null,type);
	}

	public static async Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext choiceContext, Creature target, DamageVar damageVar, Creature dealer ,LibraryDamageType type = LibraryDamageType.None)
	{
		return await Damage(choiceContext, target, damageVar.BaseValue, damageVar.Props, dealer,type);
	}

	public static async Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature dealer ,LibraryDamageType type = LibraryDamageType.None)
	{
		return await Damage(choiceContext, new List<Creature> { target }, amount, props, dealer, null,type);
	}

	public static async Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext choiceContext, Creature target, DamageVar damageVar, Creature? dealer, CardModel? cardSource ,LibraryDamageType type = LibraryDamageType.None)
	{
		return await Damage(choiceContext, new List<Creature> { target }, damageVar.BaseValue, damageVar.Props, dealer, cardSource,type);
	}

	public static async Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource ,LibraryDamageType type = LibraryDamageType.None)
	{
		return await Damage(choiceContext, new List<Creature> { target }, amount, props, dealer, cardSource,type);
	}

	public static async Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext choiceContext, IEnumerable<Creature> targets, DamageVar damageVar, Creature? dealer, CardModel? cardSource ,LibraryDamageType type = LibraryDamageType.None)
	{
		return await Damage(choiceContext, targets, damageVar.BaseValue, damageVar.Props, dealer, cardSource,type);
	}

	public static async Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext choiceContext, IEnumerable<Creature> targets, decimal damageAmount, ValueProp props, Creature? dealer, CardModel? cardSource ,LibraryDamageType type = LibraryDamageType.None)
	{
		if (dealer != null && dealer.IsDead)
		{
			return targets.Select((Creature t) => new DamageResult(t, props)).ToList();
		}
		List<DamageResult> results = new List<DamageResult>();
		List<Creature> targetList = targets.ToList();
		if (targetList.Count == 0)
		{
			return results;
		}
		ICombatState combatState = targetList[0].CombatState;
		IRunState runState = IRunState.GetFrom(targetList.Append(dealer).OfType<Creature>());
		foreach (Creature originalTarget in targetList)
		{
			if(originalTarget.IsPlayer)
			{
				await CreatureCmd.Damage(choiceContext, originalTarget, damageAmount, props, dealer, cardSource);
				continue;
			}
			if (originalTarget.IsDead)
			{
				continue;
			}
			IEnumerable<AbstractModel> modifiers;
			Log.Info("LibraryDamage");
			decimal modifiedAmountbefore = LibraryHooks.ModifyDamage(runState, combatState, originalTarget, dealer, damageAmount, props, cardSource, ModifyDamageHookType.All, CardPreviewMode.None, out modifiers,type);
			decimal modifiedAmount = LibraryDamageCalculate.CalculateDamage(modifiedAmountbefore, originalTarget as LibraryCreature, props, type);
			await LibraryHooks.AfterModifyingDamageAmount(runState, combatState, cardSource, modifiers,type);
			await LibraryHooks.BeforeDamageReceived(choiceContext, runState, combatState, originalTarget, modifiedAmount, props, dealer, cardSource,type);  
			Creature creature = originalTarget.PetOwner?.Creature ?? originalTarget;
			decimal blockedDamage = creature.DamageBlockInternal(modifiedAmount, props);
			decimal unblockedDamage = LibraryHooks.ModifyHpLostBeforeOsty(runState, combatState, originalTarget, Math.Max(modifiedAmount - blockedDamage, 0m), props, dealer, cardSource, out modifiers,type);
			await LibraryHooks.AfterModifyingHpLostBeforeOsty(runState, combatState, modifiers,type);
			Creature unblockedDamageTarget = ((combatState == null) ? originalTarget : LibraryHooks.ModifyUnblockedDamageTarget(combatState, originalTarget, unblockedDamage, props, dealer,type));
			unblockedDamage = LibraryHooks.ModifyHpLostAfterOsty(runState, combatState, unblockedDamageTarget, unblockedDamage, props, dealer, cardSource, out modifiers,type);
			await LibraryHooks.AfterModifyingHpLostAfterOsty(runState, combatState, modifiers,type);
			DamageResult unblockedDamageResult = unblockedDamageTarget.LoseHpInternal(unblockedDamage, props);
			List<DamageResult> damageResults = new List<DamageResult>(1) { unblockedDamageResult };
			bool wasBlockBroken = originalTarget.Block <= 0 && blockedDamage > 0m;
			bool wasFullyBlocked = !props.HasFlag(ValueProp.Unblockable) && (blockedDamage > 0m || originalTarget.Block > 0) && (int)unblockedDamage == 0;
			if (originalTarget == unblockedDamageTarget)
			{
				unblockedDamageResult.BlockedDamage = (int)blockedDamage;
				unblockedDamageResult.WasBlockBroken = wasBlockBroken;
				unblockedDamageResult.WasFullyBlocked = wasFullyBlocked;
			}
			else
			{
				decimal originalTargetDamage = LibraryHooks.ModifyHpLostAfterOsty(runState, combatState, originalTarget, unblockedDamageResult.OverkillDamage, props, dealer, cardSource, out modifiers,type);
				await LibraryHooks.AfterModifyingHpLostAfterOsty(runState, combatState, modifiers,type);
				DamageResult damageResult = ((!(originalTargetDamage > 0m)) ? new DamageResult(originalTarget, props) : originalTarget.LoseHpInternal(originalTargetDamage, props));
				damageResult.BlockedDamage = (int)blockedDamage;
				damageResult.WasBlockBroken = wasBlockBroken;
				damageResult.WasFullyBlocked = wasFullyBlocked;
				damageResults.Add(damageResult);
			}
			List<Task> hitTriggers = new List<Task>();
			foreach (DamageResult item in damageResults)
			{
				int damage = item.UnblockedDamage + item.OverkillDamage;
				Creature receiver = item.Receiver;
				if (CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsEnding)
				{
					CombatManager.Instance.History.DamageReceived(combatState, receiver, dealer, item, cardSource);
				}
				if (item.WasFullyBlocked)
				{
					continue;
				}
				Node vfxContainer = receiver.GetVfxContainer();
				if (damage > 0 || (modifiedAmount == 0m && item.Receiver == unblockedDamageTarget))
				{
					LibraryRuinaDamageNumberVfx? damageVfx = LibraryRuinaDamageNumberVfx.CreatePhysical(receiver, item, type);
					if (damageVfx != null)
					{
						if (vfxContainer != null)
						{
							vfxContainer.AddChildSafely(damageVfx);
						}
						else
						{
							NRun.Instance.GlobalUi.AddChildSafely(damageVfx);
						}
					}
				}
				if (damage > 0)
				{
					vfxContainer?.AddChildSafely(NHitSparkVfx.Create(receiver));
					if (receiver != dealer && !props.HasFlag(ValueProp.SkipHurtAnim))
					{
						hitTriggers.Add(CreatureCmd.TriggerAnim(receiver, "Hit", 0f));
						if (receiver.IsMonster && receiver.Monster.HasHurtSfx)
						{
							SfxCmd.Play(receiver.Monster.HurtSfx);
						}
					}
					MapPointHistoryEntry mapPointHistoryEntry = receiver.Player?.RunState.CurrentMapPointHistoryEntry;
					if (mapPointHistoryEntry != null)
					{
						mapPointHistoryEntry.GetEntry(receiver.Player.NetId).DamageTaken += item.UnblockedDamage;
					}
				}
				await Task.WhenAll(hitTriggers);
				if (damage <= 0)
				{
					continue;
				}
				if (damageResults.Any((DamageResult r) => r.WasBlockBroken))
				{
					SfxCmd.Play("event:/sfx/block_break");
				}
				if (LocalContext.IsMe(originalTarget) && (!CombatManager.Instance.IsInProgress || originalTarget.GetHpPercentRemaining() <= 0.25))
				{
					PlayerHurtVignetteHelper.Play();
				}
				if (originalTarget.Side == CombatSide.Enemy)
				{
					SfxCmd.PlayDamage(originalTarget.Monster, unblockedDamageResult.UnblockedDamage);
				}
				if (CombatManager.Instance.IsInProgress || LocalContext.ContainsMe(targetList))
				{
					if (unblockedDamageResult.UnblockedDamage < 6)
					{
						NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);
					}
					else if (unblockedDamageResult.UnblockedDamage < 11)
					{
						NGame.Instance?.ScreenShake(ShakeStrength.Medium, ShakeDuration.Short);
					}
					else
					{
						NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Short);
					}
				}
			}
			results.AddRange(damageResults);
		}
		List<Creature> killedCreatures = new List<Creature>();
		foreach (DamageResult unblockedDamageResult in results)
		{
			Creature originalTarget = unblockedDamageResult.Receiver;
			if (unblockedDamageResult.WasBlockBroken)
			{
				await LibraryHooks.AfterBlockBroken(originalTarget.CombatState, originalTarget,type);
			}
			if (unblockedDamageResult.UnblockedDamage > 0)
			{
				await LibraryHooks.AfterCurrentHpChanged(runState, combatState, originalTarget, -unblockedDamageResult.UnblockedDamage,type);
			}
			if (dealer != null && dealer.Player != null && originalTarget.Player == null)
			{
				dealer.Player.ExtraFields.DamageDealt += unblockedDamageResult.UnblockedDamage;
			}
			if (combatState != null)
			{
				await LibraryHooks.AfterDamageGiven(choiceContext, combatState, dealer, unblockedDamageResult, props, originalTarget, cardSource,type);
			}
			if (!unblockedDamageResult.WasTargetKilled || !originalTarget.IsDead)
			{
				await LibraryHooks.AfterDamageReceived(choiceContext, runState, combatState, originalTarget, unblockedDamageResult, props, dealer, cardSource,type);
			}
			else
			{
				killedCreatures.Add(originalTarget);
			}
			if (unblockedDamageResult.WasFullyBlocked && CombatManager.Instance.IsInProgress)
			{
				SfxCmd.Play("event:/sfx/block_hit");
				Node vfxContainer2 = unblockedDamageResult.Receiver.GetVfxContainer();
				vfxContainer2?.AddChildSafely(NBlockSparkVfx.Create(unblockedDamageResult.Receiver));
				vfxContainer2?.AddChildSafely(NDamageBlockedVfx.Create(unblockedDamageResult.Receiver));
				NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);
			}
		}
		await CreatureCmd.Kill(killedCreatures);

		await Cmd.CustomScaledWait(0.1f, 0.2f);
		return results;
	}
	public static async Task<IEnumerable<LibraryChaoResult>?> ChaoDamage(PlayerChoiceContext choiceContext, IEnumerable<Creature> targets, decimal damageAmount, ValueProp props, Creature? dealer, CardModel? cardSource ,LibraryDamageType type = LibraryDamageType.None, IEnumerable<DamageResult>? damageResults = null)
	//我暂时没用这个方法，走的是我当时自己用的简易混乱值判定，根据原始伤害对原版attack commmand进行patch，因此也没有检测攻击类型，后续选择一个统一的方法来用。
	{
		List<LibraryChaoResult> results = new List<LibraryChaoResult>();
		targets = targets.Where((Creature c) => c is LibraryCreature lc && lc.HasChaoResistance);
		if(!targets.Any())
		{
			return results;
		}
		if (dealer != null && dealer.IsDead)
		{
			return targets.Select((Creature t) => new LibraryChaoResult(t, props)).ToList();
		}
		List<Creature> targetList = targets.ToList();
		if (targetList.Count == 0)
		{
			return results;
		}
		ICombatState combatState = targetList[0].CombatState;
		IRunState runState = IRunState.GetFrom(targetList.Append(dealer).OfType<Creature>());
		List<DamageResult>? damageResultList = damageResults?.ToList();
		foreach (Creature Target in targetList)
		{
			if(!Target.IsMonster)
			{
				continue;
			}
			if (Target.IsDead)
			{
				continue;
			}
			IEnumerable<AbstractModel> modifiers;
			Log.Info("LibraryChaoDamage");
			decimal modifiedAmountbefore = LibraryHooks.ModifyChaoDamage(runState, combatState, Target, dealer,damageAmount, props, cardSource, ModifyChaoDamageHookType.All, CardPreviewMode.None, out modifiers,type);
			decimal modifiedAmount = LibraryDamageCalculate.CalculateChaoAmount(modifiedAmountbefore,Target as LibraryCreature, props, type);
			modifiedAmount = ApplyBlockedDamageChaoCap(Target, modifiedAmount, damageResultList);
			await LibraryHooks.AfterModifyingChaoAmount(runState, combatState, cardSource, modifiers,type);
			await LibraryHooks.BeforeChaoDamageReceived(choiceContext, runState, combatState, Target, modifiedAmount, props, dealer, cardSource,type);  
			LibraryChaoResult ChaoResult = (Target as LibraryCreature).LoseChaoValueInternal(modifiedAmount, props);
			List<Task> hitTriggers = new List<Task>();
			// 混乱伤害反馈
			// foreach (DamageResult item in damageResults) 
			// {
			// 	int damage = item.UnblockedDamage + item.OverkillDamage;
			// 	Creature receiver = item.Receiver;
			// 	if (CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsEnding)
			// 	{
			// 		CombatManager.Instance.History.DamageReceived(combatState, receiver, dealer, item, cardSource);
			// 	}
			// 	if (item.WasFullyBlocked)
			// 	{
			// 		continue;
			// 	}
			// 	Node vfxContainer = receiver.GetVfxContainer();
			// 	if (damage > 0 || (modifiedAmount == 0m && item.Receiver == unblockedDamageTarget))
			// 	{
			// 		NDamageNumVfx nDamageNumVfx = NDamageNumVfx.Create(receiver, item);
			// 		if (nDamageNumVfx != null)
			// 		{
			// 			if (vfxContainer != null)
			// 			{
			// 				vfxContainer.AddChildSafely(nDamageNumVfx);
			// 			}
			// 			else
			// 			{
			// 				NRun.Instance.GlobalUi.AddChildSafely(nDamageNumVfx);
			// 			}
			// 		}
			// 	}
			// }
			if (ChaoResult != null && (ChaoResult.ChaoValueAmount > 0 || modifiedAmount == 0m))
			{
				Node vfxContainer = Target.GetVfxContainer();
				LibraryRuinaDamageNumberVfx? chaoVfx = LibraryRuinaDamageNumberVfx.CreateChaos(Target, ChaoResult, type);
				if (chaoVfx != null)
				{
					if (vfxContainer != null)
					{
						vfxContainer.AddChildSafely(chaoVfx);
					}
					else
					{
						NRun.Instance.GlobalUi.AddChildSafely(chaoVfx);
					}
				}
			}
			results.Add(ChaoResult);
		}
		List<LibraryCreature> StunedCreatures = new List<LibraryCreature>();
		foreach (LibraryChaoResult Result in results)
		{
			LibraryCreature Target = Result.Receiver as LibraryCreature;
			if (combatState != null)
			{
				await LibraryHooks.AfterCurrentChaoValueChanged(runState, combatState, Target, -Result.ChaoValueAmount,type);
				await LibraryHooks.AfterChaoDamageGiven(choiceContext, combatState, dealer, Result, props, Target, cardSource,type);
				await LibraryHooks.AfterChaoDamageReceived(choiceContext, runState, combatState, Target, Result, props, dealer, cardSource,type);
			}
			if(Result.WasStun)
			{
				StunedCreatures.Add(Target);
			}	
		}
		foreach (var c in StunedCreatures)
		{
			await Stun(c);
		}
		await Cmd.CustomScaledWait(0.1f, 0.2f);
		return results;
	}

	private static decimal ApplyBlockedDamageChaoCap(Creature target, decimal chaoDamage, IEnumerable<DamageResult>? damageResults)
	{
		if (damageResults == null)
		{
			return chaoDamage;
		}

		DamageResult? damageResult = damageResults.FirstOrDefault((DamageResult result) => result.Receiver == target);
		if (damageResult == null)
		{
			return 0m;
		}

		if (damageResult.WasFullyBlocked || damageResult.UnblockedDamage <= 0)
		{
			return 0m;
		}

		if (damageResult.BlockedDamage > 0)
		{
			return Math.Min(damageResult.UnblockedDamage, chaoDamage);
		}

		return chaoDamage;
	}
	public static async Task SetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer ,LibraryDamageType type ,LibraryResistanceLevel resistanceValue)
	{
		if(!target.HasChaoResistance)return;
		ICombatState combatState = target.CombatState;
		if(!LibraryHooks.TrySetChaoResistance(combatState,choiceContext, target, dealer,type,resistanceValue))return;
		await LibraryHooks.BeforeSetChaoResistance(combatState, choiceContext, target, dealer, type, resistanceValue);
		target.SetChaoResistance(type,resistanceValue);
		await LibraryHooks.AfterSetChaoResistance(combatState, choiceContext, target, dealer, type);
	}
	public static async Task SetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer ,LibraryDamageType type,LibraryResistanceLevel resistanceValue)
	{
		ICombatState combatState = target.CombatState;
		if(!LibraryHooks.TrySetPhysicalResistance(combatState,choiceContext, target, dealer,type,resistanceValue))return;
		await LibraryHooks.BeforeSetPhysicalResistance(combatState, choiceContext, target, dealer, type, resistanceValue);
		target.SetPhysicalResistance(type,resistanceValue);
		await LibraryHooks.AfterSetPhysicalResistance(combatState, choiceContext, target, dealer, type);
	}	
	public static async Task SetCurrentChaoValue(LibraryCreature creature, decimal amount)
	{
		bool flag = creature.IsDead && amount > 0m;
		decimal num = creature.CurrentChaoValue;
		creature.SetCurrentChaoValueInternal(amount);
		decimal changedAmount = creature.CurrentChaoValue - num;
		if (changedAmount != 0m)
		{
			await LibraryHooks.AfterCurrentChaoValueChanged(creature.Player?.RunState ?? creature.CombatState.RunState, creature.CombatState, creature, changedAmount,LibraryDamageType.None);
		}
		if (creature.CurrentChaoValue == 0 && !creature.IsStunned && creature.MaxChaoValue!=0)
		{
			await Stun(creature);
		}
	}
	public static async Task Stun(LibraryCreature creature, string? nextMoveId = null)
	{
		await Stun(creature, (IReadOnlyList<Creature> _) => Task.CompletedTask, nextMoveId);
	}

	public static async Task Stun(LibraryCreature creature, Func<IReadOnlyList<Creature>, Task> stunMove, string? nextMoveId = null)
	{
		await LibraryHooks.BeforeStun(creature.CombatState, creature);
		creature.StunInternal(Wrapper, nextMoveId);
		await LibraryHooks.AfterStun(creature.CombatState, creature);
		return;
		async Task Wrapper(IReadOnlyList<Creature> c)
		{
			NStunnedVfx vfx = NStunnedVfx.Create(creature);
			if (vfx != null)
			{
				Node vfxContainer = creature.GetVfxContainer();
				if (vfxContainer != null)
				{
					Callable.From(delegate
					{
						vfxContainer.AddChildSafely(vfx);
					}).CallDeferred();
				}
			}
			await stunMove(c);
		}
	}

	public static async Task GainMaxChaoValue(LibraryCreature creature, decimal amount)
	{
		if (amount < 0m)
		{
			throw new ArgumentException("amount must be non-negative. Use LoseMaxHp for max HP loss.");
		}
		decimal num = await SetMaxChaoValue(creature, (decimal)creature.MaxChaoValue + amount);
		await HealChaoValue(creature, num);
	}

	public static async Task LoseMaxChaoValue(PlayerChoiceContext choiceContext, LibraryCreature creature, decimal amount, bool isFromCard)
	{
		if (amount < 0m)
		{
			throw new ArgumentException("amount must be non-negative. Use GainMaxHp for max HP gain.");
		}
		decimal newMaxChaoValue = (decimal)creature.MaxChaoValue - amount;
		MapPointHistoryEntry mapPointHistoryEntry = creature.Player?.RunState.CurrentMapPointHistoryEntry;
		if (mapPointHistoryEntry != null)
		{
			mapPointHistoryEntry.GetEntry(creature.Player.NetId).MaxHpLost += (int)amount;
		}
		if (newMaxChaoValue < (decimal)creature.CurrentChaoValue)
		{
			await ChaoDamage(choiceContext, new List<LibraryCreature>() { creature }, (decimal)creature.CurrentChaoValue - newMaxChaoValue, isFromCard ? (ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move) : (ValueProp.Unblockable | ValueProp.Unpowered), null, null);
		}
		await SetMaxChaoValue(creature, Math.Max(1.0m, newMaxChaoValue));
	}

	public static async Task<decimal> SetMaxChaoValue(LibraryCreature creature, decimal amount)
	{
		int oldMaxChaoValue = creature.MaxChaoValue;
		creature.SetMaxChaoValueInternal(Math.Max(0m, amount));
		int newMaxChaoValue = creature.MaxChaoValue;
		if (creature.MaxChaoValue <= 0)
		{
			await Stun(creature);
		}
		return newMaxChaoValue - oldMaxChaoValue;
	}
	public static async Task HealChaoValue(LibraryCreature creature, decimal amount)
	{
		if (CombatManager.Instance.IsEnding && !creature.IsPlayer)
		{
			return;
		}
		decimal num = Math.Min(amount, creature.MaxChaoValue - creature.CurrentChaoValue);
		creature.HealChaoInternal(num);
		if (CombatManager.Instance.IsInProgress)
		{
			await Cmd.CustomScaledWait(0.1f, 0.25f);
		}
		if (num > 0m && creature.CombatState != null)
		{
			await LibraryHooks.AfterCurrentChaoValueChanged(creature.Player?.RunState ?? creature.CombatState.RunState, creature.CombatState, creature, num,LibraryDamageType.None);
		}
	}

	public static async Task SetMaxAndCurrentChaoValue(LibraryCreature creature, decimal amount)
	{
		await SetMaxChaoValue(creature, amount);
		await SetCurrentChaoValue(creature, amount);
	}
}
