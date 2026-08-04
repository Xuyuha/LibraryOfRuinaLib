using Godot;
using LibraryLib.Light;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace LibraryLib.SpeedDice;

/// <summary>
/// 所有速度骰子组件的共同身份。组件按 Order、Id 的顺序稳定执行。
/// </summary>
public interface ILibrarySpeedDiceModule
{
    string Id { get; }

    int Order => 0;
}

public interface ILibrarySpeedDicePolicy : ILibrarySpeedDiceModule
{
    bool CanEquipCard(
        LibrarySpeedDiceCombatState state,
        CardModel card) => true;

    bool CanUnequipCard(
        LibrarySpeedDiceCombatState state,
        CardModel card) => true;

    bool CanTargetCard(
        LibrarySpeedDiceCombatState state,
        CardModel card,
        Creature? target) => true;

    int ModifySpeedDiceCount(
        LibrarySpeedDiceCombatState state,
        int currentCount) => currentCount;
}

/// <summary>
/// 用于多人模式的多牌堆速度骰子装备请求。下游模块可以通过实现 ILibrarySpeedDiceInputRouter 来拦截和处理这些请求。
/// </summary>
public static class LibrarySpeedDiceSelectionSourceIds
{
    public const string Hand = "library.hand";
}

public enum LibrarySpeedDiceSelectionResult
{
    Rejected,
    Canceled,
    Submitted,
}

/// <summary>
/// 速度骰子装备选择来源。下游模块可以通过实现 ILibrarySpeedDiceSelectionSource 来提供自定义的装备选择来源。
/// </summary>
public interface ILibrarySpeedDiceSelectionSource : ILibrarySpeedDiceModule
{
    /// <summary>
    /// 选择来源的唯一标识符。用于在多人模式中区分不同来源的选择请求。
    /// </summary>
    string SourceId => Id;

    bool CanSelect(
        LibrarySpeedDiceCombatState state,
        CardModel card);

    /// <summary>
    /// 返回用于速度骰子目标选择的本地UI控件。
    /// 此方法仅用于界面展示，不会在网络验证中使用。
    /// </summary>
    Control? GetTargetingOrigin(
        LibrarySpeedDiceCombatState state,
        CardModel card) => null;
}

public interface ILibrarySpeedDiceLifecycle : ILibrarySpeedDiceModule
{
    void OnStateCreated(LibrarySpeedDiceCombatState state)
    {
    }

    void BeforePlayerTurn(LibrarySpeedDiceCombatState state)
    {
    }

    void OnEmotionLevelChanged(LibraryEmotionLevelChanged change)
    {
    }

    Task AfterRollAsync(
        PlayerChoiceContext choiceContext,
        LibrarySpeedDiceCombatState state) => Task.CompletedTask;

    Task OnUseAsync(
        PlayerChoiceContext choiceContext,
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot,
        LibrarySpeedDiceCardLease lease) => Task.CompletedTask;

    Task OnTargetedUseAsync(
        PlayerChoiceContext choiceContext,
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot,
        LibrarySpeedDiceCardLease lease,
        Creature target) => Task.CompletedTask;

    Task AfterCardEquippedAsync(
        PlayerChoiceContext choiceContext,
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot) => Task.CompletedTask;

    Task OnInstantAssignmentAsync(
        LibrarySpeedDiceInstantAssignmentContext context) =>
        Task.CompletedTask;

    Task BeforeResolutionBatchAsync(
        LibrarySpeedDiceResolutionBatchContext context) =>
        Task.CompletedTask;

    Task BeforeCardResolutionAsync(
        LibrarySpeedDiceCardResolutionContext context) =>
        Task.CompletedTask;

    Task AfterCardResolutionAsync(
        LibrarySpeedDiceCardResolutionContext context,
        bool wasPlayed) => Task.CompletedTask;

    Task AfterResolutionBatchAsync(
        LibrarySpeedDiceResolutionBatchContext context) =>
        Task.CompletedTask;

    void OnCardReleased(
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot,
        CardModel card,
        LibrarySpeedDiceCardLease? lease)
    {
    }
}

