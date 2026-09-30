using LibraryLib.Hooks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace LibraryLib.Models;

/// <summary>
///     类似烧伤、流血的 dot 能力模板：<see cref="TriggerEffect"/> / <see cref="TriggerReduce"/>
///     负责派发 Try/Before/After 钩子，子类只实现 <see cref="Effect"/> 与 <see cref="Reduce"/>。
/// </summary>
public abstract class LibraryBasePowerModel : LibraryMultipleModePowerModel
{
    protected static decimal CalculateStackDecayByThird(decimal amount) =>
        amount <= 0m ? 0m : Math.Max(1m, Math.Floor(amount / 3m));

    /// <summary>子类实现触发逻辑。</summary>
    protected virtual Task Effect(PlayerChoiceContext choiceContext, decimal effectiveAmount) => Task.CompletedTask;

    /// <summary>子类实现减少逻辑。</summary>
    protected virtual Task Reduce(PlayerChoiceContext choiceContext) => Task.CompletedTask;

    public async Task TriggerEffect(PlayerChoiceContext choiceContext, Creature? dealer, CardModel? cardSource, decimal? amount = null)
    {
        ICombatState? combatState = Owner?.CombatState;
        if (combatState == null || !LibraryHooks.TryPowerEffect(combatState, choiceContext, this, dealer, cardSource))
            return;
        decimal effectiveAmount = LibraryHooks.ModifyEffectiveAmount(combatState, this, dealer, amount ?? Amount, cardSource, out IEnumerable<AbstractModel> modifiers);
        await LibraryHooks.AfterModifyingEffectiveAmount(combatState, this, cardSource, modifiers);
        await LibraryHooks.BeforePowerEffect(combatState, choiceContext, this, effectiveAmount, dealer, cardSource);
        await Effect(choiceContext, effectiveAmount);
        await LibraryHooks.AfterPowerEffect(combatState, choiceContext, this, effectiveAmount, dealer, cardSource);
    }

    public async Task TriggerReduce(PlayerChoiceContext choiceContext, Creature? dealer, CardModel? cardSource)
    {
        ICombatState? combatState = Owner?.CombatState;
        if (combatState == null || !LibraryHooks.TryPowerReduce(combatState, choiceContext, this, dealer, cardSource))
            return;
        await LibraryHooks.BeforePowerReduce(combatState, choiceContext, this, dealer, cardSource);
        await Reduce(choiceContext);
        await LibraryHooks.AfterPowerReduce(combatState, choiceContext, this, dealer, cardSource);
    }
}
