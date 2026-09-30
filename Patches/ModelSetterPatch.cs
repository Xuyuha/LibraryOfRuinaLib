using HarmonyLib;
using LibraryLib.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace LibraryLib.Patches;

[HarmonyPatch(typeof(NPower), "Model", MethodType.Setter)]
public static class ModelSetterPatch//为了实现动态power展示的Patch,截取Npower设置时的Npower并作为变量存储在power里
{
    static void Postfix(NPower __instance)
    {
        if (__instance.Model is LibraryPowerModel powerModel && powerModel.NeedNpower)
        {
            powerModel.BoundNPower = __instance;
        }
    }
}