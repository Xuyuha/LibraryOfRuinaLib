#nullable enable
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace LibraryLib.Multiplayer;

/// <summary>
/// Opt-in boundary for LibraryOfRuinaLib's stable multiplayer protocol. A mod must
/// register each assembly that owns network messages or actions during its initializer.
/// Assemblies that do not register remain on the game's native positional protocol;
/// LibraryOfRuinaLib does not scan their types or encode their payloads.
/// </summary>
public static class LibraryManagedNetTypes
{
    private static readonly object Sync = new();
    private static readonly HashSet<Assembly> RegisteredAssemblies = [];

    public static void RegisterAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        lock (Sync)
        {
            if (RegisteredAssemblies.Contains(assembly))
            {
                return;
            }
            if (LibraryManagedNetTypeRegistry.IsReady)
            {
                throw new InvalidOperationException(
                    "Managed network assemblies must be registered before the main menu initializes: "
                    + assembly.GetName().Name);
            }

            RegisteredAssemblies.Add(assembly);
        }
    }

    internal static bool IsAssemblyRegistered(Assembly assembly)
    {
        lock (Sync)
        {
            return RegisteredAssemblies.Contains(assembly);
        }
    }

    internal static Assembly[] GetRegisteredAssemblies()
    {
        lock (Sync)
        {
            return RegisteredAssemblies.ToArray();
        }
    }
}

internal readonly record struct LibraryManagedNetTypeKey(
    string ModId,
    string AssemblyName,
    string TypeFullName)
{
    public override string ToString() => ModId + "/" + AssemblyName + "/" + TypeFullName;
}

internal readonly record struct LibraryManagedNetTypeRegistration(
    string ModId,
    bool AffectsGameplay,
    Type Type);

internal sealed class LibraryManagedNetTypeCatalog
{
    private readonly HashSet<Type> _vanillaMessageTypes;
    private readonly HashSet<Type> _vanillaActionTypes;
    private readonly Dictionary<Type, LibraryManagedNetTypeKey> _messageTypeToKey = new();
    private readonly Dictionary<LibraryManagedNetTypeKey, Type> _messageKeyToType = new();
    private readonly Dictionary<Type, LibraryManagedNetTypeKey> _actionTypeToKey = new();
    private readonly Dictionary<LibraryManagedNetTypeKey, Type> _actionKeyToType = new();
    private readonly HashSet<Type> _excludedMessageTypes = new();
    private readonly HashSet<Type> _excludedActionTypes = new();

    public int MessageCount => _messageTypeToKey.Count;
    public int ActionCount => _actionTypeToKey.Count;
    public int ExcludedMessageCount => _excludedMessageTypes.Count;
    public int ExcludedActionCount => _excludedActionTypes.Count;

    public IReadOnlyList<Type> GameplayMessageTypesInWireOrder => _messageKeyToType
        .OrderBy(static pair => pair.Key.ToString(), StringComparer.Ordinal)
        .Select(static pair => pair.Value)
        .ToArray();

    public IReadOnlyList<Type> GameplayActionTypesInWireOrder => _actionKeyToType
        .OrderBy(static pair => pair.Key.ToString(), StringComparer.Ordinal)
        .Select(static pair => pair.Value)
        .ToArray();

    public string Fingerprint { get; }

    private LibraryManagedNetTypeCatalog(
        IEnumerable<LibraryManagedNetTypeRegistration> messageRegistrations,
        IEnumerable<LibraryManagedNetTypeRegistration> actionRegistrations,
        IEnumerable<Type> vanillaMessageTypes,
        IEnumerable<Type> vanillaActionTypes)
    {
        _vanillaMessageTypes = vanillaMessageTypes.ToHashSet();
        _vanillaActionTypes = vanillaActionTypes.ToHashSet();

        RegisterAll<INetMessage>(
            messageRegistrations,
            _vanillaMessageTypes,
            _messageTypeToKey,
            _messageKeyToType,
            _excludedMessageTypes,
            "message");
        RegisterAll<INetAction>(
            actionRegistrations,
            _vanillaActionTypes,
            _actionTypeToKey,
            _actionKeyToType,
            _excludedActionTypes,
            "action");

        Fingerprint = CalculateFingerprint();
    }

