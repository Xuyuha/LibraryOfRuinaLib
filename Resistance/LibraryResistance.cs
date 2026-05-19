#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Library.Resistance.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Library.Resistance;

/// <summary>抗性子系统入口：参与方注册、敌人 bootstrap、卡牌改系数 API。</summary>
public static class LibraryResistance
{
    private static readonly List<LibraryResistanceParticipant> Participants = new();

    public static void RegisterParticipant(LibraryResistanceParticipant participant)
    {
        ArgumentNullException.ThrowIfNull(participant);
        if (string.IsNullOrWhiteSpace(participant.Id))
        {
            throw new ArgumentException("Participant Id is required.", nameof(participant));
        }

        Participants.RemoveAll(p => string.Equals(p.Id, participant.Id, StringComparison.Ordinal));
        Participants.Add(participant);
    }

    public static bool IsRelevantPlayer(Player? player) =>
        Participants.Any(p => p.IsRelevantPlayer(player));

    public static bool IsRelevantDamageDealer(Creature? dealer) =>
        dealer?.Player != null && IsRelevantPlayer(dealer.Player);

    internal static bool CombatHasRelevantParticipant(ICombatState combatState) =>
        combatState.Players.Any(IsRelevantPlayer);

    internal static void NotifyCoefficientsChanged(ICombatState combatState)
    {
        foreach (LibraryResistanceParticipant participant in Participants)
        {
            participant.OnCoefficientsChanged?.Invoke(combatState);
        }
    }

    public static async Task EnsureOnEnemy(Creature enemy, PlayerChoiceContext? choiceContext = null)
    {
        await LibraryResistanceBootstrap.EnsureOnEnemy(enemy, choiceContext);
    }

    public static void ApplyCoefficientDelta(Creature enemy, LibraryDamageKind kind, decimal delta)
    {
        GetPower(enemy, kind)?.ApplyCoefficientDelta(delta);
    }

    public static void SetCoefficientAbsolute(Creature enemy, LibraryDamageKind kind, decimal coefficient)
    {
        GetPower(enemy, kind)?.SetCoefficientAbsolute(coefficient);
    }

    public static void SetAllKindsAbsolute(Creature enemy, decimal coefficient)
    {
        LibraryResistanceBundle.SetAllKindsAbsolute(enemy, coefficient);
    }

    public static void ApplyDeltaAllKinds(Creature enemy, decimal delta)
    {
        LibraryResistanceBundle.ApplyDeltaAllKinds(enemy, delta);
    }

    public static LibraryResistancePowerBase? GetPower(Creature enemy, LibraryDamageKind kind) =>
        kind switch
        {
            LibraryDamageKind.Slash => enemy.GetPower<LibrarySlashResistancePower>(),
            LibraryDamageKind.Blunt => enemy.GetPower<LibraryBluntResistancePower>(),
            LibraryDamageKind.Pierce => enemy.GetPower<LibraryPierceResistancePower>(),
            _ => null
        };
}
