using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace Test.Scripts;


[ModInitializer("Init")]
public class Entry
{
    public static void Init()
    {
        var harmony = new Harmony("LibraryOfRuinaLib");
        harmony.PatchAll();
        Log.Info("成功加载LibraryOfRuinaLib");
        ScriptManagerBridge.LookupScriptsInAssembly(typeof(Entry).Assembly);
    }
}
