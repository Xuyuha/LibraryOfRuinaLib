#nullable enable
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Library.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace Library.Patches;

[HarmonyPatch]
public static class CreateCreaturePatch//patch了CombatState.CreateCreature方法，用于将怪物的creature创建为LibraryCreature
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
        
        var libraryCtor = typeof(LibraryCreature).GetConstructor(new[] { typeof(MonsterModel), typeof(CombatSide), typeof(string) });
        
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Newobj)
            {
                var ctor = codes[i].operand as ConstructorInfo;
                if (ctor?.DeclaringType == typeof(Creature))
                {
                    codes[i].operand = libraryCtor;
                }
            }
        }
        
        return codes;
    }
}
