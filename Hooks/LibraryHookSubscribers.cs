using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

public static class LibraryHookSubscribers
{
    private static ConditionalWeakTable<CombatState,List<AbstractModel>> _combatStateSubscribers = []; 
    private static ConditionalWeakTable<RunState,List<AbstractModel>> _runStateSubscribers = []; 
    /// <summary>
    ///     整场战斗生效
    /// </summary>
    public static void SubscribeForCombatStateHooks(AbstractModel model,CombatState combatState)
    {
        if (model == null || combatState == null) return;
        if (!_combatStateSubscribers.TryGetValue(combatState, out var modifiers))
        {
            modifiers = new List<AbstractModel>();
            _combatStateSubscribers.Add(combatState, modifiers);
        }
        if (!modifiers.Contains(model))
            modifiers.Add(model);
    }
    /// <summary>
    ///     整场游戏生效
    /// </summary>
    public static void SubscribeForRunStateHooksPersistent(AbstractModel model,RunState runState)
    {
        if (model == null || runState == null) return;
        if (!_runStateSubscribers.TryGetValue(runState, out var modifiers))
        {
            modifiers = new List<AbstractModel>();
            _runStateSubscribers.Add(runState, modifiers);
        }
        if (!modifiers.Contains(model))
            modifiers.Add(model);
    }
    /// <summary>
    ///     取消订阅CombatState的钩子。
    /// </summary>
    public static void UnsubscribeForCombatStateHooks(AbstractModel model,CombatState combatState)
    {
        if (model == null || combatState == null) return;
        if (!_combatStateSubscribers.TryGetValue(combatState,out List<AbstractModel> modifiers)) return;
        modifiers.Remove(model);
    }    
    public static void UnsubscribeForRunStateHooks(AbstractModel model,RunState runState)
    {
        if (model == null || runState == null) return;
        if (!_runStateSubscribers.TryGetValue(runState,out List<AbstractModel> modifiers)) return;
        modifiers.Remove(model);
    }

    public static IEnumerable<AbstractModel> IterateAllCombatStateSubscribers(CombatState combatState)
    {
        if (combatState == null) yield break;
        if (!_combatStateSubscribers.TryGetValue(combatState, out var modifiers)) yield break;
        foreach (AbstractModel model in modifiers.ToArray())
        {
            if (model != null && modifiers.Contains(model))
                yield return model;
        }
    }
    public static IEnumerable<AbstractModel> IterateAllRunStateSubscribers(RunState runState)
    {
        if (runState == null) yield break;
        if (!_runStateSubscribers.TryGetValue(runState, out var modifiers)) yield break;
        foreach (AbstractModel model in modifiers.ToArray())
        {
            if (model != null && modifiers.Contains(model))
                yield return model;
        }
    }
    public static void CleanCombatStateSubscribers() => _combatStateSubscribers.Clear();
    public static void CleanRunStateSubscribers() => _runStateSubscribers.Clear();

}
