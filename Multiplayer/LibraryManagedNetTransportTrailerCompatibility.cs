#nullable enable
using System.Buffers.Binary;

namespace LibraryLib.Multiplayer;

internal static class LibraryManagedNetTransportTrailerCompatibility
{
    private const ushort RitsuLibTrailerVersion = 1;
    private const ushort RitsuLibSidecarSupportedFlag = 1 << 0;
    private const int VersionSize = sizeof(ushort);
    private const int FlagsSize = sizeof(ushort);
    private const int PayloadLengthSize = sizeof(uint);
    private const int PayloadCrc32Size = sizeof(uint);
    private static ReadOnlySpan<byte> RitsuLibHeader => "STS2RitsuLib"u8;
    private static ReadOnlySpan<byte> RitsuLibFooter => "biLustiR2STS"u8;

    private static int RitsuLibTrailerSize =>
        RitsuLibHeader.Length
        + VersionSize
        + FlagsSize
        + PayloadLengthSize
        + PayloadCrc32Size
        + RitsuLibFooter.Length;

    public static bool IsRecognizedTrailingData(byte[] packetBytes, int payloadEndBit)
    {
        if (payloadEndBit < 0)
        {
            return false;
        }

        int trailerStart = (payloadEndBit + 7) / 8;
        if (trailerStart < 0
            || packetBytes.Length - trailerStart != RitsuLibTrailerSize)
        {
            return false;
        }

        ReadOnlySpan<byte> trailer = packetBytes.AsSpan(trailerStart);
        int offset = 0;
        if (!trailer[..RitsuLibHeader.Length].SequenceEqual(RitsuLibHeader))
        {
            return false;
        }

        offset += RitsuLibHeader.Length;
        ushort version = BinaryPrimitives.ReadUInt16BigEndian(trailer[offset..]);
        offset += VersionSize;
        if (version != RitsuLibTrailerVersion)
        {
            return false;
        }

        ushort flags = BinaryPrimitives.ReadUInt16BigEndian(trailer[offset..]);
        offset += FlagsSize;
        if ((flags & RitsuLibSidecarSupportedFlag) == 0)
        {
            return false;
        }

        uint declaredPayloadLength = BinaryPrimitives.ReadUInt32BigEndian(trailer[offset..]);
        offset += PayloadLengthSize;
        if (declaredPayloadLength != (uint)trailerStart)
        {
            return false;
        }

        uint expectedCrc32 = BinaryPrimitives.ReadUInt32BigEndian(trailer[offset..]);
        offset += PayloadCrc32Size;
        if (!trailer[offset..].SequenceEqual(RitsuLibFooter))
        {
            return false;
        }

        return ComputeCrc32(packetBytes.AsSpan(0, trailerStart)) == expectedCrc32;
    }

    private static uint ComputeCrc32(ReadOnlySpan<byte> bytes)
    {
        uint crc = uint.MaxValue;
        foreach (byte value in bytes)
        {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0
                    ? (crc >> 1) ^ 0xEDB88320U
                    : crc >> 1;
            }
        }

        return ~crc;
    }
}
