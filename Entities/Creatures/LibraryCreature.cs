using LibraryLib.Models;
using LibraryLib.Patches;
using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Singleton;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Entities.Creatures;

/// <summary>
///     图书馆怪物的战斗实体：在原版 Creature 上增加混乱值（Chao）、物理/混乱抗性和混乱（stagger）状态。
///     只有 <see cref="LibraryMonsterModel"/> 会被 CreateCreaturePatch 创建为本类型。
/// </summary>
public class LibraryCreature : Creature
{
    private const int MaxValue = 999999999;

    private int _currentChaoValue;
    private int _maxChaoValue;
    private LibraryCreatureResistanceData _resistanceData;
    private LibraryCreatureResistanceData? _preStunResistanceData;
    private int _stunPlayerTurnsRemaining;

    public LibraryCreature(MonsterModel monster, CombatSide side, string? slotName) : base(monster, side, slotName)
    {
        _resistanceData = new LibraryCreatureResistanceData();
        if (monster is LibraryMonsterModel libraryMonster)
        {
            if (libraryMonster.DefaultChaoResistanceData != null)
                _resistanceData.ChaosResistance = new LibraryCreatureResistanceData.Resistance(libraryMonster.DefaultChaoResistanceData);
            if (libraryMonster.DefaultPhysicalResistanceData != null)
                _resistanceData.PhysicalResistance = new LibraryCreatureResistanceData.Resistance(libraryMonster.DefaultPhysicalResistanceData);
        }
    }

    public event Action<int, int>? CurrentChaoValueChanged;
    public event Action<Creature>? Stuned;
    public event Action<int, int>? MaxChaoValueChanged;

    public bool HasChaoResistance => Monster is LibraryMonsterModel { HasChaoResistance: true };

    public bool RestoreChaoOnNextOwnerTurn { get; set; }

    public bool IsStunPending => RestoreChaoOnNextOwnerTurn;

    /// <summary>是否处于混乱状态，独立于普通 Stun 和自动恢复开关。</summary>
    public bool IsChaoed { get; private set; }

    public int StunPlayerTurnsRemaining => _stunPlayerTurnsRemaining;

    public LibraryCreatureResistanceData ResistanceData => _resistanceData ??= new LibraryCreatureResistanceData();

    // Stun owns the visible Fatal layer. Resistance changes made while stunned
    // must update the state that will become effective after recovery instead.
    private LibraryCreatureResistanceData PostStunResistanceData => _preStunResistanceData ?? ResistanceData;

    public int? MonsterMaxChaoValueBeforeModification { get; private set; }

    public NHealthBar? HealthBar => GetCreatureNode()?.GetNode<NCreatureStateDisplay>("%HealthBar")?.GetNode<NHealthBar>("%HealthBar");

    public int MaxChaoValue
    {
        get => _maxChaoValue;
        private set
        {
            if (_maxChaoValue == value)
                return;
            int previous = _maxChaoValue;
            _maxChaoValue = value;
            MaxChaoValueChanged?.Invoke(previous, _maxChaoValue);
        }
    }

    public int CurrentChaoValue
    {
        get => _currentChaoValue;
        private set
        {
            if (value < 0)
                throw new ArgumentException("Current Chao Value must be positive", nameof(value));
            if (_currentChaoValue == value)
                return;
            int previous = _currentChaoValue;
            _currentChaoValue = value;
            LibraryChaoHealNumberVfx.Show(this, _currentChaoValue - previous);
            CurrentChaoValueChanged?.Invoke(previous, _currentChaoValue);
        }
    }

    // ---- 混乱（stagger） ----

    public void SaveAndSetStunResistance()
    {
        IsChaoed = true;
        _preStunResistanceData ??= new LibraryCreatureResistanceData(ResistanceData);
        _resistanceData = new LibraryCreatureResistanceData(LibraryResistanceLevel.Fatal);
        _stunPlayerTurnsRemaining = CombatState?.CurrentSide == CombatSide.Enemy ? 2 : 1;
        RestoreChaoOnNextOwnerTurn = true;
        RefreshResistanceIcons();
    }

