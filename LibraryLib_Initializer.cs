using Godot.Bridge;
using HarmonyLib;
using Library.Localization;
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

        var harmony = new Harmony("LibraryOfRuinaLib");
        harmony.PatchAll();
        Log.Info("成功加载 LibraryOfRuinaLib（含抗性子系统）");
        ScriptManagerBridge.LookupScriptsInAssembly(typeof(Entry).Assembly);
    }
}
