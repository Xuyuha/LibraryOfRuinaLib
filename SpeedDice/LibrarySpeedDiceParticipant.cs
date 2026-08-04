using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace LibraryLib.SpeedDice;

public sealed class LibrarySpeedDiceParticipant
{
    public required string Id { get; init; }

    public required Func<Player, bool> IsEnabledForPlayer { get; init; }

    public Func<CardModel, bool> CanEquipCard { get; init; } = static _ => true;

    public Func<CardModel, bool> CanUnequipCard { get; init; } = static _ => true;

    public Func<CardModel, Creature?, bool> CanTargetCard { get; init; } =
        static (_, _) => true;

    /// <summary>
    /// 可选的确定性随机数流。多人参与者应返回所有客户端均会以相同顺序推进、
    /// 且能够随跑局恢复的玩家级随机数流。
    /// </summary>
    public Func<Player, Rng>? GameplayRngFactory { get; init; }

    /// <summary>
    /// Optional deterministic RNG stream used only when an invalid target has
    /// to be repaired before resolution.
    /// </summary>
    public Func<Player, Rng>? TargetRepairRngFactory { get; init; }

    /// <summary>
    /// Returns a deterministic cross-client ordering key for target repair.
    /// </summary>
    public Func<Creature, string>? GetStableTargetKey { get; init; }

    /// <summary>
    /// Called before this participant's speed-die slots are recreated for a
    /// player turn. Implementations may promote queued, deterministic state.
    /// </summary>
    public Action<Player>? BeforePlayerTurn { get; init; }

    /// <summary>
    /// Called after the combat state and its initial slots have been created.
    /// Participants may attach persistence observers or restore external state.
    /// </summary>
    public Action<LibrarySpeedDiceCombatState>? OnStateCreated { get; init; }

    /// <summary>
    /// Optional presentation callback invoked after a speed-die slot control
    /// has been built.
    /// </summary>
    public Action<Control, LibrarySpeedDiceCombatState,
        LibrarySpeedDiceSlot>? ConfigureSlotUi { get; init; }

    /// <summary>
    /// Applies participant-specific changes to the number of speed dice.
    /// </summary>
    public Func<LibrarySpeedDiceCombatState, int, int>?
        ModifySpeedDiceCount { get; init; }

    /// <summary>
    /// Overrides the vanilla energy/star reservation for participant cards.
    /// Return null when the card cannot currently reserve its primary
    /// resources.
    /// </summary>
    public Func<LibrarySpeedDiceCombatState, CardModel,
        LibrarySpeedDicePrimaryResourceReservation?>?
        GetPrimaryResourceReservation { get; init; }

    /// <summary>
    /// 玩家在速度骰 UI 中提交操作时的路由。多人参与者应在这里排入同步 action，
    /// 不应直接修改槽位或卡堆。
    /// </summary>
    public Func<LibrarySpeedDiceInputRequest, Task>? RequestInputAsync { get; init; }

    /// <summary>
    /// Returns deterministic secondary-resource reservations for a card, or
    /// null when the card cannot currently reserve them.
    /// </summary>
    public Func<LibrarySpeedDiceCombatState, CardModel,
        IReadOnlyDictionary<string, int>?>?
        GetSecondaryResourceReservations { get; init; }

    /// <summary>
    /// Called after secondary-resource reservations have been assigned to a
    /// slot. The participant may attach its own payment metadata to the card.
    /// </summary>
    public Action<CardModel, LibrarySpeedDiceSlot,
        IReadOnlyDictionary<string, int>>?
        OnSecondaryResourcesReserved { get; init; }

    /// <summary>
    /// Called immediately before a slot releases its secondary-resource
    /// reservations. Card-bound payment metadata remains active until the
    /// card itself is released.
    /// </summary>
    public Action<LibrarySpeedDiceSlot>?
        OnSecondaryResourceReservationsReleased { get; init; }

    /// <summary>
    /// Called immediately before a card is detached from a speed-die slot.
    /// </summary>
    public Action<CardModel, LibrarySpeedDiceSlot>?
        OnCardReleased { get; init; }

