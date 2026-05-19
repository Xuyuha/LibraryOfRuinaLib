#nullable enable
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Library.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace Library.Patches;

[HarmonyPatch]
public static class PlayerConstructorPatch//patch了Player的构造函数，用于将角色的creature创建为LibraryCreature
{
    [HarmonyTargetMethod]
    public static MethodBase TargetMethod(Harmony harmony)
    {
        foreach (var ctor in typeof(Player).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (ctor.GetParameters().Length >= 10 && !ctor.IsPublic)
            {
                return ctor;
            }
        }
        return null!;
    }
    
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        
        var libraryCtor = typeof(LibraryCreature).GetConstructor(new[] { typeof(Player), typeof(int), typeof(int) });
        
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
