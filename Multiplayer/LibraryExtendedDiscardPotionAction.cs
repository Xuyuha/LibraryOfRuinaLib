#nullable enable
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace LibraryLib.Multiplayer;

/// <summary>
/// Preserves potion slot indices that no longer fit the vanilla four-bit field.
/// Modded potion belts can legitimately expose slot indices above 15.
/// </summary>
public struct NetLibraryExtendedDiscardPotionAction : INetAction, IPacketSerializable
{
    public uint PotionSlotIndex;
    public bool WasEnqueuedInCombat;

    public readonly GameAction ToGameAction(Player player) =>
        new DiscardPotionGameAction(player, PotionSlotIndex, WasEnqueuedInCombat);

    public readonly void Serialize(PacketWriter writer)
    {
        writer.WriteUInt(PotionSlotIndex);
        writer.WriteBool(WasEnqueuedInCombat);
    }

    public void Deserialize(PacketReader reader)
    {
        PotionSlotIndex = reader.ReadUInt();
        WasEnqueuedInCombat = reader.ReadBool();
    }

    public override readonly string ToString() =>
        $"NetLibraryExtendedDiscardPotionAction slot {PotionSlotIndex} "
        + $"in combat: {WasEnqueuedInCombat}";
}

[HarmonyPatch(typeof(DiscardPotionGameAction), nameof(DiscardPotionGameAction.ToNetAction))]
internal static class LibraryExtendedDiscardPotionActionPatch
{
    private const uint FirstPotionIndexOutsideVanillaRange = 1U << 4;

    [HarmonyPostfix]
    private static void Postfix(ref INetAction __result)
    {
        if (__result is not NetDiscardPotionGameAction vanillaAction
            || vanillaAction.potionSlotIndex < FirstPotionIndexOutsideVanillaRange)
        {
            return;
        }

        __result = new NetLibraryExtendedDiscardPotionAction
        {
            PotionSlotIndex = vanillaAction.potionSlotIndex,
            WasEnqueuedInCombat = vanillaAction.wasEnqueuedInCombat,
        };
    }
}
