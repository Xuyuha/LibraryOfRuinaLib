using Godot;
using LibraryLib.Commands;
using LibraryLib.Entities.Creatures;
using LibraryLib.Hooks;
using LibraryLib.Localization.Dice;
using LibraryLib.Models;
using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Localization.LibraryDynamicVars;
public class LibraryDice : DynamicVar
{
	private const int _maxAdditionalDiceRolls = 32;
    public const ValueProp Props = ValueProp.Move;
    public LibraryDice(decimal minValue, decimal floatValue, LibraryDiceType diceType, CardModel sourceCard, string name):
    base(name , minValue)
    {
        DiceType = diceType;
        SourceCard = sourceCard;
        FloatValue = floatValue;
    }
    public override string ToString()=>$"[img]{DescriptionIconPath}[/img]{Value}{DamageAdditive}{DamageResistance}{ChaoAdditive}{ChaoResistance}\n";
    
	/// <summary>
	///     表示骰子是否该关闭默认提示，从而启用自定义提示
	/// </summary>
    public bool ShouldUseDefaultTip {get;set;} = true;
    public string Value => $"{Colour1}{checked((int)PreviewValue)} - {checked((int)PreviewValue + FloatValue)}{Colour2}";
    private string Colour1 => _colour == "" ? "" : $"[{_colour}]";
    private string Colour2 => _colour == "" ? "" : $"[/{_colour}]";
    protected string _colour = "";
    public decimal DamageResistanceValue = 1m;
    public decimal ChaoResistanceValue = 0m;
    private int DamageAdditiveValue = 0;
    private int ChaoAdditiveValue = 0;
    private string DamageSign => DamageAdditiveValue < 0 ? "" : "+";
    private string ChaoSign =>  ChaoAdditiveValue < 0 ? "" : "+";
    private string DamageAdditive =>  DamageAdditiveValue != 0 ? $" [red]{DamageSign}{DamageAdditiveValue}[/red]":"";
    private string ChaoAdditive => _shouldShowChao && ChaoAdditiveValue != 0 ? $" [orange]{ChaoSign}{ChaoAdditiveValue}[/orange]":"";
    private string DamageResistance => _shouldShowDamage?$" [red]×{DamageResistanceValue}[/red]":"";
    private string ChaoResistance => _shouldShowChao?$" [orange]×{ChaoResistanceValue}[/orange]":"";
    private bool _shouldShowDamage = false;
    private bool _shouldShowChao = false;
    public decimal FloatValue {get;set;}
    public readonly LibraryDiceType DiceType ;
    public CardModel SourceCard ;
    private int IdNumber = 0;
    public static LocString DefaultDescription => new("dice","DICE_DEFAULT");
    public LocString Description =>  ShouldUseDefaultTip ? DefaultDescription:new("cards",DescriptionPath);
    public string DescriptionIconPath => $"res://LibraryOfRuinaLib/images/dice/{DiceType.String()}.png";
	/// <summary>
	///     骰子的自定义提示路径，可重写
	/// </summary>
    public virtual string DescriptionPath =>SourceCard.Id.Entry+"_"+Name.ToUpperInvariant()+ ".description";
    private Func<PlayerChoiceContext, CardPlay, int ,Task>? _diceEffct ;
    public string PackedIconPath => $"res://LibraryOfRuinaLib/images/dice/big_icon/{DiceType.String()}.tres";
    public LocString Title => new("dice",DiceType.String().ToUpper()+"_DICE");
	public Texture2D PackedIcon=> ResourceLoader.Load<Texture2D>(PackedIconPath, null, ResourceLoader.CacheMode.Reuse);
    public LibraryDamageType DamageType => (LibraryDamageType)DiceType;
    
	/// <summary>
	///     表示骰子的使用次数，默认1次
	/// </summary>
    public int UseTimes = 1;
    public bool EnableCustomUseTimes = false;
        
