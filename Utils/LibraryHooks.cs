using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Library.Models;
namespace Library.Utils;

public static class LibraryHooks//钩子类，用于在不同事件发生时触发不同的逻辑
{
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
