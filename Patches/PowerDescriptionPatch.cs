using HarmonyLib;
using Library.Models;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Ruina.Patches;

[HarmonyPatch(typeof(PowerModel), "AddDumbVariablesToDescription")]
public static class PowerDescriptionPatch
{
    static void Postfix(PowerModel __instance, LocString description, int? amountOverride = null)
    {
        if(__instance is LibraryPowerModel power){
            power.AddVariablesToDescription(description, amountOverride);
        }
    }
}