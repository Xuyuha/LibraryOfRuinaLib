using LibraryLib.Combat;
using LibraryLib.Commands;
using LibraryLib.Entities.Creatures;
using LibraryLib.Localization.Dice;
using LibraryLib.Localization.LibraryDynamicVars;
using LibraryLib.Models;
using LibraryLib.Powers.LibraryPowerMode;
using LibraryLib.SpeedDice;
using LibraryLib.Utils;
using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Hooks;

/// <summary>
/// Dispatches Library hooks to every combat hook listener. Paired hooks call the vanilla
/// <see cref="AbstractModel"/> hook first and then its <see cref="ILibraryAbstractModel"/> overload,
/// so Library commands keep vanilla listeners informed. Value-modifying hooks live in
/// LibraryHooks.Modifiers.cs.
/// </summary>
public static partial class LibraryHooks
{
    public static bool HasIncomingDamageInterceptor(
        IRunState runState,
        ICombatState combatState)
    {
        return !LibraryIncomingDamageInterception.IsSuppressed
            && runState.IterateHookListeners(combatState)
                .Any(static model => model is ILibraryIncomingDamageInterceptor);
    }

    public static async Task<LibraryIncomingDamageResolution> InterceptIncomingDamage(
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
        decimal originalDamage = Math.Max(0m, amount);
        if (originalDamage <= 0m || LibraryIncomingDamageInterception.IsSuppressed)
            return LibraryIncomingDamageResolution.PassThrough(originalDamage);

        decimal remainingDamage = originalDamage;
        decimal interceptedDamage = 0m;
        foreach (AbstractModel model in runState.IterateHookListeners(combatState))
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (model is not ILibraryIncomingDamageInterceptor interceptor)
                continue;

            await RunListener(model, choiceContext, async () =>
            {
                LibraryIncomingDamageResolution candidate = await interceptor.InterceptIncomingDamageAsync(
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
                decimal normalizedRemaining = Math.Clamp(candidate.RemainingDamage, 0m, remainingDamage);
                interceptedDamage += remainingDamage - normalizedRemaining;
                remainingDamage = normalizedRemaining;
            });
            if (remainingDamage <= 0m)
                break;
        }

        return new LibraryIncomingDamageResolution(
            remainingDamage,
            interceptedDamage,
            interceptedDamage > 0m && remainingDamage <= 0m);
    }

    public static Task BeforeSetPhysicalResistance(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) =>
        ForEachLibraryListener(combatState.IterateHookListeners(), m => m.BeforeSetPhysicalResistance(choiceContext, target, dealer, type, resistanceValue));

    public static bool TrySetPhysicalResistance(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) =>
        AllLibraryListenersAllow(combatState, m => m.TrySetPhysicalResistance(choiceContext, target, dealer, type, resistanceValue));

    public static Task AfterSetPhysicalResistance(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) =>
        ForEachLibraryListener(combatState.IterateHookListeners(), m => m.AfterSetPhysicalResistance(choiceContext, target, dealer, type));

    public static Task BeforeSetChaoResistance(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) =>
        ForEachLibraryListener(combatState.IterateHookListeners(), m => m.BeforeSetChaoResistance(choiceContext, target, dealer, type, resistanceValue));

    public static bool TrySetChaoResistance(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) =>
        AllLibraryListenersAllow(combatState, m => m.TrySetChaoResistance(choiceContext, target, dealer, type, resistanceValue));

    public static Task AfterSetChaoResistance(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) =>
        ForEachLibraryListener(combatState.IterateHookListeners(), m => m.AfterSetChaoResistance(choiceContext, target, dealer, type));

    public static Task BeforeAttack(ICombatState combatState, LibraryAttackCommand command) =>
        ForEachListener(
            combatState.IterateHookListeners(),
            m => m.BeforeAttack(command.ToAttackCommand),
            m => m.BeforeAttack(command));

    public static Task AfterAttack(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryAttackCommand command) =>
        ForEachListener(
            combatState.IterateHookListeners(),
            m => m.AfterAttack(choiceContext, command.ToAttackCommand),
            m => m.AfterAttack(choiceContext, command));

    public static Task AfterBlockBroken(ICombatState combatState, PlayerChoiceContext choiceContext, Creature target, Creature? breaker, LibraryDamageType type) =>
        ForEachListener(
            combatState.IterateHookListeners(),
            m => m.AfterBlockBroken(choiceContext, target, breaker),
            m => m.AfterBlockBroken(target, type),
            failure: (model, phase, exception) => LogListenerFailure(
                nameof(AfterBlockBroken), model, phase, exception,
                $"target={LibraryDiagnostics.Describe(target)}, damageType={type}"));

