using LibraryLib.Light;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Random;

namespace LibraryLib.SpeedDice;

public sealed class LibrarySpeedDiceCombatState
{
    private readonly List<LibrarySpeedDiceSlot> _slots = [];
    private readonly object _gameplayNotificationSync = new();
    private bool _gameplayNotificationBatchActive;
    private int _lifecycleBusy;

    internal LibrarySpeedDiceCombatState(
        Player player,
        LibrarySpeedDiceRegistration registration)
    {
        Player = player;
        Registration = registration;
        Participant = registration.CompatibilityParticipant;
        GameplayRng = registration.Dispatcher.CreateGameplayRng(player)
            ?? new Rng(player.RunState.Rng.Seed, "library_speed_dice");
        TargetRepairRng =
            registration.Dispatcher.CreateTargetRepairRng(player)
            ?? new Rng(player.RunState.Rng.Seed, "library_speed_target_repair");
        Emotion.LevelChanged += HandleEmotionLevelChanged;
        if (registration.Light != null)
        {
            ILibraryLightStore store =
                registration.LightStoreFactory?.Invoke(
                    player,
                    registration.Light)
                ?? new LibraryInMemoryLightStore(
                    registration.Light.Starting);
            Light = new LibraryLightState(
                this,
                registration.Light,
                store);
        }
    }

    public Player Player { get; }

    public LibrarySpeedDiceParticipant Participant { get; }

    internal LibrarySpeedDiceRegistration Registration { get; }

    public LibraryEmotionState Emotion { get; } = new();

    public LibraryLightState? Light { get; }

    public IReadOnlyList<LibrarySpeedDiceSlot> Slots => _slots;

    public bool HasRolled { get; internal set; }

    public bool IsLocked { get; internal set; }

    public bool IsResolving { get; internal set; }

    public bool IsSelectingTarget { get; internal set; }

    public bool IsLifecycleBusy => Volatile.Read(ref _lifecycleBusy) != 0;

    public LibrarySpeedDiceSlot? ResolvingSlot { get; internal set; }

    public int CurrentTurnTriggeredCards { get; internal set; }

    public int PreviousTurnTriggeredCards { get; internal set; }

    /// <summary>
    /// 仅在确定性战斗状态发生变化时递增；本地 hover/目标选择等表现状态不影响该值。
    /// </summary>
    public int Revision { get; internal set; }

    public int ReservedEnergy => _slots.Sum(x => x.ReservedEnergy);

    public int ReservedStars => _slots.Sum(x => x.ReservedStars);

    public event Action? Changed;

    /// <summary>
    /// 仅在确定性战斗状态变化（动作驱动，host/client 两端对称执行）时触发。
    /// 本地 UI 交互（装备选择、目标选择等）只触发 <see cref="Changed"/>，
    /// 不触发本事件，避免多人快照捕获在两端不对称。
    /// </summary>
    public event Action? GameplayChanged;

    internal Rng GameplayRng { get; set; }

    internal Rng TargetRepairRng { get; set; }

    internal int DamageGivenAccumulator { get; set; }

    internal int DamageGivenAccumulatorThreshold { get; set; }

    internal int DamageReceivedAccumulator { get; set; }

    internal int DamageReceivedAccumulatorThreshold { get; set; }

    public bool BonusDrawPending { get; internal set; }

    internal int LeaseSequence { get; set; }

    internal int PendingEmotionPreviousLevel { get; private set; } = -1;

    internal int PendingEmotionCurrentLevel { get; private set; } = -1;

    internal bool DeferEmotionLevelChangedLifecycle { get; set; }

    internal int PreparedTurnNumber { get; set; } = -1;

    internal SemaphoreSlim Gate { get; } = new(1, 1);

    internal void ReplaceSlots(int count)
    {
        foreach (LibrarySpeedDiceSlot slot in _slots)
            ReleaseSlotForReplacement(slot);
        _slots.Clear();
        for (int i = 0; i < count; i++)
        {
            _slots.Add(
                new LibrarySpeedDiceSlot(
                    i,
                    Registration.Options.MinRoll));
        }
        HasRolled = false;
        IsLocked = false;
        IsResolving = false;
        IsSelectingTarget = false;
        Volatile.Write(ref _lifecycleBusy, 0);
        ResolvingSlot = null;
        NotifyGameplayChanged();
    }

