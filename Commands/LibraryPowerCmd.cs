using Library.Models;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Library.Utils;

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
    ///     将同类持续 power 的层数和剩余回合精确设置为指定值。
    ///     <paramref name="turns"/> 小于等于 0 时表示永久。
    /// </summary>
    public static async Task<T?> SetAmount<T>(
        Creature target,
        decimal amount,
        int turns,
        Creature? applier,
        CardModel? cardSource
    ) where T : LibraryDurationPowerModel
    {
        T? existingPower = LibraryDurationPowerModel.FindStackablePower<T>(target, turns);
        if (existingPower == null)
            return await LibraryDurationPowerModel.ApplyWithDuration<T>(target, amount, turns, applier, cardSource);

        decimal amountDelta = amount - existingPower.Amount;
        existingPower.SetTurnsRemaining(turns, notifyDisplay: amountDelta == 0);
        LibraryDurationPowerModel.CorrectDurationSkipFlag(existingPower, target);
        if (amountDelta == 0)
            return existingPower;

        int newAmount = await PowerCmd.ModifyAmount(
            new ThrowingPlayerChoiceContext(),
            existingPower,
            amountDelta,
            applier,
            cardSource);
        return newAmount == 0 ? null : existingPower;
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
    ///     施加持续 power；已有同类实例时叠加层数，并把剩余回合刷新为 <paramref name="turns"/>。
    /// </summary>
    public static async Task<T?> Apply<T>(
        Creature target,
        decimal amount,
        int turns,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false) where T : LibraryDurationPowerModel
    {
        T? existingPower = LibraryDurationPowerModel.FindStackablePower<T>(target, turns);
        if (existingPower == null)
            return await LibraryDurationPowerModel.ApplyWithDuration<T>(target, amount, turns, applier, cardSource, silent);

        existingPower.SetTurnsRemaining(turns, notifyDisplay: amount == 0m);
        LibraryDurationPowerModel.CorrectDurationSkipFlag(existingPower, target);
        if (amount == 0m)
            return existingPower;

        int newAmount = await PowerCmd.ModifyAmount(
            new ThrowingPlayerChoiceContext(),
            existingPower,
            amount,
            applier,
            cardSource,
            silent);
        return newAmount == 0 ? null : existingPower;
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
    ///     调整同类持续 power 的层数，并将剩余回合精确设置为 <paramref name="turns"/>。
    ///     <paramref name="turns"/> 小于等于 0 时表示永久。
    /// </summary>
    public static async Task<int> ModifyAmount<T>(
        Creature target,
        decimal offset,
        int turns,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false) where T : LibraryDurationPowerModel
    {
        T? existingPower = LibraryDurationPowerModel.FindStackablePower<T>(target, turns);
        if (existingPower == null)
            return 0;

        existingPower.SetTurnsRemaining(turns);
        LibraryDurationPowerModel.CorrectDurationSkipFlag(existingPower, target);
        return await PowerCmd.ModifyAmount(
            new ThrowingPlayerChoiceContext(),
            existingPower,
            offset,
            applier,
            cardSource,
            silent);
    }
}
