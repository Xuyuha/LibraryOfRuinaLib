#nullable enable
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace LibraryLib.Multiplayer;

/// <summary>
/// Preserves potion slot and target IDs that no longer fit the vanilla four-bit
/// and six-bit fields.
/// Long encounters with repeated summons can legitimately allocate combat IDs above 63.
/// </summary>
public struct NetLibraryExtendedUsePotionAction : INetAction, IPacketSerializable
{
    public uint PotionIndex;
    public uint? TargetId;
    public ulong? TargetPlayerId;
    public bool EnqueuedInCombat;

    public readonly GameAction ToGameAction(Player player) =>
        new UsePotionAction(
            player,
            PotionIndex,
            TargetId,
            TargetPlayerId,
            EnqueuedInCombat);

    public readonly void Serialize(PacketWriter writer)
    {
        writer.WriteUInt(PotionIndex);
        writer.WriteBool(EnqueuedInCombat);
        writer.WriteBool(TargetId.HasValue);
        if (TargetId.HasValue)
        {
            writer.WriteUInt(TargetId.Value);
        }

        writer.WriteBool(TargetPlayerId.HasValue);
        if (TargetPlayerId.HasValue)
        {
            writer.WriteULong(TargetPlayerId.Value);
        }
    }

    public void Deserialize(PacketReader reader)
    {
        PotionIndex = reader.ReadUInt();
        EnqueuedInCombat = reader.ReadBool();
        TargetId = reader.ReadBool() ? reader.ReadUInt() : null;
        TargetPlayerId = reader.ReadBool() ? reader.ReadULong() : null;
    }

    public override readonly string ToString() =>
        $"NetLibraryExtendedUsePotionAction {PotionIndex} target: {TargetId} "
        + $"player: {TargetPlayerId} combat: {EnqueuedInCombat}";
}

[HarmonyPatch(typeof(UsePotionAction), nameof(UsePotionAction.ToNetAction))]
internal static class LibraryExtendedUsePotionActionPatch
{
    private const uint FirstTargetIdOutsideVanillaRange = 1U << 6;
    private const uint FirstPotionIndexOutsideVanillaRange = 1U << 4;

    [HarmonyPostfix]
    private static void Postfix(
        UsePotionAction __instance,
        ref INetAction __result)
    {
        bool targetNeedsExtension =
            __instance.TargetId is uint targetId
            && targetId >= FirstTargetIdOutsideVanillaRange;
        if ((!targetNeedsExtension
             && __instance.PotionIndex < FirstPotionIndexOutsideVanillaRange)
            || __result is not NetUsePotionAction vanillaAction)
        {
            return;
        }

        __result = new NetLibraryExtendedUsePotionAction
        {
            PotionIndex = vanillaAction.potionIndex,
            TargetId = __instance.TargetId,
            TargetPlayerId = vanillaAction.targetPlayerId,
            EnqueuedInCombat = vanillaAction.enqueuedInCombat,
        };
    }
}
