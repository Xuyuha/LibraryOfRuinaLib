#nullable enable
using LibraryLib.Patches;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace LibraryLib.Multiplayer;

/// <summary>
/// Sends a gameplay mod message through LibraryOfRuinaLib's stable keyed envelope
/// without depending on the message's value-type Serialize method being Harmony-patched.
/// Returns false when the message is not registered with the managed protocol or the
/// supplied network service cannot currently send it.
/// </summary>
public static class LibraryManagedNetMessageSender
{
    public static bool TrySend(
        INetGameService netService,
        INetMessage message,
        ulong? targetPeerId = null)
    {
        ArgumentNullException.ThrowIfNull(netService);
        ArgumentNullException.ThrowIfNull(message);
        if (!netService.IsConnected
            || !LibraryManagedNetMessageTransport.TrySerialize(
                netService.NetId,
                message,
                out byte[] bytes,
                out int length))
        {
            return false;
        }

        int channel = message.Mode.ToChannelId();
        if (netService is NetClientGameService client)
        {
            if (client.NetClient == null)
            {
                return false;
            }
            if (targetPeerId.HasValue
                && targetPeerId.Value != client.NetClient.HostNetId)
            {
                throw new NotSupportedException(
                    "A client can send managed messages only to its host.");
            }

            client.NetClient.SendMessageToHost(
                bytes,
                length,
                message.Mode,
                channel);
            return true;
        }

        if (netService is not NetHostGameService host || host.NetHost == null)
        {
            return false;
        }

        if (targetPeerId.HasValue)
        {
            host.NetHost.SendMessageToClient(
                targetPeerId.Value,
                bytes,
                length,
                message.Mode,
                channel);
            return true;
        }

        foreach (NetClientData peer in host.ConnectedPeers)
        {
            if (peer.readyForBroadcasting)
            {
                host.NetHost.SendMessageToClient(
                    peer.peerId,
                    bytes,
                    length,
                    message.Mode,
                    channel);
            }
        }

        return true;
    }
}
