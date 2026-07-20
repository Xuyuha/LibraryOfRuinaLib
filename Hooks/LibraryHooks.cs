using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Library.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using Library.Entities.Creatures;
using Library.Utils;
using Library.Resistance;
using Library.Powers.Mode;
using MegaCrit.Sts2.Core.Logging;
using System.Text.RegularExpressions;
using System.Runtime;
using System.Threading.Tasks;
using Library.Combat;
using Library.SpeedDice;
namespace Library.Hooks;

public static class LibraryHooks
{
    public static bool HasIncomingDamageInterceptor(
        IRunState runState,
        ICombatState combatState)
    {
        return !LibraryIncomingDamageInterception.IsSuppressed
            && runState.IterateHookListeners(combatState)
                .Any(static model =>
                    model is ILibraryIncomingDamageInterceptor);
    }

    public static async Task<LibraryIncomingDamageResolution>
        InterceptIncomingDamage(
            PlayerChoiceContext choiceContext,
            IRunState runState,
            ICombatState combatState,
            Creature target,
            decimal amount,
            ValueProp props,
            Creature? dealer,
            CardModel? cardSource,
            CardPlay? cardPlay,
            LibraryDamageType type)
    {
        var originalDamage = Math.Max(0m, amount);
        if (originalDamage <= 0m
            || LibraryIncomingDamageInterception.IsSuppressed)
        {
            return LibraryIncomingDamageResolution.PassThrough(
                originalDamage);
        }

        var remainingDamage = originalDamage;
        var interceptedDamage = 0m;
        foreach (var model in
                 runState.IterateHookListeners(combatState))
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (model is not ILibraryIncomingDamageInterceptor interceptor)
            {
                continue;
            }

            var pushedModel = false;
            try
            {
                choiceContext.PushModel(model);
                pushedModel = true;
                var candidate =
                    await interceptor.InterceptIncomingDamageAsync(
                        new LibraryIncomingDamageContext(
                            choiceContext,
                            target,
                            dealer,
                            originalDamage,
                            remainingDamage,
                            props,
                            cardSource,
                            cardPlay,
                            type));
                var normalizedRemaining = Math.Clamp(
                    candidate.RemainingDamage,
                    0m,
                    remainingDamage);
                interceptedDamage +=
                    remainingDamage - normalizedRemaining;
                remainingDamage = normalizedRemaining;
            }
            finally
            {
                if (pushedModel)
                {
                    choiceContext.PopModel(model);
                }
            }

            model.InvokeExecutionFinished();
            if (remainingDamage <= 0m)
            {
                break;
            }
        }

