#nullable enable
using System.Reflection;
using HarmonyLib;
using LibraryLib.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace LibraryLib.Patches;

internal static class LibraryManagedNetDiagnostics
{
    private static readonly HashSet<string> WarnedFailures = new(StringComparer.Ordinal);
    private static readonly object Sync = new();
    private static Action<string> _warningSink = static message => Log.Warn(message);

    public static void WarnOnce(LibraryManagedNetDecodeFailure failure)
    {
        string key = failure.Code;
        lock (Sync)
        {
            if (!WarnedFailures.Add(key))
            {
                return;
            }
        }

        _warningSink("[LibraryOfRuinaLib.Multiplayer] Dropped managed network payload. " + failure);
    }

    internal static void SetWarningSinkForTesting(Action<string> warningSink) =>
        _warningSink = warningSink;
}

[HarmonyPatch]
internal static class LibraryManagedNetDecodeSafetyPatch
{
    [HarmonyTargetMethod]
    private static MethodBase TargetMethod() =>
        AccessTools.GetDeclaredMethods(typeof(NetMessageBus)).Single(static method =>
            method.Name == nameof(NetMessageBus.TryDeserializeMessage));

    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static bool Prefix(
        byte[] packetBytes,
        ref INetMessage? message,
        ref ulong? overrideSenderId,
        ref bool __result)
    {
        if (!LibraryManagedNetActionTransport.TryDecodeManagedCarrier(
                packetBytes,
                out INetMessage? decoded,
                out ulong senderId))
        {
            return true;
        }

        message = decoded;
        overrideSenderId = senderId;
        __result = true;
        return false;
    }

    [HarmonyFinalizer]
    private static Exception? Finalizer(
        Exception? __exception,
        byte[] packetBytes,
        ref INetMessage? message,
        ref ulong? overrideSenderId,
        ref bool __result)
    {
        LibraryManagedNetDecodeFailure? failure = __exception switch
        {
            null => null,
            LibraryManagedNetDecodeException managedException => managedException.Failure,
            _ when IsManagedActionCarrierPacket(packetBytes, out Type? carrierType) =>
                new LibraryManagedNetDecodeFailure(
                    "malformed_action_carrier",
                    (carrierType?.FullName ?? "unknown")
                    + ": "
                    + __exception.GetType().Name
                    + ": "
                    + __exception.Message),
            _ => null,
        };
        if (!failure.HasValue)
        {
            return __exception;
        }

        message = null;
        overrideSenderId = null;
        __result = false;
        LibraryManagedNetDiagnostics.WarnOnce(failure.Value);
        return null;
    }

    private static bool IsManagedActionCarrierPacket(
        byte[] packetBytes,
        out Type? messageType)
    {
        messageType = null;
        if (!LibraryManagedNetTypeRegistry.IsReady
            || packetBytes.Length == 0
            || !MessageTypes.TryGetMessageType(packetBytes[0], out messageType)
            || (messageType != typeof(RequestEnqueueActionMessage)
                && messageType != typeof(ActionEnqueuedMessage)))
        {
            return false;
        }

        try
        {
            return LibraryManagedNetActionTransport.TryDecodeManagedCarrier(
                packetBytes,
                out _,
                out _);
        }
        catch (LibraryManagedNetDecodeException)
        {
            return true;
        }
        catch
        {
            return false;
        }
    }
}
