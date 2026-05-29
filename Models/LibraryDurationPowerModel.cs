using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Library.Models;

/// <summary>
///     带持续回合的 power 抽象基类。提供以下基础设施：
///     <list type="bullet">
///         <item>内部 TurnsRemaining 状态追踪</item>
///         <item>TurnsVar DynamicVar 用于本地化显示（{Turns}）</item>
///         <item><see cref="ISecondaryDisplayAmountPower"/> 实现，在图标上显示剩余回合</item>
///         <item>泛型 <see cref="ApplyWithDuration{T}"/> 静态方法（新增 / 合并 / 修正时序）</item>
///         <item>回合结束时自动递减并移除</item>
///     </list>
///     子类只需指定 <see cref="LibraryPowerModel.LegacyPowerId"/>、<see cref="PowerModel.Type"/> 等属性，
///     以及覆写 <see cref="OnExpired"/> 等可选钩子。
/// </summary>
public abstract class LibraryDurationPowerModel : LibraryPowerModel, ISecondaryDisplayAmountPower
{
    private sealed class Data
    {
        public int TurnsRemaining = 1;
    }

    private sealed class TurnsVar : DynamicVar
    {
        public TurnsVar() : base("Turns", 0m) { }

        protected override decimal GetBaseValueForIConvertible()
        {
            if (_owner is LibraryDurationPowerModel power && !power.IsPermanentByDesign)
                return power.TurnsRemaining;
            return base.GetBaseValueForIConvertible();
        }

