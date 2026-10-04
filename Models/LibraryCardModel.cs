using LibraryLib.Commands;
using LibraryLib.Entities.Creatures;
using LibraryLib.Localization.LibraryDynamicVars;
using LibraryLib.Powers.LibraryPowerMode;
using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using LibraryLib.Localization.Dice;

namespace LibraryLib.Models;
public abstract class LibraryCardModel : CardModel//加入了使用前/中/后的方法，调用时更灵活，不过一般卡牌类不继承这个类影响也不大
{    public LibraryCardModel(int canonicalEnergyCost, CardType type, CardRarity rarity, TargetType targetType, bool shouldShowInCardLibrary = true) : base(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
        
    }
    public virtual Task BeforeUseEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }
    public virtual Task OnUse(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterUseEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await BeforeUseEffect(choiceContext, cardPlay);
        await OnUse(choiceContext, cardPlay);
        await AfterUseEffect(choiceContext, cardPlay);
    }
    
}

