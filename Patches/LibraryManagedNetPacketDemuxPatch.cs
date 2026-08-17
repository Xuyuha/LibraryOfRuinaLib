#nullable enable
using System.Buffers.Binary;
using HarmonyLib;
using LibraryLib.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace LibraryLib.Patches;

/// <summary>
/// Demuxes LibraryOfRuinaLib managed message envelopes on the host and client receive
/// entry points, before the vanilla <see cref="NetMessageBus"/> ever sees the packet.
///
/// This mirrors RitsuLib's receive pipeline: packets are only claimed when they start
/// with the LoR envelope magic; everything else is returned untouched to the vanilla
/// deserializer. Because the managed wire format is self-describing, the game-owned
/// message-ID table is never read, written, or rebuilt by this code path.
/// </summary>
internal static class LibraryManagedNetPacketDemux
{
    private static AccessTools.FieldRef<NetHostGameService, NetMessageBus>? _hostBusRef =
        TryCreateBusRef<NetHostGameService>();

    private static AccessTools.FieldRef<NetClientGameService, NetMessageBus>? _clientBusRef =
        TryCreateBusRef<NetClientGameService>();

    /// <summary>
    /// Returns true when the vanilla deserializer must not process this packet, i.e.
    /// when the packet was recognized as a managed LoR envelope (successfully decoded,
    /// dropped after a decode failure, or claimed for an unknown peer).
    /// </summary>
    internal static bool TryHandleHost(
        NetHostGameService host,
        ulong senderId,
        byte[] packetBytes,
        NetTransferMode mode,
        int channel)
    {
        if (!StartsWithEnvelopeMagic(packetBytes))
        {
            return false;
        }

        // Mirror the vanilla behavior: packets from unknown peers are not processed.
        bool knownPeer = false;
        foreach (NetClientData peer in host.ConnectedPeers)
        {
            if (peer.peerId == senderId)
            {
                knownPeer = true;
                break;
            }
        }
        if (!knownPeer)
        {
            return true;
        }

        if (!TryDecode(packetBytes, out INetMessage inner, out ulong effectiveSenderId))
        {
            return true;
        }

        if (inner.ShouldBroadcast && host.NetHost != null)
        {
            foreach (NetClientData peer in host.ConnectedPeers)
            {
                if (peer.readyForBroadcasting && peer.peerId != senderId)
                {
                    host.NetHost.SendMessageToClient(
                        peer.peerId,
                        packetBytes,
                        packetBytes.Length,
                        mode,
                        channel);
                }
            }
        }

        NetMessageBus? bus = _hostBusRef?.Invoke(host);
        if (bus == null)
        {
            Log.Error(
                "[LibraryOfRuinaLib.Multiplayer] Managed envelope received but the host "
                + "message bus is unavailable; message dropped.");
            return true;
        }

        bus.SendMessageToAllHandlers(inner, effectiveSenderId);
        return true;
    }

    internal static bool TryHandleClient(
        NetClientGameService client,
        ulong senderId,
        byte[] packetBytes,
        NetTransferMode mode,
        int channel)
    {
        if (!StartsWithEnvelopeMagic(packetBytes))
        {
            return false;
        }

        if (!TryDecode(packetBytes, out INetMessage inner, out ulong effectiveSenderId))
        {
            return true;
        }

        NetMessageBus? bus = _clientBusRef?.Invoke(client);
        if (bus == null)
        {
            Log.Error(
                "[LibraryOfRuinaLib.Multiplayer] Managed envelope received but the client "
                + "message bus is unavailable; message dropped.");
            return true;
        }

        bus.SendMessageToAllHandlers(inner, effectiveSenderId);
        return true;
    }

    private static bool StartsWithEnvelopeMagic(byte[] packetBytes) =>
        LibraryManagedNetTypeRegistry.IsReady
        && packetBytes.Length >= sizeof(ulong)
        && BinaryPrimitives.ReadUInt64LittleEndian(packetBytes)
        == LibraryManagedNetMessageEnvelope.Magic;

    private static bool TryDecode(
        byte[] packetBytes,
        out INetMessage inner,
        out ulong effectiveSenderId)
    {
        inner = null!;
        effectiveSenderId = 0UL;
        try
        {
            var reader = new PacketReader();
            reader.Reset(packetBytes);
            var envelope = new LibraryManagedNetMessageEnvelope();
            envelope.Deserialize(reader);
            if (envelope.InnerMessage == null)
            {
                LibraryManagedNetDiagnostics.WarnOnce(new LibraryManagedNetDecodeFailure(
                    "empty_message_envelope",
                    "Managed message envelope completed without an inner message."));
                return false;
            }

            inner = envelope.InnerMessage;
            effectiveSenderId = envelope.SenderId;
            return true;
        }
        catch (LibraryManagedNetDecodeException e)
        {
            LibraryManagedNetDiagnostics.WarnOnce(e.Failure);
            return false;
        }
        catch (Exception e)
        {
            LibraryManagedNetDiagnostics.WarnOnce(new LibraryManagedNetDecodeFailure(
                "malformed_message_envelope",
                e.GetType().Name + ": " + e.Message));
            return false;
        }
    }

    private static AccessTools.FieldRef<TService, NetMessageBus>? TryCreateBusRef<TService>()
        where TService : class
    {
        try
        {
            return AccessTools.FieldRefAccess<TService, NetMessageBus>("_messageBus");
        }
        catch (Exception)
        {
            return null;
        }
    }
}

[HarmonyPatch(typeof(NetHostGameService), nameof(NetHostGameService.OnPacketReceived))]
internal static class LibraryManagedNetHostPacketDemuxPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static bool Prefix(
        NetHostGameService __instance,
        ulong senderId,
        byte[] packetBytes,
        NetTransferMode mode,
        int channel) =>
        !LibraryManagedNetPacketDemux.TryHandleHost(
            __instance,
            senderId,
            packetBytes,
            mode,
            channel);
}

[HarmonyPatch(typeof(NetClientGameService), nameof(NetClientGameService.OnPacketReceived))]
internal static class LibraryManagedNetClientPacketDemuxPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static bool Prefix(
        NetClientGameService __instance,
        ulong senderId,
        byte[] packetBytes,
        NetTransferMode mode,
        int channel) =>
        !LibraryManagedNetPacketDemux.TryHandleClient(
            __instance,
            senderId,
            packetBytes,
            mode,
            channel);
}
