using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using Library.Utils;

namespace Library.Models;

public abstract class LibraryBasePowerModel: LibraryPowerModel
{
    protected virtual Task Effect(PlayerChoiceContext ChoiceContext){
        return Task.CompletedTask;
    }
    protected virtual Task Reduce(PlayerChoiceContext ChoiceContext){
        return Task.CompletedTask;
    }
    public async Task TriggerEffect(PlayerChoiceContext choiceContext, Creature? dealer, CardModel? cardSource)
    {
        ICombatState combatState = Owner.CombatState!;
        if (!LibraryHooks.TryEffect(combatState, choiceContext, this, dealer, cardSource)) return;
        Log.Info("Effect");
        await LibraryHooks.BeforeEffect(combatState, choiceContext, this, dealer, cardSource);
        await Effect(choiceContext);
        await LibraryHooks.AfterEffect(combatState, choiceContext, this, dealer, cardSource);
    }
    public async Task TriggerReduce(PlayerChoiceContext choiceContext, Creature? dealer, CardModel? cardSource)
    {
        ICombatState combatState = Owner.CombatState!;
        if (!LibraryHooks.TryReduce(combatState, choiceContext, this, dealer, cardSource)) return;
        Log.Info("Reduce");
        await LibraryHooks.BeforeReduce(combatState, choiceContext, this, dealer, cardSource);
        await Reduce(choiceContext);
        await LibraryHooks.AfterReduce(combatState, choiceContext, this, dealer, cardSource);
    }
}