#nullable enable
using HarmonyLib;
using LibraryLib.Multiplayer;
using LibraryLib.Utils.RelicRightClick;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace LibraryLib.Patches;

/// <summary>
/// Class-level transport for LibraryOfRuinaLib managed net actions.
///
/// The codec previously hooked the vanilla action carriers through Harmony patches on
/// the STRUCT methods <c>RequestEnqueueActionMessage.Serialize</c> and
/// <c>ActionEnqueuedMessage.Serialize</c>. Those patches never engage inside the game
/// process (observed on STS2 v0.111.0), so every managed action fell through to the
/// vanilla positional path and was rejected by the action-type safety patch.
///
/// This patch adds the same class-level interception:
///  - client request: ActionQueueSynchronizer.RequestEnqueue -> direct codec send to host
///  - host broadcast: ActionQueueSynchronizer.EnqueueAction -> direct codec broadcast
///  - receive: NetMessageBus.TryDeserializeMessage -> codec decode for managed carriers
///
/// The existing struct-level patches are kept as a redundant fast path for runtimes
/// where they do engage; they never double-encode because this interception happens
/// earlier on the send path and skips the vanilla serializer entirely.
/// </summary>
internal static class LibraryManagedNetActionTransport
{
    private static readonly AccessTools.FieldRef<ActionQueueSynchronizer, INetGameService>
        NetServiceRef =
            AccessTools.FieldRefAccess<ActionQueueSynchronizer, INetGameService>(
                "_netService");

    private static readonly AccessTools.FieldRef<ActionQueueSynchronizer, RunLocationTargetedMessageBuffer>
        MessageBufferRef =
            AccessTools.FieldRefAccess<ActionQueueSynchronizer, RunLocationTargetedMessageBuffer>(
                "_messageBuffer");

    private static readonly AccessTools.FieldRef<ActionQueueSynchronizer, ActionQueueSet>
        ActionQueueSetRef =
            AccessTools.FieldRefAccess<ActionQueueSynchronizer, ActionQueueSet>(
                "_actionQueueSet");

    /// <summary>
    /// Serializes a client-to-host action request with a managed action through the
    /// codec and sends it directly. Returns false when the request is not eligible
    /// (not a client, or the action is not a managed action), so the caller can fall
    /// back to the vanilla path.
    /// </summary>
    internal static bool TrySendClientRequest(
        ActionQueueSynchronizer synchronizer,
        GameAction action)
    {
        INetGameService netService = NetServiceRef(synchronizer);
        if (netService is not NetClientGameService client
            || !client.IsConnected
            || client.NetClient == null)
        {
            return false;
        }

        INetAction netAction = action.ToNetAction();
        if (!LibraryManagedNetActionCodec.CanWrite(netAction))
        {
            return false;
        }

        var message = new RequestEnqueueActionMessage
        {
            action = netAction,
            location = MessageBufferRef(synchronizer).CurrentLocation,
        };

        byte[] packet = SerializeManagedRequest(
            client.NetId,
            message,
            out int packetLength);

        client.NetClient.SendMessageToHost(
            packet,
            packetLength,
            message.Mode,
            message.Mode.ToChannelId());
        Log.Info("[LibraryOfRuinaLib.Multiplayer] Sent managed action request directly: " + action);
        return true;
    }

    /// <summary>
    /// Serializes a host-to-clients action announcement with a managed action through
    /// the codec, broadcasts it to every ready peer, and enqueues the action locally.
    /// Returns false when the announcement is not eligible so the caller can fall back
    /// to the vanilla path.
    /// </summary>
    internal static bool TrySendHostAnnouncement(
        ActionQueueSynchronizer synchronizer,
        GameAction action,
        ulong actionOwnerId)
    {
        INetGameService netService = NetServiceRef(synchronizer);
        if (netService is not NetHostGameService host
            || !host.IsConnected
            || host.NetHost == null)
        {
            return false;
        }

        INetAction netAction = action.ToNetAction();
        if (!LibraryManagedNetActionCodec.CanWrite(netAction))
        {
            return false;
        }

        var message = new ActionEnqueuedMessage
        {
            playerId = actionOwnerId,
            location = MessageBufferRef(synchronizer).CurrentLocation,
            action = netAction,
        };

        byte[] packet = SerializeManagedAnnouncement(
            host.NetId,
            message,
            out int packetLength);

        foreach (NetClientData peer in host.ConnectedPeers)
        {
            if (peer.readyForBroadcasting)
            {
                host.NetHost.SendMessageToClient(
                    peer.peerId,
                    packet,
                    packetLength,
                    message.Mode,
                    message.Mode.ToChannelId());
            }
        }

        ActionQueueSetRef(synchronizer).EnqueueWithoutSynchronizing(action);
        Log.Info("[LibraryOfRuinaLib.Multiplayer] Sent managed action announcement directly: " + action);
        return true;
    }

    internal static byte[] SerializeManagedRequest(
        ulong senderId,
        RequestEnqueueActionMessage message,
        out int length)
    {
        var writer = CreateCarrierWriter(senderId, message);
        writer.Write(message.location);
        WriteManagedActionOrThrow(writer, message.action, "request");
        length = writer.BytePosition;
        return writer.Buffer;
    }

    internal static byte[] SerializeManagedAnnouncement(
        ulong senderId,
        ActionEnqueuedMessage message,
        out int length)
    {
        var writer = CreateCarrierWriter(senderId, message);
        writer.WriteULong(message.playerId);
        writer.Write(message.location);
        WriteManagedActionOrThrow(writer, message.action, "announcement");
        length = writer.BytePosition;
        return writer.Buffer;
    }

