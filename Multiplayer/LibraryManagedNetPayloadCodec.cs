#nullable enable
using System.Text;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace LibraryLib.Multiplayer;

internal readonly record struct LibraryManagedNetDecodeFailure(
    string Code,
    string Detail)
{
    public override string ToString() => Code + ": " + Detail;
}

internal sealed class LibraryManagedNetDecodeException : InvalidOperationException
{
    public LibraryManagedNetDecodeFailure Failure { get; }

    public LibraryManagedNetDecodeException(LibraryManagedNetDecodeFailure failure)
        : base(failure.ToString())
    {
        Failure = failure;
    }
}

internal static class LibraryManagedNetPayloadCodec
{
    private const int MaxTypeKeyComponentBytes = 4096;
    internal const int MaxPayloadBytes = 4 * 1024 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static void Write(
        PacketWriter writer,
        LibraryManagedNetTypeKey key,
        IPacketSerializable payload)
    {
        WriteKey(writer, key);

        var payloadWriter = new PacketWriter { WarnOnGrow = false };
        payload.Serialize(payloadWriter);
        payloadWriter.ZeroByteRemainder();
        int payloadLength = payloadWriter.BytePosition;
        if (payloadLength > MaxPayloadBytes)
        {
            throw new InvalidOperationException(
                $"Managed network payload is too large: {payloadLength} bytes.");
        }

        writer.WriteInt(payloadLength);
        writer.WriteBytes(payloadWriter.Buffer, payloadLength);
    }

    public static bool TryRead(
        PacketReader reader,
        out LibraryManagedNetTypeKey key,
        out byte[] payload,
        out LibraryManagedNetDecodeFailure? failure)
    {
        key = default;
        payload = [];
        failure = null;

        try
        {
            if (!TryReadKey(reader, out key, out failure))
            {
                return false;
            }

            if (!HasRemainingBytes(reader, sizeof(int)))
            {
                failure = new LibraryManagedNetDecodeFailure(
                    "truncated_payload_length",
                    "Managed network packet ended before its payload length.");
                return false;
            }

            int payloadLength = reader.ReadInt();
            if (payloadLength < 0 || payloadLength > MaxPayloadBytes)
            {
                failure = new LibraryManagedNetDecodeFailure(
                    "invalid_payload_length",
                    $"Managed network payload length {payloadLength} is outside the allowed range.");
                return false;
            }
            if (!HasRemainingBytes(reader, payloadLength))
            {
                failure = new LibraryManagedNetDecodeFailure(
                    "truncated_payload",
                    $"Managed network packet declared {payloadLength} payload bytes but ended early.");
                return false;
            }

            payload = new byte[payloadLength];
            reader.ReadBytes(payload, payloadLength);
            return true;
        }
        catch (Exception exception)
        {
            failure = new LibraryManagedNetDecodeFailure(
                "malformed_managed_payload",
                exception.GetType().Name + ": " + exception.Message);
            return false;
        }
    }

    public static void WriteKey(PacketWriter writer, LibraryManagedNetTypeKey key)
    {
        WriteBoundedString(writer, key.ModId);
        WriteBoundedString(writer, key.AssemblyName);
        WriteBoundedString(writer, key.TypeFullName);
    }

    public static bool TryReadKey(
        PacketReader reader,
        out LibraryManagedNetTypeKey key,
        out LibraryManagedNetDecodeFailure? failure)
    {
        key = default;
        if (!TryReadBoundedString(reader, "mod ID", out string modId, out failure)
            || !TryReadBoundedString(reader, "assembly name", out string assemblyName, out failure)
            || !TryReadBoundedString(reader, "type name", out string typeFullName, out failure))
        {
            return false;
        }

        key = new LibraryManagedNetTypeKey(modId, assemblyName, typeFullName);
        return true;
    }

    public static bool HasRemainingBytes(PacketReader reader, int byteCount)
    {
        if (byteCount < 0)
        {
            return false;
        }

        long requiredBits = (long)byteCount * 8L;
        long availableBits = (long)reader.Buffer.Length * 8L - reader.BitPosition;
        return requiredBits <= availableBits;
    }

    private static void WriteBoundedString(PacketWriter writer, string value)
    {
        byte[] bytes = StrictUtf8.GetBytes(value);
        if (bytes.Length == 0 || bytes.Length > MaxTypeKeyComponentBytes)
        {
            throw new InvalidOperationException(
                $"Managed network type-key component has invalid UTF-8 length {bytes.Length}.");
        }

        writer.WriteInt(bytes.Length);
        writer.WriteBytes(bytes, bytes.Length);
    }

    private static bool TryReadBoundedString(
        PacketReader reader,
        string componentName,
        out string value,
        out LibraryManagedNetDecodeFailure? failure)
    {
        value = string.Empty;
        failure = null;
        if (!HasRemainingBytes(reader, sizeof(int)))
        {
            failure = new LibraryManagedNetDecodeFailure(
                "truncated_type_key",
                $"Managed network packet ended before the {componentName} length.");
            return false;
        }

        int byteLength = reader.ReadInt();
        if (byteLength <= 0 || byteLength > MaxTypeKeyComponentBytes)
        {
            failure = new LibraryManagedNetDecodeFailure(
                "invalid_type_key",
                $"Managed network {componentName} length {byteLength} is outside the allowed range.");
            return false;
        }
        if (!HasRemainingBytes(reader, byteLength))
        {
            failure = new LibraryManagedNetDecodeFailure(
                "truncated_type_key",
                $"Managed network packet ended inside the {componentName}.");
            return false;
        }

        byte[] bytes = new byte[byteLength];
        reader.ReadBytes(bytes, byteLength);
        try
        {
            value = StrictUtf8.GetString(bytes);
            return true;
        }
        catch (DecoderFallbackException exception)
        {
            failure = new LibraryManagedNetDecodeFailure(
                "invalid_type_key_utf8",
                $"Managed network {componentName} is not valid UTF-8: {exception.Message}");
            return false;
        }
    }
}