    internal void Restore(LibrarySpeedDiceStateSnapshot snapshot)
    {
        foreach (LibrarySpeedDiceSlot slot in _slots)
            ReleaseSlotForReplacement(slot);
        _slots.Clear();
        foreach (LibrarySpeedDiceSlotSnapshot savedSlot in
                 snapshot.Slots.OrderBy(slot => slot.Index))
        {
            var slot = new LibrarySpeedDiceSlot(
                savedSlot.Index,
                savedSlot.DisplayValue)
            {
                DisplayValue = savedSlot.DisplayValue,
                FinalValue = savedSlot.FinalValue,
                IsLocked = savedSlot.IsLocked,
                IsSpent = savedSlot.IsSpent,
                Card = savedSlot.Card,
                Target = savedSlot.Target,
            };
            foreach ((string resourceId, int amount) in
                     savedSlot.ReservedSecondaryResources)
            {
                slot.SetSecondaryResourceReservation(resourceId, amount);
            }

            _slots.Add(slot);
        }

        HasRolled = snapshot.HasRolled;
        PreparedTurnNumber = snapshot.TurnNumber;
        IsLocked = snapshot.IsLocked;
        IsResolving = false;
        IsSelectingTarget = false;
        Volatile.Write(ref _lifecycleBusy, 0);
        ResolvingSlot = null;
        CurrentTurnTriggeredCards = Math.Max(
            0,
            snapshot.CurrentTurnTriggeredCards);
        PreviousTurnTriggeredCards = Math.Max(
            0,
            snapshot.PreviousTurnTriggeredCards);
        BonusDrawPending = snapshot.BonusDrawPending;
        DamageGivenAccumulator = Math.Max(
            0,
            snapshot.DamageGivenAccumulator);
        DamageReceivedAccumulator = Math.Max(
            0,
            snapshot.DamageReceivedAccumulator);
        DamageGivenAccumulatorThreshold = Math.Max(
            0,
            snapshot.Extension?.DamageGivenAccumulatorThreshold ?? 0);
        DamageReceivedAccumulatorThreshold = Math.Max(
            0,
            snapshot.Extension?.DamageReceivedAccumulatorThreshold ?? 0);
        Emotion.Restore(
            snapshot.EmotionLevel,
            snapshot.EmotionUnits,
            Registration.Emotion);
        LeaseSequence = Math.Max(
            0,
            snapshot.Extension?.LeaseSequence ?? 0);
        int pendingPrevious =
            snapshot.Extension?.PendingEmotionPreviousLevel ?? -1;
        int pendingCurrent =
            snapshot.Extension?.PendingEmotionCurrentLevel ?? -1;
        if (pendingPrevious >= 0 && pendingCurrent >= pendingPrevious)
        {
            PendingEmotionPreviousLevel = pendingPrevious;
            PendingEmotionCurrentLevel = pendingCurrent;
        }
        else
        {
            PendingEmotionPreviousLevel = -1;
            PendingEmotionCurrentLevel = -1;
        }
        Revision = Math.Max(0, snapshot.Revision);
    }

    public void SetSlotRollValue(int slotIndex, int value)
    {
        if (slotIndex < 0 || slotIndex >= _slots.Count)
            throw new ArgumentOutOfRangeException(nameof(slotIndex));

        LibrarySpeedDiceSlot slot = _slots[slotIndex];
        slot.FinalValue = value;
        slot.DisplayValue = value;
        NotifyGameplayChanged();
    }

    public void EnsureSlotCount(int count, bool rollNewSlots)
    {
        count = Math.Max(0, count);
        int before = _slots.Count;
        bool changed = false;
        while (_slots.Count < count)
        {
            var slot = new LibrarySpeedDiceSlot(
                _slots.Count,
                Registration.Options.MinRoll);
            if (rollNewSlots && HasRolled)
            {
                int value = GameplayRng.NextInt(
                    Registration.Options.MinRoll,
                    Registration.Options.MaxRoll + 1);
                slot.FinalValue = value;
                slot.DisplayValue = value;
            }

            _slots.Add(slot);
            changed = true;
        }

        if (changed)
            NotifyGameplayChanged();
        Log.Info(
            "[LibraryOfRuinaLib] [DEBUG-speed-ui-v3] ensure "
            + $"requested={count} before={before} after={_slots.Count} "
            + $"changed={changed} rolled={HasRolled}");
    }

    public void SetBonusDrawPending(bool value)
    {
        if (BonusDrawPending == value)
            return;

        BonusDrawPending = value;
        NotifyGameplayChanged();
    }

    public void MarkAllSlotsSpent()
    {
        bool changed = false;
        foreach (LibrarySpeedDiceSlot slot in _slots)
        {
            if (slot.IsSpent)
                continue;

            slot.IsSpent = true;
            changed = true;
        }

        if (changed)
            NotifyGameplayChanged();
    }

    public void MarkSlotSpent(LibrarySpeedDiceSlot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        if (!_slots.Contains(slot))
        {
            throw new ArgumentException(
                "The speed-die slot does not belong to this combat state.",
                nameof(slot));
        }

        if (slot.IsSpent)
            return;

        slot.IsSpent = true;
        NotifyGameplayChanged();
    }

    internal void NotifyGameplayChanged()
    {
        lock (_gameplayNotificationSync)
        {
            if (_gameplayNotificationBatchActive)
                return;

            IncrementRevision();
        }
        PublishChanged(gameplay: true);
    }

