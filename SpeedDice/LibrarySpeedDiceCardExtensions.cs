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

internal static class LibrarySpeedDiceCardExtensions
{
    public static LibrarySpeedDiceAssignmentMode
        GetSpeedDiceAssignmentMode(this CardModel card)
    {
        return card is ILibrarySpeedDiceCard speedDiceCard
            ? speedDiceCard.AssignmentMode
            : LibrarySpeedDiceAssignmentMode.Persistent;
    }

    public static TargetType GetSpeedDiceTargetType(this CardModel card)
    {
        return card is ILibrarySpeedDiceCard speedDiceCard
            ? speedDiceCard.SpeedDiceTargetType
            : card.TargetType;
    }

    public static bool RequiresSpeedDiceTarget(this CardModel card)
    {
        return card.GetSpeedDiceTargetType()
            is TargetType.AnyEnemy or TargetType.AnyAlly;
    }

    public static bool IsValidSpeedDiceTarget(
        this CardModel card,
        Creature? target)
    {
        TargetType targetType = card.GetSpeedDiceTargetType();
        if (target == null)
        {
            return targetType is not TargetType.AnyEnemy
                and not TargetType.AnyAlly
                && LibrarySpeedDiceService.IsParticipantTargetAllowed(
                    card,
                    null);
        }

        if (!target.IsAlive)
            return false;

        bool isValid = targetType switch
        {
            TargetType.AnyEnemy => target.Side != card.Owner.Creature.Side,
            TargetType.AnyAlly => target.Side == card.Owner.Creature.Side,
            _ => false,
        };
        return isValid
            && LibrarySpeedDiceService.IsParticipantTargetAllowed(
                card,
                target);
    }
}
