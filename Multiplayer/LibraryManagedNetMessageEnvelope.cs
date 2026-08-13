#nullable enable
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace LibraryLib.Multiplayer;

internal sealed class LibraryManagedNetMessageEnvelope : INetMessage
{
    internal const ulong Magic = 0x4C_4F_52_4D_53_47_4E_56UL; // VNGSMROL
    internal const byte CurrentVersion = 1;

    public INetMessage? InnerMessage { get; private set; }

    public bool ShouldBroadcast => InnerMessage?.ShouldBroadcast ?? false;

    public NetTransferMode Mode => InnerMessage?.Mode ?? NetTransferMode.Reliable;

    public LogLevel LogLevel => InnerMessage?.LogLevel ?? LogLevel.VeryDebug;

    public bool ShouldBuffer => InnerMessage?.ShouldBuffer ?? true;

    public void Serialize(PacketWriter writer) =>
        throw new InvalidOperationException(
            "The managed message envelope is emitted by the wire codec and cannot be sent directly.");

    public void Deserialize(PacketReader reader)
    {
        if (!LibraryManagedNetPayloadCodec.HasRemainingBytes(reader, sizeof(ulong) + 1))
        {
            throw DecodeException(
                "truncated_message_header",
                "Managed message ended before its magic and protocol version.");
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
        long availableBits = (long)reader.Buffer.Length * 8L - reader.BitPosition;
        long paddingBits = availableBits - payloadBitLength;
        if (payloadBitLength < 0 || payloadBitLength > maximumPayloadBits)
        {
            throw DecodeException(
                "invalid_message_payload_length",
                $"Managed message payload bit length {payloadBitLength} is outside the allowed range.");
        }
        if (paddingBits is < 0 or > 7)
        {
            throw DecodeException(
                paddingBits < 0
                    ? "truncated_message_payload"
                    : "forged_message_payload_length",
                $"Managed message declared {payloadBitLength} payload bits with "
                + $"{availableBits} bits remaining in the packet.");
        }

        int payloadByteLength = (payloadBitLength + 7) / 8;
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