    public void RestorePreStunResistance()
    {
        IsChaoed = false;
        if (_preStunResistanceData != null)
        {
            _resistanceData = _preStunResistanceData;
            _preStunResistanceData = null;
        }
        _stunPlayerTurnsRemaining = 0;
        RestoreChaoOnNextOwnerTurn = false;
        RefreshResistanceIcons();
    }

    public void DecrementStunTurns()
    {
        if (_stunPlayerTurnsRemaining > 0)
            _stunPlayerTurnsRemaining--;
    }

    public new void StunInternal(Func<IReadOnlyList<Creature>, Task> stunMove, string? nextMoveId)
    {
        if (Monster == null)
            throw new InvalidOperationException("Can't stun a player.");
        if (CombatState == null || IsDead || IsChaoed)
            return;

        SaveAndSetStunResistance();
        SetCurrentChaoValueInternal(0m);
        MoveState state = new LibraryStunMoveState(this, stunMove)
        {
            FollowUpStateId = ResolvePostStunMoveId(Monster, nextMoveId),
            MustPerformOnceBeforeTransitioning = true,
        };
        Monster.SetMoveImmediate(state, forceTransition: true);
        Stuned?.Invoke(this);
    }

    internal sealed class LibraryStunMoveState(
        LibraryCreature owner,
        Func<IReadOnlyList<Creature>, Task> stunMove)
        : MoveState("STUNNED", stunMove, new StunIntent())
    {
        internal bool IsChaosLocked => owner.IsChaoed;

        // Enemy-side stagger lasts through the next enemy turn. Keep its move
        // until the same recovery lifecycle releases the Fatal resistance layer.
        public override bool CanTransitionAway => base.CanTransitionAway && !owner.IsChaoed;
    }

    internal static string? ResolvePostStunMoveId(MonsterModel monster, string? nextMoveId)
    {
        if (IsValidPostStunMoveId(monster, nextMoveId))
            return nextMoveId;

        // 动态路由怪物在混乱锁下无法再改写恢复招式，恢复时必须重新经过路由选招。
        string? recoveryStateId = (monster as LibraryMonsterModel)?.StunRecoveryStateId;
        if (IsValidPostStunMoveId(monster, recoveryStateId))
            return recoveryStateId;

        string? loggedMoveId = monster.MoveStateMachine?.StateLog
            .LastOrDefault(state => IsValidPostStunMoveId(monster, state.Id))
            ?.Id;
        if (loggedMoveId != null)
            return loggedMoveId;

        string? currentMoveId = monster.NextMove?.Id;
        if (IsValidPostStunMoveId(monster, currentMoveId))
            return currentMoveId;

        return monster.MoveStateMachine?.States.Values
            .OfType<MoveState>()
            .FirstOrDefault(state => IsValidPostStunMoveId(monster, state.Id))
            ?.Id;
    }

    private static bool IsValidPostStunMoveId(MonsterModel monster, string? moveId)
    {
        return !string.IsNullOrEmpty(moveId)
            && moveId != MonsterModel.stunnedMoveId
            && moveId != "UNSET_MOVE"
            && monster.MoveStateMachine?.States.TryGetValue(moveId, out MonsterState? state) == true
            && state is not LibraryPhaseTransitionMoveState;
    }

    // ---- 混乱值 ----

    public void HealChaoInternal(decimal amount)
    {
        if (HasChaoResistance && !IsChaoed)
            SetCurrentChaoValueInternal(CurrentChaoValue + amount);
    }

    public void SetCurrentChaoValueInternal(decimal amount)
    {
        if (HasChaoResistance)
            CurrentChaoValue = (int)Math.Min(amount, MaxChaoValue);
    }

