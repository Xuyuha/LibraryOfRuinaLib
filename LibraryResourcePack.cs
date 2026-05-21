#nullable enable
using System.IO;
using System.Reflection;
using Godot;

namespace Library;

internal static class LibraryResourcePack
{
    private static readonly string[] PackFileNames =
    {
        "LibraryOfRuinaLib.pck"
    };

    private static bool _attempted;

    private static bool _loaded;

    internal static bool Loaded => _loaded;

    internal static void TryLoad()
    {
        if (_attempted)
        {
            return;
        }

        _attempted = true;
        string? dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(dir))
        {
            return;
        }

        foreach (string packFileName in PackFileNames)
        {
            string pckPath = Path.Combine(dir, packFileName);
            if (!File.Exists(pckPath))
            {
                continue;
            }

            if (ProjectSettings.LoadResourcePack(pckPath))
            {
                _loaded = true;
                return;
            }
        }
    }
}
