#nullable enable
using LibraryLib.Multiplayer;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace LibraryLib.Utils.RelicRightClick;

internal static class LibraryManagedNetActionCodec
{
    internal const ulong Magic = 0x4C_4F_52_41_43_54_4E_41UL; // ANTCAROL
    internal const byte LegacyVersion = 1;
    internal const byte CurrentVersion = 2;

    private enum LibraryManagedActionKind : byte
    {
        RelicRightClick = 1
    }

    public static bool CanWrite(INetAction action)
    {
        Type actionType = action.GetType();
        if (LibraryManagedNetTypeRegistry.IsVanillaAction(actionType))
        {
            return false;
        }
        if (!LibraryManagedNetTypeRegistry.IsReady)
        {
            throw new InvalidOperationException(
                "Refusing positional serialization for mod action before the managed protocol is ready: "
                + actionType.FullName);
        }
        if (LibraryManagedNetTypeRegistry.Catalog.IsExcludedAction(actionType))
        {
            throw new InvalidOperationException(
                "A mod with affects_gameplay=false attempted to send INetAction "
                + actionType.FullName
                + ". Network-bearing mods must declare affects_gameplay=true.");
        }
        if (!LibraryManagedNetTypeRegistry.Catalog.TryGetActionKey(actionType, out _))
        {
            throw new InvalidOperationException(
                "Refusing positional serialization for unregistered mod action type: "
                + actionType.FullName);
        }

        return true;
    }

    public static bool TryWrite(PacketWriter writer, INetAction action) =>
        TryWrite(writer, action, LibraryManagedNetTypeRegistry.Catalog);

    internal static bool TryWrite(
        PacketWriter writer,
        INetAction action,
        LibraryManagedNetTypeCatalog catalog)
    {
        if (catalog.IsVanillaAction(action.GetType()))
        {
            return false;
        }
        if (catalog.IsExcludedAction(action.GetType()))
        {
            throw new InvalidOperationException(
                "A mod with affects_gameplay=false attempted to send INetAction "
                + action.GetType().FullName
                + ". Network-bearing mods must declare affects_gameplay=true.");
        }
        if (!catalog.TryGetActionKey(action.GetType(), out LibraryManagedNetTypeKey key))
        {
            throw new InvalidOperationException(
                "Refusing positional serialization for unregistered mod action type: "
                + action.GetType().FullName);
        }

        writer.WriteULong(Magic);
        writer.WriteByte(CurrentVersion);
        LibraryManagedNetPayloadCodec.Write(writer, key, action);
        return true;
    }

    public static bool NextPayloadIsManagedAction(PacketReader reader)
    {
        if (!LibraryManagedNetPayloadCodec.HasRemainingBytes(reader, sizeof(ulong)))
        {
            return false;
        }
        if (reader.ReadULong() != Magic)
        {
            return false;
        }
        if (!LibraryManagedNetPayloadCodec.HasRemainingBytes(reader, 1))
        {
            throw DecodeException(
                "truncated_action_header",
                "Managed action packet ended before its version.");
        }

        byte version = reader.ReadByte();
        if (version is not LegacyVersion and not CurrentVersion)
        {
            throw DecodeException(
                "unsupported_action_version",
                $"Managed action version {version} is not supported.");
        }

        return true;
    }

    public static INetAction Read(PacketReader reader) =>
        Read(reader, LibraryManagedNetTypeRegistry.Catalog);

    internal static INetAction Read(
        PacketReader reader,
        LibraryManagedNetTypeCatalog catalog)
    {
        if (!LibraryManagedNetPayloadCodec.HasRemainingBytes(reader, sizeof(ulong) + 1))
        {
            throw DecodeException(
                "truncated_action_header",
                "Managed action packet ended before its magic and version.");
        }

        ulong magic = reader.ReadULong();
        if (magic != Magic)
        {
            throw DecodeException(
                "invalid_action_magic",
                $"Managed action packet used unknown magic 0x{magic:X16}.");
        }

        byte version = reader.ReadByte();
        return version switch
        {
            LegacyVersion => ReadLegacy(reader),
            CurrentVersion => ReadCurrent(reader, catalog),
            _ => throw DecodeException(
                "unsupported_action_version",
                $"Managed action version {version} is not supported.")
        };
    }

    private static INetAction ReadLegacy(PacketReader reader)
    {
        if (!LibraryManagedNetPayloadCodec.HasRemainingBytes(reader, 1))
        {
            throw DecodeException(
                "truncated_legacy_action",
                "Legacy managed action packet ended before its action kind.");
        }

        var kind = (LibraryManagedActionKind)reader.ReadByte();
        try
        {
            return kind switch
            {
                LibraryManagedActionKind.RelicRightClick => ReadRelicRightClick(reader),
                _ => throw DecodeException(
                    "unknown_legacy_action_kind",
                    "Unknown legacy managed action kind: " + kind)
            };
        }
        catch (LibraryManagedNetDecodeException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw DecodeException(
                "legacy_action_deserialize_failed",
                exception.GetType().Name + ": " + exception.Message);
        }
    }

    private static INetAction ReadCurrent(
        PacketReader reader,
        LibraryManagedNetTypeCatalog catalog)
    {
        if (!LibraryManagedNetPayloadCodec.TryRead(
                reader,
                out LibraryManagedNetTypeKey key,
                out byte[] payload,
                out LibraryManagedNetDecodeFailure? failure))
        {
            throw new LibraryManagedNetDecodeException(
                failure ?? new LibraryManagedNetDecodeFailure(
                    "managed_action_decode_failed",
                    "Managed action payload could not be decoded."));
        }
        if (!catalog.TryResolveAction(key, out Type? actionType) || actionType == null)
        {
            throw DecodeException(
                "unknown_action_type_key",
                "No local managed action type is registered for " + key + ".");
        }

        try
        {
            if (Activator.CreateInstance(actionType, nonPublic: true) is not INetAction action)
            {
                throw DecodeException(
                    "action_activation_failed",
                    "Could not create managed action type " + key + ".");
            }

            var payloadReader = new PacketReader();
            payloadReader.Reset(payload);
            action.Deserialize(payloadReader);
            int remainingBits = payload.Length * 8 - payloadReader.BitPosition;
            if (remainingBits is < 0 or > 7)
            {
                throw DecodeException(
                    "action_payload_consumption_mismatch",
                    key + " left " + remainingBits
                    + " bits outside normal byte padding.");
            }
            return action;
        }
        catch (LibraryManagedNetDecodeException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw DecodeException(
                "managed_action_deserialize_failed",
                key + ": " + exception.GetType().Name + ": " + exception.Message);
        }
    }

    private static NetLibraryRelicRightClickAction ReadRelicRightClick(PacketReader reader)
    {
        var action = new NetLibraryRelicRightClickAction();
        action.Deserialize(reader);
        return action;
    }

    private static LibraryManagedNetDecodeException DecodeException(
        string code,
        string detail) =>
        new(new LibraryManagedNetDecodeFailure(code, detail));
}