    private static PacketWriter CreateCarrierWriter(
        ulong senderId,
        INetMessage message)
    {
        var writer = new PacketWriter();
        writer.WriteByte((byte)MessageTypes.ToId(message));
        writer.WriteULong(senderId);
        return writer;
    }

    private static void WriteManagedActionOrThrow(
        PacketWriter writer,
        INetAction action,
        string carrierKind)
    {
        if (LibraryManagedNetActionCodec.TryWrite(writer, action))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Managed action {carrierKind} unexpectedly fell back to positional serialization: "
            + action.GetType().FullName);
    }

    /// <summary>
    /// Attempts to decode a raw packet whose message id is one of the vanilla action
    /// carriers and whose payload starts with the managed codec magic. Returns false
    /// when the packet is not a managed carrier, leaving the vanilla decoder in charge.
    /// </summary>
    internal static bool TryDecodeManagedCarrier(
        byte[] packetBytes,
        out INetMessage? message,
        out ulong senderId)
    {
        message = null;
        senderId = 0UL;
        if (!LibraryManagedNetTypeRegistry.IsReady || packetBytes.Length < 1)
        {
            return false;
        }

        byte messageId = packetBytes[0];
        if (!MessageTypes.TryGetMessageType(messageId, out Type? carrierType))
        {
            return false;
        }

        bool isRequest = carrierType == typeof(RequestEnqueueActionMessage);
        bool isAnnouncement = carrierType == typeof(ActionEnqueuedMessage);
        if (!isRequest && !isAnnouncement)
        {
            return false;
        }

        var probe = new PacketReader();
        probe.Reset(packetBytes);
        probe.ReadByte();
        senderId = probe.ReadULong();

        ulong playerId = 0UL;
        if (isAnnouncement)
        {
            playerId = probe.ReadULong();
        }

        RunLocation location = probe.Read<RunLocation>();

        PacketReader payloadProbe =
            LibraryManagedNetActionMessagePatch.CreateProbeReader(probe);
        if (!LibraryManagedNetActionCodec.NextPayloadIsManagedAction(payloadProbe))
        {
            return false;
        }

        INetAction action = LibraryManagedNetActionCodec.Read(probe);
        ValidateCarrierEnd(packetBytes, probe.BitPosition);

        message = isRequest
            ? new RequestEnqueueActionMessage { location = location, action = action }
            : new ActionEnqueuedMessage
            {
                playerId = playerId,
                location = location,
                action = action,
            };
        return true;
    }

    private static void ValidateCarrierEnd(
        byte[] packetBytes,
        int payloadEndBit)
    {
        // The carrier must contain everything the codec declared; trailing bytes
        // beyond the payload (e.g. transport-level decorations added by other mods)
        // are not LoR's concern and are ignored, mirroring the vanilla bus behavior.
        long remainingBits = (long)packetBytes.Length * 8L - payloadEndBit;
        if (remainingBits >= 0)
        {
            return;
        }

        throw new LibraryManagedNetDecodeException(
            new LibraryManagedNetDecodeFailure(
                "truncated_action_carrier",
                $"Managed action carrier ended at bit {payloadEndBit} with "
                + $"{remainingBits} packet bits remaining."));
    }
}

/// <summary>
/// Intercepts client action requests before the vanilla serializer runs, mirroring
/// the deferral gate the vanilla body would otherwise apply.
/// </summary>
[HarmonyPatch(
    typeof(ActionQueueSynchronizer),
    nameof(ActionQueueSynchronizer.RequestEnqueue))]
internal static class LibraryManagedRequestEnqueueTransportPatch
{
    [HarmonyPriority(Priority.Last)]
    private static bool Prefix(
        ActionQueueSynchronizer __instance,
        GameAction action)
    {
        // Keep the vanilla deferral for combat play-phase actions requested outside
        // the play phase; the deferred action is re-requested later and reaches this
        // patch again.
        if (action.ActionType == GameActionType.CombatPlayPhaseOnly
            && __instance.CombatState == ActionSynchronizerCombatState.NotPlayPhase)
        {
            return true;
        }

        try
        {
            return !LibraryManagedNetActionTransport.TrySendClientRequest(
                __instance,
                action);
        }
        catch (Exception ex)
        {
            Log.Error(
                "[LibraryOfRuinaLib.Multiplayer] Managed action request send failed: "
                + ex);
            throw;
        }
    }
}

/// <summary>
/// Intercepts host action announcements before the vanilla serializer runs, so the
/// broadcast uses the codec instead of the positional action id.
/// </summary>
[HarmonyPatch(
    typeof(ActionQueueSynchronizer),
    "EnqueueAction",
    new[] { typeof(GameAction), typeof(ulong) })]
internal static class LibraryManagedEnqueueActionTransportPatch
{
    [HarmonyPriority(Priority.Last)]
    private static bool Prefix(
        ActionQueueSynchronizer __instance,
        GameAction action,
        ulong actionOwnerId)
    {
        try
        {
            return !LibraryManagedNetActionTransport.TrySendHostAnnouncement(
                __instance,
                action,
                actionOwnerId);
        }
        catch (Exception ex)
        {
            Log.Error(
                "[LibraryOfRuinaLib.Multiplayer] Managed action announcement send failed: "
                + ex);
            throw;
        }
    }
}
