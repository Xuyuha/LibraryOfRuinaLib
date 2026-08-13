#nullable enable
using System.Buffers.Binary;
using System.Reflection;
using HarmonyLib;
using LibraryLib.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace LibraryLib.Patches;

internal static class LibraryManagedNetMessageWriteContext
{
    [ThreadStatic]
    private static PendingMessage? _pending;

    public static void Begin(Type messageType, LibraryManagedNetTypeKey key)
    {
        if (_pending.HasValue)
        {
            throw new InvalidOperationException(
                "Previous managed message serialization context was not consumed: "
                + _pending.Value.MessageType.FullName + ".");
        }

        _pending = new PendingMessage(messageType, key);
    }

    public static bool TryTake(
        Type messageType,
        out LibraryManagedNetTypeKey key)
    {
        PendingMessage? pending = _pending;
        if (!pending.HasValue)
        {
            key = default;
            return false;
        }

        _pending = null;
        if (pending.Value.MessageType != messageType)
        {
            throw new InvalidOperationException(
                "Managed message serialization context expected "
                + pending.Value.MessageType.FullName
                + " but received "
                + messageType.FullName
                + ".");
        }

        key = pending.Value.Key;
        return true;
    }

    private readonly record struct PendingMessage(
        Type MessageType,
        LibraryManagedNetTypeKey Key);
}

internal static class LibraryManagedNetMessageSerializePatch
{
    internal readonly record struct WriteState(
        bool IsManaged,
        int PayloadLengthByteOffset,
        int PayloadStartBit);

    internal static void Prefix(
        object __instance,
        PacketWriter writer,
        out WriteState __state)
    {
        Type messageType = __instance.GetType();
        if (!LibraryManagedNetMessageWriteContext.TryTake(
                messageType,
                out LibraryManagedNetTypeKey key))
        {
            __state = default;
            return;
        }
        if (writer.BitPosition % 8 != 0)
        {
            throw new InvalidOperationException(
                "Managed message envelope must begin on a byte boundary.");
        }

        writer.WriteULong(LibraryManagedNetMessageEnvelope.Magic);
        writer.WriteByte(LibraryManagedNetMessageEnvelope.CurrentVersion);
        LibraryManagedNetPayloadCodec.WriteKey(writer, key);
        int payloadLengthByteOffset = writer.BytePosition;
        writer.WriteInt(0);
        __state = new WriteState(
            true,
            payloadLengthByteOffset,
            writer.BitPosition);
    }

    internal static Exception? Finalizer(
        PacketWriter writer,
        WriteState __state,
        Exception? __exception)
    {
        if (!__state.IsManaged || __exception != null)
        {
            return __exception;
        }

        int payloadBitLength = writer.BitPosition - __state.PayloadStartBit;
        if (payloadBitLength < 0
            || (long)payloadBitLength
            > (long)LibraryManagedNetPayloadCodec.MaxPayloadBytes * 8L)
        {
            throw new InvalidOperationException(
                $"Managed message payload is too large: {payloadBitLength} bits.");
        }

        BinaryPrimitives.WriteInt32LittleEndian(
            writer.Buffer.AsSpan(__state.PayloadLengthByteOffset, sizeof(int)),
            payloadBitLength);
        return null;
    }
}

internal static class LibraryManagedNetMessagePatchInstaller
{
    private static bool _installed;

    public static int Install(Harmony harmony) =>
        Install(
            harmony,
            LibraryManagedNetTypeRegistry.Catalog.GameplayMessageTypesInWireOrder);

    internal static int Install(Harmony harmony, IEnumerable<Type> messageTypes)
    {
        if (_installed)
        {
            return 0;
        }

        MethodInfo prefixMethod = AccessTools.Method(
            typeof(LibraryManagedNetMessageSerializePatch),
            nameof(LibraryManagedNetMessageSerializePatch.Prefix));
        MethodInfo finalizerMethod = AccessTools.Method(
            typeof(LibraryManagedNetMessageSerializePatch),
            nameof(LibraryManagedNetMessageSerializePatch.Finalizer));
        var prefix = new HarmonyMethod(prefixMethod) { priority = Priority.First };
        var finalizer = new HarmonyMethod(finalizerMethod) { priority = Priority.Last };

        int patchedCount = 0;
        foreach (MethodInfo serializeMethod in messageTypes
                     .Select(GetSerializeMethod)
                     .Distinct())
        {
            harmony.Patch(serializeMethod, prefix: prefix, finalizer: finalizer);
            patchedCount++;
        }

        _installed = true;
        return patchedCount;
    }

    private static MethodInfo GetSerializeMethod(Type messageType)
    {
        InterfaceMapping map = messageType.GetInterfaceMap(typeof(IPacketSerializable));
        for (int index = 0; index < map.InterfaceMethods.Length; index++)
        {
            if (map.InterfaceMethods[index].Name == nameof(IPacketSerializable.Serialize))
            {
                return map.TargetMethods[index].GetBaseDefinition();
            }
        }

        throw new MissingMethodException(
            messageType.FullName,
            nameof(IPacketSerializable.Serialize));
    }
}
