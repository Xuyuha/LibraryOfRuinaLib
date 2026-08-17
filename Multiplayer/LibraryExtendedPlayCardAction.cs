#nullable enable
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace LibraryLib.Multiplayer;

/// <summary>
/// Preserves play-card target IDs that no longer fit the vanilla six-bit field.
/// Long encounters with repeated summons can legitimately allocate combat IDs above 63.
/// </summary>
public struct NetLibraryExtendedPlayCardAction : INetAction, IPacketSerializable
{
    public NetCombatCard Card;
    public ModelId CardModelId;
    public uint? TargetId;

    public readonly GameAction ToGameAction(Player player) =>
        new PlayCardAction(player, Card, CardModelId, TargetId);

    public readonly void Serialize(PacketWriter writer)
    {
        writer.Write(Card);
        writer.WriteFullModelId(CardModelId);
        writer.WriteBool(TargetId.HasValue);
        if (TargetId.HasValue)
        {
            writer.WriteUInt(TargetId.Value);
        }
    }

    public void Deserialize(PacketReader reader)
    {
        Card = reader.Read<NetCombatCard>();
        CardModelId = reader.ReadFullModelId();
        TargetId = reader.ReadBool() ? reader.ReadUInt() : null;
    }

    public override readonly string ToString() =>
        $"NetLibraryExtendedPlayCardAction ({Card}) target: {TargetId?.ToString() ?? "null"}";
}

[HarmonyPatch(typeof(PlayCardAction), nameof(PlayCardAction.ToNetAction))]
internal static class LibraryExtendedPlayCardActionPatch
{
    private const uint FirstTargetIdOutsideVanillaRange = 1U << 6;

    [HarmonyPostfix]
    private static void Postfix(PlayCardAction __instance, ref INetAction __result)
    {
        if (__instance.TargetId is not uint targetId
            || targetId < FirstTargetIdOutsideVanillaRange)
        {
            return;
        }

        __result = new NetLibraryExtendedPlayCardAction
        {
            Card = __instance.NetCombatCard,
            CardModelId = __instance.CardModelId,
            TargetId = targetId,
        };
    }
}
