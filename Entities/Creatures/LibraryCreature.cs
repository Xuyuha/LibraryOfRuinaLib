using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using Library.Models;
using Library.Resistance;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Singleton;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Library.Resistance.Patches;
using Library.Hooks;
using Library.Patches;

namespace Library.Entities.Creatures;
public class LibraryCreature : Creature//扩展Creature，添加Chao值属性
{
//todo：后续改成与混乱相关的都改成resitanceData，删除_damageResistance和_chaoResistance
    public LibraryCreature(MonsterModel monster, CombatSide side, string? slotName) : base(monster, side, slotName)
    {
		Log.Info("LibraryCreature Create");
        _resistanceData = new();
        if(monster is LibraryMonsterModel libraryMonsterModel){
            if (libraryMonsterModel.DefaultChaoResistanceData != null)
            {
                _resistanceData.ChaosResistance = new LibraryCreatureResistanceData.Resistance(libraryMonsterModel.DefaultChaoResistanceData);
            }
            if (libraryMonsterModel.DefaultPhysicalResistanceData != null)
            {
                _resistanceData.PhysicalResistance = new LibraryCreatureResistanceData.Resistance(libraryMonsterModel.DefaultPhysicalResistanceData);
            }
        }
    }
    private int _currentChaoValue;
    private int _maxChaoValue;
    public bool HasChaoResistance => Monster is LibraryMonsterModel libraryMonsterModel && libraryMonsterModel.HasChaoResistance;
    private LibraryCreatureResistanceData _resistanceData;
    private LibraryCreatureResistanceData? _preStunResistanceData;
    private int _stunPlayerTurnsRemaining;
    public bool RestoreChaoOnNextOwnerTurn { get; set; }
    public bool IsStunPending => RestoreChaoOnNextOwnerTurn;
    public int StunPlayerTurnsRemaining => _stunPlayerTurnsRemaining;
    public LibraryCreatureResistanceData ResistanceData => _resistanceData ??= new();

    public void SaveAndSetStunResistance()
    {
        _preStunResistanceData ??= new LibraryCreatureResistanceData(ResistanceData);
        _resistanceData = new(LibraryResistanceLevel.Fatal);
        _stunPlayerTurnsRemaining = CombatState?.CurrentSide == CombatSide.Enemy ? 2 : 1;
        RestoreChaoOnNextOwnerTurn = true;
        LibraryPhysicalResistanceIconsUi.Refresh(HealthBar);
        LibraryChaosResistanceIconsUi.Refresh(HealthBar);
    }

    public void RestorePreStunResistance()
    {
        if (_preStunResistanceData == null)
        {
            _stunPlayerTurnsRemaining = 0;
            RestoreChaoOnNextOwnerTurn = false;
            LibraryPhysicalResistanceIconsUi.Refresh(HealthBar);
            LibraryChaosResistanceIconsUi.Refresh(HealthBar);
            return;
        }

        _resistanceData = _preStunResistanceData;
        _preStunResistanceData = null;
        _stunPlayerTurnsRemaining = 0;
        RestoreChaoOnNextOwnerTurn = false;
        LibraryPhysicalResistanceIconsUi.Refresh(HealthBar);
        LibraryChaosResistanceIconsUi.Refresh(HealthBar);
    }
    public void DecrementStunTurns()
    {
        if (_stunPlayerTurnsRemaining > 0)
            _stunPlayerTurnsRemaining--;
    }
    public event Action<int, int>? CurrentChaoValueChanged;
    public event Action<Creature>? Stuned;
    public event Action<int, int>? MaxChaoValueChanged;
    public int? MonsterMaxChaoValueBeforeModification { get; private set; }
    public int MaxChaoValue
    {
        get
        {
            return _maxChaoValue;
        }
        private set
        {
            if (_maxChaoValue != value)
            {
                int maxChaoValue = _maxChaoValue;
                _maxChaoValue = value;
                this.MaxChaoValueChanged?.Invoke(maxChaoValue, _maxChaoValue);
            }
        }
    }
    public int CurrentChaoValue
    {
        get
        {
            return _currentChaoValue;
        }
        private set
        {
            if (value < 0)
            {
                throw new ArgumentException("Current Chao Value must be positive", nameof(value));
            }
            if (_currentChaoValue != value)
            {
                int currentChaoValue = _currentChaoValue;
                _currentChaoValue = value;
                LibraryChaoHealNumberVfx.Show(this, _currentChaoValue - currentChaoValue);
                this.CurrentChaoValueChanged?.Invoke(currentChaoValue, _currentChaoValue);
            }
        }
    }
    public void HealChaoInternal(decimal amount)
    {
        if(!HasChaoResistance)return;
        SetCurrentChaoValueInternal((decimal)CurrentChaoValue + amount);
    }