public interface ILibrarySpeedDiceInputRouter : ILibrarySpeedDiceModule
{
    Task<bool> RouteAsync(LibrarySpeedDiceInputRequest request);
}

public interface ILibrarySpeedDiceDeterminism : ILibrarySpeedDiceModule
{
    Rng? CreateGameplayRng(Player player) => null;

    Rng? CreateTargetRepairRng(Player player) => null;

    string? GetStableTargetKey(Creature target) => null;
}

public interface ILibrarySpeedDicePresentation : ILibrarySpeedDiceModule
{
    void ConfigureSlotUi(
        Control control,
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot)
    {
    }

    void OnEquipSelectionChanged(
        LibrarySpeedDiceCombatState state,
        CardModel card,
        bool isSelecting)
    {
    }

    /// <summary>
    /// 覆盖速度骰子悬停线的目标集合。返回 null 时使用槽位主目标。
    /// </summary>
    IReadOnlyList<Creature>? GetTargetLineTargets(
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot) => null;
}

public interface ILibraryLightPolicy : ILibrarySpeedDiceModule
{
    int ModifyLightCost(
        CardModel card,
        int currentCost) => currentCost;

    int ModifyMaximumLight(
        LibraryLightState state,
        int currentMaximum) => currentMaximum;

    int ModifyTurnRecovery(
        LibraryLightState state,
        int currentRecovery) => currentRecovery;

    bool ShouldRecoverForTurn(LibraryLightState state) => true;

    bool AllowOverflow(LibraryLightState state) => false;
}

public sealed record LibraryEmotionLevelChanged(
    LibrarySpeedDiceCombatState State,
    int PreviousLevel,
    int CurrentLevel);

public sealed class LibrarySpeedDiceResolutionBatchContext
{
    internal LibrarySpeedDiceResolutionBatchContext(
        PlayerChoiceContext choiceContext,
        LibrarySpeedDiceCombatState state,
        IReadOnlyList<LibrarySpeedDiceSlot> slots)
    {
        ChoiceContext = choiceContext;
        State = state;
        Slots = slots;
    }

    public PlayerChoiceContext ChoiceContext { get; }

    public LibrarySpeedDiceCombatState State { get; }

    public IReadOnlyList<LibrarySpeedDiceSlot> Slots { get; }
}

public sealed class LibrarySpeedDiceCardResolutionContext
{
    internal LibrarySpeedDiceCardResolutionContext(
        LibrarySpeedDiceResolutionBatchContext batch,
        LibrarySpeedDiceSlot slot,
        CardModel card,
        LibrarySpeedDiceCardLease lease)
    {
        Batch = batch;
        Slot = slot;
        Card = card;
        Lease = lease;
    }

    public LibrarySpeedDiceResolutionBatchContext Batch { get; }

    public LibrarySpeedDiceCombatState State => Batch.State;

    public PlayerChoiceContext ChoiceContext => Batch.ChoiceContext;

    public LibrarySpeedDiceSlot Slot { get; }

    public CardModel Card { get; }

    public LibrarySpeedDiceCardLease Lease { get; }
}

public sealed class LibrarySpeedDiceInstantAssignmentContext
{
    internal LibrarySpeedDiceInstantAssignmentContext(
        PlayerChoiceContext choiceContext,
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot,
        CardModel card,
        Creature? target,
        string sourceId,
        LibrarySpeedDiceReservationPlan reservationPlan)
    {
        ChoiceContext = choiceContext;
        State = state;
        Slot = slot;
        Card = card;
        Target = target;
        SourceId = sourceId;
        ReservationPlan = reservationPlan;
    }

    public PlayerChoiceContext ChoiceContext { get; }

    public LibrarySpeedDiceCombatState State { get; }

    public LibrarySpeedDiceSlot Slot { get; }

    public CardModel Card { get; }

    public Creature? Target { get; }

    public string SourceId { get; }

    public LibrarySpeedDiceReservationPlan ReservationPlan { get; }
}
