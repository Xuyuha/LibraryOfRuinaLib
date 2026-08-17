#nullable enable
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace LibraryLib.Multiplayer;

/// <summary>
/// Decoder container for the LoR managed message wire format. The envelope deliberately
/// does not implement <see cref="INetMessage"/>: it never occupies a slot in the
/// game-owned message-ID table and can only travel through the magic-prefixed direct
/// transport path demuxed by <c>LibraryManagedNetPacketDemuxPatch</c>.
/// </summary>
internal sealed class LibraryManagedNetMessageEnvelope
{
    internal const ulong Magic = 0x4C_4F_52_4D_53_47_4E_56UL; // VNGSMROL
    internal const byte CurrentVersion = 1;

    public INetMessage? InnerMessage { get; private set; }

    /// <summary>
    /// The sender id carried inside the envelope. On the receiving end this is the id
    /// the inner message is dispatched with; the transport-level peer id is only used
    /// for host-side rebroadcast routing.
    /// </summary>
    public ulong SenderId { get; private set; }

    public void Deserialize(PacketReader reader)
    {
        if (!LibraryManagedNetPayloadCodec.HasRemainingBytes(reader, sizeof(ulong) + sizeof(ulong) + 1))
        {
            throw DecodeException(
                "truncated_message_header",
                "Managed message ended before its magic, sender id and protocol version.");
        }

        ulong magic = reader.ReadULong();
        if (magic != Magic)
        {
            throw DecodeException(
                "invalid_message_magic",
                $"Managed message used unknown magic 0x{magic:X16}.");
        }

        byte version = reader.ReadByte();
        if (version != CurrentVersion)
        {
            throw DecodeException(
                "unsupported_message_version",
                $"Managed message protocol version {version} is not supported.");
        }

        SenderId = reader.ReadULong();

        if (!LibraryManagedNetPayloadCodec.TryReadKey(
                reader,
                out LibraryManagedNetTypeKey key,
                out LibraryManagedNetDecodeFailure? keyFailure))
        {
            throw new LibraryManagedNetDecodeException(
                keyFailure ?? new LibraryManagedNetDecodeFailure(
                    "message_type_key_decode_failed",
                    "Managed message type key could not be decoded."));
        }
        if (!LibraryManagedNetTypeRegistry.Catalog.TryResolveMessage(
                key,
                out Type? messageType)
            || messageType == null)
        {
            throw DecodeException(
                "unknown_message_type_key",
                "No local managed message type is registered for " + key + ".");
        }
        if (!LibraryManagedNetPayloadCodec.HasRemainingBytes(reader, sizeof(int)))
        {
            throw DecodeException(
                "truncated_message_payload_length",
                "Managed message ended before its payload bit length.");
        }

        int payloadBitLength = reader.ReadInt();
        long maximumPayloadBits = (long)LibraryManagedNetPayloadCodec.MaxPayloadBytes * 8L;
        if (payloadBitLength < 0 || payloadBitLength > maximumPayloadBits)
        {
            throw DecodeException(
                "invalid_message_payload_length",
                $"Managed message payload bit length {payloadBitLength} is outside the allowed range.");
        }

        int payloadByteLength = (payloadBitLength + 7) / 8;
        if (!LibraryManagedNetPayloadCodec.HasRemainingBytes(reader, payloadByteLength))
        {
            throw DecodeException(
                "truncated_message_payload",
                $"Managed message declared {payloadBitLength} payload bits "
                + "but the packet ended before the payload.");
        }

        byte[] payload = new byte[payloadByteLength];
        reader.ReadBytes(payload, payloadByteLength);

        try
        {
            if (Activator.CreateInstance(messageType, nonPublic: true) is not INetMessage message)
            {
                throw DecodeException(
                    "message_activation_failed",
                    "Could not create managed message type " + key + ".");
            }

            var payloadReader = new PacketReader();
            payloadReader.Reset(payload);
            message.Deserialize(payloadReader);
            if (payloadReader.BitPosition != payloadBitLength)
            {
                throw DecodeException(
                    "message_payload_consumption_mismatch",
                    key + " consumed " + payloadReader.BitPosition
                    + " of " + payloadBitLength + " declared payload bits.");
            }

            InnerMessage = message;
        }
        catch (LibraryManagedNetDecodeException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw DecodeException(
                "managed_message_deserialize_failed",
                key + ": " + exception.GetType().Name + ": " + exception.Message);
        }
    }

    private static LibraryManagedNetDecodeException DecodeException(
        string code,
        string detail) =>
        new(new LibraryManagedNetDecodeFailure(code, detail));
}
