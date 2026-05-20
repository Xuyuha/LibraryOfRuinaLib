using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Library.Models;
using Library.Resistance;
using MegaCrit.Sts2.Core.Entities.Cards;
namespace Library.Utils;

public static class LibraryHooks//钩子类，用于在不同事件发生时触发不同的逻辑，还有很多只是列举出来，还没开始写
{
    public static Task BeforeAttack(ICombatState combatState, object attackCommand,LibraryDamageKind type) => Task.CompletedTask;
    public static Task AfterAttack(ICombatState combatState, PlayerChoiceContext choiceContext, object attackCommand,LibraryDamageKind type) => Task.CompletedTask;
    public static decimal ModifyAttackHitCount(ICombatState combatState, object attackCommand, int hitCount,LibraryDamageKind type) => hitCount;
    public static Task AfterDiceEffect(PlayerChoiceContext choiceContext, Creature? target, CardModel cardSource,LibraryDamageKind type){
        return Task.CompletedTask;
    }
    public static decimal ModifyDamage(IRunState runState, ICombatState combatState, Creature target, Creature? dealer, decimal amount, ValueProp props, CardModel? cardSource, ModifyDamageHookType hookType, CardPreviewMode previewMode, out IEnumerable<AbstractModel> modifiers,LibraryDamageKind type)
    {
        modifiers = Enumerable.Empty<AbstractModel>();
        return amount;
    }
    public static Task AfterModifyingDamageAmount(IRunState runState, ICombatState combatState, CardModel? cardSource, IEnumerable<AbstractModel> modifiers,LibraryDamageKind type) => Task.CompletedTask;
    public static Task BeforeDamageReceived(PlayerChoiceContext choiceContext, IRunState runState, ICombatState combatState, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource,LibraryDamageKind type) => Task.CompletedTask;
    public static decimal ModifyHpLostBeforeOsty(IRunState runState, ICombatState combatState, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, out IEnumerable<AbstractModel> modifiers,LibraryDamageKind type)
    {
        modifiers = Enumerable.Empty<AbstractModel>();
        return amount;
    }
    public static Task AfterModifyingHpLostBeforeOsty(IRunState runState, ICombatState combatState, IEnumerable<AbstractModel> modifiers,LibraryDamageKind type) => Task.CompletedTask;
    public static Creature ModifyUnblockedDamageTarget(ICombatState combatState, Creature target, decimal amount, ValueProp props, Creature? dealer,LibraryDamageKind type) => target;
    public static decimal ModifyHpLostAfterOsty(IRunState runState, ICombatState combatState, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, out IEnumerable<AbstractModel> modifiers,LibraryDamageKind type)
    {
        modifiers = Enumerable.Empty<AbstractModel>();
        return amount;
    }
    public static Task AfterModifyingHpLostAfterOsty(IRunState runState, ICombatState combatState, IEnumerable<AbstractModel> modifiers,LibraryDamageKind type) => Task.CompletedTask;
    public static Task AfterBlockBroken(ICombatState combatState, Creature target,LibraryDamageKind type) => Task.CompletedTask;
    public static Task AfterCurrentHpChanged(IRunState runState, ICombatState combatState, Creature target, decimal amount,LibraryDamageKind type) => Task.CompletedTask;
    public static Task AfterDamageGiven(PlayerChoiceContext choiceContext, ICombatState combatState, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource,LibraryDamageKind type) => Task.CompletedTask;
    public static Task AfterDamageReceived(PlayerChoiceContext choiceContext, IRunState runState, ICombatState combatState, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource,LibraryDamageKind type) => Task.CompletedTask;
    public static async Task AfterEffect(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        foreach (AbstractModel model in combatState.IterateHookListeners())
        {
            if (model is LibraryAbstractModel libraryModel)
            {
                await libraryModel.AfterEffect(choiceContext, power, dealer, cardSource);
            }
        }
    }
    public static async Task AfterReduce(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        foreach (AbstractModel model in combatState.IterateHookListeners())
        {
            if (model is LibraryAbstractModel libraryModel) 
            {
                await libraryModel.AfterReduce(choiceContext, power, dealer, cardSource);
            }
        }
    }
    public static async Task AfterSetMode(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, int mode)
    {
        foreach (AbstractModel model in combatState.IterateHookListeners())
        {
            if (model is LibraryAbstractModel libraryModel)
            {
                await libraryModel.AfterSetMode(choiceContext, power, dealer, cardSource, mode);
            }
        }
    }
    public static Task BeforeDiceEffect(PlayerChoiceContext choiceContext, Creature? target, CardModel cardSource){
        return Task.CompletedTask;
    }
    public static async Task BeforeEffect(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        foreach (AbstractModel model in combatState.IterateHookListeners())
        {

            if (model is LibraryAbstractModel libraryModel)
            {
                await libraryModel.BeforeEffect(choiceContext, power, dealer, cardSource);
            }
        }
    }

    public static async Task BeforeReduce(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        foreach (AbstractModel model in combatState.IterateHookListeners())
        {
            if (model is LibraryAbstractModel libraryModel) 
            {
                await libraryModel.BeforeReduce(choiceContext, power, dealer, cardSource);
            }
        }
    }
    public static async Task BeforeSetMode(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, int mode)
    {
        foreach (AbstractModel model in combatState.IterateHookListeners())
        {
            if (model is LibraryAbstractModel libraryModel)
            {
                await libraryModel.BeforeSetMode(choiceContext, power, dealer, cardSource, mode);   
            }
        }
    }
    public static bool TryDiceEffect(PlayerChoiceContext choiceContext,Creature? target, CardModel cardSource)
    {
        return true;
    }
    public static bool TryEffect(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        foreach (AbstractModel model in combatState.IterateHookListeners())
        {
            if (model is LibraryAbstractModel libraryModel)
            {
                if (!libraryModel.TryEffect(choiceContext, power, dealer, cardSource))
                {
                    return false;
                }
            }
        }
        return true;
    }
    public static bool TryReduce(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        foreach (AbstractModel model in combatState.IterateHookListeners())
        {
            if (model is LibraryAbstractModel libraryModel)
            {
                if (!libraryModel.TryReduce(choiceContext, power, dealer, cardSource))  
                {
                    return false;
                }
            }
        }
        return true;
    }
}