        public override string ToString()
        {
            if (_owner is LibraryDurationPowerModel { IsPermanentByDesign: true })
                return string.Empty;
            return GetBaseValueForIConvertible().ToString();
        }
    }

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new TurnsVar()];

    /// <inheritdoc />
    protected override object InitInternalData() => new Data();

    /// <summary>
    ///     剩余回合数。0 或负数表示永久。
    /// </summary>
    public int TurnsRemaining => GetInternalData<Data>().TurnsRemaining;

    /// <summary>
    ///     覆写为 true 表示此 power 在设计上就是永久的，不受持续回合机制影响。
    ///     永久 power 不会显示回合数徽标，本地化中 {Turns} 返回空字符串。
    /// </summary>
    protected virtual bool IsPermanentByDesign => false;

    /// <summary>
    ///     是否永久（设计为永久，或运行时回合数为 0）。
    /// </summary>
    public bool IsPermanent => IsPermanentByDesign || TurnsRemaining <= 0;

    // ── ISecondaryDisplayAmountPower ──

    /// <inheritdoc />
    public virtual bool ShowSecondaryDisplayAmount => !IsPermanent;

    /// <inheritdoc />
    public virtual int SecondaryDisplayAmount => TurnsRemaining;

    /// <inheritdoc />
    public virtual Color SecondaryDisplayAmountLabelColor => _normalAmountLabelColor;

    // ── 持续回合管理 ──

    /// <summary>
    ///     设置剩余回合数。
    /// </summary>
    /// <returns>回合数是否实际发生了变化。</returns>
    public bool SetTurnsRemaining(int turnsRemaining, bool notifyDisplay = true)
    {
        AssertMutable();
        int clamped = Math.Max(0, turnsRemaining);
        Data data = GetInternalData<Data>();
        if (data.TurnsRemaining == clamped)
            return false;

        data.TurnsRemaining = clamped;
        if (notifyDisplay)
            InvokeDisplayAmountChanged();
        return true;
    }

    // ── 泛型 ApplyWithDuration ──

    /// <summary>
    ///     施加一个带持续回合的 power。若已存在则合并回合数并取较大的 amount。
    ///     <para>合并策略：任一方永久(≤0) → 永久；否则叠加回合。</para>
    /// </summary>
    public static async Task<T?> ApplyWithDuration<T>(
        Creature target,
        decimal amount,
        int turns,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false)
        where T : LibraryDurationPowerModel
    {
        T? existing = target.GetPower<T>();
        if (existing == null)
        {
            if (amount == 0m)
                return null;

            var mutable = (T)ModelDb.Power<T>().ToMutable();
            mutable.SetTurnsRemaining(turns, notifyDisplay: false);
            CorrectDurationSkipFlag(mutable, target);
            await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), mutable, target, amount, applier, cardSource, silent);
            return mutable;
        }

        int mergedTurns = MergeTurns(existing.TurnsRemaining, turns);
        int targetAmount = Math.Max(existing.Amount, (int)amount);
        int amountDelta = targetAmount - existing.Amount;

        existing.SetTurnsRemaining(mergedTurns, notifyDisplay: amountDelta == 0);
        if (amountDelta != 0)
            await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), existing, amountDelta, applier, cardSource, silent);

        CorrectDurationSkipFlag(existing, target);
        return existing;
    }

    /// <summary>
    ///     对多个目标施加带持续回合的 power。
    /// </summary>
    public static async Task ApplyWithDuration<T>(
        IEnumerable<Creature> targets,
        decimal amount,
        int turns,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false)
        where T : LibraryDurationPowerModel
    {
        foreach (Creature target in targets)
            await ApplyWithDuration<T>(target, amount, turns, applier, cardSource, silent);
    }

    // ── 生命周期钩子 ──

    /// <inheritdoc />
    public sealed override Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        CorrectDurationSkipFlag(this, target);
        BeforeApplied(target, amount, applier, cardSource, null);
        return Task.CompletedTask;
    }
    protected virtual Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource,object? _ = null)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power == this && amount > 0m && !IsPermanent)
            SkipNextDurationTick = CombatState.CurrentSide == DecaySide;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public sealed override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await AfterSideTurnEnd(choiceContext, side, participants, null);
        if (side != DecaySide || IsPermanent)
            return;

        if (SkipNextDurationTick)
        {
            SkipNextDurationTick = false;
            return;
        }
        int nextTurns = TurnsRemaining - 1;
        SetTurnsRemaining(nextTurns);
        if (nextTurns <= 0)
            await OnExpired(choiceContext);
    }
    protected virtual Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants,object? _ = null)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     持续回合在哪一方的回合结束时递减。
    ///     <list type="bullet">
    ///         <item>Buff：默认为 Owner 所在方（自己回合结束时衰减）。</item>
    ///         <item>Debuff：默认为 Owner 的对方（施加者一方的回合结束时衰减），
    ///               确保 debuff 能覆盖到施加方的下一个攻击回合。</item>
    ///     </list>
    ///     子类仍可覆写以自定义衰减时机。
    /// </summary>
    protected virtual CombatSide DecaySide => Type == PowerType.Debuff
        ? (Owner.Side == CombatSide.Player ? CombatSide.Enemy : CombatSide.Player)
        : Owner.Side;

    /// <summary>
    ///     当持续回合耗尽时调用。默认行为是移除自身。
    /// </summary>
    protected virtual Task OnExpired(PlayerChoiceContext choiceContext)
    {
        return PowerCmd.Remove(this);
    }

    // ── 内部工具 ──

    private static void CorrectDurationSkipFlag(LibraryDurationPowerModel power, Creature target)
    {
        if (power.IsPermanent)
            return;
        // 计算 DecaySide：Buff 在 Owner(target) 方回合结束衰减，
        // Debuff 在对方回合结束衰减（确保覆盖施加方的攻击回合）。
        // 注意：此处 Owner 可能尚未设置，所以用 target.Side 代替。
        CombatSide decaySide = power.Type == PowerType.Debuff
            ? (target.Side == CombatSide.Player ? CombatSide.Enemy : CombatSide.Player)
            : target.Side;
        power.SkipNextDurationTick = target.CombatState?.CurrentSide == decaySide;
    }

    private static int MergeTurns(int current, int incoming)
    {
        if (current <= 0 || incoming <= 0)
            return 0;
        return current + incoming;
    }
}
