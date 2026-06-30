#nullable enable
using HarmonyLib;
using Library.Models;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Replay;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;

namespace Library.Patches;

internal static class LibraryManagedNetActionMessagePatch
{
    private static readonly AccessTools.FieldRef<PacketReader, int> BitPositionRef =
        AccessTools.FieldRefAccess<PacketReader, int>("<BitPosition>k__BackingField");

    public static PacketReader CreateProbeReader(PacketReader reader)
    {
        var probe = new PacketReader();
        probe.Reset(reader.Buffer);
        BitPositionRef(probe) = reader.BitPosition;
        return probe;
    }
}

[HarmonyPatch(typeof(RequestEnqueueActionMessage), nameof(RequestEnqueueActionMessage.Serialize), typeof(PacketWriter))]
internal static class LibraryManagedRequestEnqueueActionSerializePatch
{
    private static bool Prefix(RequestEnqueueActionMessage __instance, PacketWriter writer)
    {
        if (!LibraryManagedNetActionCodec.CanWrite(__instance.action))
            return true;

        writer.Write(__instance.location);
        LibraryManagedNetActionCodec.TryWrite(writer, __instance.action);
        return false;
    }
}

[HarmonyPatch(typeof(RequestEnqueueActionMessage), nameof(RequestEnqueueActionMessage.Deserialize), typeof(PacketReader))]
internal static class LibraryManagedRequestEnqueueActionDeserializePatch
{
    private static bool Prefix(ref RequestEnqueueActionMessage __instance, PacketReader reader)
    {
        PacketReader probe = LibraryManagedNetActionMessagePatch.CreateProbeReader(reader);
        probe.Read<RunLocation>();
        if (!LibraryManagedNetActionCodec.NextPayloadIsManagedAction(probe))
            return true;

        __instance.location = reader.Read<RunLocation>();
        __instance.action = LibraryManagedNetActionCodec.Read(reader);
        return false;
    }
}

[HarmonyPatch(typeof(ActionEnqueuedMessage), nameof(ActionEnqueuedMessage.Serialize), typeof(PacketWriter))]
internal static class LibraryManagedActionEnqueuedSerializePatch
{
    private static bool Prefix(ActionEnqueuedMessage __instance, PacketWriter writer)
    {
        if (!LibraryManagedNetActionCodec.CanWrite(__instance.action))
            return true;

        writer.WriteULong(__instance.playerId);
        writer.Write(__instance.location);
        LibraryManagedNetActionCodec.TryWrite(writer, __instance.action);
        return false;
    }
}

[HarmonyPatch(typeof(ActionEnqueuedMessage), nameof(ActionEnqueuedMessage.Deserialize), typeof(PacketReader))]
internal static class LibraryManagedActionEnqueuedDeserializePatch
{
    private static bool Prefix(ref ActionEnqueuedMessage __instance, PacketReader reader)
    {
        PacketReader probe = LibraryManagedNetActionMessagePatch.CreateProbeReader(reader);
        probe.ReadULong();
        probe.Read<RunLocation>();
        if (!LibraryManagedNetActionCodec.NextPayloadIsManagedAction(probe))
            return true;

        __instance.playerId = reader.ReadULong();
        __instance.location = reader.Read<RunLocation>();
        __instance.action = LibraryManagedNetActionCodec.Read(reader);
        return false;
    }
}

[HarmonyPatch(typeof(CombatReplayEvent), nameof(CombatReplayEvent.Serialize), typeof(PacketWriter))]
internal static class LibraryManagedCombatReplayEventSerializePatch
{
    private static bool Prefix(CombatReplayEvent __instance, PacketWriter writer)
    {
        if (__instance.eventType != CombatReplayEventType.GameAction
            || __instance.action == null
            || !LibraryManagedNetActionCodec.CanWrite(__instance.action))
        {
            return true;
        }

        writer.WriteInt((int)__instance.eventType, 3);
        writer.WriteULong(__instance.playerId!.Value);
        LibraryManagedNetActionCodec.TryWrite(writer, __instance.action);
        return false;
    }
}

[HarmonyPatch(typeof(CombatReplayEvent), nameof(CombatReplayEvent.Deserialize), typeof(PacketReader))]
internal static class LibraryManagedCombatReplayEventDeserializePatch
{
    private static bool Prefix(ref CombatReplayEvent __instance, PacketReader reader)
    {
        PacketReader probe = LibraryManagedNetActionMessagePatch.CreateProbeReader(reader);
        var eventType = (CombatReplayEventType)probe.ReadInt(3);
        if (eventType != CombatReplayEventType.GameAction)
            return true;

        probe.ReadULong();
        if (!LibraryManagedNetActionCodec.NextPayloadIsManagedAction(probe))
            return true;

        __instance.eventType = (CombatReplayEventType)reader.ReadInt(3);
        __instance.playerId = reader.ReadULong();
        __instance.action = LibraryManagedNetActionCodec.Read(reader);
        return false;
    }
}
