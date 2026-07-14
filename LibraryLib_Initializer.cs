using Godot.Bridge;
using HarmonyLib;
using Library.Localization;
using Library.SpeedDice;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace Library;

[ModInitializer("Init")]
public class Entry
{
    public static void Init()
    {
        LibraryResourcePack.TryLoad();
        LibraryResistanceLocalization.Install();
        CombatManager.Instance.CombatEnded += _ => LibrarySpeedDiceService.ClearCombat();

        var harmony = new Harmony("LibraryOfRuinaLib");
        harmony.PatchAll();
        Log.Info("成功加载 LibraryOfRuinaLib基础库");
        ScriptManagerBridge.LookupScriptsInAssembly(typeof(Entry).Assembly);
    }
}
