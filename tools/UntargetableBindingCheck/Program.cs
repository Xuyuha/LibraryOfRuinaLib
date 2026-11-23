using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using LibraryLib.Commands;
using LibraryLib.Powers;

// Real assembly metadata/IL checks, without installing Harmony patches or executing game commands.
int checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine($"PASS {++checks}: {message}");
}
Assembly library = typeof(LibraryUntargetablePower).Assembly;
const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
foreach (string name in new[]
{
    "LibraryUntargetableAttackTargetsPatch", "LibraryUntargetableCardTargetPatch",
    "LibraryUntargetableStunPatch", "LibraryAttackChaoDamagePatch"
})
{
    Type type = library.GetType("LibraryLib.Patches." + name, throwOnError: true)!;
    var patch = type.GetCustomAttributes<HarmonyPatch>().Single().info;
    MethodInfo? target = patch.argumentTypes == null
        ? patch.declaringType.GetMethod(patch.methodName, all)
        : patch.declaringType.GetMethod(patch.methodName, all, null, patch.argumentTypes, null);
    Check(target != null, $"{name}: target resolves in the real game assemblies");
    foreach (MethodInfo handler in type.GetMethods(all).Where(m =>
        m.IsDefined(typeof(HarmonyPrefix)) || m.IsDefined(typeof(HarmonyPostfix))))
    {
        foreach (ParameterInfo p in handler.GetParameters())
        {
            if (p.Name!.StartsWith("__")) continue;
            ParameterInfo? native = target!.GetParameters().FirstOrDefault(t => t.Name == p.Name);
            Type actual = p.ParameterType.IsByRef ? p.ParameterType.GetElementType()! : p.ParameterType;
            Check(native != null && actual == native.ParameterType,
                $"{name}.{handler.Name}: argument {p.Name} matches");
        }
    }
}

var opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
    .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)!)
    .ToDictionary(op => unchecked((ushort)op.Value));
List<MethodBase> Calls(MethodInfo method)
{
    Type? state = method.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;
    if (state != null) method = state.GetMethod("MoveNext", all)!;
    byte[] il = method.GetMethodBody()!.GetILAsByteArray()!;
    List<MethodBase> calls = [];
    for (int i = 0; i < il.Length;)
    {
        ushort key = il[i++];
        if (key == 0xfe) key = (ushort)(0xfe00 | il[i++]);
        OpCode op = opcodes[key];
        if (op.OperandType == OperandType.InlineMethod)
        {
            int token = BitConverter.ToInt32(il, i);
            MethodBase? call = method.Module.ResolveMethod(token,
                method.DeclaringType?.GetGenericArguments(), method.GetGenericArguments());
            if (call != null) calls.Add(call);
        }
        i += op.OperandType switch
        {
            OperandType.InlineNone => 0,
            OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
            OperandType.InlineVar => 2,
            OperandType.InlineI8 or OperandType.InlineR => 8,
            OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, i),
            _ => 4
        };
    }
    return calls;
}

Type commands = typeof(LibraryCreatureCmd);
MethodInfo hp = commands.GetMethods().Single(m => m.Name == "Damage"
    && m.GetParameters().Any(p => p.Name == "beforeApplyingDamage"));
MethodInfo chaos = commands.GetMethods().Single(m => m.Name == "ChaoDamage"
    && m.GetParameters().Length == 9 && m.GetParameters()[1].ParameterType.IsGenericType);
MethodInfo stun = commands.GetMethods().Single(m => m.Name == "Stun" && m.GetParameters().Length == 3);
foreach (var (method, minimum) in new[] { (hp, 3), (chaos, 2), (stun, 1) })
{
    var calls = Calls(method);
    var guards = calls.Where(m => m.DeclaringType?.FullName == "LibraryLib.Combat.LibraryUntargetable"
        && m.Name == "BlocksHit").ToArray();
    Check(guards.Length >= minimum, $"{method.Name}: expected command guards exist in compiled async IL");
}
Type nativeDamage = library.GetType("LibraryLib.Patches.LibraryAttackChaoDamagePatch", true)!;
var prefix = nativeDamage.GetMethod("PrefixCore", all) ?? nativeDamage.GetMethod("Prefix", all)!;
var prefixCalls = Calls(prefix);
Check(prefixCalls[0].DeclaringType?.FullName == "LibraryLib.Combat.LibraryUntargetable"
    && prefixCalls[0].Name == "FilterHittable", "native damage filter precedes unpowered/move early exits");
Check(typeof(LibraryUntargetablePower).GetMethod("ShouldAllowHitting")!.GetBaseDefinition()
    == typeof(MegaCrit.Sts2.Core.Models.PowerModel).GetMethod("ShouldAllowHitting")!.GetBaseDefinition(),
    "hit veto overrides the real inherited PowerModel hook");
Console.WriteLine($"{checks} real-assembly binding/IL checks passed. No patches were installed and no combat was run.");

// Compile the documented usage against the actual game API without executing it.
#pragma warning disable CS8321
static async Task DocumentedUsage(MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext choiceContext,
    MegaCrit.Sts2.Core.Entities.Creatures.Creature target,
    MegaCrit.Sts2.Core.Entities.Creatures.Creature? applier,
    MegaCrit.Sts2.Core.Models.CardModel? cardSource)
{
    await MegaCrit.Sts2.Core.Commands.PowerCmd.Apply<LibraryUntargetablePower>(choiceContext, target, 1m, applier, cardSource);
    if (target.GetPower<LibraryUntargetablePower>() is { } protection)
        await MegaCrit.Sts2.Core.Commands.PowerCmd.Remove(protection);
}
#pragma warning restore CS8321
