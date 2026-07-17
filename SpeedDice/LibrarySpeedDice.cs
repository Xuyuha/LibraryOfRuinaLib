using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;

namespace Library.SpeedDice;

public static class LibrarySpeedDice
{
    public static void RegisterParticipant(LibrarySpeedDiceParticipant participant)
    {
        LibrarySpeedDiceService.RegisterParticipant(participant);
    }

    public static bool TryGetState(
        Player player,
        out LibrarySpeedDiceCombatState? state)
    {
        return LibrarySpeedDiceService.TryGetState(player, out state);
    }

    public static bool TryGetEquippedSlot(
        CardModel card,
        out LibrarySpeedDiceSlot? slot)
    {
        return LibrarySpeedDiceService.TryGetEquippedSlot(card, out slot);
    }

    public static bool TryGetResolvingSlot(
        CardModel card,
        out LibrarySpeedDiceSlot? slot)
    {
        return LibrarySpeedDiceService.TryGetResolvingSlot(card, out slot);
    }

    public static bool CanEquipCard(CardModel card)
    {
        return LibrarySpeedDiceService.CanEquipCard(card);
    }

    public static bool TryBeginEquipSelection(CardModel card)
    {
        return LibrarySpeedDiceService.TryBeginEquipSelection(card);
    }

    public static void EndEquipSelection(CardModel card)
    {
        LibrarySpeedDiceService.EndEquipSelection(card);
    }

    public static Task EquipCardAsync(
        CardModel card,
        int slotIndex,
        Control targetingOrigin)
    {
        return LibrarySpeedDiceService.EquipCardAsync(
            card,
            slotIndex,
            targetingOrigin);
    }
}
