using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace LibraryLib.Models;

/// <summary>
///     把 OnPlay 拆成使用前/中/后三段，调用时更灵活。普通卡牌不继承本类也不受影响。
///     库钩子的默认实现见 LibraryModelHookDefaults.cs。
/// </summary>
public abstract partial class LibraryCardModel : CardModel, ILibraryAbstractModel
{
    public LibraryCardModel(int canonicalEnergyCost, CardType type, CardRarity rarity, TargetType targetType, bool shouldShowInCardLibrary = true)
        : base(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public virtual Task BeforeUseEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Task.CompletedTask;

    public virtual Task OnUse(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Task.CompletedTask;

    public virtual Task AfterUseEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Task.CompletedTask;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await BeforeUseEffect(choiceContext, cardPlay);
        await OnUse(choiceContext, cardPlay);
        await AfterUseEffect(choiceContext, cardPlay);
    }

    [Obsolete("Never invoked by LibraryHooks; override the LibraryPowerMode overload instead.")]
    public virtual Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, int mode) =>
        Task.CompletedTask;
}
