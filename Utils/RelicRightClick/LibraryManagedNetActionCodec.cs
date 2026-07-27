#nullable enable
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace Library.Models;

internal static class LibraryManagedNetActionCodec
{
    private const ulong Magic = 0x4C_4F_52_41_43_54_4E_41; // ANTCAROL
    private const byte Version = 1;

    private enum LibraryManagedActionKind : byte
    {
        RelicRightClick = 1
    }

    public static bool CanWrite(INetAction action)
    {
        return action is NetLibraryRelicRightClickAction;
    }

    public static bool TryWrite(PacketWriter writer, INetAction action)
    {
        if (action is not NetLibraryRelicRightClickAction rightClick)
            return false;

        writer.WriteULong(Magic);
        writer.WriteByte(Version);
        writer.WriteByte((byte)LibraryManagedActionKind.RelicRightClick);
        rightClick.Serialize(writer);
        return true;
    }

    public static bool NextPayloadIsManagedAction(PacketReader reader)
    {
        try
        {
            return reader.ReadULong() == Magic
                && reader.ReadByte() == Version;
        }
        catch
        {
            return false;
        }
    }

    public static INetAction Read(PacketReader reader)
    {
        ulong magic = reader.ReadULong();
        if (magic != Magic)
            throw new InvalidOperationException("Malformed LibraryOfRuinaLib managed action header.");

        byte version = reader.ReadByte();
        if (version != Version)
            throw new InvalidOperationException("Unsupported LibraryOfRuinaLib managed action version: " + version);

        var kind = (LibraryManagedActionKind)reader.ReadByte();
        return kind switch
        {
            LibraryManagedActionKind.RelicRightClick => ReadRelicRightClick(reader),
            _ => throw new InvalidOperationException("Unknown LibraryOfRuinaLib managed action kind: " + kind)
        };
    }

    private static NetLibraryRelicRightClickAction ReadRelicRightClick(PacketReader reader)
    {
        var action = new NetLibraryRelicRightClickAction();
        action.Deserialize(reader);
        return action;
    }
}
