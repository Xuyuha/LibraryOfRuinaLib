using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace LibraryLib.SpeedDice;

public static class LibrarySpeedDice
{
    /// <summary>
    /// Sets the volume (dB) of the finger-snap SFX played when speed dice
    /// advance. The default is 0 dB; negative values are quieter.
    /// </summary>
    public static void SetFingerSnapVolumeDb(float volumeDb)
    {
        LibrarySpeedDiceAudio.SetVolumeDb(volumeDb);
    }

    public static LibrarySpeedDiceRegistrationBuilder<TCharacter>
        ForCharacter<TCharacter>(
            string id,
            LibrarySpeedDiceOptions options)
        where TCharacter : CharacterModel
    {
        return new LibrarySpeedDiceRegistrationBuilder<TCharacter>(
            id,
            options);
    }

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

    public static bool IsValidTarget(
        CardModel card,
        Creature? target)
    {
        ArgumentNullException.ThrowIfNull(card);
        return card.IsValidSpeedDiceTarget(target);
    }

    public static bool TryBeginEquipSelection(CardModel card)
    {
        return LibrarySpeedDiceService.TryBeginEquipSelection(card);
    }

    /// <summary>
    /// 处理速度骰子卡牌的右键装备入口。基础库会自动处理鼠标右键；
    /// 下游控制器/交互框架可将等价右键事件转发到此方法。
    /// </summary>
    public static bool TryHandleRightClickSelection(
        CardModel card,
        bool usingController = false)
    {
        return LibrarySpeedDiceRightClickService.TryHandle(
            card,
            usingController);
    }

    /// <summary>
    /// Starts speed-die slot selection for either the vanilla hand or a
    /// registered custom selection source. A null source id requires exactly
    /// one source to match the card.
    /// </summary>
    public static Task<LibrarySpeedDiceSelectionResult>
        BeginSlotSelectionAsync(
            CardModel card,
            bool usingController = false,
            string? sourceId = null)
    {
        return LibrarySpeedDiceRightClickService.BeginSlotSelectionAsync(
            card,
            usingController,
            sourceId);
    }

    public static void CancelActiveSelection()
    {
        LibrarySpeedDiceRightClickService.CancelActiveSelection();
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

    public static Task RollForPlayerAsync(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        return LibrarySpeedDiceService.RollForPlayerAsync(
            choiceContext,
            player);
    }

    public static Task RequestEquipAsync(
        Player player,
        CardModel card,
        int slotIndex,
        Creature? target,
        int expectedTurnNumber,
        int expectedRevision)
    {
        return LibrarySpeedDiceService.RequestEquipAsync(
            player,
            card,
            slotIndex,
            target,
            expectedTurnNumber,
            expectedRevision);
    }

    public static Task RequestEquipAsync(
        Player player,
        CardModel card,
        int slotIndex,
        Creature? target,
        int expectedTurnNumber,
        int expectedRevision,
        string sourceId)
    {
        return LibrarySpeedDiceService.RequestEquipAsync(
            player,
            card,
            slotIndex,
            target,
            expectedTurnNumber,
            expectedRevision,
            sourceId);
    }

    public static Task RequestUnequipAsync(
        Player player,
        int slotIndex,
        int expectedTurnNumber,
        int expectedRevision)
    {
        return LibrarySpeedDiceService.RequestUnequipAsync(
            player,
            slotIndex,
            expectedTurnNumber,
            expectedRevision);
    }

    public static Task RequestRetargetAsync(
        Player player,
        int slotIndex,
        Creature target,
        int expectedTurnNumber,
        int expectedRevision)
    {
        return LibrarySpeedDiceService.RequestRetargetAsync(
            player,
            slotIndex,
            target,
            expectedTurnNumber,
            expectedRevision);
    }

    public static Task ResolveForPlayerAsync(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        return LibrarySpeedDiceService.ResolveForPlayerAsync(
            choiceContext,
            player);
    }

    /// <summary>
    /// Resolves several registered speed-dice states as one deterministic
    /// batch. Cards are ordered by final speed, player network id, then slot
    /// index. Downstream multiplayer adapters should use this API instead of
    /// reflecting private resolution methods.
    /// </summary>
    public static Task ResolveBatchAsync(
        PlayerChoiceContext choiceContext,
        IReadOnlyList<LibrarySpeedDiceCombatState> states)
    {
        return LibrarySpeedDiceService.ResolveBatchAsync(
            choiceContext,
            states);
    }

    public static Task<bool> ExecuteEquipAsync(
        PlayerChoiceContext choiceContext,
        Player player,
        CardModel card,
        int slotIndex,
        Creature? target,
        int expectedTurnNumber,
        int expectedRevision)
    {
        return LibrarySpeedDiceService.ExecuteEquipAsync(
            choiceContext,
            player,
            card,
            slotIndex,
            target,
            expectedTurnNumber,
            expectedRevision);
    }

    public static Task<bool> ExecuteEquipAsync(
        PlayerChoiceContext choiceContext,
        Player player,
        CardModel card,
        int slotIndex,
        Creature? target,
        int expectedTurnNumber,
        int expectedRevision,
        string sourceId)
    {
        return LibrarySpeedDiceService.ExecuteEquipAsync(
            choiceContext,
            player,
            card,
            slotIndex,
            target,
            expectedTurnNumber,
            expectedRevision,
            sourceId);
    }

    public static Task<bool> ExecuteUnequipAsync(
        Player player,
        int slotIndex,
        int expectedTurnNumber,
        int expectedRevision)
    {
        return LibrarySpeedDiceService.ExecuteUnequipAsync(
            player,
            slotIndex,
            expectedTurnNumber,
            expectedRevision);
    }

    public static Task<bool> ExecuteRetargetAsync(
        Player player,
        int slotIndex,
        Creature target,
        int expectedTurnNumber,
        int expectedRevision)
    {
        return LibrarySpeedDiceService.ExecuteRetargetAsync(
            player,
            slotIndex,
            target,
            expectedTurnNumber,
            expectedRevision);
    }

    public static void RefreshSlotCount(
        Player player,
        bool rollNewSlots = true)
    {
        LibrarySpeedDiceService.RefreshSlotCount(player, rollNewSlots);
    }

    public static void AddEmotionUnits(Player player, int units)
    {
        LibrarySpeedDiceService.AddEmotionUnits(player, units);
    }

    public static LibrarySpeedDiceStateSnapshot CreateSnapshot(
        LibrarySpeedDiceCombatState state)
    {
        return LibrarySpeedDiceService.CreateSnapshot(state);
    }

    public static bool TryRestoreSnapshot(
        Player player,
        LibrarySpeedDiceStateSnapshot snapshot)
    {
        return LibrarySpeedDiceService.TryRestoreSnapshot(player, snapshot);
    }

    public static void NotifyParticipantStateChanged(
        LibrarySpeedDiceCombatState state)
    {
        LibrarySpeedDiceService.NotifyParticipantStateChanged(state);
    }

    public static bool TryForceEmotionLevelUp(
        Player player,
        out int previousLevel,
        out int currentLevel)
    {
        return LibrarySpeedDiceService.TryForceEmotionLevelUp(
            player,
            out previousLevel,
            out currentLevel);
    }
}