    /// <summary>
    /// Commits the participant-owned secondary-resource payment before the
    /// card's normal speed-die resources are spent.
    /// </summary>
    public Func<LibrarySpeedDiceCombatState, CardModel,
        IReadOnlyDictionary<string, int>, Task<bool>>?
        CommitSecondaryResourcesAsync { get; init; }

    /// <summary>
    /// 速度骰完成本回合投掷后的扩展点。
    /// </summary>
    public Func<PlayerChoiceContext, LibrarySpeedDiceCombatState, Task>?
        AfterSpeedRollAsync { get; init; }

    /// <summary>
    /// Runs after an equip has been committed. The supplied choice context is
    /// the managed-action context in multiplayer and a blocking context in
    /// singleplayer.
    /// </summary>
    public Func<PlayerChoiceContext, LibrarySpeedDiceCombatState,
        LibrarySpeedDiceSlot, Task>?
        AfterCardEquippedAsync { get; init; }

    /// <summary>
    /// 速度骰完成本回合全部书页结算后的扩展点。此回调运行在原版允许玩家选择的
    /// AutoPostPlay 上下文中。
    /// </summary>
    public Func<PlayerChoiceContext, LibrarySpeedDiceCombatState, Task>?
        AfterSpeedResolutionAsync { get; init; }

    public int BaseSpeedDiceCount { get; init; } = 1;
    // 骰子默认最小值，可以自己new的时候改
    public int MinSpeed { get; init; } = 1;

    // 骰子默认最大值，可以自己new的时候改
    public int MaxSpeed { get; init; } = 4;

    public LibraryEmotionConfig Emotion { get; init; } = new();

    internal void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Id);
        ArgumentNullException.ThrowIfNull(IsEnabledForPlayer);
        ArgumentNullException.ThrowIfNull(CanEquipCard);
        ArgumentNullException.ThrowIfNull(CanUnequipCard);
        ArgumentNullException.ThrowIfNull(CanTargetCard);

        if (BaseSpeedDiceCount < 0)
            throw new ArgumentOutOfRangeException(nameof(BaseSpeedDiceCount));
        if (MinSpeed < 1)
            throw new ArgumentOutOfRangeException(nameof(MinSpeed));
        if (MaxSpeed < MinSpeed)
            throw new ArgumentOutOfRangeException(nameof(MaxSpeed));
        ValidateEmotion(Emotion);
    }

    internal static void ValidateEmotion(LibraryEmotionConfig emotion)
    {
        ArgumentNullException.ThrowIfNull(emotion);
        if (emotion.UnitThresholds == null
            || emotion.UnitThresholds.Count != 5
            || emotion.UnitThresholds.Any(x => x <= 0))
        {
            throw new ArgumentException(
                "Emotion thresholds must contain five positive values.",
                nameof(emotion));
        }

        if (emotion.GainEmotionFromDamage
            && emotion.DamageUnitFractionOfMaxHp <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(emotion),
                "Damage emotion fraction must be positive when enabled.");
        }

        if (emotion.ExtremeRollEmotionUnits < 0
            || emotion.KillEmotionUnits < 0
            || emotion.AllyDeathEmotionUnits < 0
            || emotion.MaxEnergyPerLevel < 0
            || emotion.ExtraSpeedDieLevel < 0
            || emotion.ExtraSpeedDice < 0
            || emotion.BonusDrawLevel < 0
            || emotion.BonusDrawRequiredTriggeredCards < 0
            || emotion.BonusDrawAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(emotion),
                "Emotion rewards, levels, and bonuses cannot be negative.");
        }
    }
}

public readonly record struct LibrarySpeedDicePrimaryResourceReservation(
    int Energy,
    int Stars);

public enum LibrarySpeedDiceInputKind
{
    Equip,
    Unequip,
    Retarget,
}

public readonly record struct LibrarySpeedDiceInputRequest(
    LibrarySpeedDiceInputKind Kind,
    Player Player,
    int SlotIndex,
    int TurnNumber,
    int Revision,
    CardModel? Card = null,
    Creature? Target = null)
{
    public string? SourceId { get; init; }
}
