#nullable enable
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LibraryLib.Utils.RelicRightClick;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Replay;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;

namespace LibraryLib.Patches;

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

/// <summary>
/// Class-level combat-replay seam for managed actions. The game serializes replay
/// events through a generic list of value types; relying only on Harmony patches on
/// <see cref="CombatReplayEvent"/> repeats the same value-type dispatch risk that
/// prevented the request/announcement serializer patches from running in-game.
/// </summary>
internal static class LibraryManagedCombatReplayListCodec
{
    internal static void WriteEvents(
        PacketWriter writer,
        IReadOnlyList<CombatReplayEvent> events,
        int lengthBits)
    {
        writer.WriteInt(events.Count, lengthBits);
        foreach (CombatReplayEvent replayEvent in events)
        {
            if (replayEvent.eventType == CombatReplayEventType.GameAction
                && replayEvent.action != null
                && LibraryManagedNetActionCodec.CanWrite(replayEvent.action))
            {
                writer.WriteInt((int)replayEvent.eventType, 3);
                writer.WriteULong(replayEvent.playerId!.Value);
                LibraryManagedNetActionCodec.TryWrite(writer, replayEvent.action);
                continue;
            }

            replayEvent.Serialize(writer);
        }
    }

    internal static List<CombatReplayEvent> ReadEvents(
        PacketReader reader,
        int lengthBits)
    {
        var events = new List<CombatReplayEvent>();
        int count = reader.ReadInt(lengthBits);
        for (int index = 0; index < count; index++)
        {
            PacketReader probe =
                LibraryManagedNetActionMessagePatch.CreateProbeReader(reader);
            var eventType = (CombatReplayEventType)probe.ReadInt(3);
            if (eventType == CombatReplayEventType.GameAction)
            {
                probe.ReadULong();
                if (LibraryManagedNetActionCodec.NextPayloadIsManagedAction(probe))
                {
                    events.Add(new CombatReplayEvent
                    {
                        eventType = (CombatReplayEventType)reader.ReadInt(3),
                        playerId = reader.ReadULong(),
                        action = LibraryManagedNetActionCodec.Read(reader),
                    });
                    continue;
                }
            }

            var replayEvent = new CombatReplayEvent();
            replayEvent.Deserialize(reader);
            events.Add(replayEvent);
        }

        return events;
    }
}

[HarmonyPatch(typeof(CombatReplay), nameof(CombatReplay.Serialize), typeof(PacketWriter))]
internal static class LibraryManagedCombatReplaySerializePatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo replacement = AccessTools.Method(
            typeof(LibraryManagedCombatReplayListCodec),
            nameof(LibraryManagedCombatReplayListCodec.WriteEvents));
        int replaced = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (IsReplayEventListCall(instruction, nameof(PacketWriter.WriteList)))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
                replaced++;
            }

            yield return instruction;
        }

        if (replaced != 1)
        {
            throw new InvalidOperationException(
                $"Expected one CombatReplayEvent WriteList call, replaced {replaced}.");
        }
    }

    private static bool IsReplayEventListCall(
        CodeInstruction instruction,
        string methodName) =>
        instruction.operand is MethodInfo method
        && method.Name == methodName
        && method.IsGenericMethod
        && method.GetGenericArguments() is [Type itemType]
        && itemType == typeof(CombatReplayEvent);
}

[HarmonyPatch(typeof(CombatReplay), nameof(CombatReplay.Deserialize), typeof(PacketReader))]
internal static class LibraryManagedCombatReplayDeserializePatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo replacement = AccessTools.Method(
            typeof(LibraryManagedCombatReplayListCodec),
            nameof(LibraryManagedCombatReplayListCodec.ReadEvents));
        int replaced = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (IsReplayEventListCall(instruction, nameof(PacketReader.ReadList)))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
                replaced++;
            }

            yield return instruction;
        }

        if (replaced != 1)
        {
            throw new InvalidOperationException(
                $"Expected one CombatReplayEvent ReadList call, replaced {replaced}.");
        }
    }

    private static bool IsReplayEventListCall(
        CodeInstruction instruction,
        string methodName) =>
        instruction.operand is MethodInfo method
        && method.Name == methodName
        && method.IsGenericMethod
        && method.GetGenericArguments() is [Type itemType]
        && itemType == typeof(CombatReplayEvent);
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
