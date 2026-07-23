#nullable enable
using System;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Runs;

namespace Library.Patches;

// 自己测试的时候发现开godmode有概率导致多人不同步（似乎原版的bug？）让ai写了一个小补丁，平时也用不到
[HarmonyPatch(typeof(GodModeConsoleCmd), "OnCombatSetUp")]
internal static class GodModeMultiplayerSyncPatch
{
    private static readonly MethodInfo? EnableGodModeMethod =
        AccessTools.Method(typeof(GodModeConsoleCmd), "EnableGodMode");

    [HarmonyPrefix]
    private static void Prefix(CombatState combatState, Player? ___godModePlayer)
    {
        if (___godModePlayer == null || !RunManager.Instance.IsInProgress)
            return;

        if (LocalContext.GetMe(combatState.RunState) == ___godModePlayer)
            return;

        try
        {
            if (EnableGodModeMethod?.Invoke(null, [___godModePlayer]) is Task enableTask)
                TaskHelper.RunSafely(enableTask);
        }
        catch (Exception e)
        {
            Log.Warn("[LibraryOfRuinaLib.Multiplayer] Failed to mirror godmode combat setup: " + e);
        }
    }
}
