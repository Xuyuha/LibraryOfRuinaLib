#nullable enable
using System.Reflection;
using Godot;

namespace LibraryLib;

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

        // 实现位于 lib/<目标>/，两个目标共用模组根目录的资源包。
        if (Directory.GetParent(dir)?.Name == "lib")
            dir = Directory.GetParent(dir)!.Parent!.FullName;

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
