using Godot.Bridge;
using HarmonyLib;
using LibraryLib.Localization;
using LibraryLib.Multiplayer;
using LibraryLib.SpeedDice;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace LibraryLib;

[ModInitializer("Init")]
public class Entry
{
    public static void Init()
    {
        LibraryManagedNetTypes.RegisterAssembly(typeof(Entry).Assembly);
        LibraryResourcePack.TryLoad();
        LibraryResistanceLocalization.Install();
        CombatManager.Instance.CombatEnded += _ => LibrarySpeedDiceService.ClearCombat();

        var harmony = new Harmony("LibraryOfRuinaLib");
        harmony.PatchAll();
        LibrarySpeedDiceMobileRightClickCompat.TryInstall(harmony);
        Log.Info("成功加载 LibraryOfRuinaLib基础库");
        ScriptManagerBridge.LookupScriptsInAssembly(typeof(Entry).Assembly);
    }
}