    public static Task BeforeDamageReceived(PlayerChoiceContext choiceContext, IRunState runState, ICombatState combatState, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) =>
        ForEachListener(
            runState.IterateHookListeners(combatState),
            m => m.BeforeDamageReceived(choiceContext, target, amount, props, dealer, cardSource),
            m => m.BeforeDamageReceived(choiceContext, target, amount, props, dealer, cardSource, type),
            choiceContext,
            (model, phase, exception) => LogListenerFailure(
                nameof(BeforeDamageReceived), model, phase, exception,
                $"target={LibraryDiagnostics.Describe(target)}, dealer={LibraryDiagnostics.Describe(dealer)}, "
                + $"card={LibraryDiagnostics.Describe(cardSource)}, amount={amount}, damageType={type}, props={props}"));

    public static async Task AfterDamageGiven(PlayerChoiceContext choiceContext, ICombatState combatState, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type)
    {
        LibrarySpeedDiceService.RecordDamageGiven(dealer, results, target);
        await ForEachListener(
            combatState.IterateHookListeners(),
            m => m.AfterDamageGiven(choiceContext, dealer, results, props, target, cardSource),
            m => m.AfterDamageGiven(choiceContext, dealer, results, props, target, cardSource, type),
            choiceContext);
    }

    public static async Task AfterDamageReceived(PlayerChoiceContext choiceContext, IRunState runState, ICombatState combatState, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        LibrarySpeedDiceService.RecordDamageReceived(target, result);
        await ForEachListener(
            runState.IterateHookListeners(combatState),
            m => m.AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource),
            m => m.AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource, type),
            choiceContext);
        await ForEachListener(
            runState.IterateHookListeners(combatState),
            m => m.AfterDamageReceivedLate(choiceContext, target, result, props, dealer, cardSource),
            m => m.AfterDamageReceivedLate(choiceContext, target, result, props, dealer, cardSource, type),
            choiceContext);
    }

    public static Task AfterCurrentHpChanged(IRunState runState, ICombatState combatState, Creature creature, decimal delta, LibraryDamageType type) =>
        ForEachListener(
            runState.IterateHookListeners(combatState),
            m => m.AfterCurrentHpChanged(creature, delta),
            m => m.AfterCurrentHpChanged(creature, delta, type));

    public static Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, IRunState runState, ICombatState combatState, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) =>
        ForEachLibraryListener(
            runState.IterateHookListeners(combatState),
            m => m.BeforeChaoDamageReceived(choiceContext, target, amount, props, dealer, cardSource, type),
            choiceContext);

    public static Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, ICombatState combatState, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type) =>
        ForEachLibraryListener(
            combatState.IterateHookListeners(),
            m => m.AfterChaoDamageGiven(choiceContext, dealer, results, props, target, cardSource, type),
            choiceContext);

    public static async Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, IRunState runState, ICombatState combatState, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type)
    {
        await ForEachLibraryListener(
            runState.IterateHookListeners(combatState),
            m => m.AfterChaoDamageReceived(choiceContext, target, result, props, dealer, cardSource, type),
            choiceContext);
        await ForEachLibraryListener(
            runState.IterateHookListeners(combatState),
            m => m.AfterChaoDamageReceivedLate(choiceContext, target, result, props, dealer, cardSource, type),
            choiceContext);
    }

    public static Task AfterCurrentChaoValueChanged(IRunState runState, ICombatState combatState, Creature target, decimal amount, LibraryDamageType type) =>
        ForEachLibraryListener(runState.IterateHookListeners(combatState), m => m.AfterCurrentChaoValueChanged(target, amount, type));

    public static Task AfterModifyingDamageAmount(IRunState runState, ICombatState combatState, CardModel? cardSource, IEnumerable<AbstractModel> modifiers, LibraryDamageType type) =>
        ForEachModifier(
            runState.IterateHookListeners(combatState),
            modifiers,
            m => m.AfterModifyingDamageAmount(cardSource, type),
            m => m.AfterModifyingDamageAmount(cardSource));

    public static Task AfterModifyingHpLostBeforeOsty(IRunState runState, ICombatState combatState, IEnumerable<AbstractModel> modifiers, LibraryDamageType type) =>
        ForEachModifier(
            runState.IterateHookListeners(combatState),
            modifiers,
            m => m.AfterModifyingHpLostBeforeOsty(type),
            m => m.AfterModifyingHpLostBeforeOsty());

    public static Task AfterModifyingHpLostAfterOsty(IRunState runState, ICombatState combatState, IEnumerable<AbstractModel> modifiers, LibraryDamageType type) =>
        ForEachModifier(
            runState.IterateHookListeners(combatState),
            modifiers,
            m => m.AfterModifyingHpLostAfterOsty(type),
            m => m.AfterModifyingHpLostAfterOsty());

    public static Task AfterModifyingChaoAmount(IRunState runState, ICombatState combatState, CardModel? cardSource, IEnumerable<AbstractModel> modifiers, LibraryDamageType type) =>
        ForEachModifier(runState.IterateHookListeners(combatState), modifiers, m => m.AfterModifyingChaoDamageAmount(cardSource, type));

    public static Task AfterModifyingEffectiveAmount(ICombatState combatState, LibraryBasePowerModel power, CardModel? cardSource, IEnumerable<AbstractModel> modifiers) =>
        ForEachModifier(combatState.IterateHookListeners(), modifiers, m => m.AfterModifyingEffectiveAmount(cardSource, power));

    public static bool TryPowerEffect(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) =>
        AllLibraryListenersAllow(combatState, m => m.TryPowerEffect(choiceContext, power, dealer, cardSource));

    public static Task BeforePowerEffect(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) =>
        ForEachLibraryListener(combatState.IterateHookListeners(), m => m.BeforePowerEffect(choiceContext, power, amount, dealer, cardSource));

    public static Task AfterPowerEffect(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) =>
        ForEachLibraryListener(combatState.IterateHookListeners(), m => m.AfterPowerEffect(choiceContext, power, amount, dealer, cardSource));

    public static bool TryPowerReduce(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) =>
        AllLibraryListenersAllow(combatState, m => m.TryPowerReduce(choiceContext, power, dealer, cardSource));

    public static Task BeforePowerReduce(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) =>
        ForEachLibraryListener(combatState.IterateHookListeners(), m => m.BeforePowerReduce(choiceContext, power, dealer, cardSource));

    public static Task AfterPowerReduce(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) =>
        ForEachLibraryListener(combatState.IterateHookListeners(), m => m.AfterPowerReduce(choiceContext, power, dealer, cardSource));

    public static Task BeforeSetPowerMode(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) =>
        ForEachLibraryListener(combatState.IterateHookListeners(), m => m.BeforeSetPowerMode(choiceContext, power, dealer, cardSource, mode));

    public static Task AfterSetPowerMode(ICombatState combatState, PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) =>
        ForEachLibraryListener(combatState.IterateHookListeners(), m => m.AfterSetPowerMode(choiceContext, power, dealer, cardSource, mode));

    public static Task BeforeStun(ICombatState? combatState, Creature creature) =>
        combatState == null
            ? Task.CompletedTask
            : ForEachLibraryListener(combatState.IterateHookListeners(), m => m.BeforeStun(creature));

    public static Task AfterStun(ICombatState? combatState, Creature creature) =>
        combatState == null
            ? Task.CompletedTask
            : ForEachLibraryListener(combatState.IterateHookListeners(), m => m.AfterStun(creature));

    public static Task BeforeDiceRoll(ICombatState combatState, PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice) =>
        ForEachLibraryListener(combatState.IterateHookListeners(), m => m.BeforeDiceRoll(choiceContext, targets, dice));

    public static Task AfterDiceRoll(ICombatState combatState, PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) =>
        ForEachLibraryListener(combatState.IterateHookListeners(), m => m.AfterDiceRoll(choiceContext, targets, dice, result));

    public static bool ShouldReroll(ICombatState combatState, IEnumerable<Creature>? targets, LibraryDice dice, out ILibraryAbstractModel? trigger, DiceRollResult result)
    {
        trigger = FirstLibraryListener(combatState, m => m.ShouldReroll(targets, dice, result));
        return trigger != null;
    }

    public static bool ShouldReuse(ICombatState combatState, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result, out ILibraryAbstractModel? trigger)
    {
        trigger = FirstLibraryListener(combatState, m => m.ShouldReuse(targets, dice, result));
        return trigger != null;
    }

    public static bool TryDiceEffect(ICombatState combatState, PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) =>
        AllLibraryListenersAllow(combatState, m => m.TryDiceEffect(choiceContext, targets, cardSource, dice, result));

    public static Task BeforeDiceEffect(ICombatState combatState, PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) =>
        ForEachLibraryListener(combatState.IterateHookListeners(), m => m.BeforeDiceEffect(choiceContext, targets, cardSource, dice, result));

    public static Task AfterDiceEffect(ICombatState combatState, PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) =>
        ForEachLibraryListener(combatState.IterateHookListeners(), m => m.AfterDiceEffect(choiceContext, targets, cardSource, dice, result));

    /// <summary>Runs a Library-only hook on every <see cref="ILibraryAbstractModel"/> listener.</summary>
    private static async Task ForEachLibraryListener(
        IEnumerable<AbstractModel> listeners,
        Func<ILibraryAbstractModel, Task> hook,
        PlayerChoiceContext? modelScope = null)
    {
        foreach (AbstractModel model in listeners)
        {
            if (model is ILibraryAbstractModel libraryModel)
                await RunListener(model, modelScope, () => hook(libraryModel));
        }
    }

    /// <summary>Runs the vanilla hook and then its Library overload on every listener.</summary>
    private static async Task ForEachListener(
        IEnumerable<AbstractModel> listeners,
        Func<AbstractModel, Task> vanillaHook,
        Func<ILibraryAbstractModel, Task> libraryHook,
        PlayerChoiceContext? modelScope = null,
        Action<AbstractModel, string, Exception>? failure = null)
    {
        foreach (AbstractModel model in listeners)
        {
            string phase = nameof(AbstractModel);
            try
            {
                await RunListener(model, modelScope, async () =>
                {
                    await vanillaHook(model);
                    if (model is ILibraryAbstractModel libraryModel)
                    {
                        phase = nameof(ILibraryAbstractModel);
                        await libraryHook(libraryModel);
                    }
                });
            }
            catch (Exception exception) when (failure != null)
            {
                failure(model, phase, exception);
                throw;
            }
        }
    }

    /// <summary>
    /// Notifies the listeners that modified a value. The Library overload runs before the vanilla one.
    /// </summary>
    private static async Task ForEachModifier(
        IEnumerable<AbstractModel> listeners,
        IEnumerable<AbstractModel> modifiers,
        Func<ILibraryAbstractModel, Task> libraryHook,
        Func<AbstractModel, Task>? vanillaHook = null)
    {
        foreach (AbstractModel model in listeners)
        {
            if (!modifiers.Contains(model))
                continue;
            if (model is ILibraryAbstractModel libraryModel)
                await libraryHook(libraryModel);
            if (vanillaHook != null)
                await vanillaHook(model);
            model.InvokeExecutionFinished();
        }
    }

    /// <summary>
    /// Runs one listener's hook with the model pushed onto <paramref name="modelScope"/>, then signals
    /// that the model finished executing (vanilla hand-selection relies on this signal).
    /// </summary>
    private static async Task RunListener(AbstractModel model, PlayerChoiceContext? modelScope, Func<Task> hook)
    {
        modelScope?.PushModel(model);
        try
        {
            await hook();
        }
        finally
        {
            modelScope?.PopModel(model);
        }
        model.InvokeExecutionFinished();
    }

    private static bool AllLibraryListenersAllow(ICombatState combatState, Func<ILibraryAbstractModel, bool> predicate)
    {
        foreach (AbstractModel model in combatState.IterateHookListeners())
        {
            if (model is ILibraryAbstractModel libraryModel && !predicate(libraryModel))
                return false;
        }
        return true;
    }

    private static ILibraryAbstractModel? FirstLibraryListener(ICombatState combatState, Func<ILibraryAbstractModel, bool> predicate)
    {
        foreach (AbstractModel model in combatState.IterateHookListeners())
        {
            if (model is ILibraryAbstractModel libraryModel && predicate(libraryModel))
                return libraryModel;
        }
        return null;
    }

    private static void LogListenerFailure(string hook, AbstractModel model, string phase, Exception exception, string details)
    {
        try
        {
            Log.Error(
                $"[LibraryOfRuinaLib] {hook} listener failed. hook={phase}.{hook}, "
                + $"modelId={LibraryDiagnostics.Describe(model)}, modelType={model.GetType().FullName}, "
                + $"{details}, exception={exception}");
        }
        catch
        {
            // Diagnostics must never mask the listener failure.
        }
    }
}
