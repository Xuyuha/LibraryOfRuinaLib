#nullable enable
using LibraryLib.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace LibraryLib.Patches;

/// <summary>
/// Builds a complete managed-message packet for the direct stable sender. The packet is
/// self-describing and never depends on the game's message-ID table: it starts with the
/// LoR envelope magic, so <see cref="LibraryManagedNetPacketDemuxPatch"/> can intercept it
/// on the receive entry points before the vanilla <c>NetMessageBus</c> runs.
///
/// Wire layout:
///   [8B magic][1B version][8B senderId][type key][4B payload bit length][payload bits]
/// </summary>
internal static class LibraryManagedNetMessageTransport
{
    internal static bool TrySerialize(
        ulong senderId,
        INetMessage message,
        out byte[] bytes,
        out int length)
    {
        bytes = [];
        length = 0;
        Type messageType = message.GetType();
        if (!LibraryManagedNetTypeRegistry.IsReady
            || !LibraryManagedNetTypeRegistry.Catalog.TryGetMessageKey(
                messageType,
                out LibraryManagedNetTypeKey key))
        {
            return false;
        }

        var payloadWriter = new PacketWriter();
        message.Serialize(payloadWriter);
        int payloadBitLength = payloadWriter.BitPosition;
        if (payloadBitLength < 0
            || (long)payloadBitLength
            > (long)LibraryManagedNetPayloadCodec.MaxPayloadBytes * 8L)
        {
            throw new InvalidOperationException(
                $"Managed message payload is too large: {payloadBitLength} bits.");
        }

        var writer = new PacketWriter();
        writer.WriteULong(LibraryManagedNetMessageEnvelope.Magic);
        writer.WriteByte(LibraryManagedNetMessageEnvelope.CurrentVersion);
        writer.WriteULong(senderId);
        LibraryManagedNetPayloadCodec.WriteKey(writer, key);
        writer.WriteInt(payloadBitLength);
        writer.WriteBytes(payloadWriter.Buffer, (payloadBitLength + 7) / 8);

        length = writer.BytePosition;
        bytes = writer.Buffer;
        return true;
    }
}
