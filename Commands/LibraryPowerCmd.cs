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
        if (IsTurnsPower<T>())
        {
            if (ModelDb.Power<T>() is not LibraryTurnsPowerModel turnsModel)
                return null;
            if (FindTurnsInstance(turnsModel, target, applier) is not { } existing)
                return amount == 0m ? null : await ApplyNewTurnsPower<T>(turnsModel, target, amount, turns, applier, cardSource, silent: false);

            decimal delta = amount - existing.Amount;
            existing.AmountPlan = turns < 0
                ? new SortedDictionary<int, int>()
                : new SortedDictionary<int, int> { [(existing.Owner.CombatState?.RoundNumber ?? 0) + turns] = (int)amount };
            if (delta == 0m)
                return existing as T;
            int newAmount = await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), existing, delta, applier, cardSource);
            return newAmount == 0 ? null : existing as T;
        }

        LibraryDurationPowerModel durationModel = RequireDurationModel<T>(nameof(SetAmount));
        return FindDurationInstance<T>(target, durationModel, turns) is { } durationPower
            ? await ModifyDurationPower<T>(durationPower, target, amount - durationPower.Amount, turns, applier, cardSource, silent: false)
            : await ApplyNewDurationPower<T>(durationModel, target, amount, turns, applier, cardSource, silent: false);
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
        if (IsTurnsPower<T>())
        {
            if (ModelDb.Power<T>() is not LibraryTurnsPowerModel turnsModel)
                return null;
            if (FindTurnsInstance(turnsModel, target, applier) is not { } existing)
                return await ApplyNewTurnsPower<T>(turnsModel, target, amount, turns, applier, cardSource, silent);
            int newAmount = await ModifyAmount(new ThrowingPlayerChoiceContext(), existing, amount, turns, turns < 0, applier, cardSource, silent);
            return newAmount == 0 ? null : existing as T;
        }

        LibraryDurationPowerModel durationModel = RequireDurationModel<T>(nameof(Apply));
        return FindDurationInstance<T>(target, durationModel, turns) is { } durationPower
            ? await ModifyDurationPower<T>(durationPower, target, amount, turns, applier, cardSource, silent)
            : await ApplyNewDurationPower<T>(durationModel, target, amount, turns, applier, cardSource, silent);
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
        if (IsTurnsPower<T>())
        {
            return ModelDb.Power<T>() is LibraryTurnsPowerModel turnsModel && FindTurnsInstance(turnsModel, target, applier) is { } existing
                ? await ModifyAmount(new ThrowingPlayerChoiceContext(), existing, offset, turns, turns < 0, applier, cardSource, silent)
                : 0;
        }

        LibraryDurationPowerModel durationModel = RequireDurationModel<T>(nameof(ModifyAmount));
        if (FindDurationInstance<T>(target, durationModel, turns) is not { } durationPower)
            return 0;
        durationPower.SetTurnsRemaining(turns);
        LibraryDurationPowerModel.CorrectDurationSkipFlag(durationPower, target);
        return await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), durationPower, offset, applier, cardSource, silent);
    }

    private static bool IsTurnsPower<T>() => typeof(LibraryTurnsPowerModel).IsAssignableFrom(typeof(T));

    private static LibraryTurnsPowerModel? FindTurnsInstance(LibraryTurnsPowerModel canonical, Creature target, Creature? applier) =>
        PowerCmd.FindExistingInstanceForStacking(canonical, target, applier) as LibraryTurnsPowerModel;

    /// <summary>Applies a fresh instance; null when nothing ended up on the target.</summary>
    private static async Task<T?> ApplyNewTurnsPower<T>(LibraryTurnsPowerModel canonical, Creature target, decimal amount, int turns, Creature? applier, CardModel? cardSource, bool silent)
        where T : PowerModel
    {
        if (canonical.ToMutable() is not LibraryTurnsPowerModel mutable)
            return null;
        await Apply(new ThrowingPlayerChoiceContext(), mutable, target, amount, turns, turns < 0, applier, cardSource, silent);
        return target.GetPowerInstances<T>().Any(instance => ReferenceEquals(instance, mutable)) ? mutable as T : null;
    }

    private static LibraryDurationPowerModel RequireDurationModel<T>(string method) where T : PowerModel =>
        ModelDb.Power<T>() as LibraryDurationPowerModel
        ?? throw new InvalidOperationException($"{method}<T> 仅支持 LibraryDurationPowerModel / LibraryTurnsPowerModel：{typeof(T).Name}");

    /// <summary>Permanent and timed instances of the same duration power never stack with each other.</summary>
    private static LibraryDurationPowerModel? FindDurationInstance<T>(Creature target, LibraryDurationPowerModel canonical, int turns) where T : PowerModel
    {
        bool incomingIsPermanent = LibraryDurationPowerModel.IsIncomingPermanent(canonical, turns);
        return target.GetPowerInstances<T>()
            .OfType<LibraryDurationPowerModel>()
            .FirstOrDefault(power => power.IsPermanent == incomingIsPermanent);
    }

    private static async Task<T?> ApplyNewDurationPower<T>(LibraryDurationPowerModel canonical, Creature target, decimal amount, int turns, Creature? applier, CardModel? cardSource, bool silent)
        where T : PowerModel
    {
        if (amount == 0m || canonical.ToMutable() is not LibraryDurationPowerModel mutable)
            return null;
        mutable.SetTurnsRemaining(turns, notifyDisplay: false);
        LibraryDurationPowerModel.CorrectDurationSkipFlag(mutable, target);
        await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), mutable, target, amount, applier, cardSource, silent);
        LibraryDurationPowerModel.CorrectDurationSkipFlag(mutable, target);
        return mutable as T;
    }

    /// <summary>Resets the remaining turns and changes the amount by <paramref name="delta"/>; null when the power was removed.</summary>
    private static async Task<T?> ModifyDurationPower<T>(LibraryDurationPowerModel power, Creature target, decimal delta, int turns, Creature? applier, CardModel? cardSource, bool silent)
        where T : PowerModel
    {
        power.SetTurnsRemaining(turns, notifyDisplay: delta == 0m);
        LibraryDurationPowerModel.CorrectDurationSkipFlag(power, target);
        if (delta == 0m)
            return power as T;
        int newAmount = await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), power, delta, applier, cardSource, silent);
        return newAmount == 0 ? null : power as T;
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
