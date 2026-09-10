#nullable enable
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LibraryLib.Entities.Creatures;
using LibraryLib.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace LibraryLib.Patches;

[HarmonyPatch]
public static class CreateCreaturePatch
{
    [HarmonyTargetMethod]
    public static MethodBase TargetMethod(Harmony harmony)
    {
        return AccessTools.Method(
            typeof(CombatState), 
            nameof(CombatState.CreateCreature),
            new[] { typeof(MonsterModel), typeof(CombatSide), typeof(string) })!;
    }
    
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        
        var creatureCtor = typeof(Creature).GetConstructor(
            new[] { typeof(MonsterModel), typeof(CombatSide), typeof(string) });
        var factory = AccessTools.Method(typeof(CreateCreaturePatch), nameof(CreateMonsterCreature));
        
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Newobj)
            {
                var ctor = codes[i].operand as ConstructorInfo;
                if (ctor == creatureCtor)
                {
                    codes[i].opcode = OpCodes.Call;
                    codes[i].operand = factory;
                }
            }
        }
        
        return codes;
    }

    private static Creature CreateMonsterCreature(MonsterModel monster, CombatSide side, string? slotName)
    {
        // 仅图书馆怪物接入抗性与混乱系统，普通怪物保留原版 Creature 类型。
        if (monster is LibraryMonsterModel)
        {
            return new LibraryCreature(monster, side, slotName);
        }

        return new Creature(monster, side, slotName);
    }
}
