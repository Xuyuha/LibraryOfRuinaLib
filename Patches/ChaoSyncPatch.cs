using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Models;
using Library.Entities.Creatures;

namespace Library.Patches;

[HarmonyPatch]
public static class ChaoSyncPatch//设置血量时，同步设置混乱值，（虽然这么写不是很符合sethp方法的逻辑）
{
    [HarmonyTargetMethod]
    public static System.Reflection.MethodBase TargetMethod1(Harmony harmony)
    {
        return AccessTools.Method(typeof(Creature), nameof(Creature.SetUniqueMonsterHpValue));
    }

    [HarmonyPostfix]
    public static void SetUniqueMonsterHpValuePostfix(
        Creature __instance,
        IReadOnlyList<Creature> creaturesOnSide,
        Rng rng)
    {
        if (__instance is LibraryCreature libraryCreature)
        {
            libraryCreature.SetUniqueMonsterChaoValue(creaturesOnSide, rng);
        }
    }

    [HarmonyTargetMethod]
    public static System.Reflection.MethodBase TargetMethod2(Harmony harmony)
    {
        return AccessTools.Method(typeof(Creature), nameof(Creature.ScaleMonsterHpForMultiplayer));
    }

    [HarmonyPostfix]
    public static void ScaleMonsterHpForMultiplayerPostfix(
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
