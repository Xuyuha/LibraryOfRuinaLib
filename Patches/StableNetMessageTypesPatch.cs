#nullable enable
using HarmonyLib;
using LibraryLib.Multiplayer;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace LibraryLib.Patches;

[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready))]
internal static class StableNetMessageTypesPatch
{
    private static bool _normalized;

    [HarmonyPrefix]
    private static void Prefix()
    {
        NormalizeOnce();
    }

    private static void NormalizeOnce()
    {
        if (_normalized)
            return;

        try
        {
            LibraryManagedNetTypeRegistry.Initialize();
            bool normalizedMessages = TryNormalizeMessages(out int vanillaMessageCount, out int modMessageCount);
            bool normalizedActions = TryNormalizeActions(out int vanillaActionCount, out int modActionCount);
            if (!normalizedMessages || !normalizedActions)
            {
                throw new InvalidOperationException(
                    "STS2 network type caches were not both initialized: "
                    + $"messages={normalizedMessages}, actions={normalizedActions}.");
            }

            int patchedMessageSerializers = LibraryManagedNetMessagePatchInstaller.Install(
                new Harmony("LibraryOfRuinaLib.ManagedNetMessageEnvelope"));

            _normalized = true;
            LibraryManagedNetTypeCatalog catalog = LibraryManagedNetTypeRegistry.Catalog;
            Log.Info("[LibraryOfRuinaLib.Multiplayer] Stable gameplay net type table ready. "
                + $"messages={vanillaMessageCount}/{modMessageCount} "
                + $"actions={vanillaActionCount}/{modActionCount} "
                + $"envelopeId={LibraryManagedNetTypeRegistry.EnvelopeMessageId} "
                + $"serializers={patchedMessageSerializers} "
                + $"excludedNonGameplay={catalog.ExcludedMessageCount}/{catalog.ExcludedActionCount} "
                + $"registry={catalog.Fingerprint}");
        }
        catch (Exception e)
        {
            Log.Error("[LibraryOfRuinaLib.Multiplayer] Managed net protocol initialization failed: " + e);
            throw;
        }
    }

    private static bool TryNormalizeMessages(out int vanillaCount, out int modCount)
    {
        vanillaCount = 0;
        modCount = 0;

        var cache = AccessTools.Field(typeof(MessageTypes), "_cache")?.GetValue(null);
        if (cache == null)
            return false;

        var typeToId = AccessTools.Field(cache.GetType(), "_typeToId")?.GetValue(cache) as Dictionary<Type, int>;
        var idToType = AccessTools.Field(cache.GetType(), "_idToType")?.GetValue(cache) as List<Type>;
        if (typeToId == null || idToType == null)
            return false;

        IReadOnlyList<Type> vanillaTypes = GetOriginalVanillaMessageWireOrder();

        IReadOnlyList<Type> modTypes =
            LibraryManagedNetTypeRegistry.Catalog.GameplayMessageTypesInWireOrder;

        idToType.Clear();
        typeToId.Clear();

        foreach (Type type in vanillaTypes)
            AddType(typeToId, idToType, type);

        AddType(typeToId, idToType, typeof(LibraryManagedNetMessageEnvelope));
        int envelopeId = typeToId[typeof(LibraryManagedNetMessageEnvelope)];
        if (envelopeId != LibraryManagedNetTypeRegistry.EnvelopeMessageId)
        {
            throw new InvalidOperationException(
                "Managed message envelope did not receive the first post-vanilla message ID.");
        }
        if (envelopeId > byte.MaxValue)
        {
            throw new InvalidOperationException(
                $"Managed message envelope ID {envelopeId} does not fit in the game's byte-sized message ID.");
        }

        foreach (Type type in modTypes)
            AddAliasedType(typeToId, type, envelopeId);

        vanillaCount = vanillaTypes.Count;
        modCount = modTypes.Count;
        return true;
    }

    private static bool TryNormalizeActions(out int vanillaCount, out int modCount)
    {
        vanillaCount = 0;
        modCount = 0;

        var cache = AccessTools.Field(typeof(ActionTypes), "_cache")?.GetValue(null);
        if (cache == null)
            return false;

        var typeToId = AccessTools.Field(cache.GetType(), "_typeToId")?.GetValue(cache) as Dictionary<Type, int>;
        var idToType = AccessTools.Field(cache.GetType(), "_idToType")?.GetValue(cache) as List<Type>;
        if (typeToId == null || idToType == null)
            return false;

        IReadOnlyList<Type> vanillaTypes = GetOriginalVanillaActionWireOrder();

        idToType.Clear();
        typeToId.Clear();

        foreach (Type type in vanillaTypes)
            AddType(typeToId, idToType, type);

        vanillaCount = vanillaTypes.Count;
        modCount = LibraryManagedNetTypeRegistry.Catalog.ActionCount;
        return true;
    }

    internal static IReadOnlyList<Type> GetOriginalVanillaMessageWireOrder() =>
        INetMessageSubtypes.All
            .OrderBy(static type => type.Name)
            .ToArray();

    internal static IReadOnlyList<Type> GetOriginalVanillaActionWireOrder() =>
        INetActionSubtypes.All
            .OrderBy(static type => type.Name)
            .ToArray();

    private static void AddType(Dictionary<Type, int> typeToId, List<Type> idToType, Type type)
    {
        if (typeToId.ContainsKey(type))
            return;

        typeToId[type] = idToType.Count;
        idToType.Add(type);
    }

    private static void AddAliasedType(
        Dictionary<Type, int> typeToId,
        Type type,
        int sharedId)
    {
        if (!typeToId.TryAdd(type, sharedId))
        {
            throw new InvalidOperationException(
                "Duplicate managed message type in wire table: " + type.FullName);
        }
    }
}