	/// <summary>
	///     表示本次骰子投出的值
	/// </summary>
    public override void SetOwner(AbstractModel owner)
    {
        base.SetOwner(owner);
        if (owner is CardModel card)
            SourceCard = card;
    }
	/// <summary>
	///     设置骰子的使用次数
	/// </summary>
    public LibraryDice WithUseTimes (int useTimes)
    {
        if (useTimes < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(useTimes), "Dice use times must be at least 1.");
        }
        UseTimes = useTimes;
        EnableCustomUseTimes = true;
        return this;
    }
    public DiceRollResult Roll(DiceRollResult result,Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return Roll(result,player.RunState);
    }
    public DiceRollResult Roll(DiceRollResult result,IRunState runState)
    {
        ArgumentNullException.ThrowIfNull(runState);
        if (BaseValue != decimal.Truncate(BaseValue) || FloatValue != decimal.Truncate(FloatValue))
        {
            throw new InvalidOperationException($"Dice {Name} requires integer minimum and range values.");
        }
        if (FloatValue < 0)
        {
            throw new InvalidOperationException($"Dice {Name} cannot have a negative range.");
        }
        result.CurrentValue = runState.Rng.Niche.NextInt(result.MinValue, result.MaxValue);
        return result;
    }
    public async Task TriggerDiceEffect(PlayerChoiceContext choiceContext, CardPlay? cardPlay,DiceRollResult result)
    {
        if (cardPlay == null)
        {
            return;
        }
        IEnumerable<Creature> targets = cardPlay.Target == null ? [] : [cardPlay.Target];
        await TriggerDiceEffect(choiceContext, cardPlay, result, targets);
    }
    public async Task TriggerDiceEffect(PlayerChoiceContext choiceContext, CardPlay? cardPlay,DiceRollResult result,IEnumerable<Creature>? targets)
    {
        if(_diceEffct == null || cardPlay == null)return;
        ICombatState? combatState = cardPlay.Card.CombatState;
        if(combatState == null)return;
        if(!LibraryHooks.TryDiceEffect(combatState, choiceContext, targets, cardPlay.Card,this,result)) return;
        await LibraryHooks.BeforeDiceEffect(combatState, choiceContext, targets, cardPlay.Card, this,result);
        await _diceEffct(choiceContext, cardPlay, result.CurrentValue);
        await LibraryHooks.AfterDiceEffect(combatState, choiceContext, targets, cardPlay.Card, this,result);
    }
	/// <summary>
	///     可将标签为Task Function (PlayerChoiceContext , CardPlay)的方法作为骰子特殊效果，使用后，骰子将启用自定义描述；
    ///     注意该方法中调用的card的子属性一定得从cardplay中获取
	/// </summary>
    public LibraryDice WithDiceEffect(Func<PlayerChoiceContext, CardPlay, int ,Task>? diceEffct){
		if (_diceEffct != null)
		{
			throw new InvalidOperationException($"Tried to set extra dice effect on {this.Name} twice!");
		}
		_diceEffct = diceEffct;
        HasUniqueDescriptionTip();
		return this;
    }
	/// <summary>
	///     启用自定义描述
	/// </summary>
    
    public LibraryDice HasUniqueDescriptionTip(){
        ShouldUseDefaultTip = false;
        return this;
    }
    //由于骰子附属于卡牌，所以table与卡牌描述一致，为card.
	/// <summary>
	///     骰子的提示，设置了WithDiceEffect或HasUniqueDescriptionTip会显示自定义提示，反之则显示默认提示。
    ///     自定义提示在卡牌描述中定义，如CARD1需给Name为Dice1的骰子添加自定义,则key为CARD1_DICE1.description。
	/// </summary>
    public HoverTip DiceTip {
        get
        {
        var tip = new HoverTip(Title, GetDescriptionForPile(SourceCard.Pile?.Type ?? PileType.None, SourceCard.CurrentTarget), PackedIcon);
        tip.Id += '_' +Name + '_' + IdNumber++;
        return tip;
        }
    }
    private LocString GetDescriptionForPile(PileType pileType, Creature? target = null)
    {
        LocString description = Description;
        SourceCard.DynamicVars.AddTo(description);
        UpgradeDisplay upgradeDisplay =SourceCard.IsUpgraded ? UpgradeDisplay.Upgraded : UpgradeDisplay.Normal;
        description.Add(new IfUpgradedVar(upgradeDisplay));
        bool flag = pileType == PileType.Hand || pileType == PileType.Play;
        bool variable = flag;
        description.Add("OnTable", variable);
        bool variable2 = CombatManager.Instance.IsInProgress && (SourceCard.Pile?.IsCombatPile ?? pileType.IsCombatPile());
        description.Add("InCombat", variable2);
        description.Add("IsTargeting", target != null);
        description.Add("TargetType", SourceCard.TargetType.ToString());
        description.Add("GainsBlock", SourceCard.GainsBlock);
        string prefix = EnergyIconHelper.GetPrefix(SourceCard);
        description.Add("energyPrefix", prefix);
        description.Add("singleStarIcon", "[img]res://images/packed/sprite_fonts/star_icon.png[/img]");
        return description;
    }
	public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
	{
        if(DiceType != LibraryDiceType.Block){
            decimal num = base.BaseValue;
            decimal num1 = base.BaseValue;
            EnchantmentModel enchantment = card.Enchantment;
            if (enchantment != null)
            {
                num += enchantment.EnchantDamageAdditive(num, Props);
                num *= enchantment.EnchantDamageMultiplicative(num, Props);
                if (!card.IsEnchantmentPreview)
                {
                    base.EnchantedValue = num;
                }
                if(enchantment is LibraryEnchantmentModel le){
                    num1 +=le.EnchantChaoDamageAdditive(num1,Props);
                    num1 *=le.EnchantChaoDamageMultiplicative(num1,Props);
                }
            }
            if (runGlobalHooks)
            {
                num = LibraryHooks.ModifyDamage(card.Owner.RunState, card.CombatState, target, card.Owner.Creature, base.BaseValue, Props, card, null, ModifyDamageHookType.All, previewMode, out IEnumerable<AbstractModel> _, DamageType);
                num1 = LibraryHooks.ModifyChaoDamage(card.Owner.RunState, card.CombatState, target, card.Owner.Creature, base.BaseValue, Props, card, null, ModifyChaoDamageHookType.All, previewMode, out IEnumerable<AbstractModel> _, DamageType);
            }
            if(target is LibraryCreature lc)
            {
                _shouldShowDamage =true;
                DamageResistanceValue = lc.GetPhysicalResistanceLevel(DamageType).GetMultiplier();
                if (lc.HasChaoResistance)
                {
                    ChaoResistanceValue = lc.GetChaosResistanceLevel(DamageType).GetMultiplier();
                    _shouldShowChao =true;
                }
                else
                    _shouldShowChao =false;
            }
            else
            {
                _shouldShowChao =false;
                _shouldShowDamage =false;
            }
            DamageAdditiveValue = (int)(num - BaseValue);
            ChaoAdditiveValue = (int)(num1 - BaseValue);
        }
        else{
            PreviewValue = BaseValue;
            decimal num = base.BaseValue;
            EnchantmentModel enchantment = card.Enchantment;
            if (enchantment != null)
            {
                num += enchantment.EnchantBlockAdditive(num);
                num *= enchantment.EnchantBlockMultiplicative(num);
                if (!card.IsEnchantmentPreview)
                {
                    base.EnchantedValue = num;
                }
            }
            if (runGlobalHooks)
            {
                num = Hook.ModifyBlock(card.CombatState, card.Owner.Creature, base.BaseValue, Props, card, null, out IEnumerable<AbstractModel> _);
            }
            if(num - BaseValue > 0)
                _colour = "green";
            else if(num - BaseValue < 0)
                _colour = "red";
            else
                _colour = "";
            base.PreviewValue = num;
        }
        ResistancePreview.ApplyPhysicalResistancePreview(
			card,
			previewMode,
			target,
			0,
			Props,
			DamageType);
    }
    public static async Task<DiceRollResult?> GetResultWithRoll(ICombatState combatState,PlayerChoiceContext? choiceContext,LibraryDice? dice,List<Creature> targets)
    {
        if(dice == null) return null;
        decimal maxValue = LibraryHooks.ModifyDiceMaxValue(combatState,dice,dice.BaseValue + dice.FloatValue);
        decimal minValue = LibraryHooks.ModifyDiceMaxValue(combatState,dice,dice.BaseValue);
        DiceRollResult rollResult = new DiceRollResult
        {
            MaxValue = checked((int)maxValue),
            MinValue = Math.Min(checked((int)minValue),checked((int)maxValue))
        };
        int j;
        for(j=0;j<32;j++)
        {
            await LibraryHooks.BeforeDiceRoll(combatState, choiceContext ?? new BlockingPlayerChoiceContext(), targets, dice);
            rollResult = dice.Roll(rollResult,combatState.RunState);
            await LibraryHooks.AfterDiceRoll(combatState, choiceContext ?? new BlockingPlayerChoiceContext(), targets, dice,rollResult);
            if(!LibraryHooks.ShouldReroll(combatState,targets,dice,out ILibraryAbstractModel? trigger,rollResult))
            {
                break;
            }
            if(trigger != null)
                await trigger.AfterRerolling(choiceContext ?? new BlockingPlayerChoiceContext(),targets,dice,rollResult);
        }
        if(j == _maxAdditionalDiceRolls)
        {
            Log.Warn($"[LibraryOfRuinaLib.Dice] Reroll limit reached for {dice.Name}.");
        }
        return rollResult;
    }
}
