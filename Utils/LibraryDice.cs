using Godot;
using Library.Entities.Creatures;
using Library.Hooks;
using Library.Models;
using Library.Resistance;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands.Builders;
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
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.ValueProps;

namespace Library.Utils;
public class LibraryDice : LibraryDamageVar
{
    public LibraryDice( decimal minValue,decimal floatValue, LibraryDiceType diceType, LibraryCardModel sourceCard, string name):
    base(name ,minValue,ValueProp.Move,(LibraryDamageType)diceType)
    {
        DiceType = diceType;
        SourceCard = sourceCard;
        FloatValue = floatValue;
    }
    public override string ToString()=>$"\n[img]{DescriptionIconPath}[/img]{PreviewValue} - {PreviewValue + FloatValue}{DamageResistance}{ChaoResistance}";
    public decimal DamageResistanceValue = 1m;
    public decimal ChaoResistanceValue = 0m;
    private string DamageResistance => _shouldShowDamage?$"  [red]×{DamageResistanceValue}[/red]":"";
    private string ChaoResistance => _shouldShowChao?$"  [orange]×{ChaoResistanceValue}[/orange]":"";
    private bool _shouldShowDamage = false;
    private bool _shouldShowChao = false;
    public decimal FloatValue {get;}
    readonly LibraryDiceType DiceType ;
    readonly LibraryCardModel SourceCard ;
    public LocString RawDescription => new("cards",DescriptionPath);
    public static LocString DefaultDescription => new("dice","DICE_DEFAULT");
    public LocString Description => RawDescription.GetFormattedText() == "" ? DefaultDescription : RawDescription;
    public string DescriptionIconPath => $"res://images/dice/{DiceType.String()}.png";
    public virtual string DescriptionPath =>SourceCard.Id.Entry+"_"+Name.ToUpperInvariant()+ ".description";
    public Func<PlayerChoiceContext, CardPlay, int ,Task>? DiceEffct {get;set;}
    public string PackedIconPath => ImageHelper.GetImagePath("dice/big_icon/" + DiceType.String() + ".tres");
    public LocString Title => new("dice",DiceType.String().ToUpper()+"_DICE");
	public Texture2D PackedIcon=> ResourceLoader.Load<Texture2D>($"res://images/dice/big_icon/{DiceType.String()}.png", null, ResourceLoader.CacheMode.Reuse);
    public LibraryDamageType DamageType => (LibraryDamageType)DiceType;
    public int CurrentBaseValue {
        get;
        private set;
    }
    public void Roll(Player player){
        int minValue = (int)BaseValue;
        int maxValue = (int)(BaseValue + FloatValue + 1);
        minValue = maxValue < minValue ? maxValue : minValue;
        CurrentBaseValue = player.RunState.Rng.Niche.NextInt(minValue,maxValue);
    }
    public async Task TriggerDiceEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if(DiceEffct == null)return;
        ICombatState combatState = cardPlay.Card.CombatState;
        if(!LibraryHooks.TryDiceEffect(combatState, choiceContext, cardPlay.Target, cardPlay.Card,this)) return;
        await LibraryHooks.BeforeDiceEffect(combatState, choiceContext, cardPlay.Target, cardPlay.Card, this);
        await DiceEffct(choiceContext, cardPlay, CurrentBaseValue);
        await LibraryHooks.AfterDiceEffect(combatState, choiceContext, cardPlay.Target, cardPlay.Card, this);
    }
    public HoverTip DiceTip=>new(Title,GetDescriptionForPile(SourceCard.Pile?.Type ?? PileType.None, SourceCard.CurrentTarget), PackedIcon);
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
		decimal num = base.BaseValue;
		EnchantmentModel enchantment = card.Enchantment;
		if (enchantment != null)
		{
			num += enchantment.EnchantDamageAdditive(num, Props);
			num *= enchantment.EnchantDamageMultiplicative(num, Props);
			if (!card.IsEnchantmentPreview)
			{
				base.EnchantedValue = num;
			}
		}
		if (runGlobalHooks)
		{
			num = LibraryHooks.ModifyDamage(card.Owner.RunState, card.CombatState, target, card.Owner.Creature, base.BaseValue, Props, card, ModifyDamageHookType.All, previewMode, out IEnumerable<AbstractModel> _, DamageType);
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
		base.PreviewValue = num;
	}
}