    public static LibraryManagedNetTypeCatalog Create(
        IEnumerable<LibraryManagedNetTypeRegistration> messageRegistrations,
        IEnumerable<LibraryManagedNetTypeRegistration> actionRegistrations,
        IEnumerable<Type> vanillaMessageTypes,
        IEnumerable<Type> vanillaActionTypes) =>
        new(
            messageRegistrations,
            actionRegistrations,
            vanillaMessageTypes,
            vanillaActionTypes);

    public bool IsVanillaMessage(Type type) => _vanillaMessageTypes.Contains(type);

    public bool IsVanillaAction(Type type) => _vanillaActionTypes.Contains(type);

    public bool TryGetMessageKey(Type type, out LibraryManagedNetTypeKey key) =>
        _messageTypeToKey.TryGetValue(type, out key);

    public bool TryResolveMessage(LibraryManagedNetTypeKey key, out Type? type) =>
        _messageKeyToType.TryGetValue(key, out type);

    public bool IsExcludedMessage(Type type) => _excludedMessageTypes.Contains(type);

    public bool IsRegisteredMessage(Type type) =>
        _messageTypeToKey.ContainsKey(type) || _excludedMessageTypes.Contains(type);

    public bool TryGetActionKey(Type type, out LibraryManagedNetTypeKey key) =>
        _actionTypeToKey.TryGetValue(type, out key);

    public bool TryResolveAction(LibraryManagedNetTypeKey key, out Type? type) =>
        _actionKeyToType.TryGetValue(key, out type);

    public bool IsExcludedAction(Type type) => _excludedActionTypes.Contains(type);

    public bool IsRegisteredAction(Type type) =>
        _actionTypeToKey.ContainsKey(type) || _excludedActionTypes.Contains(type);

    private static void RegisterAll<TBase>(
        IEnumerable<LibraryManagedNetTypeRegistration> registrations,
        HashSet<Type> vanillaTypes,
        Dictionary<Type, LibraryManagedNetTypeKey> typeToKey,
        Dictionary<LibraryManagedNetTypeKey, Type> keyToType,
        HashSet<Type> excludedTypes,
        string kind)
        where TBase : class
    {
        var seenKeys = new HashSet<LibraryManagedNetTypeKey>();
        foreach (LibraryManagedNetTypeRegistration registration in registrations)
        {
            Type type = registration.Type;
            if (vanillaTypes.Contains(type))
            {
                throw new InvalidOperationException(
                    $"Vanilla net {kind} type cannot be registered as managed: {type.FullName}");
            }
            if (type.IsAbstract || type.IsInterface || !typeof(TBase).IsAssignableFrom(type))
            {
                throw new InvalidOperationException(
                    $"Invalid managed net {kind} type: {type.FullName}");
            }
            if (string.IsNullOrWhiteSpace(registration.ModId))
            {
                throw new InvalidOperationException(
                    $"Managed net {kind} type has no owner mod ID: {type.FullName}");
            }

            string assemblyName = type.Assembly.GetName().Name
                ?? throw new InvalidOperationException(
                    $"Managed net {kind} type has no assembly name: {type.FullName}");
            string fullName = type.FullName
                ?? throw new InvalidOperationException(
                    $"Managed net {kind} type has no full name: {type}");
            var key = new LibraryManagedNetTypeKey(registration.ModId, assemblyName, fullName);

            if (typeToKey.ContainsKey(type) || excludedTypes.Contains(type))
            {
                throw new InvalidOperationException(
                    $"Managed net {kind} type was registered more than once: {key}");
            }
            if (!seenKeys.Add(key))
            {
                throw new InvalidOperationException(
                    $"Duplicate managed net {kind} stable key: {key}");
            }

            if (!registration.AffectsGameplay)
            {
                excludedTypes.Add(type);
                continue;
            }

            typeToKey.Add(type, key);
            keyToType.Add(key, type);
        }
    }

    private string CalculateFingerprint()
    {
        IEnumerable<string> entries = _messageKeyToType.Keys
            .Select(static key => "M:" + key)
            .Concat(_actionKeyToType.Keys.Select(static key => "A:" + key))
            .OrderBy(static value => value, StringComparer.Ordinal);
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", entries)));
        return Convert.ToHexString(digest.AsSpan(0, 8));
    }
}

