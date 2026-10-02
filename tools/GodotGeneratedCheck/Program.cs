using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

// 核对 Godot 源码生成器的产物确实进了 DLL。GodotSharp 的 Node.InvokeGodotClassMethod 只有在
// 生成的 HasGodotClassMethod 覆写返回 true 时才会把 _Ready/_Process/_Draw 派发到 C# 子类；
// 用普通 Microsoft.NET.Sdk 编译时这些覆写全部缺失，编译却照样零错误，所以必须查产物。
// 只解析元数据，不加载程序集。
if (args.Length < 2)
{
    Console.Error.WriteLine("用法：GodotGeneratedCheck <仓库根目录> <dll>...");
    return 2;
}

string root = Path.GetFullPath(args[0]);
string[] generatedNested = ["MethodName", "PropertyName", "SignalName"];
string[] requiredOverrides = ["InvokeGodotClassMethod", "HasGodotClassMethod"];
// 速度骰界面的 _Process 是这次回归最直接的受害者，作为最低限度的存在性锚点。
const string anchor = "LibraryLib.SpeedDice.LibrarySpeedDiceUi";
var failures = new List<string>();
List<string>? firstScriptPaths = null;

foreach (string dll in args.Skip(1))
{
    using var stream = File.OpenRead(dll);
    using var pe = new PEReader(stream);
    MetadataReader md = pe.GetMetadataReader();

    string Name(TypeDefinitionHandle handle)
    {
        TypeDefinition t = md.GetTypeDefinition(handle);
        string name = md.GetString(t.Name);
        TypeDefinitionHandle outer = t.GetDeclaringType();
        if (!outer.IsNil) return Name(outer) + "+" + name;
        string ns = md.GetString(t.Namespace);
        return ns.Length == 0 ? name : ns + "." + name;
    }

    // 直接继承 Godot 命名空间类型，或继承本程序集内这样的类型，即视为脚本类型。
    var cache = new Dictionary<TypeDefinitionHandle, bool>();
    bool IsGodotDerived(TypeDefinitionHandle handle)
    {
        if (cache.TryGetValue(handle, out bool known)) return known;
        cache[handle] = false;
        EntityHandle baseType = md.GetTypeDefinition(handle).BaseType;
        // 接口和 <Module> 没有基类，空句柄的 Kind 仍是 TypeDefinition，必须先排除。
        bool result = !baseType.IsNil && baseType.Kind switch
        {
            HandleKind.TypeReference => md.GetString(md.GetTypeReference((TypeReferenceHandle)baseType).Namespace) == "Godot",
            HandleKind.TypeDefinition => IsGodotDerived((TypeDefinitionHandle)baseType),
            _ => false,
        };
        return cache[handle] = result;
    }

    string AttributeTypeName(CustomAttribute attribute)
    {
        EntityHandle parent = attribute.Constructor.Kind == HandleKind.MemberReference
            ? md.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent
            : md.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType();
        return parent.Kind switch
        {
            HandleKind.TypeReference => md.GetString(md.GetTypeReference((TypeReferenceHandle)parent).Name),
            HandleKind.TypeDefinition => md.GetString(md.GetTypeDefinition((TypeDefinitionHandle)parent).Name),
            _ => "",
        };
    }

    var names = md.TypeDefinitions.ToDictionary(h => h, Name);
    if (!names.Values.Contains("GodotPlugins.Game.Main")) failures.Add($"{dll}: 缺少 GodotPlugins.Game.Main");

    var scriptTypes = md.TypeDefinitions.Where(h => !names[h].Contains('<') && IsGodotDerived(h)).OrderBy(h => names[h], StringComparer.Ordinal).ToArray();
    if (!scriptTypes.Any(h => names[h] == anchor)) failures.Add($"{dll}: 未找到 {anchor}");

    var scriptPaths = new List<string>();
    foreach (TypeDefinitionHandle handle in scriptTypes)
    {
        TypeDefinition type = md.GetTypeDefinition(handle);
        var nested = type.GetNestedTypes().Select(n => md.GetString(md.GetTypeDefinition(n).Name)).ToHashSet();
        foreach (string missing in generatedNested.Where(n => !nested.Contains(n)))
            failures.Add($"{dll}: {names[handle]} 缺少生成类型 +{missing}");

        foreach (string method in requiredOverrides)
        {
            bool overridden = type.GetMethods().Select(md.GetMethodDefinition).Any(m =>
                md.GetString(m.Name) == method
                && (m.Attributes & MethodAttributes.Virtual) != 0
                && (m.Attributes & MethodAttributes.NewSlot) == 0);
            if (!overridden) failures.Add($"{dll}: {names[handle]} 未覆写 {method}");
        }

        foreach (CustomAttribute attribute in type.GetCustomAttributes().Select(md.GetCustomAttribute))
        {
            if (AttributeTypeName(attribute) != "ScriptPathAttribute") continue;
            BlobReader blob = md.GetBlobReader(attribute.Value);
            blob.ReadUInt16();
            string path = blob.ReadSerializedString() ?? "";
            scriptPaths.Add(path);
            // res:// 以 GodotProjectDir 为根；默认是工程目录，路径必须落在仓库里真实存在的源文件上。
            if (!path.StartsWith("res://", StringComparison.Ordinal) || !File.Exists(Path.Combine(root, path["res://".Length..])))
                failures.Add($"{dll}: {names[handle]} 的 ScriptPath 不对应仓库文件：{path}");
        }
    }

    scriptPaths.Sort(StringComparer.Ordinal);
    if (scriptPaths.Count == 0) failures.Add($"{dll}: 没有任何 ScriptPathAttribute");
    if (firstScriptPaths == null) firstScriptPaths = scriptPaths;
    else if (!firstScriptPaths.SequenceEqual(scriptPaths)) failures.Add($"{dll}: ScriptPath 集合与第一个 DLL 不一致");

    Console.WriteLine($"{dll}: {scriptTypes.Length} 个 Godot 脚本类型，{scriptPaths.Count} 个 ScriptPath");
    foreach (string path in scriptPaths) Console.WriteLine("  " + path);
}

foreach (string failure in failures) Console.Error.WriteLine(failure);
return failures.Count == 0 ? 0 : 1;
