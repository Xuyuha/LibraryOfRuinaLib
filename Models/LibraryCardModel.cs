using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;

namespace Library.Models;
public abstract class LibraryCardModel : CardModel, ILibraryAbstractModel//加入了使用前/中/后的方法，调用时更灵活，不过一般卡牌类不继承这个类影响也不大
{
    public LibraryCardModel(int canonicalEnergyCost, CardType type, CardRarity rarity, TargetType targetType, bool shouldShowInCardLibrary = true) : base(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
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

