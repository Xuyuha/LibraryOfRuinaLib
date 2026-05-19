#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;

namespace Library.Localization;

internal static class LibraryResistanceLocalization
{
    private const string PowersTableName = "powers";

    private const string DefaultLanguage = "eng";

    internal static void Install()
    {
        TryApply(LocManager.Instance);
    }

    internal static void TryApply(LocManager? locManager)
    {
        if (locManager == null)
        {
            return;
        }

        try
        {
            string language = NormalizeLanguage(locManager.Language);
            MergeTable(
                locManager,
                PowersTableName,
                WithSlugifiedModelIdKeys(LoadPowerEntries(language) ?? LoadPowerEntries(DefaultLanguage)));
        }
        catch (Exception)
        {
        }
    }

    private static Dictionary<string, string>? WithSlugifiedModelIdKeys(Dictionary<string, string>? source)
    {
        if (source == null || source.Count == 0)
        {
            return source;
        }

        var merged = new Dictionary<string, string>(source, StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> kv in source)
        {
            int dot = kv.Key.IndexOf('.');
            if (dot <= 0)
            {
                continue;
            }

            string prefix = kv.Key.Substring(0, dot);
            string rest = kv.Key.Substring(dot);
            string slug = StringHelper.Slugify(prefix);
            string alt = slug + rest;
            if (!string.Equals(alt, kv.Key, StringComparison.Ordinal))
            {
                merged[alt] = kv.Value;
            }
        }

        return merged;
    }

    private static void MergeTable(LocManager locManager, string tableName, Dictionary<string, string>? entries)
    {
        if (entries == null || entries.Count == 0)
        {
            return;
        }

        locManager.GetTable(tableName).MergeWith(entries);
    }

    private static Dictionary<string, string>? LoadPowerEntries(string language)
    {
        string resourceName = $"LibraryOfRuinaLib.Localization.{language}.powers.json";
        return LoadEntries(resourceName);
    }

    private static Dictionary<string, string>? LoadEntries(string resourceName)
    {
        Assembly assembly = typeof(LibraryResistanceLocalization).Assembly;
        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        string json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
    }

    private static string NormalizeLanguage(string? language)
    {
        return language?.ToLowerInvariant() switch
        {
            "zhs" => "zhs",
            "zh_cn" => "zhs",
            "eng" => "eng",
            "kor" => "kor",
            _ => DefaultLanguage
        };
    }
}

[HarmonyPatch(typeof(LocManager), nameof(LocManager.SetLanguage))]
internal static class LibraryResistanceLocManagerSetLanguagePatch
{
    [HarmonyPostfix]
    private static void Postfix(LocManager __instance, string language)
    {
        _ = language;
        LibraryResistanceLocalization.TryApply(__instance);
    }
}
