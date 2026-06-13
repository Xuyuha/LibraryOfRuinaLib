#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace Library.Patches;

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

        _normalized = true;

        try
        {
            if (!TryNormalize(out int vanillaCount, out int modCount))
                return;

            Log.Info($"[LibraryOfRuinaLib.Multiplayer] Stable INetMessage IDs applied. vanilla={vanillaCount}, modded={modCount}");
        }
        catch (Exception e)
        {
            Log.Warn("[LibraryOfRuinaLib.Multiplayer] Failed to stabilize INetMessage IDs: " + e);
        }
    }

    private static bool TryNormalize(out int vanillaCount, out int modCount)
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

        var vanillaTypes = INetMessageSubtypes.All
            .OrderBy(static t => t.Name, StringComparer.Ordinal)
            .ToList();

        var vanillaSet = new HashSet<Type>(vanillaTypes);
        var modTypes = ReflectionHelper.GetSubtypesInMods<INetMessage>()
            .Where(t => !vanillaSet.Contains(t))
            .OrderBy(static t => t.FullName ?? t.Name, StringComparer.Ordinal)
            .ToList();

        idToType.Clear();
        typeToId.Clear();

        foreach (var type in vanillaTypes)
            AddType(typeToId, idToType, type);

        foreach (var type in modTypes)
            AddType(typeToId, idToType, type);

        vanillaCount = vanillaTypes.Count;
        modCount = modTypes.Count;
        return true;
    }

    private static void AddType(Dictionary<Type, int> typeToId, List<Type> idToType, Type type)
    {
        if (typeToId.ContainsKey(type))
            return;

        typeToId[type] = idToType.Count;
        idToType.Add(type);
    }
}
