#nullable enable
using HarmonyLib;
using LibraryLib.Multiplayer;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace LibraryLib.Patches;

[HarmonyPatch(typeof(MessageTypes), nameof(MessageTypes.ToId))]
internal static class LibraryManagedMessageTypeSafetyPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static bool Prefix(INetMessage message, ref int __result)
    {
        Type type = message.GetType();
        if (LibraryManagedNetTypeRegistry.IsVanillaMessage(type))
        {
            return true;
        }
        if (!LibraryManagedNetTypeRegistry.IsReady)
        {
            throw new InvalidOperationException(
                "Refusing positional serialization for mod message before the managed protocol is ready: "
                + type.FullName);
        }
        if (type == typeof(LibraryManagedNetMessageEnvelope))
        {
            throw new InvalidOperationException(
                "LibraryManagedNetMessageEnvelope cannot be sent directly.");
        }
        if (LibraryManagedNetTypeRegistry.Catalog.IsExcludedMessage(type))
        {
            throw new InvalidOperationException(
                "A mod with affects_gameplay=false attempted to send INetMessage "
                + type.FullName
                + ". Network-bearing mods must declare affects_gameplay=true.");
        }
        if (!LibraryManagedNetTypeRegistry.Catalog.TryGetMessageKey(
                type,
                out LibraryManagedNetTypeKey key))
        {
            throw new InvalidOperationException(
                "Refusing positional serialization for unregistered mod message type: "
                + type.FullName);
        }

        LibraryManagedNetMessageWriteContext.Begin(type, key);
        __result = LibraryManagedNetTypeRegistry.EnvelopeMessageId;
        return false;
    }
}

[HarmonyPatch(typeof(ActionTypes), nameof(ActionTypes.ToId))]
internal static class LibraryManagedActionTypeSafetyPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void Prefix(INetAction message)
    {
        Type type = message.GetType();
        if (LibraryManagedNetTypeRegistry.IsVanillaAction(type))
        {
            return;
        }
        if (!LibraryManagedNetTypeRegistry.IsReady)
        {
            throw new InvalidOperationException(
                "Refusing positional serialization for mod action before the managed protocol is ready: "
                + type.FullName);
        }
        if (LibraryManagedNetTypeRegistry.Catalog.IsExcludedAction(type))
        {
            throw new InvalidOperationException(
                "A mod with affects_gameplay=false attempted to send INetAction "
                + type.FullName
                + ". Network-bearing mods must declare affects_gameplay=true.");
        }
        if (!LibraryManagedNetTypeRegistry.Catalog.TryGetActionKey(type, out _))
        {
            throw new InvalidOperationException(
                "Refusing positional serialization for unregistered mod action type: "
                + type.FullName);
        }

        throw new InvalidOperationException(
            "Mod action must be encoded through LibraryManagedNetActionCodec v2: "
            + type.FullName);
    }
}
