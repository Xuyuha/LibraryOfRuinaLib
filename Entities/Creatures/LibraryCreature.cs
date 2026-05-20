using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using Library.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Singleton;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace Library.Entities.Creatures;
public class LibraryCreature : Creature//扩展Creature，添加Chao值属性
{
    public LibraryCreature(MonsterModel monster, CombatSide side, string? slotName) : base(monster, side, slotName)
    {
		Log.Info("Creature LibraryCreate");
        if(monster is LibraryMonsterModel libraryMonsterModel){
            _damageResistance = libraryMonsterModel.DefaultDamageResistance;
            _chaoResistance = libraryMonsterModel.DefaultChaoResistance;
        }
        else{
            _damageResistance = [1,1,1,1];
            _chaoResistance = [1,1,1,1];
        }
    }
    private decimal[] _damageResistance;
    private decimal[] _chaoResistance;
    private int _currentChaoValue;
    private int _maxChaoValue;
    public event Action<int, int>? CurrentChaoValueChanged;
    public event Action<Creature>? Stuned;
    public event Action<int, int>? MaxChaoValueChanged;

    //待添加混乱条public HpDisplay HpDisplay { get; set; }
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
                throw new ArgumentException("Current Chao Value must be positive", "value");
            }
            if (_currentChaoValue != value)
            {
                int currentChaoValue = _currentChaoValue;
                _currentChaoValue = value;
                this.CurrentChaoValueChanged?.Invoke(currentChaoValue, _currentChaoValue);
            }
        }
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
        CurrentChaoValue = (int)Math.Min(amount, MaxChaoValue);
    }
    public void SetMaxChaoValueInternal(decimal amount){
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
        int minInitialHp = Monster.MinInitialHp;
        int num = Monster.MaxInitialHp + 1;
        HashSet<int> hashSet = Enumerable.Range(minInitialHp, num - minInitialHp).ToHashSet();
        hashSet.ExceptWith(from e in creaturesOnSide.Except(new List<Creature>{this})
            select e.MaxHp);
        MonsterMaxChaoValueBeforeModification = _currentChaoValue = _maxChaoValue = (hashSet.Count <= 0) ? rng.NextInt(minInitialHp, num) : rng.NextItem(hashSet);
    }
    public double GetChaoValuePercentRemaining() => (double)CurrentChaoValue / (double)MaxChaoValue;
    public static decimal ScaleChaoValueForMultiplayer(decimal chaoValue, EncounterModel? encounter, int playerCount, int actIndex){
        if (playerCount == 1)
        {
            return chaoValue;
        }
        return chaoValue * (decimal)playerCount * MultiplayerScalingModel.GetMultiplayerScaling(encounter, actIndex);
    }
    public LibraryChaoResult LoseChaoValueInternal(decimal amount, ValueProp props)
    {
        bool flag = CurrentHp > 0 && amount >= (decimal)CurrentHp;
        int currentChaoValue = CurrentChaoValue;
        int num = (int)Math.Min(amount, 999999999m);
        CurrentChaoValue = Math.Max(CurrentChaoValue - num, 0);
        return new LibraryChaoResult(this, props)
        {
		    OverStunChaoValue = flag ? Math.Max(num - currentChaoValue, 0) : 0,
            ChaoValueAmount = currentChaoValue - CurrentChaoValue,
            WasStun = CurrentChaoValue == 0,
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
            //更改抗性图标
            if (string.IsNullOrEmpty(nextMoveId))
            {
                List<MonsterState> stateLog = Monster.MoveStateMachine.StateLog;
                nextMoveId = stateLog.Last().Id;
            }
            MoveState state = new MoveState("STUNNED", stunMove, new StunIntent())
            {
                FollowUpStateId = nextMoveId,
                MustPerformOnceBeforeTransitioning = true
            };
            Monster.SetMoveImmediate(state);
        }
    }


    public decimal GetDamageResistance(LibraryDamageType type) =>_damageResistance[(int)type] ;
    public decimal GetChaoResistance(LibraryDamageType type) =>_chaoResistance[(int)type] ;
    private void SetDamageResistance(LibraryDamageType type,decimal resistanceValue) 
    {
        //待添加伤害抗性图标改变逻辑
        _damageResistance[(int)type] = resistanceValue;
    }
    private void SetChaoResistance(LibraryDamageType type,decimal resistanceValue) {
        //待添加混乱抗性图标改变逻辑
        _chaoResistance[(int)type] = resistanceValue;
    }
    private void SetDamageResistance(decimal[] resistanceValue) 
    {
        //待添加伤害抗性图标改变逻辑
        _damageResistance = resistanceValue;
    }
    private void SetChaoResistance(decimal[] resistanceValue) {
        //待添加混乱抗性图标改变逻辑
        _chaoResistance = resistanceValue;
    }
    
    public async Task SetChaoResistance(PlayerChoiceContext choiceContext, Creature? dealer,LibraryDamageType type,decimal resistanceValue)
    {
        //LibraryHooks.TrySetChaoResistance();
        //LibraryHooks.BeforeSetChaoResistance();
        SetChaoResistance(type,resistanceValue);
        //LibraryHooks.AfterSetChaoResistance();
    }
    public async Task SetDamageResistance(PlayerChoiceContext choiceContext, Creature? dealer,LibraryDamageType type,decimal resistanceValue)
    {
        //LibraryHooks.TrySetDamageResistance();
        //LibraryHooks.BeforeSetDamageResistance();
        SetDamageResistance(type,resistanceValue);
        //LibraryHooks.AfterSetDamageResistance();
    }
}
