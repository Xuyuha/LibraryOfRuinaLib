#nullable enable
using HarmonyLib;
using LibraryLib.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace LibraryLib.Patches;

/// <summary>
/// Initializes the LibraryOfRuinaLib managed net registry when the main menu is ready.
///
/// The game-owned <c>MessageTypes</c>/<c>ActionTypes</c> caches are the exclusive property of the
/// vanilla game and of libraries that register their own carriers in them (e.g. BaseLib's
/// <c>CustomMessageWrapper</c> shadow IDs). Following RitsuLib's approach, LibraryOfRuinaLib
/// never rebuilds or writes into those tables. Managed messages travel through the LoR magic
/// envelope directly over the transport and are demuxed in
/// <see cref="LibraryManagedNetPacketDemuxPatch"/> before the vanilla bus ever sees them.
/// </summary>
[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready))]
internal static class StableNetMessageTypesPatch
{
    private static bool _initialized;

    [HarmonyPrefix]
    private static void Prefix()
    {
        if (_initialized)
        {
            return;
        }

        try
        {
            LibraryManagedNetTypeRegistry.Initialize();
            _initialized = true;

            LibraryManagedNetTypeCatalog catalog = LibraryManagedNetTypeRegistry.Catalog;
            Log.Info("[LibraryOfRuinaLib.Multiplayer] Managed net protocol ready. "
                + $"messages={catalog.MessageCount} "
                + $"actions={catalog.ActionCount} "
                + $"excludedNonGameplay={catalog.ExcludedMessageCount}/{catalog.ExcludedActionCount} "
                + $"registry={catalog.Fingerprint} "
                + "(game-owned message type table left untouched)");
        }
        catch (Exception e)
        {
            Log.Error("[LibraryOfRuinaLib.Multiplayer] Managed net protocol initialization failed: " + e);
            throw;
        }
    }
}
