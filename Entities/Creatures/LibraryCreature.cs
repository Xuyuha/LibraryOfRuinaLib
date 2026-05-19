using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using Library.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace Library.Entities.Creatures;
public class LibraryCreature : Creature//扩展Creature，添加Chao值属性
{
    public LibraryCreature(MonsterModel monster, CombatSide side, string? slotName) : base(monster, side, slotName)
    {
		Log.Info("Creature Create");
        if(monster is LibraryMonsterModel libraryMonsterModel){
            _damageResistance = libraryMonsterModel.DefaultDamageResistance;
            _chaoResistance = libraryMonsterModel.DefaultChaoResistance;
        }
        else{
            _damageResistance = [1,1,1,1];
            _chaoResistance = [1,1,1,1];
        }
    }
    public LibraryCreature(Player player, int currentHp, int maxHp) : base(player, currentHp, maxHp)
    {
		Log.Info("Creature Create");
        _damageResistance = [1,1,1,1];
        _chaoResistance = [1,1,1,1];
    }
        
    private decimal[] _damageResistance;
    private decimal[] _chaoResistance;
    private int _currentChaoValue;
    public event Action<int, int>? CurrentChaoValueChanged;

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

    public LibraryChaoResult LoseChaoValueInternal(decimal amount, ValueProp props)
    {
        bool flag = CurrentHp > 0 && amount >= (decimal)CurrentHp;
        int currentChaoValue = CurrentChaoValue;
        int num = (int)Math.Min(amount, 999999999m);
        CurrentChaoValue = Math.Max(CurrentChaoValue - num, 0);
        return new LibraryChaoResult(this, props)
        {
		    OverStunChao = flag ? Math.Max(num - currentChaoValue, 0) : 0,
            ChaoAmount = currentChaoValue - CurrentChaoValue,
            WasStun = CurrentChaoValue == 0,
        };
    }

    public decimal GetDamageResistance(LibraryDamageType type) =>_damageResistance[(int)type] ;
    public decimal GetChaoResistance(LibraryDamageType type) =>_chaoResistance[(int)type] ;
    private void SetDamageResistance(LibraryDamageType type,decimal resistanceValue) 
    {
        //伤害抗性图标改变逻辑
        _damageResistance[(int)type] = resistanceValue;
    }
    private void SetChaoResistance(LibraryDamageType type,decimal resistanceValue) {
        //混乱抗性图标改变逻辑
        _chaoResistance[(int)type] = resistanceValue;
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
