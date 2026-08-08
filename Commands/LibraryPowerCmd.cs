using LibraryLib.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace LibraryLib.Commands;

/// <summary>
///     类似 <see cref="PowerCmd"/> 的 power 命令便捷方法。
/// </summary>
public static class LibraryPowerCmd
{
    /// <summary>
    ///     若目标尚无该能力则施加，若已有则调整至 <paramref name="amount"/>。
    /// </summary>
    public static async Task<T?> SetAmount<T>(
        Creature target,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
        where T : PowerModel
    {
        T? existingPower = target.GetPower<T>();
        if (existingPower == null)
            return await PowerCmd.Apply<T>(new ThrowingPlayerChoiceContext(), target, amount, applier, cardSource);
        await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), existingPower, amount - existingPower.Amount, applier, cardSource);
        return existingPower;
    }

    /// <summary>
    ///     将同类持续 power（Duration/Turns 通用）的层数和剩余回合精确设置为指定值。
    ///     <paramref name="turns"/> 小于 0 时表示永久；等于 0 时表示当前回合的 DecaySide 回合结束时衰减。
    /// </summary>
    public static async Task<T?> SetAmount<T>(
        Creature target,
        decimal amount,
        int turns,
        Creature? applier,
        CardModel? cardSource
    ) where T : LibraryPowerModel
    {
        if (typeof(LibraryTurnsPowerModel).IsAssignableFrom(typeof(T)))
        {
            LibraryTurnsPowerModel? powerModel = ModelDb.Power<T>() as LibraryTurnsPowerModel;
            if (powerModel == null)
            {
                return null;
            }
            LibraryTurnsPowerModel? existingPower = PowerCmd.FindExistingInstanceForStacking(powerModel, target, applier) as LibraryTurnsPowerModel;
            if (existingPower == null)
            {
                if (amount == 0m)
                {
                    return null;
                }
                LibraryTurnsPowerModel? mutable = powerModel.ToMutable() as LibraryTurnsPowerModel;
                if (mutable == null)
                {
                    return null;
                }
                await Apply(new ThrowingPlayerChoiceContext(), mutable, target, amount, turns, turns < 0, applier, cardSource);
                return target.GetPowerInstances<T>()
                    .Any(instance => ReferenceEquals(instance, mutable))
                    ? mutable as T
                    : null;
            }

            decimal amountDelta = amount - existingPower.Amount;
            existingPower.AmountPlan = turns < 0
                ? new SortedDictionary<int, int>()
                : new SortedDictionary<int, int> { [(existingPower.Owner.CombatState?.RoundNumber ?? 0) + turns] = (int)amount };
            if (amountDelta == 0m)
            {
                return existingPower as T;
            }

            int newAmount = await PowerCmd.ModifyAmount(
                new ThrowingPlayerChoiceContext(),
                existingPower,
                amountDelta,
                applier,
                cardSource);
            return newAmount == 0 ? null : existingPower as T;
        }

        LibraryDurationPowerModel? durationModel = ModelDb.Power<T>() as LibraryDurationPowerModel;
        if (durationModel == null)
        {
            throw new InvalidOperationException($"SetAmount<T> 仅支持 LibraryDurationPowerModel / LibraryTurnsPowerModel：{typeof(T).Name}");
        }

        bool incomingIsPermanent = LibraryDurationPowerModel.IsIncomingPermanent(durationModel, turns);
        LibraryDurationPowerModel? durationExistingPower = target.GetPowerInstances<T>()
	        .OfType<LibraryDurationPowerModel?>()
	        .FirstOrDefault(p => p?.IsPermanent == incomingIsPermanent);
        if (durationExistingPower == null)
        {
            if (amount == 0m)
            {
                return null;
            }
            LibraryDurationPowerModel? mutable = durationModel.ToMutable() as LibraryDurationPowerModel;
            if (mutable == null)
            {
                return null;
            }
            mutable.SetTurnsRemaining(turns, notifyDisplay: false);
            LibraryDurationPowerModel.CorrectDurationSkipFlag(mutable, target);
            await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), mutable, target, amount, applier, cardSource);
            LibraryDurationPowerModel.CorrectDurationSkipFlag(mutable, target);
            return mutable as T;
        }

        decimal durationAmountDelta = amount - durationExistingPower.Amount;
        durationExistingPower.SetTurnsRemaining(turns, notifyDisplay: durationAmountDelta == 0);
        LibraryDurationPowerModel.CorrectDurationSkipFlag(durationExistingPower, target);
        if (durationAmountDelta == 0m)
        {
            return durationExistingPower as T;
        }

        int durationNewAmount = await PowerCmd.ModifyAmount(
            new ThrowingPlayerChoiceContext(),
            durationExistingPower,
            durationAmountDelta,
            applier,
            cardSource);
        return durationNewAmount == 0 ? null : durationExistingPower as T;
    }

    /// <summary>
    ///     施加普通 power。
    /// </summary>
    public static async Task<T?> Apply<T>(
        Creature target,
        decimal amount,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false) where T : PowerModel
    {
        return await PowerCmd.Apply<T>(new ThrowingPlayerChoiceContext(), target, amount, applier, cardSource, silent);
    }

    /// <summary>
    ///     施加持续 power（Duration/Turns 通用）；已有同类实例时叠加层数，并刷新/追加剩余回合。
    ///     <paramref name="turns"/> 小于 0 时表示永久；等于 0 时表示当前回合的 DecaySide 回合结束时衰减。
    /// </summary>
    public static async Task<T?> Apply<T>(
        Creature target,
        decimal amount,
        int turns,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false) where T : LibraryPowerModel
    {
        if (typeof(LibraryTurnsPowerModel).IsAssignableFrom(typeof(T)))
        {
            LibraryTurnsPowerModel? powerModel = ModelDb.Power<T>() as LibraryTurnsPowerModel;
            if (powerModel == null)
            {
                return null;
            }
            LibraryTurnsPowerModel? power = PowerCmd.FindExistingInstanceForStacking(powerModel, target, applier) as LibraryTurnsPowerModel;
            if (power == null)
            {
                power = powerModel.ToMutable() as LibraryTurnsPowerModel;
                if (power == null)
                {
                    return null;
                }
                await Apply(new ThrowingPlayerChoiceContext(), power, target, amount, turns, turns < 0, applier, cardSource, silent);
                if (!target.GetPowerInstances<T>()
                        .Any(instance => ReferenceEquals(instance, power)))
                {
                    power = null;
                }
            }
            else if (await ModifyAmount(new ThrowingPlayerChoiceContext(), power, amount, turns, turns < 0, applier, cardSource, silent) == 0)
            {
                power = null;
            }
            return power as T;
        }

        LibraryDurationPowerModel? durationModel = ModelDb.Power<T>() as LibraryDurationPowerModel;
        if (durationModel == null)
        {
            throw new InvalidOperationException($"Apply<T> 仅支持 LibraryDurationPowerModel / LibraryTurnsPowerModel：{typeof(T).Name}");
        }

        bool incomingIsPermanent = LibraryDurationPowerModel.IsIncomingPermanent(durationModel, turns);
        LibraryDurationPowerModel? durationExistingPower = target.GetPowerInstances<T>()
	        .OfType<LibraryDurationPowerModel?>()
	        .FirstOrDefault(p => p?.IsPermanent == incomingIsPermanent);
        if (durationExistingPower == null)
        {
            if (amount == 0m)
            {
                return null;
            }
            LibraryDurationPowerModel? mutable = durationModel.ToMutable() as LibraryDurationPowerModel;
            if (mutable == null)
            {
                return null;
            }
            mutable.SetTurnsRemaining(turns, notifyDisplay: false);
            LibraryDurationPowerModel.CorrectDurationSkipFlag(mutable, target);
            await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), mutable, target, amount, applier, cardSource, silent);
            LibraryDurationPowerModel.CorrectDurationSkipFlag(mutable, target);
            return mutable as T;
        }

        durationExistingPower.SetTurnsRemaining(turns, notifyDisplay: amount == 0m);
        LibraryDurationPowerModel.CorrectDurationSkipFlag(durationExistingPower, target);
        if (amount == 0m)
        {
            return durationExistingPower as T;
        }

        int newAmount = await PowerCmd.ModifyAmount(
            new ThrowingPlayerChoiceContext(),
            durationExistingPower,
            amount,
            applier,
            cardSource,
            silent);
        return newAmount == 0 ? null : durationExistingPower as T;
    }

    /// <summary>
    ///     调整普通 power 的层数。
    /// </summary>
    public static async Task<int> ModifyAmount<T>(
        Creature target,
        decimal offset,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false) where T : PowerModel
    {
        var power = target.GetPower<T>();
        if (power == null) return 0;
        return await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), power, offset, applier, cardSource, silent);
    }

    /// <summary>
    ///     调整同类持续 power（Duration/Turns 通用）的层数，并刷新/追加剩余回合。
    ///     <paramref name="turns"/> 小于 0 时表示永久；等于 0 时表示当前回合的 DecaySide 回合结束时衰减。
    /// </summary>
    public static async Task<int> ModifyAmount<T>(
        Creature target,
        decimal offset,
        int turns,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false) where T : LibraryPowerModel
    {
        if (typeof(LibraryTurnsPowerModel).IsAssignableFrom(typeof(T)))
        {
            LibraryTurnsPowerModel? powerModel = ModelDb.Power<T>() as LibraryTurnsPowerModel;
            if (powerModel == null)
            {
                return 0;
            }
            LibraryTurnsPowerModel? power = PowerCmd.FindExistingInstanceForStacking(powerModel, target, applier) as LibraryTurnsPowerModel;
            if (power == null)
            {
                return 0;
            }
            return await ModifyAmount(new ThrowingPlayerChoiceContext(), power, offset, turns, turns < 0, applier, cardSource, silent);
        }

        LibraryDurationPowerModel? durationModel = ModelDb.Power<T>() as LibraryDurationPowerModel;
        if (durationModel == null)
        {
            throw new InvalidOperationException($"ModifyAmount<T> 仅支持 LibraryDurationPowerModel / LibraryTurnsPowerModel：{typeof(T).Name}");
        }

        bool incomingIsPermanent = LibraryDurationPowerModel.IsIncomingPermanent(durationModel, turns);
        LibraryDurationPowerModel? durationExistingPower = target.GetPowerInstances<T>()
            .Select(p => p as LibraryDurationPowerModel)
            .FirstOrDefault(p => p != null && p.IsPermanent == incomingIsPermanent);
        if (durationExistingPower == null)
        {
            return 0;
        }

        durationExistingPower.SetTurnsRemaining(turns);
        LibraryDurationPowerModel.CorrectDurationSkipFlag(durationExistingPower, target);
        return await PowerCmd.ModifyAmount(
            new ThrowingPlayerChoiceContext(),
            durationExistingPower,
            offset,
            applier,
            cardSource,
            silent);
    }
	/// <summary>
	/// 	turns代表持续回合数（0表示该回合减少），IsPermanent代表是否永久性改变
	/// </summary>
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
		int previousAmount = power.Amount;
		long rawNewAmount = (long)previousAmount + (int)modifiedOffset;
		int newAmount = (int)Math.Clamp(rawNewAmount, -999_999_999L, 999_999_999L);
		if (!power.AllowNegative && newAmount < 0)
		{
			newAmount = 0;
		}
		power.SetAmount(newAmount, silent);
		int actualDelta = power.Amount - previousAmount;
		bool shouldRemove = power.ShouldRemoveDueToAmount();
        if (!IsPermanent && actualDelta != 0 && !shouldRemove)
            power.AddPlan(actualDelta,turns);
		if (modifiers != null)
		{
			await Hook.AfterModifyingPowerAmountGiven(combatState, modifiers, power);
		}
		await Hook.AfterModifyingPowerAmountReceived(combatState, receivedModifiers, power);
		if (actualDelta != 0)
		{
			await Hook.AfterPowerAmountChanged(combatState, choiceContext, power, actualDelta, applier, cardSource);
		}
		if (shouldRemove)
		{
			power.AmountPlan.Clear();
			await PowerCmd.Remove(power);
		}
		if (CombatManager.Instance.IsInProgress && owner != null && owner.IsMonster && owner.IsAlive)
		{
			NCreature? nCreature = NCombatRoom.Instance?.GetCreatureNode(owner);
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
		return power.Amount;
	}
	/// <summary>
	/// 	turns代表持续回合数（0表示该回合减少），IsPermanent代表是否永久性改变
	/// </summary>
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
			int appliedAmount = (int)modifiedAmount;
			bool shouldApply = appliedAmount != 0
				&& (power.AllowNegative || appliedAmount > 0);
			if (shouldApply)
			{
				power.ApplyInternal(target, appliedAmount, silent);
				CombatManager.Instance.History.PowerReceived(
					combatState,
					power,
					appliedAmount,
					applier);
				if (power.IsVisible && CombatManager.Instance.IsInProgress)
				{
					await Cmd.CustomScaledWait(0.1f, 0.25f);
				}
				if (target.Side == CombatSide.Player && power.Type == PowerType.Debuff)
				{
					power.SkipNextDurationTick = true;
				}
			}
			if (givenModifiers != null)
			{
				await Hook.AfterModifyingPowerAmountGiven(combatState, givenModifiers, power);
			}
			if (shouldApply && !IsPermanent)
				power.AddPlan(power.Amount,turns);
			await Hook.AfterModifyingPowerAmountReceived(combatState, receivedModifiers, power);
			if (shouldApply)
			{
				await power.AfterApplied(applier, cardSource);
				await Hook.AfterPowerAmountChanged(combatState, choiceContext, power, appliedAmount, applier, cardSource);
			}
		}
	}
	/// <summary>
	/// 	turns代表持续回合数（0表示该回合减少），IsPermanent代表是否永久性改变
	/// </summary>
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
	/// <summary>
	/// 	turns代表持续回合数（0表示该回合减少），IsPermanent代表是否永久性改变
	/// </summary>
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
			if (!target.GetPowerInstances<T>()
					.Any(instance => ReferenceEquals(instance, power)))
			{
				power = null;
			}
		}
		else if (await ModifyAmount(choiceContext, power, amount, turns, IsPermanent, applier, cardSource, silent) == 0)
		{
			power = null;
		}
		return power as T;
	}
}
