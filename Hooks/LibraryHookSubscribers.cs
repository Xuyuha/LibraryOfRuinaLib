using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

public static class LibraryHookSubscribers
{
    private static List<AbstractModel> _combatStateSubscribers = []; 
    /// <summary>
    ///     战斗结束时,自动取消订阅
    /// </summary>
    public static void SubscribeForCombatStateHooks(AbstractModel model)
    {
        if (_combatStateSubscribers.Contains(model)) return;
        _combatStateSubscribers.Add(model);
        void action(CombatRoom _)
        {
            UnsubscribeForCombatStateHooks(model);
            CombatManager.Instance.CombatEnded -= action;
        }
        CombatManager.Instance.CombatEnded += action;
    }
    /// <summary>
    ///     该方法不会在战斗结束时自动取消订阅。
    /// </summary>
    public static void SubscribeForCombatStateHooksPersistent(AbstractModel model)
    {
        if (!_combatStateSubscribers.Contains(model))
            _combatStateSubscribers.Add(model);
    }
    /// <summary>
    ///     取消订阅CombatState的钩子。
    /// </summary>
    public static void UnsubscribeForCombatStateHooks(AbstractModel model)
    {
        _combatStateSubscribers.Remove(model);
    }

    public static IEnumerable<AbstractModel> IterateAllCombatStateSubscribers()
    {
        // 钩子可以施加、移除能力或切换模式；遍历时避免集合被修改，并跳过已退订项。
        foreach (AbstractModel model in _combatStateSubscribers.ToArray())
        {
            if (_combatStateSubscribers.Contains(model))
            {
                yield return model;
            }
        }
    }
    public static void CleanCombatStateSubscribers()
    {
        _combatStateSubscribers.Clear();
    }

}