    public void ScaleMonsterChaoValueForMultiplayer( EncounterModel? encounter, int playerCount, int actIndex)
    {
        if(playerCount != 1)
        {
            SetMaxChaoValueInternal(ScaleHpForMultiplayer(MaxChaoValue, encounter, playerCount, actIndex));
            SetCurrentChaoValueInternal(MaxChaoValue);
        }
    }
    public void SetCurrentChaoValueInternal(decimal amount){
        if(!HasChaoResistance)return;
        CurrentChaoValue = (int)Math.Min(amount, MaxChaoValue);
    }
    public void SetMaxChaoValueInternal(decimal amount){
        if(!HasChaoResistance)return;
        if (amount < 0m)
        {
            throw new ArgumentException("amount must be non-negative.");
        }
        MaxChaoValue = Math.Min((int)amount, 999999999);
        CurrentChaoValue = Math.Min(CurrentChaoValue, MaxChaoValue);
    }
    public void SetUniqueMonsterChaoValue(IReadOnlyList<Creature> creaturesOnSide, Rng rng)
    {
        if (Monster == null)
        {
            throw new InvalidOperationException("Can't set unique monster Chao value for a player.");
        }
        if (Monster is LibraryMonsterModel model && model.DefaultChaoResistance > 0)
        {
            MonsterMaxChaoValueBeforeModification = _currentChaoValue = _maxChaoValue = model.DefaultChaoResistance;
        }
    }
    public double GetChaoValuePercentRemaining() => (double)CurrentChaoValue / (double)MaxChaoValue;
    public static decimal ScaleChaoValueForMultiplayer(decimal chaoValue, EncounterModel? encounter, int playerCount, int actIndex){
        if (playerCount == 1)
        {
            return chaoValue;
        }
        return chaoValue * (decimal)playerCount * MultiplayerScalingModel.GetMultiplayerScaling(encounter, actIndex);
    }
    public LibraryChaoResult? LoseChaoValueInternal(decimal amount, ValueProp props)
    {   
        if(!HasChaoResistance)return null;
        bool flag = CurrentChaoValue > 0 && amount >= (decimal)CurrentChaoValue;
        int currentChaoValue = CurrentChaoValue;
        int num = (int)Math.Min(amount, 999999999m);
        CurrentChaoValue = Math.Max(CurrentChaoValue - num, 0);
        return new LibraryChaoResult(this, props)
        {
		    OverStunChaoValue = flag ? Math.Max(num - currentChaoValue, 0) : 0,
            ChaoValueAmount = currentChaoValue - CurrentChaoValue,
            WasStun = CurrentChaoValue == 0 && !IsStunned,
        };
    }
    public new void StunInternal(Func<IReadOnlyList<Creature>, Task> stunMove, string? nextMoveId)
    {
        if (Monster == null)
        {
            throw new InvalidOperationException("Can't stun a player.");
        }
        if (CombatState != null && !IsDead)
        {
            SaveAndSetStunResistance();
            nextMoveId = ResolvePostStunMoveId(Monster, nextMoveId);
            MoveState state = new MoveState("STUNNED", stunMove, new StunIntent())
            {
                FollowUpStateId = nextMoveId,
                MustPerformOnceBeforeTransitioning = true
            };
            Monster?.SetMoveImmediate(state);
        }
    }
    private static string? ResolvePostStunMoveId(MonsterModel monster, string? nextMoveId)
    {
        if (IsValidPostStunMoveId(monster, nextMoveId))
        {
            return nextMoveId;
        }

        string? loggedMoveId = monster.MoveStateMachine?.StateLog
            .LastOrDefault(state => IsValidPostStunMoveId(monster, state.Id))
            ?.Id;
        if (loggedMoveId != null)
        {
            return loggedMoveId;
        }

        string? currentMoveId = monster.NextMove?.Id;
        if (IsValidPostStunMoveId(monster, currentMoveId))
        {
            return currentMoveId;
        }

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
            && monster.MoveStateMachine?.States.ContainsKey(moveId) == true;
    }
    public NHealthBar? HealthBar => GetCreatureNode()?.GetNode<NCreatureStateDisplay>("%HealthBar")?.GetNode<NHealthBar>("%HealthBar");
    public LibraryResistanceLevel GetChaosResistanceLevel(LibraryDamageType type) => type switch{
        LibraryDamageType.Blunt=>ResistanceData.ChaosResistance.Blunt,
        LibraryDamageType.Slash=>ResistanceData.ChaosResistance.Slash,
        LibraryDamageType.Pierce=>ResistanceData.ChaosResistance.Pierce,
        _=>LibraryResistanceLevel.Normal
    } ;
    public LibraryResistanceLevel GetPhysicalResistanceLevel(LibraryDamageType type) => type switch{
        LibraryDamageType.Blunt=>ResistanceData.PhysicalResistance.Blunt,
        LibraryDamageType.Slash=>ResistanceData.PhysicalResistance.Slash,
        LibraryDamageType.Pierce=>ResistanceData.PhysicalResistance.Pierce,
        _=>LibraryResistanceLevel.Normal
    } ;

    public void SetPhysicalResistance(LibraryDamageType type,LibraryResistanceLevel resistanceValue) 
    {
        switch (type)
        {
            case LibraryDamageType.Blunt:
                ResistanceData.PhysicalResistance.Blunt = resistanceValue;
                break;
            case LibraryDamageType.Slash:
                ResistanceData.PhysicalResistance.Slash = resistanceValue;
                break;
            case LibraryDamageType.Pierce:
                ResistanceData.PhysicalResistance.Pierce = resistanceValue;
                break;
        }
        LibraryPhysicalResistanceIconsUi.Refresh(HealthBar);
    }
    public void SetChaoResistance(LibraryDamageType type,LibraryResistanceLevel resistanceValue) {
        if(!HasChaoResistance)return;
        switch (type)
        {
            case LibraryDamageType.Blunt:
                ResistanceData.ChaosResistance.Blunt = resistanceValue;
                break;
            case LibraryDamageType.Slash:
                ResistanceData.ChaosResistance.Slash = resistanceValue;
                break;
            case LibraryDamageType.Pierce:
                ResistanceData.ChaosResistance.Pierce = resistanceValue;
                break;
        }
        LibraryChaosResistanceIconsUi.Refresh(HealthBar);
    }
}