    public void SetMaxChaoValueInternal(decimal amount)
    {
        if (!HasChaoResistance)
            return;
        if (amount < 0m)
            throw new ArgumentException("amount must be non-negative.");
        MaxChaoValue = Math.Min((int)amount, MaxValue);
        CurrentChaoValue = Math.Min(CurrentChaoValue, MaxChaoValue);
    }

    public void SetUniqueMonsterChaoValue(IReadOnlyList<Creature> creaturesOnSide, Rng rng)
    {
        if (Monster == null)
            throw new InvalidOperationException("Can't set unique monster Chao value for a player.");
        if (Monster is LibraryMonsterModel { DefaultChaoResistance: > 0 } model)
            MonsterMaxChaoValueBeforeModification = _currentChaoValue = _maxChaoValue = model.DefaultChaoResistance;
    }

    public void ScaleMonsterChaoValueForMultiplayer(EncounterModel? encounter, int playerCount, int actIndex)
    {
        if (playerCount == 1)
            return;
        SetMaxChaoValueInternal(ScaleHpForMultiplayer(MaxChaoValue, encounter, playerCount, actIndex));
        SetCurrentChaoValueInternal(MaxChaoValue);
    }

    public static decimal ScaleChaoValueForMultiplayer(decimal chaoValue, EncounterModel? encounter, int playerCount, int actIndex) =>
        playerCount == 1
            ? chaoValue
            : chaoValue * playerCount * MultiplayerScalingModel.GetMultiplayerScaling(encounter, actIndex);

    public double GetChaoValuePercentRemaining() => (double)CurrentChaoValue / MaxChaoValue;

    public LibraryChaoResult? LoseChaoValueInternal(decimal amount, ValueProp props)
    {
        // 已陷入混乱或混乱值已耗尽时，不再产生混乱伤害及重复混乱结果。
        if (!HasChaoResistance || IsChaoed || CurrentChaoValue <= 0)
            return null;

        int before = CurrentChaoValue;
        bool breaksThrough = amount >= before;
        int loss = (int)Math.Min(amount, MaxValue);
        CurrentChaoValue = Math.Max(before - loss, 0);
        return new LibraryChaoResult(this, props)
        {
            OverStunChaoValue = breaksThrough ? Math.Max(loss - before, 0) : 0,
            ChaoValueAmount = before - CurrentChaoValue,
            WasStun = CurrentChaoValue == 0,
        };
    }

    // ---- 抗性 ----

    public LibraryResistanceLevel GetChaosResistanceLevel(LibraryDamageType type) => ResistanceData.ChaosResistance.Get(type);

    public LibraryResistanceLevel GetPhysicalResistanceLevel(LibraryDamageType type) => ResistanceData.PhysicalResistance.Get(type);

    // Temporary effects must snapshot these values so an active stun Fatal
    // overlay is not mistaken for the creature's recoverable resistance.
    public LibraryResistanceLevel GetPostStunChaosResistanceLevel(LibraryDamageType type) => PostStunResistanceData.ChaosResistance.Get(type);

    public LibraryResistanceLevel GetPostStunPhysicalResistanceLevel(LibraryDamageType type) => PostStunResistanceData.PhysicalResistance.Get(type);

    public void SetPhysicalResistance(LibraryDamageType type, LibraryResistanceLevel resistanceValue)
    {
        PostStunResistanceData.PhysicalResistance.Set(type, resistanceValue);
        LibraryResistanceIconsUi.Physical.Refresh(HealthBar);
    }

    public void SetChaoResistance(LibraryDamageType type, LibraryResistanceLevel resistanceValue)
    {
        if (!HasChaoResistance)
            return;
        PostStunResistanceData.ChaosResistance.Set(type, resistanceValue);
        LibraryResistanceIconsUi.Chaos.Refresh(HealthBar);
    }

    private void RefreshResistanceIcons()
    {
        LibraryResistanceIconsUi.Physical.Refresh(HealthBar);
        LibraryResistanceIconsUi.Chaos.Refresh(HealthBar);
    }
}
