using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Library.Models;
[HarmonyPatch(typeof(NPower), "Model", MethodType.Setter)]
public static class ModelSetterPatch//为了实现动态power展示的Patch
{
    static void Postfix(NPower __instance)
    {
        if (__instance.Model is LibraryPowerModel powerModel)
        {
            powerModel.BoundNPower = __instance;
        }
    }
}