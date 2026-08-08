#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Godot;

namespace LibraryLib.Utils;

internal static class LibraryTextureSafety
{
    internal static bool IsValid([NotNullWhen(true)] Texture2D? texture)
    {
        try
        {
            if (texture == null || !GodotObject.IsInstanceValid(texture))
            {
                return false;
            }

            _ = texture.GetRid();
            _ = texture.GetWidth();
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }
}