internal static class LibraryManagedNetTypeRegistry
{
    private static readonly HashSet<Type> VanillaMessages = INetMessageSubtypes.All.ToHashSet();
    private static readonly HashSet<Type> VanillaActions = INetActionSubtypes.All.ToHashSet();
    private static LibraryManagedNetTypeCatalog? _catalog;

    public static bool IsReady => _catalog != null;

    public static int EnvelopeMessageId => VanillaMessages.Count;

    public static LibraryManagedNetTypeCatalog Catalog =>
        _catalog ?? throw new InvalidOperationException(
            "LibraryOfRuinaLib managed multiplayer protocol is not initialized.");

    public static bool IsVanillaMessage(Type type) => VanillaMessages.Contains(type);

    public static bool IsVanillaAction(Type type) => VanillaActions.Contains(type);

    public static void Initialize()
    {
        if (_catalog != null)
        {
            return;
        }

        Assembly[] registeredAssemblies =
            LibraryManagedNetTypes.GetRegisteredAssemblies();
        Dictionary<Assembly, AssemblyOwner> owners = BuildAssemblyOwners();
        foreach (Assembly assembly in registeredAssemblies)
        {
            if (!owners.ContainsKey(assembly))
            {
                throw new InvalidOperationException(
                    "Registered managed network assembly has no owning loaded mod: "
                    + assembly.GetName().Name);
            }
        }

        List<LibraryManagedNetTypeRegistration> messages = BuildRegistrations(
            GetRegisteredSubtypes<INetMessage>(registeredAssemblies)
                .Where(static type => type != typeof(LibraryManagedNetMessageEnvelope)),
            owners,
            "message");
        List<LibraryManagedNetTypeRegistration> actions = BuildRegistrations(
            GetRegisteredSubtypes<INetAction>(registeredAssemblies),
            owners,
            "action");

        _catalog = LibraryManagedNetTypeCatalog.Create(
            messages,
            actions,
            VanillaMessages,
            VanillaActions);
    }

    internal static void SetCatalogForTesting(LibraryManagedNetTypeCatalog catalog) =>
        _catalog = catalog;

    private static IEnumerable<Type> GetRegisteredSubtypes<TBase>(
        IEnumerable<Assembly> assemblies)
    {
        Type baseType = typeof(TBase);
        return assemblies
            .SelectMany(static assembly => assembly.GetTypes())
            .Where(type => type != baseType && baseType.IsAssignableFrom(type));
    }

    private readonly record struct AssemblyOwner(string ModId, bool AffectsGameplay);

    private static Dictionary<Assembly, AssemblyOwner> BuildAssemblyOwners()
    {
        var owners = new Dictionary<Assembly, AssemblyOwner>();
        foreach (Mod mod in ModManager.GetLoadedMods())
        {
            string modId = mod.manifest?.id
                ?? throw new InvalidOperationException(
                    "Loaded mod with managed assembly has no manifest ID: " + mod.path);
            if (string.IsNullOrWhiteSpace(modId))
            {
                throw new InvalidOperationException(
                    "Loaded mod with managed assembly has an empty manifest ID: " + mod.path);
            }

            var owner = new AssemblyOwner(modId, mod.manifest?.affectsGameplay ?? true);
            foreach (Assembly assembly in mod.assemblies)
            {
                if (owners.TryGetValue(assembly, out AssemblyOwner existingOwner))
                {
                    if (existingOwner != owner)
                    {
                        throw new InvalidOperationException(
                            $"Managed assembly {assembly.GetName().Name} belongs to both "
                            + $"{existingOwner.ModId} and {modId}.");
                    }
                    continue;
                }

                owners.Add(assembly, owner);
            }
        }

        return owners;
    }

    private static List<LibraryManagedNetTypeRegistration> BuildRegistrations(
        IEnumerable<Type> types,
        IReadOnlyDictionary<Assembly, AssemblyOwner> owners,
        string kind)
    {
        var registrations = new List<LibraryManagedNetTypeRegistration>();
        foreach (Type type in types.Distinct())
        {
            if (!owners.TryGetValue(type.Assembly, out AssemblyOwner owner))
            {
                throw new InvalidOperationException(
                    $"Managed net {kind} type has no owning loaded mod: {type.FullName}");
            }

            registrations.Add(new LibraryManagedNetTypeRegistration(
                owner.ModId,
                owner.AffectsGameplay,
                type));
        }

        return registrations;
    }
}
