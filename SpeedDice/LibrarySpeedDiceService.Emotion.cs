using System.Runtime.CompilerServices;
using Godot;
using LibraryLib.Light;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace LibraryLib.SpeedDice;

/// <summary>情感：伤害、击杀、极值投掷等来源的情感点数与等级。</summary>
internal static partial class LibrarySpeedDiceService
{
    public static int GetMaxEnergyBonus(Player player)
    {
        return States.TryGetValue(player, out var state)
            && IsStateUsable(state)
            ? state.Emotion.Level
              * state.Registration.Emotion.MaxEnergyPerLevel
            : 0;
    }

    public static void AddInitialHandDrawBonus(
        Player player,
        bool fromHandDraw,
        ref decimal count)
    {
        if (!fromHandDraw
            || !TryGetState(player, out var state)
            || state == null
            || !state.BonusDrawPending)
        {
            return;
        }

        state.BonusDrawPending = false;
        count += state.Registration.Emotion.BonusDrawAmount;
        state.NotifyGameplayChanged();
    }

    public static void RecordDamageGiven(
        Creature? dealer,
        DamageResult result,
        Creature target)
    {
        if (dealer?.Player == null
            || target.Side == dealer.Side
            || !TryGetState(dealer.Player, out var state)
            || state == null)
        {
            return;
        }

        if (state.Registration.Emotion.GainEmotionFromDamage)
        {
            AddDamageEmotion(
                state,
                Math.Max(0, result.UnblockedDamage - result.OverkillDamage),
                target.MaxHp,
                isDamageGiven: true);
        }
        if (result.WasTargetKilled)
            AddEmotionUnits(
                state,
                state.Registration.Emotion.KillEmotionUnits);
    }

    public static void RecordDamageReceived(Creature target, DamageResult result)
    {
        if (target.Player == null)
        {
            return;
        }

        if (TryGetState(
                target.Player,
                out var targetState)
            && targetState != null
            && targetState.Registration.Emotion.GainEmotionFromDamage)
        {
            AddDamageEmotion(
                targetState,
                Math.Max(0, result.UnblockedDamage - result.OverkillDamage),
                target.MaxHp,
                isDamageGiven: false);
        }
    }

    public static void RecordAllyDeath(
        ICombatState? combatState,
        Creature creature,
        bool wasRemovalPrevented)
    {
        if (wasRemovalPrevented
            || creature.Player == null
            || combatState == null)
        {
            return;
        }

        foreach (var ally in combatState.Players)
        {
            if (ally == creature.Player
                || ally.Creature.IsDead
                || !TryGetState(
                    ally,
                    out var allyState)
                || allyState == null)
            {
                continue;
            }

            AddEmotionUnits(
                allyState,
                allyState.Registration.Emotion.AllyDeathEmotionUnits);
        }
    }

    private static void AddDamageEmotion(
        LibrarySpeedDiceCombatState state,
        int damage,
        int referenceMaxHp,
        bool isDamageGiven)
    {
        if (damage <= 0)
            return;

        var threshold = Math.Max(
            1,
            (int)Math.Ceiling(
                Math.Max(1, referenceMaxHp)
                * state.Registration.Emotion
                    .DamageUnitFractionOfMaxHp));
        int accumulator = isDamageGiven
            ? state.DamageGivenAccumulator
            : state.DamageReceivedAccumulator;
        int previousThreshold = isDamageGiven
            ? state.DamageGivenAccumulatorThreshold
            : state.DamageReceivedAccumulatorThreshold;
        if (previousThreshold <= 0)
        {
            accumulator = 0;
        }
        else if (previousThreshold != threshold)
        {
            accumulator = (int)Math.Floor(
                (decimal)accumulator
                * threshold
                / previousThreshold);
        }

        var total = damage + accumulator;
        var units = total / threshold;
        var remainder = total % threshold;
        if (isDamageGiven)
        {
            state.DamageGivenAccumulator = remainder;
            state.DamageGivenAccumulatorThreshold = threshold;
        }
        else
        {
            state.DamageReceivedAccumulator = remainder;
            state.DamageReceivedAccumulatorThreshold = threshold;
        }

        AddEmotionUnits(state, units);
    }

    private static void AddEmotionUnits(
        LibrarySpeedDiceCombatState state,
        int units)
    {
        if (units <= 0)
            return;

        state.Emotion.AddUnits(
            units,
            state.Registration.Emotion);
        state.NotifyGameplayChanged();
    }

    public static void AddEmotionUnits(Player player, int units)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (TryGetState(player, out LibrarySpeedDiceCombatState? state)
            && state != null)
        {
            AddEmotionUnits(state, units);
        }
    }

    public static bool TryForceEmotionLevelUp(
        Player player,
        out int previousLevel,
        out int currentLevel)
    {
        ArgumentNullException.ThrowIfNull(player);
        previousLevel = 0;
        currentLevel = 0;
        if (!TryGetState(player, out LibrarySpeedDiceCombatState? state)
            || state == null)
        {
            return false;
        }

        previousLevel = state.Emotion.Level;
        if (!state.Emotion.ForceLevelUp(
                state.Registration.Emotion))
        {
            currentLevel = previousLevel;
            return false;
        }

        currentLevel = state.Emotion.Level;
        return true;
    }
}
