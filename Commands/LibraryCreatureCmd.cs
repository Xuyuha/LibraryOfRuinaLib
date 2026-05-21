using System.Text.Json.Serialization;
using Godot;
using Library.Entities.Creatures;
using Library.Hooks;
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
					NDamageNumVfx nDamageNumVfx = NDamageNumVfx.Create(receiver, item);
					if (nDamageNumVfx != null)
					{
						if (vfxContainer != null)
						{
							vfxContainer.AddChildSafely(nDamageNumVfx);
						}
						else
						{
							NRun.Instance.GlobalUi.AddChildSafely(nDamageNumVfx);
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

		await ApplyStaggerReduction(choiceContext, results, props, dealer, cardSource);

		await Cmd.CustomScaledWait(0.1f, 0.2f);
		return results;
	}
		public static async Task<IEnumerable<LibraryChaoResult>> ChaoDamage(PlayerChoiceContext choiceContext, IEnumerable<Creature> targets, decimal damageAmount, ValueProp props, Creature? dealer, CardModel? cardSource ,LibraryDamageType type = LibraryDamageType.None)
		//我暂时没用这个方法，走的是我当时自己用的简易混乱值判定，根据原始伤害对原版attack commmand进行patch，因此也没有检测攻击类型，后续选择一个统一的方法来用。
		{
			if (dealer != null && dealer.IsDead)
			{
				return targets.Select((Creature t) => new LibraryChaoResult(t, props)).ToList();
			}
			List<LibraryChaoResult> results = new List<LibraryChaoResult>();
			List<Creature> targetList = targets.ToList();
			if (targetList.Count == 0)
			{
				return results;
			}
			ICombatState combatState = targetList[0].CombatState;
			IRunState runState = IRunState.GetFrom(targetList.Append(dealer).OfType<Creature>());
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
			results.Add(ChaoResult);
		}
		List<Creature> StunedCreatures = new List<Creature>();
		foreach (LibraryChaoResult Result in results)
		{
			Creature Target = Result.Receiver;
			if (Result.ChaoValueAmount > 0)
			{
				await LibraryHooks.AfterCurrentChaoValueChanged(runState, combatState, Target, -Result.ChaoValueAmount,type);
			}
			if (combatState != null)
				{
				await LibraryHooks.AfterChaoDamageGiven(choiceContext, combatState, dealer, Result, props, Target, cardSource,type);
			}
			if (!Result.WasStun || !Target.IsDead)
			{
				await LibraryHooks.AfterChaoDamageReceived(choiceContext, runState, combatState, Target, Result, props, dealer, cardSource,type);
			}
			else
			{
				await LibraryHooks.AfterStun(runState, combatState, Target);
				StunedCreatures.Add(Target);
			}	
		}
		foreach (var c in StunedCreatures)
		{
			await CreatureCmd.Stun(c);
		}
		await Cmd.CustomScaledWait(0.1f, 0.2f);
		return results;
	}

	internal static async Task ApplyStaggerReduction(PlayerChoiceContext choiceContext, IEnumerable<DamageResult> results, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		foreach (DamageResult dmgResult in results)
		{
			if (dmgResult.Receiver is not LibraryCreature libCreature
				|| libCreature.MaxChaoValue <= 0
				|| dmgResult.UnblockedDamage <= 0
				|| !libCreature.IsAlive
				|| libCreature.IsStunPending)
				continue;

			bool isOrbDmg = LibraryStaggerResistanceOrbDamageContext.IsOrbDamage(dealer);
			if (props.HasFlag(ValueProp.Unpowered) && !isOrbDmg)
				continue;

			// 使用缩放前的原始HP作为分母，避免多人HP缩放导致混乱值扣减被稀释
			int effectiveMaxHp = libCreature.MonsterMaxHpBeforeModification ?? libCreature.MaxHp;

			int loss;
			if (props.HasFlag(ValueProp.Move) && !props.HasFlag(ValueProp.Unpowered) && cardSource?.Type == CardType.Attack)
				loss = LibraryDamageCalculate.CalculateAttackCardResistanceLoss(
					dmgResult.UnblockedDamage, 1, effectiveMaxHp);
			else
				loss = LibraryDamageCalculate.CalculateOtherDamageResistanceLoss(
					dmgResult.UnblockedDamage, effectiveMaxHp);

			if (loss > 0)
			{
				loss = LibraryDamageCalculate.ApplyChaosResistanceMultiplier(loss, cardSource, libCreature);
				await ReduceStaggerResistance(choiceContext, libCreature, loss);
			}
		}
	}

	public static async Task ReduceStaggerResistance(PlayerChoiceContext ctx, LibraryCreature creature, int amount)
	{
		int next = Math.Max(0, creature.CurrentChaoValue - amount);
		if (next == creature.CurrentChaoValue) return;
		creature.SetCurrentChaoValueInternal(next);
		if (next <= 0 && !creature.RestoreChaoOnNextOwnerTurn && creature.IsAlive)
		{
			creature.RestoreChaoOnNextOwnerTurn = true;
			creature.SaveAndSetStunResistance();
			await CreatureCmd.Stun(creature, creature.Monster?.NextMove.Id);
		}
	}

	public static async Task ReduceStaggerResistance(Creature creature, int amount, Creature? dealer, CardModel? cardSource)
	{
		if (creature is not LibraryCreature lc || lc.MaxChaoValue <= 0) return;
		await ReduceStaggerResistance(new ThrowingPlayerChoiceContext(), lc, amount);
	}

	public static void SetStaggerResistance(Creature creature, int value)
	{
		if (creature is not LibraryCreature lc) return;
		lc.SetCurrentChaoValueInternal(Math.Max(0, value));
	}

	public static void SetMaxStaggerResistance(Creature creature, int value, bool clampCurrent = true)
	{
		if (creature is not LibraryCreature lc) return;
		lc.SetMaxChaoValueInternal(value);
		if (clampCurrent)
			lc.SetCurrentChaoValueInternal(Math.Min(lc.CurrentChaoValue, value));
	}
}