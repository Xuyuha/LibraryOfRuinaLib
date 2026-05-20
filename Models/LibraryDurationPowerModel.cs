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
    public override Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        CorrectDurationSkipFlag(this, target);
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
            SkipNextDurationTick = CombatState.CurrentSide == Owner.Side;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
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

    /// <summary>
    ///     持续回合在哪一方的回合结束时递减。默认为 Owner 所在方。
    ///     Debuff 类 power 可能需要覆写为 <see cref="CombatSide.Player"/>。
    /// </summary>
    protected virtual CombatSide DecaySide => Owner.Side;

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
        power.SkipNextDurationTick = target.CombatState?.CurrentSide == target.Side;
    }

    private static int MergeTurns(int current, int incoming)
    {
        if (current <= 0 || incoming <= 0)
            return 0;
        return current + incoming;
    }
}
