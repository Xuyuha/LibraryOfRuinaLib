#nullable enable
using System.Reflection;
using HarmonyLib;
using LibraryLib.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
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
}

// 托管动作解码失败必须终止对应连接；丢弃入队广播后继续运行会使动作编号永久错位。
[HarmonyPatch(typeof(NetHostGameService), nameof(NetHostGameService.OnPacketReceived))]
internal static class LibraryManagedNetHostActionDecodeFailurePatch
{
    [HarmonyFinalizer]
    private static Exception? Finalizer(
        NetHostGameService __instance,
        ulong senderId,
        Exception? __exception)
    {
        if (__exception is not LibraryManagedNetDecodeException decodeException)
        {
            return __exception;
        }

        Log.Error(
            $"[LibraryOfRuinaLib.Multiplayer] Disconnecting peer {senderId} after managed action decode failure: "
            + decodeException.Failure);
        __instance.DisconnectClient(senderId, NetError.InternalError, now: true);
        return null;
    }
}

[HarmonyPatch(typeof(NetClientGameService), nameof(NetClientGameService.OnPacketReceived))]
internal static class LibraryManagedNetClientActionDecodeFailurePatch
{
    [HarmonyFinalizer]
    private static Exception? Finalizer(
        NetClientGameService __instance,
        Exception? __exception)
    {
        if (__exception is not LibraryManagedNetDecodeException decodeException)
        {
            return __exception;
        }

        Log.Error(
            "[LibraryOfRuinaLib.Multiplayer] Disconnecting from host after managed action decode failure: "
            + decodeException.Failure);
        __instance.Disconnect(NetError.InternalError, now: true);
        return null;
    }
}
