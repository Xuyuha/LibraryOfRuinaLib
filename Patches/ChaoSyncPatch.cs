using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Models;
using Library.Entities.Creatures;

namespace Library.Patches;

[HarmonyPatch(typeof(Creature), nameof(Creature.SetUniqueMonsterHpValue))]
public static class ChaoSyncSetUniquePatch//设置血量时，同步设置混乱值
{
    [HarmonyPostfix]
    public static void Postfix(
        Creature __instance,
        IReadOnlyList<Creature> creaturesOnSide,
        Rng rng)
    {
        if (__instance is LibraryCreature libraryCreature)
        {
            libraryCreature.SetUniqueMonsterChaoValue(creaturesOnSide, rng);
        }
    }
}

[HarmonyPatch(typeof(Creature), nameof(Creature.ScaleMonsterHpForMultiplayer))]
public static class ChaoSyncScalePatch//多人模式缩放血量时，同步缩放混乱值
{
    [HarmonyPostfix]
    public static void Postfix(
        Creature __instance,
        EncounterModel? encounter,
        int playerCount,
        int actIndex)
    {
        if (__instance is LibraryCreature libraryCreature)
        {
            libraryCreature.ScaleMonsterChaoValueForMultiplayer(encounter, playerCount, actIndex);
        }
    }
}
