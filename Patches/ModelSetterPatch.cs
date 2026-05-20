using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Library.Models;
[HarmonyPatch(typeof(NPower), "Model", MethodType.Setter)]
public static class ModelSetterPatch//为了实现动态power展示的Patch,截取Npower设置时的Npower并作为变量存储在power里
{
    static void Postfix(NPower __instance)
    {
        if (__instance.Model is LibraryPowerModel powerModel && powerModel.IsDynamic)
        {
            powerModel.BoundNPower = __instance;
        }
    }
}