    internal GameplayNotificationBatch BeginGameplayNotificationBatch()
    {
        lock (_gameplayNotificationSync)
        {
            if (_gameplayNotificationBatchActive)
            {
                throw new InvalidOperationException(
                    "A speed-dice gameplay notification batch is already active.");
            }

            _gameplayNotificationBatchActive = true;
        }

        return new GameplayNotificationBatch(this);
    }

    private void EndGameplayNotificationBatch(bool publish)
    {
        bool notify = false;
        lock (_gameplayNotificationSync)
        {
            if (!_gameplayNotificationBatchActive)
                return;

            _gameplayNotificationBatchActive = false;
            if (publish)
            {
                IncrementRevision();
                notify = true;
            }
        }

        if (notify)
            PublishChanged(gameplay: true);
    }

    private void IncrementRevision()
    {
        Revision = Revision == int.MaxValue ? 1 : Revision + 1;
    }

    internal void NotifyChanged()
    {
        PublishChanged(gameplay: false);
    }

    /// <summary>
    /// 通知 gameplay 订阅者但不递增 Revision；用于快照恢复等
    /// 已经设置了确定 Revision 的路径。
    /// </summary>
    internal void PublishGameplayChangedWithoutRevision()
    {
        PublishChanged(gameplay: true);
    }

    private void PublishChanged(bool gameplay)
    {
        if (gameplay)
        {
            foreach (Delegate handler in GameplayChanged?.GetInvocationList() ?? [])
            {
                try
                {
                    ((Action)handler)();
                }
                catch (Exception exception)
                {
                    Log.Error(
                        "[LibraryOfRuinaLib] Speed-dice gameplay state listener failed: "
                        + exception);
                }
            }
        }

        foreach (Delegate handler in Changed?.GetInvocationList() ?? [])
        {
            try
            {
                ((Action)handler)();
            }
            catch (Exception exception)
            {
                Log.Error(
                    "[LibraryOfRuinaLib] Speed-dice state listener failed: "
                    + exception);
            }
        }
    }

    internal bool TryBeginLifecycle()
    {
        if (Interlocked.CompareExchange(ref _lifecycleBusy, 1, 0) != 0)
            return false;

        PublishChanged(gameplay: true);
        return true;
    }

    internal void EndLifecycle()
    {
        if (Interlocked.Exchange(ref _lifecycleBusy, 0) != 0)
            PublishChanged(gameplay: true);
    }

    internal sealed class GameplayNotificationBatch : IDisposable
    {
        private LibrarySpeedDiceCombatState? _state;

        internal GameplayNotificationBatch(
            LibrarySpeedDiceCombatState state)
        {
            _state = state;
        }

        public void Complete()
        {
            LibrarySpeedDiceCombatState? state =
                Interlocked.Exchange(ref _state, null);
            state?.EndGameplayNotificationBatch(publish: true);
        }

        public void Dispose()
        {
            LibrarySpeedDiceCombatState? state =
                Interlocked.Exchange(ref _state, null);
            state?.EndGameplayNotificationBatch(publish: false);
        }
    }

    internal string CreateLeaseId(int slotIndex)
    {
        int sequence = checked(++LeaseSequence);
        int turn = Player.PlayerCombatState?.TurnNumber ?? -1;
        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{Registration.Id}:{Player.NetId}:{turn}:{slotIndex}:{sequence}");
    }

    internal bool ConsumePendingEmotionChange(
        out int previousLevel,
        out int currentLevel)
    {
        previousLevel = PendingEmotionPreviousLevel;
        currentLevel = PendingEmotionCurrentLevel;
        PendingEmotionPreviousLevel = -1;
        PendingEmotionCurrentLevel = -1;
        return previousLevel >= 0 && currentLevel >= previousLevel;
    }

    private void HandleEmotionLevelChanged(
        int previousLevel,
        int currentLevel)
    {
        if (DeferEmotionLevelChangedLifecycle)
        {
            if (PendingEmotionPreviousLevel < 0)
                PendingEmotionPreviousLevel = previousLevel;
            PendingEmotionCurrentLevel = currentLevel;
        }
        else
        {
            DispatchEmotionLevelChanged(
                previousLevel,
                currentLevel);
        }

        NotifyGameplayChanged();
    }

    internal void DispatchEmotionLevelChanged(
        int previousLevel,
        int currentLevel)
    {
        Registration.Dispatcher.OnEmotionLevelChanged(
            new LibraryEmotionLevelChanged(
                this,
                previousLevel,
                currentLevel));
        Light?.RefreshMaximum();
    }

    private void ReleaseSlotForReplacement(
        LibrarySpeedDiceSlot slot)
    {
        var card = slot.Card;
        LibrarySpeedDiceCardLease? lease = slot.Lease;
        slot.ClearReservation();
        if (card != null)
        {
            Registration.Dispatcher.OnCardReleased(
                this,
                slot,
                card,
                lease);
        }
    }
}
