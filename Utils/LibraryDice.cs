using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using Godot;
using Library.Entities.Creatures;
using Library.Hooks;
using Library.Models;
using Library.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Library.Utils;
public abstract class LibraryDice(int minValue, int floatValue, LibraryDiceType diceType, LibraryCardModel sourceCard, string name) //骰子，准备做成伤害容器
{
    //todo:添加动态计算功能，伤害改为在抽到牌时计算，且有其他改变该值
    public override string ToString() => $"[img]{PackedIconPath}[/img]\t";
    public string Damage => $"{(int)MinValue.BaseValue} - {(int)MinValue.BaseValue + FloatValue.BaseValue}";
    public Creature? Target => SourceCard.CurrentTarget;
    public LibraryCreature? LibraryTarget => SourceCard.CurrentTarget as LibraryCreature;
    public string DamageResistance => $"[red] ×{LibraryTarget?.GetDamageResistance(DamageType)}[yellow][/red]";
    public string ChaoResistance => $"[yellow] ×{((LibraryTarget.CurrentChaoValue == 0)?LibraryTarget?.GetChaoResistance(DamageType):0)}[/yellow]";
    public string Resistance => DamageResistance + ChaoResistance;
    readonly DamageVar MinValue = new("MinValue", minValue, ValueProp.Move);
    readonly DamageVar FloatValue = new("FloatValue", floatValue, ValueProp.Move);
    readonly LibraryDiceType DiceType = diceType;
    readonly LibraryCardModel SourceCard = sourceCard;
    readonly public string Name = name;
    public LocString Description => new("cards",DescriptionPath);
    public string PackedIconPath => $"res://images/Dice/{DiceType}.png";
    public virtual string DescriptionPath =>SourceCard.Id.Entry +Name+ ".description";
    public Func<PlayerChoiceContext, CardPlay, int ,Task>? DiceEffct {get;set;}
    public Texture2D PackedIcon => ResourceLoader.Load<Texture2D>(PackedIconPath, null, ResourceLoader.CacheMode.Reuse);
    public LibraryDamageType DamageType => (LibraryDamageType)DiceType;
    public async Task TriggerDiceEffct (PlayerChoiceContext choiceContext, CardPlay cardPlay){
        int minValue = (int)MinValue.BaseValue;
        int maxValue = minValue + (int)FloatValue.BaseValue;
        int BaseValue = cardPlay.Card.Owner.RunState.Rng.Niche.NextInt(minValue,maxValue);
        if(DiceEffct == null)return;
        if(!LibraryHooks.TryDiceEffect(choiceContext, cardPlay.Target, cardPlay.Card)) return;
        await LibraryHooks.BeforeDiceEffect(choiceContext, cardPlay.Target, cardPlay.Card, this);
        await DiceEffct(choiceContext, cardPlay, BaseValue);
        await LibraryHooks.AfterDiceEffect(choiceContext, cardPlay.Target, cardPlay.Card, this, BaseValue);
    }
    public HoverTip DiceTip{
        get{
            return new(GetDescriptionForPile(SourceCard.Pile?.Type ?? PileType.None, SourceCard.CurrentTarget), PackedIcon);
        }
    }
    private LocString GetDescriptionForPile(PileType pileType, Creature? target = null)
    {
        LocString description = Description;
        SourceCard.DynamicVars.AddTo(description);
        UpgradeDisplay upgradeDisplay =SourceCard.IsUpgraded ? UpgradeDisplay.Upgraded : UpgradeDisplay.Normal;
        description.Add(new IfUpgradedVar(upgradeDisplay));
        bool flag = (pileType == PileType.Hand || pileType == PileType.Play) ? true : false;
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
}