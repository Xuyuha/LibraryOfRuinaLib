using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using Library.Utils;
using Library.Hooks;

namespace Library.Models;

public abstract class LibraryBasePowerModel: LibraryPowerModel//类似于烧伤，流血的基础模板
{//todo：加入生效层数和减少层数相关的计算钩子
    protected virtual Task Effect(PlayerChoiceContext ChoiceContext){//子类实现触发逻辑
        return Task.CompletedTask;
    }
    protected virtual Task Reduce(PlayerChoiceContext ChoiceContext){//子类实现减少逻辑
        return Task.CompletedTask;
    }
    public async Task TriggerEffect(PlayerChoiceContext choiceContext, Creature? dealer, CardModel? cardSource)//通过触发方法实现钩子的触发
    {
        if(Owner.IsDead) return;
        ICombatState combatState = Owner.CombatState!;
        if (!LibraryHooks.TryPowerEffect(combatState, choiceContext, this, dealer, cardSource)) return;
        Log.Info(Id+"Effect");
        await LibraryHooks.BeforePowerEffect(combatState, choiceContext, this, dealer, cardSource);
        await Effect(choiceContext);
        await LibraryHooks.AfterPowerEffect(combatState, choiceContext, this, dealer, cardSource);
    }
    public async Task TriggerReduce(PlayerChoiceContext choiceContext, Creature? dealer, CardModel? cardSource)//通过减少方法实现减少钩子的触发
    {
        if(Owner.IsDead) return;
        ICombatState combatState = Owner.CombatState!;
        if (!LibraryHooks.TryPowerReduce(combatState, choiceContext, this, dealer, cardSource)) return;
        Log.Info(Id+"Reduce");
        await LibraryHooks.BeforePowerReduce(combatState, choiceContext, this, dealer, cardSource);
        await Reduce(choiceContext);
        await LibraryHooks.AfterPowerReduce(combatState, choiceContext, this, dealer, cardSource);
    }
}