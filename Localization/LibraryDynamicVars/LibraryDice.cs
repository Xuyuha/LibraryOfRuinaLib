using Godot;
using Library.Entities.Creatures;
using Library.Hooks;
using Library.Models;
using Library.Resistance;
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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Library.Utils;
public class LibraryDice : DynamicVar
{
    public const ValueProp Props = ValueProp.Move;
    public LibraryDice( decimal minValue,decimal floatValue, LibraryDiceType diceType, LibraryCardModel sourceCard, string name):
    base(name , minValue)
    {
        DiceType = diceType;
        SourceCard = sourceCard;
        FloatValue = floatValue;
    }
    public override string ToString()=>$"[img]{DescriptionIconPath}[/img]{PreviewValue} - {PreviewValue + FloatValue}{DamageAdditive}{DamageResistance}{ChaoAdditive}{ChaoResistance}\n";
    public  bool ShouldUseDefaultTip {get;set;} = true;
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
    public readonly LibraryCardModel SourceCard ;
    private int IdNumber = 0;
    public static LocString DefaultDescription => new("dice","DICE_DEFAULT");
    public LocString Description =>  ShouldUseDefaultTip ? DefaultDescription:new("cards",DescriptionPath);
    public string DescriptionIconPath => $"res://images/dice/{DiceType.String()}.png";
    public virtual string DescriptionPath =>SourceCard.Id.Entry+"_"+Name.ToUpperInvariant()+ ".description";
    private Func<PlayerChoiceContext, CardPlay, int ,Task>? _diceEffct ;
    public string PackedIconPath => ImageHelper.GetImagePath("dice/big_icon/" + DiceType.String() + ".tres");
    public LocString Title => new("dice",DiceType.String().ToUpper()+"_DICE");
	public Texture2D PackedIcon=> ResourceLoader.Load<Texture2D>($"res://images/dice/big_icon/{DiceType.String()}.png", null, ResourceLoader.CacheMode.Reuse);
    public LibraryDamageType DamageType => (LibraryDamageType)DiceType;
    public int UseTimes = 1;
    public bool EnableCustomUseTimes = false;
    public int HasUseTimes = 0;
    public int CurrentBaseValue {
        get;
        private set;
    }
    public LibraryAttackCommand? Command = null;
    public LibraryDice WithUseTimes (int useTimes)
    {
        UseTimes = useTimes;
        EnableCustomUseTimes = true;
        return this;
    }
    public void Roll(Player player){
        int minValue = (int)BaseValue;
        int maxValue = (int)(BaseValue + FloatValue + 1);
        minValue = maxValue < minValue ? maxValue : minValue;
        CurrentBaseValue = player.RunState.Rng.Niche.NextInt(minValue,maxValue);
    }
    public async Task TriggerDiceEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if(_diceEffct == null)return;
        ICombatState? combatState = cardPlay?.Card?.CombatState;
        if(combatState == null)return;
        if(!LibraryHooks.TryDiceEffect(combatState, choiceContext, [cardPlay.Target], cardPlay.Card,this)) return;
        await LibraryHooks.BeforeDiceEffect(combatState, choiceContext, [cardPlay.Target], cardPlay.Card, this);
        await _diceEffct(choiceContext, cardPlay, CurrentBaseValue);
        await LibraryHooks.AfterDiceEffect(combatState, choiceContext, [cardPlay.Target], cardPlay.Card, this);
    }
    public LibraryDice WithDiceEffect(Func<PlayerChoiceContext, CardPlay, int ,Task>? diceEffct){
		if (_diceEffct != null)
		{
			throw new InvalidOperationException($"Tried to set extra dice effect on {this.Name} twice!");
		}
		_diceEffct = diceEffct;
        HasUniqueDescriptionTip();
		return this;
    }
    public LibraryDice HasUniqueDescriptionTip(){
        ShouldUseDefaultTip = false;
        return this;
    }
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
                num1 = LibraryHooks.ModifyChaoDamage(card.Owner.RunState, card.CombatState, target, card.Owner.Creature, base.BaseValue, Props, card, ModifyChaoDamageHookType.All, previewMode, out IEnumerable<AbstractModel> _, DamageType);
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
}
