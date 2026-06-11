using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using Library.Utils;
using Library.Hooks;

namespace Library.Models;

public abstract class LibraryBasePowerModel: LibraryMultipleModePowerModel//类似于烧伤，流血的基础模板
{
    protected virtual Task Effect(PlayerChoiceContext ChoiceContext, decimal effectiveAmount){//子类实现触发逻辑
        return Task.CompletedTask;
    }
    protected virtual Task Reduce(PlayerChoiceContext ChoiceContext){//子类实现减少逻辑
        return Task.CompletedTask;
    }
    public async Task TriggerEffect(PlayerChoiceContext choiceContext, Creature? dealer, CardModel? cardSource)//通过触发方法实现钩子的触发
    {
        ICombatState? combatState = Owner?.CombatState;
        if(combatState == null)return;
        if (!LibraryHooks.TryPowerEffect(combatState, choiceContext, this, dealer, cardSource)) return;
        Log.Info(Id+"Effect");
        decimal effectiveAmount = LibraryHooks.ModifyEffectiveAmount(combatState,Owner,  dealer , Amount, cardSource, out IEnumerable<AbstractModel> modifiers);
        await LibraryHooks.AfterModifyingEffectiveAmount(combatState, cardSource, modifiers);
        await LibraryHooks.BeforePowerEffect(combatState, choiceContext, this, dealer, cardSource);
        await Effect(choiceContext, effectiveAmount);
        await LibraryHooks.AfterPowerEffect(combatState, choiceContext, this, dealer, cardSource);
    }
    public async Task TriggerReduce(PlayerChoiceContext choiceContext, Creature? dealer, CardModel? cardSource)//通过减少方法实现减少钩子的触发
    {
        ICombatState? combatState = Owner?.CombatState;
        if(combatState == null)return;
        if (!LibraryHooks.TryPowerReduce(combatState, choiceContext, this, dealer, cardSource)) return;
        Log.Info(Id+"Reduce");
        await LibraryHooks.BeforePowerReduce(combatState, choiceContext, this, dealer, cardSource);
        await Reduce(choiceContext);
        await LibraryHooks.AfterPowerReduce(combatState, choiceContext, this, dealer, cardSource);
    }
}