        return new LibraryIncomingDamageResolution(
            remainingDamage,
            interceptedDamage,
            interceptedDamage > 0m && remainingDamage <= 0m);
    }

    public static async Task BeforeSetPhysicalResistance(ICombatState combatState,PlayerChoiceContext choiceContext,LibraryCreature target, Creature? dealer,LibraryDamageType type,LibraryResistanceLevel resistanceValue)
    {
        foreach (var model in combatState.IterateHookListeners())
        {
            if(model is ILibraryAbstractModel libraryAbstractModel)
            {
                await libraryAbstractModel.BeforeSetPhysicalResistance(choiceContext, target, dealer, type, resistanceValue);
                model.InvokeExecutionFinished();
            }
        }
    }
    public static bool TrySetPhysicalResistance(ICombatState combatState, PlayerChoiceContext choiceContext,LibraryCreature target, Creature? dealer,LibraryDamageType type,LibraryResistanceLevel resistanceValue)
    {
        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is ILibraryAbstractModel libraryModel)
            {
                if (!libraryModel.TrySetPhysicalResistance(choiceContext, target, dealer, type, resistanceValue))
                {
                    return false;
                }
            }
        }
        return true;
    }
    public static async Task AfterSetPhysicalResistance(ICombatState combatState,PlayerChoiceContext choiceContext,LibraryCreature target, Creature? dealer,LibraryDamageType type)
    {
        foreach (var model in combatState.IterateHookListeners())
        {
            if(model is ILibraryAbstractModel libraryAbstractModel)
            {
                await libraryAbstractModel.AfterSetPhysicalResistance(choiceContext, target, dealer, type);
                model.InvokeExecutionFinished();
            }
        }
    }
    public static async Task BeforeSetChaoResistance(ICombatState combatState,PlayerChoiceContext choiceContext,LibraryCreature target, Creature? dealer,LibraryDamageType type,LibraryResistanceLevel resistanceValue)
    {
        foreach (var model in combatState.IterateHookListeners())
        {
            if(model is ILibraryAbstractModel libraryAbstractModel)
            {
                await libraryAbstractModel.BeforeSetChaoResistance(choiceContext, target, dealer, type, resistanceValue);
                model.InvokeExecutionFinished();
            }
        }
    }
    public static bool TrySetChaoResistance(ICombatState combatState, PlayerChoiceContext choiceContext,LibraryCreature target, Creature? dealer,LibraryDamageType type,LibraryResistanceLevel resistanceValue)
    {
        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is ILibraryAbstractModel libraryModel)
            {
                if (!libraryModel.TrySetChaoResistance(choiceContext, target, dealer, type, resistanceValue))
                {
                    return false;
                }
            }
        }
        return true;
    }
    public static async Task AfterSetChaoResistance(ICombatState combatState,PlayerChoiceContext choiceContext,LibraryCreature target, Creature? dealer,LibraryDamageType type)
    {
        foreach (var model in combatState.IterateHookListeners())
        {
            if(model is ILibraryAbstractModel libraryAbstractModel)
            {
                await libraryAbstractModel.AfterSetChaoResistance(choiceContext, target, dealer, type);
                model.InvokeExecutionFinished();
            }
        }
    }


    public static async Task AfterAttack(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryAttackCommand command) 
    {
        foreach (var model in combatState.IterateHookListeners())
        {
            await model.AfterAttack(choiceContext, command.ToAttackCommand);
            if(model is ILibraryAbstractModel libraryAbstractModel)
            {
                await libraryAbstractModel.AfterAttack(choiceContext, command);
            }
            model.InvokeExecutionFinished();
        }
    }
    public static async Task AfterBlockBroken(
        ICombatState combatState,
        PlayerChoiceContext choiceContext,
        Creature target,
        Creature? breaker,
        LibraryDamageType type)
    public static async Task AfterBlockBroken(
        ICombatState combatState,
        PlayerChoiceContext choiceContext,
        Creature target,
        Creature? breaker,
        LibraryDamageType type)
    {
        foreach (var model in combatState.IterateHookListeners())
        {
            var hookPhase = "AbstractModel.AfterBlockBroken";
            try
            {
                await model.AfterBlockBroken(choiceContext, target, breaker);
                await model.AfterBlockBroken(choiceContext, target, breaker);
                if(model is ILibraryAbstractModel libraryAbstractModel)
                {
                    hookPhase = "ILibraryAbstractModel.AfterBlockBroken";
                    await libraryAbstractModel.AfterBlockBroken(target, type);
                }
            }
            catch (Exception exception)
            {
                LogAfterBlockBrokenListenerFailure(exception, model, hookPhase, target, type);
                throw;
            }
            model.InvokeExecutionFinished();
        }
    }
    public static async Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, ICombatState combatState, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type)
    {
        foreach (var model in combatState.IterateHookListeners())
        {
            if(model is ILibraryAbstractModel libraryAbstractModel)
            {
                choiceContext.PushModel(model);
                await libraryAbstractModel.AfterChaoDamageGiven(choiceContext, dealer, results, props, target, cardSource, type);
                choiceContext.PopModel(model);
                model.InvokeExecutionFinished();
            }
        }
    }
    public static async Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, IRunState runState, ICombatState combatState, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) 
    {
        foreach (var model in runState.IterateHookListeners(combatState))
        {
            if(model is ILibraryAbstractModel libraryAbstractModel)
            {
                choiceContext.PushModel(model);
                await libraryAbstractModel.AfterChaoDamageReceived(choiceContext, target, result, props, dealer, cardSource, type);
                choiceContext.PopModel(model);
                model.InvokeExecutionFinished();
            }
        }
        foreach (var model in runState.IterateHookListeners(combatState))
        {
            if(model is ILibraryAbstractModel libraryAbstractModel)
            {
                choiceContext.PushModel(model);
                await libraryAbstractModel.AfterChaoDamageReceivedLate(choiceContext, target, result, props, dealer, cardSource, type);
                choiceContext.PopModel(model);
                model.InvokeExecutionFinished();
            }
        }
    }
    public static async Task AfterCurrentChaoValueChanged(IRunState runState, ICombatState combatState, Creature target, decimal amount, LibraryDamageType type)
    {
        foreach (var model in runState.IterateHookListeners(combatState))
        {
            if(model is ILibraryAbstractModel libraryAbstractModel)
            {
                await libraryAbstractModel.AfterCurrentChaoValueChanged(target, amount, type);
                model.InvokeExecutionFinished();
            }
        }
    }
    public static async Task AfterCurrentHpChanged(IRunState runState, ICombatState combatState, Creature creature, decimal delta,LibraryDamageType type)
    {
        foreach (var model in runState.IterateHookListeners(combatState))
        {
            await model.AfterCurrentHpChanged(creature, delta);
            if(model is ILibraryAbstractModel libraryAbstractModel)
            {
                await libraryAbstractModel.AfterCurrentHpChanged(creature, delta, type);
            }
            model.InvokeExecutionFinished();
        }
    }
    public static async Task AfterDamageGiven(PlayerChoiceContext choiceContext, ICombatState combatState, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource,LibraryDamageType type)
    {
        LibrarySpeedDiceService.RecordDamageGiven(dealer, results, target);
        foreach (var model in combatState.IterateHookListeners())
        {
            choiceContext.PushModel(model);
            await model.AfterDamageGiven(choiceContext, dealer, results, props, target, cardSource);
            if(model is ILibraryAbstractModel libraryAbstractModel)
            {
                await libraryAbstractModel.AfterDamageGiven(choiceContext, dealer, results, props, target, cardSource, type);
            }
            choiceContext.PopModel(model);
            model.InvokeExecutionFinished();
        }
    }
    public static async Task AfterDamageReceived(PlayerChoiceContext choiceContext, IRunState runState, ICombatState combatState, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource,LibraryDamageType type) 
    {
        LibrarySpeedDiceService.RecordDamageReceived(target, result);
        foreach (var model in runState.IterateHookListeners(combatState))
        {
            choiceContext.PushModel(model);
            await model.AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource);
            if(model is ILibraryAbstractModel libraryAbstractModel)
            {
                await libraryAbstractModel.AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource, type);
            }
            choiceContext.PopModel(model);
            model.InvokeExecutionFinished();
        }
        foreach (var model in runState.IterateHookListeners(combatState))
        {
            choiceContext.PushModel(model);
            await model.AfterDamageReceivedLate(choiceContext, target, result, props, dealer, cardSource);
            if(model is ILibraryAbstractModel libraryAbstractModel)
            {
                await libraryAbstractModel.AfterDamageReceivedLate(choiceContext, target, result, props, dealer, cardSource, type);
            }
            choiceContext.PopModel(model);
            model.InvokeExecutionFinished();
        }
    }
    public static async Task BeforeDiceEffect(ICombatState combatState, PlayerChoiceContext choiceContext,  IEnumerable<Creature>? targets, CardModel cardSource,LibraryDice dice){
        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is ILibraryAbstractModel libraryModel)
            {
                await libraryModel.BeforeDiceEffect(choiceContext,targets, cardSource,dice);
                model.InvokeExecutionFinished();
            }
        }
    }
    public static async Task AfterDiceEffect(ICombatState combatState, PlayerChoiceContext choiceContext,  IEnumerable<Creature>? targets, CardModel cardSource,LibraryDice dice){
        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is ILibraryAbstractModel libraryModel)
            {
                await libraryModel.AfterDiceEffect(choiceContext,targets, cardSource,dice);
                model.InvokeExecutionFinished();
            }
        }
    }
    public static async Task AfterPowerEffect(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is ILibraryAbstractModel libraryModel)
            {
                await libraryModel.AfterPowerEffect(choiceContext, power, dealer, cardSource);
                model.InvokeExecutionFinished();
            }
        }
    }
    public static async Task AfterModifyingDamageAmount(IRunState runState, ICombatState combatState, CardModel? cardSource, IEnumerable<AbstractModel> modifiers,LibraryDamageType type)
    {
        foreach (var modifier in runState.IterateHookListeners(combatState))
        {   
            if (modifiers.Contains(modifier))
            {
                if(modifier is ILibraryAbstractModel libraryAbstractModel)
                {
                    await libraryAbstractModel.AfterModifyingDamageAmount(cardSource,type);
                }
                await modifier.AfterModifyingDamageAmount(cardSource);
                modifier.InvokeExecutionFinished();
            }
        }
    }

    public static async Task AfterModifyingHpLostAfterOsty(IRunState runState, ICombatState combatState, IEnumerable<AbstractModel> modifiers,LibraryDamageType type)
    {
        foreach (var modifier in runState.IterateHookListeners(combatState))
        {
            if (modifiers.Contains(modifier))
            {
                if(modifier is ILibraryAbstractModel libraryAbstractModel)
                {
                    await libraryAbstractModel.AfterModifyingHpLostAfterOsty(type);
                }
                await modifier.AfterModifyingHpLostAfterOsty();
                modifier.InvokeExecutionFinished();
            }
        }
    }
    public static async Task AfterModifyingHpLostBeforeOsty(IRunState runState, ICombatState combatState, IEnumerable<AbstractModel> modifiers,LibraryDamageType type)
    {
        foreach (var modifier in runState.IterateHookListeners(combatState))
        {
            if (modifiers.Contains(modifier))
            {
                if(modifier is ILibraryAbstractModel libraryAbstractModel)
                {
                    await libraryAbstractModel.AfterModifyingHpLostBeforeOsty(type);
                }
                await modifier.AfterModifyingHpLostBeforeOsty();
                modifier.InvokeExecutionFinished();
            }
        }
    }
    public static async Task AfterModifyingChaoAmount(IRunState runState, ICombatState combatState, CardModel? cardSource, IEnumerable<AbstractModel> modifiers,LibraryDamageType type)
    {
        foreach (var modifier in runState.IterateHookListeners(combatState))
        {   
            if (modifiers.Contains(modifier))
            {
                if(modifier is ILibraryAbstractModel libraryAbstractModel)
                {
                    await libraryAbstractModel.AfterModifyingChaoDamageAmount(cardSource,type);
                    modifier.InvokeExecutionFinished();
                }
            }
        }
    }
    public static async Task AfterPowerReduce(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is ILibraryAbstractModel libraryModel)    
            {
                await libraryModel.AfterPowerReduce(choiceContext, power, dealer, cardSource);
                model.InvokeExecutionFinished();
            }
        }
    }
    public static async Task AfterSetPowerMode(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode)
    {
        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is ILibraryAbstractModel libraryModel)    
            {
                await libraryModel.AfterSetPowerMode(choiceContext, power, dealer, cardSource, mode);
            }
        }
    }

    public static async Task BeforeStun(ICombatState? combatState, Creature creature)
    {
        if (combatState == null)
        {
            return;
        }

        foreach (var model in combatState.IterateHookListeners())
        {
            if(model is ILibraryAbstractModel libraryModel)    
            {
                await libraryModel.BeforeStun(creature);
                model.InvokeExecutionFinished();
            }
        }
    }
    public static async Task AfterStun( ICombatState? combatState, Creature creature)
    {
        if (combatState == null)
        {
            return;
        }

        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is ILibraryAbstractModel libraryModel)    
            {
                await libraryModel.AfterStun(creature);
                model.InvokeExecutionFinished();
            }
        }
    }
    public static async Task BeforeAttack(ICombatState combatState, LibraryAttackCommand command)
    {
        foreach (var model in combatState.IterateHookListeners())
        {
            await model.BeforeAttack(command.ToAttackCommand);
            if (model is ILibraryAbstractModel libraryModel)    
            {
                await libraryModel.BeforeAttack(command);
            }
                model.InvokeExecutionFinished();
        }
    }
    public static async Task BeforeDamageReceived(PlayerChoiceContext choiceContext, IRunState runState, ICombatState combatState, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource,LibraryDamageType type)
    {
        foreach (var model in runState.IterateHookListeners(combatState))
        {
            var pushedModel = false;
            var hookPhase = "AbstractModel.BeforeDamageReceived";
            try
            {
                choiceContext.PushModel(model);
                pushedModel = true;
                await model.BeforeDamageReceived(choiceContext, target, amount, props, dealer, cardSource);
                if (model is ILibraryAbstractModel libraryModel)
                {
                    hookPhase = "ILibraryAbstractModel.BeforeDamageReceived";
                    await libraryModel.BeforeDamageReceived(choiceContext, target, amount, props, dealer, cardSource,type);
                }
            }
            catch (Exception exception)
            {
                LogBeforeDamageReceivedListenerFailure(exception, model, hookPhase, target, amount, props, dealer, cardSource, type);
                throw;
            }
            finally
            {
                if (pushedModel)
                {
                    choiceContext.PopModel(model);
                }
            }
            model.InvokeExecutionFinished();
        }
    }

    private static void LogBeforeDamageReceivedListenerFailure(Exception exception, AbstractModel model, string hookPhase, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        try
        {
            Log.Error(
                "[LibraryOfRuinaLib] BeforeDamageReceived listener failed. " +
                $"hook={hookPhase}, modelId={GetModelId(model)}, modelType={model.GetType().FullName}, " +
                $"target={GetCreatureId(target)}, dealer={GetCreatureId(dealer)}, card={GetCardId(cardSource)}, " +
                $"amount={amount}, damageType={type}, props={props}, exception={exception}");
        }
        catch
        {
            
        }
    }

    private static void LogAfterBlockBrokenListenerFailure(Exception exception, AbstractModel model, string hookPhase, Creature target, LibraryDamageType type)
    {
        try
        {
            Log.Error(
                "[LibraryOfRuinaLib] AfterBlockBroken listener failed. " +
                $"hook={hookPhase}, modelId={GetModelId(model)}, modelType={model.GetType().FullName}, " +
                $"target={GetCreatureId(target)}, damageType={type}, exception={exception}");
        }
        catch
        {
            
        }
    }

    private static string GetModelId(AbstractModel model)
    {
        try
        {
            return model.Id.ToString();
        }
        catch
        {
            return "unknown-model-id";
        }
    }

    private static string GetCardId(CardModel? card)
    {
        if (card == null)
            return "null";

        try
        {
            return card.Id.Entry;
        }
        catch
        {
            return card.GetType().FullName ?? card.GetType().Name;
        }
    }

    private static string GetCreatureId(Creature? creature)
    {
        if (creature == null)
            return "null";

        try
        {
            if (creature.IsMonster)
                return creature.Monster?.Id.Entry ?? "unknown-monster";

            if (creature.IsPlayer)
                return creature.Player?.Character.Id.Entry ?? "unknown-player";
        }
        catch
        {
            
        }

        return creature.GetType().FullName ?? creature.GetType().Name;
    }
    public static async Task BeforeDiceRoll(ICombatState combatState,PlayerChoiceContext choiceContext,  IEnumerable<Creature>? targets, LibraryDice dice)
    {
        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is ILibraryAbstractModel libraryModel)    
            {
                await libraryModel.BeforeDiceRoll(choiceContext,targets,dice);
                model.InvokeExecutionFinished();
            }
        }
    }
    public static async Task AfterDiceRoll(ICombatState combatState,PlayerChoiceContext choiceContext,  IEnumerable<Creature>? targets, LibraryDice dice)
    {
        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is ILibraryAbstractModel libraryModel)    
            {
                await libraryModel.AfterDiceRoll(choiceContext,targets,dice);
                model.InvokeExecutionFinished();
            }
        }
    }
    public static async Task BeforePowerEffect(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is ILibraryAbstractModel libraryModel)    
            {
                await libraryModel.BeforePowerEffect(choiceContext, power, dealer, cardSource);
                model.InvokeExecutionFinished();
            }
        }
    }
    public static async Task BeforePowerReduce(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is ILibraryAbstractModel libraryModel)    
            {
                await libraryModel.BeforePowerReduce(choiceContext, power, dealer, cardSource);
                model.InvokeExecutionFinished();
            }
        }
    }
    public static async Task BeforeSetPowerMode(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode)
    {
        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is ILibraryAbstractModel libraryModel)    
            {
                await libraryModel.BeforeSetPowerMode(choiceContext, power, dealer, cardSource, mode);
                model.InvokeExecutionFinished();
            }
        }
    }
    public static async Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, IRunState runState, ICombatState combatState, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource,LibraryDamageType type)
    {
        foreach (var model in runState.IterateHookListeners(combatState))
        {
            if (model is ILibraryAbstractModel libraryModel)    
            {
                choiceContext.PushModel(model);
                await libraryModel.BeforeChaoDamageReceived(choiceContext, target, amount, props, dealer, cardSource,type);
                choiceContext.PopModel(model);
                model.InvokeExecutionFinished();
            }
        }
    }    
    public static decimal ModifyAttackHitCount(ICombatState combatState, LibraryAttackCommand attackCommand, int originalHitCount)
    {
        var num = originalHitCount;
        foreach (var item in combatState.IterateHookListeners())
        {
            num = item.ModifyAttackHitCount(attackCommand.ToAttackCommand, num);
            if(item is ILibraryAbstractModel libraryAbstractModel)    
                num = libraryAbstractModel.ModifyAttackHitCount(attackCommand, num);
        }
        return num;
    }
    public static decimal ModifyEffectiveAmount( ICombatState combatState, LibraryBasePowerModel power, Creature? dealer, decimal amount, CardModel? cardSource, out IEnumerable<AbstractModel> modifiers)
    {
        var num = amount;
        List<AbstractModel> list = new();
        foreach (var item in combatState.IterateHookListeners())
        {
            if(item is ILibraryAbstractModel lm){
                var num2 = lm.ModifyEffectiveAmountAdditive(power, num, dealer,cardSource);
                num += num2;
                if (num2 != 0m)
                {
                    list.Add(item);
                }
            }
        }
        foreach (var item in combatState.IterateHookListeners())
        {
            if(item is ILibraryAbstractModel lm){
                var num3 = lm.ModifyEffectiveAmountMultiplicative(power, num, dealer,cardSource);
                num *= num3;
                if (num3 != 1m)
                {
                    list.Add(item);
                }
            }
        }
        modifiers = list;
        return num;
    }
    public static async Task AfterModifyingEffectiveAmount(ICombatState combatState, LibraryBasePowerModel power, CardModel? cardSource, IEnumerable<AbstractModel> modifiers)
    {
        foreach (var modifier in combatState.IterateHookListeners())
        {   
            if (modifiers.Contains(modifier))
            {
                if(modifier is ILibraryAbstractModel libraryAbstractModel)
                {
                    await libraryAbstractModel.AfterModifyingEffectiveAmount(cardSource,power);
                }
                modifier.InvokeExecutionFinished();
            }
        }
    }
    public static decimal ModifyDamage(IRunState runState, ICombatState combatState, Creature? target, Creature? dealer, decimal damage, ValueProp props, CardModel? cardSource, CardPlay? cardPlay, ModifyDamageHookType modifyDamageHookType, CardPreviewMode previewMode, out IEnumerable<AbstractModel> modifiers,LibraryDamageType type)
    {
        var modifiers2 = new List<AbstractModel>();
        var num = damage;
        if (cardSource != null && cardSource.Enchantment != null)
        {
            if (modifyDamageHookType.HasFlag(ModifyDamageHookType.Additive))
            {
                num += cardSource.Enchantment.EnchantDamageAdditive(num, props);
            }
            if (modifyDamageHookType.HasFlag(ModifyDamageHookType.Multiplicative))
            {
                num *= cardSource.Enchantment.EnchantDamageMultiplicative(num, props);
            }
        }
        var flag = target == null && previewMode == CardPreviewMode.MultiCreatureTargeting;
        var flag2 = flag;
        bool flag3;
        if (flag2)
        {
            if (cardSource != null)
            {
                var targetType = cardSource.TargetType;
                if ((uint)(targetType - 3) <= 1u)
                {
                    var pile = cardSource.Pile;
                    if (pile != null)
                    {
                        var pileType = pile.Type;
                        if (pileType == PileType.Hand || pileType == PileType.Play)
                        {
                            flag3 = true;
                            goto IL_00bb;
                        }
                    }
                }
            }
            flag3 = false;
            goto IL_00bb;
        }
        goto IL_00bf;
        IL_00bf:
        var flag4 = flag2;
        var flag5 = false;
        if (flag4)
        {
            var flag6 = true;
            decimal? num2 = null;
            foreach (var item in combatState?.HittableEnemies ?? Array.Empty<Creature>())
            {
                List<AbstractModel> modifiers3;
                var num3 = ModifyDamageInternal(runState, combatState, item, dealer, num, props, cardSource, cardPlay, modifyDamageHookType, out modifiers3,type);
                if (!num2.HasValue)
                {
                    num2 = num3;
                }
                else if ((int)num3 != (int)num2.Value)
                {
                    flag6 = false;
                    break;
                }
                modifiers2.AddRange(modifiers3);
            }
            if (num2.HasValue && flag6)
            {
                flag5 = true;
                num = num2.Value;
                modifiers2 = modifiers2.Distinct().ToList();
            }
            else
            {
                modifiers2.Clear();
            }
        }
        if (!flag4 || !flag5)
        {
            num = ModifyDamageInternal(runState, combatState, target, dealer, num, props, cardSource, cardPlay, modifyDamageHookType, out modifiers2,type);
        }
        modifiers = modifiers2;
        return Math.Max(0m, num);
        IL_00bb:
        flag2 = flag3;
        goto IL_00bf;
    }
    private static decimal ModifyDamageInternal(IRunState runState, ICombatState? combatState, Creature? target, Creature? dealer, decimal damage, ValueProp props, CardModel? cardSource, CardPlay? cardPlay, ModifyDamageHookType modifyDamageHookType, out List<AbstractModel> modifiers,LibraryDamageType type)
    {
        var num = damage;
        var list = new List<AbstractModel>();
        if (modifyDamageHookType.HasFlag(ModifyDamageHookType.Additive))
        {
            foreach (var item in runState.IterateHookListeners(combatState))
            {
                var num2 = item.ModifyDamageAdditive(target, num, props, dealer, cardSource, cardPlay);
                if(item is ILibraryAbstractModel libraryAbstractModel)    
                    num2 += libraryAbstractModel.ModifyDamageAdditive(target, num, props, dealer, cardSource, cardPlay,type);
                num += num2;
                if (num2 != 0m)
                {
                    list.Add(item);
                }
            }
        }
        if (modifyDamageHookType.HasFlag(ModifyDamageHookType.Multiplicative))
        {
            foreach (var item2 in runState.IterateHookListeners(combatState))
            {
                var num3 = item2.ModifyDamageMultiplicative(target, num, props, dealer, cardSource, cardPlay);
                if(item2 is ILibraryAbstractModel libraryAbstractModel)    
                    num3 *= libraryAbstractModel.ModifyDamageMultiplicative(target, num, props, dealer, cardSource, cardPlay,type);
                num *= num3;
                if (num3 != 1m)
                {
                    list.Add(item2);
                }
            }
        }
        var num4 = decimal.MaxValue;
        foreach (var item3 in runState.IterateHookListeners(combatState))
        {
            var num5 = item3.ModifyDamageCap(target, props, dealer, cardSource, cardPlay);
            if(item3 is ILibraryAbstractModel libraryAbstractModel)    
                num5 = Math.Min(num5, libraryAbstractModel.ModifyDamageCap(target, props, dealer, cardSource, cardPlay,type));
            if (num5 < num4)
            {
                num4 = num5;
                if (num > num5)
                {
                    num = num5;
                    list.Add(item3);
                }
            }
        }
        modifiers = list;
        return num;
    }
    public static decimal ModifyHpLostAfterOsty(IRunState runState, ICombatState combatState, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, out IEnumerable<AbstractModel> modifiers,LibraryDamageType type)
    {
        var num = amount;
        var list = new List<AbstractModel>();
        foreach (var item in runState.IterateHookListeners(combatState))
        {
            var d = num;
            num = item.ModifyHpLostAfterOsty(target, num, props, dealer, cardSource);
            if(item is ILibraryAbstractModel libraryAbstractModel)    
                num = libraryAbstractModel.ModifyHpLostAfterOsty(target, num, props, dealer, cardSource,type);
            if (decimal.Truncate(d) != decimal.Truncate(num))
            {
                list.Add(item);
            }
        }
        foreach (var item2 in runState.IterateHookListeners(combatState))
        {
            var d2 = num;
            num = item2.ModifyHpLostAfterOstyLate(target, num, props, dealer, cardSource);
            if(item2 is ILibraryAbstractModel libraryAbstractModel)    
                num = libraryAbstractModel.ModifyHpLostAfterOstyLate(target, num, props, dealer, cardSource,type);
            if (decimal.Truncate(d2) != decimal.Truncate(num))
            {
                list.Add(item2);
            }
        }
        modifiers = list;
        return num;
    }
    public static decimal ModifyHpLostBeforeOsty(IRunState runState, ICombatState combatState, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, out IEnumerable<AbstractModel> modifiers,LibraryDamageType type)
    {
        var num = amount;
		var d = num;
        var list = new List<AbstractModel>();
        foreach (var item in runState.IterateHookListeners(combatState))
        {
            num = item.ModifyHpLostBeforeOsty(target, num, props, dealer, cardSource);
            if(item is ILibraryAbstractModel libraryAbstractModel)    
                num = libraryAbstractModel.ModifyHpLostBeforeOsty(target, num, props, dealer, cardSource,type);
            if (decimal.Truncate(d) != decimal.Truncate(num))
            {
                list.Add(item);
            }
        }
        foreach (var item2 in runState.IterateHookListeners(combatState))
        {
            var d2 = num;
            num = item2.ModifyHpLostBeforeOstyLate(target, num, props, dealer, cardSource);
            if(item2 is ILibraryAbstractModel libraryAbstractModel)     
                num = libraryAbstractModel.ModifyHpLostBeforeOstyLate(target, num, props, dealer, cardSource,type);
            if (decimal.Truncate(d2) != decimal.Truncate(num))
            {
                list.Add(item2);
            }
        }
        modifiers = list;
        return num;
    }
    public static decimal ModifyChaoDamage(IRunState runState, ICombatState combatState, Creature target, Creature? dealer, decimal chaoDamage, ValueProp props, CardModel? cardSource, CardPlay? cardPlay, ModifyChaoDamageHookType modifyChaoDamageHookType, CardPreviewMode previewMode, out IEnumerable<AbstractModel> modifiers,LibraryDamageType type)
    {
        if(target is not LibraryCreature libraryCreature)
        {
            modifiers = Array.Empty<AbstractModel>();
            return 0m;
        }
        var modifiers2 = new List<AbstractModel>();
        var num = chaoDamage;
        if (cardSource != null && cardSource.Enchantment != null && cardSource.Enchantment is LibraryEnchantmentModel libraryEnchantment)
        {
            if (modifyChaoDamageHookType.HasFlag(ModifyChaoDamageHookType.Additive))
            {
                num += libraryEnchantment.EnchantChaoDamageAdditive(num, props);
            }
            if (modifyChaoDamageHookType.HasFlag(ModifyChaoDamageHookType.Multiplicative))
            {
                num *= libraryEnchantment.EnchantChaoDamageMultiplicative(num, props);
            }
        }
        var flag = target == null && previewMode == CardPreviewMode.MultiCreatureTargeting;
        var flag2 = flag;
        bool flag3;
        if (flag2)
        {
            if (cardSource != null)
            {
                var targetType = cardSource.TargetType;
                if ((uint)(targetType - 3) <= 1u)
                {
                    var pile = cardSource.Pile;
                    if (pile != null)
                    {
                        var pileType = pile.Type;
                        if (pileType == PileType.Hand || pileType == PileType.Play)
                        {
                            flag3 = true;
                            goto IL_00bb;
                        }
                    }
                }
            }
            flag3 = false;
            goto IL_00bb;
        }
        goto IL_00bf;
        IL_00bf:
        var flag4 = flag2;
        var flag5 = false;
        if (flag4)
        {
            var flag6 = true;
            decimal? num2 = null;
            foreach (var item in combatState?.HittableEnemies ?? Array.Empty<Creature>())
            {
                List<AbstractModel> modifiers3;
                var num3 = ModifyChaoDamageInternal(runState, combatState, item, dealer, num, props, cardSource, modifyChaoDamageHookType, out modifiers3,type);
                if (!num2.HasValue)
                {
                    num2 = num3;
                }
                else if ((int)num3 != (int)num2.Value)
                {
                    flag6 = false;
                    break;
                }
                modifiers2.AddRange(modifiers3);
            }
            if (num2.HasValue && flag6)
            {
                flag5 = true;
                num = num2.Value;
                modifiers2 = modifiers2.Distinct().ToList();
            }
            else
            {
                modifiers2.Clear();
            }
        }
        if (!flag4 || !flag5)
        {
            num = ModifyChaoDamageInternal(runState, combatState, target, dealer, num, props, cardSource, cardPlay, modifyChaoDamageHookType, out modifiers2,type);
        }
        modifiers = modifiers2;
        return Math.Max(0m, num);
        IL_00bb:
        flag2 = flag3;
        goto IL_00bf;
    }    
    private static decimal ModifyChaoDamageInternal(IRunState runState, ICombatState? combatState, Creature? target, Creature? dealer, decimal damage, ValueProp props, CardModel? cardSource, CardPlay? cardPlay, ModifyChaoDamageHookType modifyChaoDamageHookType, out List<AbstractModel> modifiers,LibraryDamageType type)
    {
        var num = damage;
        var list = new List<AbstractModel>();
        if (modifyChaoDamageHookType.HasFlag(ModifyChaoDamageHookType.Additive))
        {
            foreach (var item in runState.IterateHookListeners(combatState))
            {
                if(item is not ILibraryAbstractModel libraryAbstractModel)
                    continue;
                var num2 = libraryAbstractModel.ModifyChaoDamageAdditive(target, num, props, dealer, cardSource,type);
                num += num2;
                if (num2 != 0m)
                {
                    list.Add(item);
                }
            }
        }
        if (modifyChaoDamageHookType.HasFlag(ModifyChaoDamageHookType.Multiplicative))
        {
            foreach (var item2 in runState.IterateHookListeners(combatState))
            {
                if(item2 is not ILibraryAbstractModel libraryAbstractModel)
                    continue;
                var num3 = libraryAbstractModel.ModifyChaoDamageMultiplicative(target, num, props, dealer, cardSource,type);
                num *= num3;
                if (num3 != 1m)
                {
                    list.Add(item2);
                }
            }
        }
        var num4 = decimal.MaxValue;
        foreach (var item3 in runState.IterateHookListeners(combatState))
        {
            if(item3 is not ILibraryAbstractModel libraryAbstractModel)
                continue;
            var num5 = libraryAbstractModel.ModifyChaoDamageCap(target, props, dealer, cardSource,type);
            if (num5 < num4)
            {
                num4 = num5;
                if (num > num5)
                {
                    num = num5;
                    list.Add(item3);
                }
            }
        }
        modifiers = list;
        return num;
    }
    public static Creature ModifyUnblockedDamageTarget(ICombatState combatState, Creature originalTarget, decimal amount, ValueProp props, Creature? dealer,LibraryDamageType type)
    {
        var creature = originalTarget;
        foreach (var item in combatState.IterateHookListeners())
        {
            creature = item.ModifyUnblockedDamageTarget(creature, amount, props, dealer);
            if(item is ILibraryAbstractModel libraryAbstractModel)
                creature = libraryAbstractModel.ModifyUnblockedDamageTarget(creature, amount, props, dealer,type);
        }
        return creature;
    }
    public static bool ShouldReroll(ICombatState combatState,IEnumerable<Creature>? targets,LibraryDice dice,out ILibraryAbstractModel? trigger)
    {
        foreach (var item in combatState.IterateHookListeners())
        {
            if (item is ILibraryAbstractModel libraryAbstractModel 
                && libraryAbstractModel.ShouldReroll(targets,dice)){
                trigger = libraryAbstractModel;
                return true;
            }
        }
        trigger = null;
        return false;
    }
    public static bool ShouldReuse(ICombatState combatState,IEnumerable<Creature>? targets,LibraryDice dice,out ILibraryAbstractModel? trigger)
    {
        foreach (var item in combatState.IterateHookListeners())
        {
            if (item is ILibraryAbstractModel libraryAbstractModel 
                && libraryAbstractModel.ShouldReuse(targets,dice)){
                trigger = libraryAbstractModel;
                return true;
            }
        }
        trigger = null;
        return false;
    }
    public static bool TryDiceEffect(ICombatState combatState,PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource,LibraryDice dice)
    {
        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is ILibraryAbstractModel libraryModel)
            {
                if (!libraryModel.TryDiceEffect(choiceContext, targets, cardSource,dice))
                {
                    return false;
                }
            }
        }
        return true;
    }
    public static bool TryPowerEffect(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is ILibraryAbstractModel libraryModel)
            {
                if (!libraryModel.TryPowerEffect(choiceContext, power, dealer, cardSource))
                {
                    return false;
                }
            }
        }
        return true;
    }
    public static bool TryPowerReduce(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is ILibraryAbstractModel libraryModel)
            {
                if (!libraryModel.TryPowerReduce(choiceContext, power, dealer, cardSource))
                {
                    return false;
                }
            }
        }
        return true;
    }
}
