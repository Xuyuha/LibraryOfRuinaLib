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
///     带持续回合的 power 抽象基类。
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

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new TurnsVar()];

    protected override object InitInternalData() => new Data();

    /// <summary>
    ///     剩余回合数。0 表示永久。
    /// </summary>
    public int TurnsRemaining => GetInternalData<Data>().TurnsRemaining;

    /// <summary>
    ///     设计上永续的 power 不受持续回合机制影响，也不显示回合副计数。
    /// </summary>
    protected virtual bool IsPermanentByDesign => false;

    public bool IsPermanent => IsPermanentByDesign || TurnsRemaining <= 0;

    public virtual bool ShowSecondaryDisplayAmount => !IsPermanent;

    public virtual int SecondaryDisplayAmount => TurnsRemaining;

    public virtual Color SecondaryDisplayAmountLabelColor => _normalAmountLabelColor;

    /// <summary>
    ///     设置剩余回合数。
    /// </summary>
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

    /// <summary>
    ///     施加一个带持续回合的 power。已存在时合并回合数，并把 amount 调整到较大值。
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
        T? existing = FindStackablePower<T>(target, turns);
        if (existing == null)
        {
            if (amount == 0m)
                return null;

            var mutable = (T)ModelDb.Power<T>().ToMutable();
            mutable.SetTurnsRemaining(turns, notifyDisplay: false);
            CorrectDurationSkipFlag(mutable, target);
            await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), mutable, target, amount, applier, cardSource, silent);
            CorrectDurationSkipFlag(mutable, target);
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

    public sealed override Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        CorrectDurationSkipFlag(this, target);
        return BeforeApplied(target, amount, applier, cardSource, null);
    }

    protected virtual Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource, object? _ = null)
    {
        return Task.CompletedTask;
    }

    public override Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power == this && amount > 0m && !IsPermanent)
            CorrectDurationSkipFlag(this, Owner);
        return Task.CompletedTask;
    }

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

    protected virtual Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants, object? _ = null)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     实际消耗持续回合的侧。默认是 owner 自己的行动回合。
    /// </summary>
    protected virtual CombatSide DecaySide => GetDecaySide(Owner);

    /// <summary>
    ///     用指定 owner 计算消耗侧。受击、防御、易损类 power 应按实际生效窗口覆盖此方法。
    /// </summary>
    protected virtual CombatSide GetDecaySide(Creature owner) => owner.Side;

    protected static CombatSide OppositeSideOf(Creature owner)
    {
        return owner.Side == CombatSide.Player ? CombatSide.Enemy : CombatSide.Player;
    }

    /// <summary>
    ///     默认只保留原版“敌方回合给玩家挂敌方生效 debuff”的首 tick 保护。
    /// </summary>
    protected virtual bool ShouldSkipInitialDurationTick(
        CombatSide applicationSide,
        CombatSide decaySide,
        Creature target)
    {
        return target.Side == CombatSide.Player
            && Type == PowerType.Debuff
            && decaySide != target.Side
            && applicationSide == decaySide;
    }

    /// <summary>
    ///     持续回合耗尽时调用。默认移除自身。
    /// </summary>
    protected virtual Task OnExpired(PlayerChoiceContext choiceContext)
    {
        return PowerCmd.Remove(this);
    }

    private static void CorrectDurationSkipFlag(LibraryDurationPowerModel power, Creature target)
    {
        if (power.IsPermanent)
        {
            power.SkipNextDurationTick = false;
            return;
        }

        CombatSide? applicationSide = target.CombatState?.CurrentSide;
        if (applicationSide == null)
        {
            power.SkipNextDurationTick = false;
            return;
        }

        CombatSide decaySide = power.GetDecaySide(target);
        power.SkipNextDurationTick = power.ShouldSkipInitialDurationTick(applicationSide.Value, decaySide, target);
    }

    private static int MergeTurns(int current, int incoming)
    {
        if (current <= 0 || incoming <= 0)
            return 0;
        return current + incoming;
    }

    private static T? FindStackablePower<T>(Creature target, int incomingTurns)
        where T : LibraryDurationPowerModel
    {
        bool incomingIsPermanent = ModelDb.Power<T>().IsPermanentByDesign || incomingTurns <= 0;
        return target.GetPowerInstances<T>().FirstOrDefault(power => power.IsPermanent == incomingIsPermanent);
    }
}
