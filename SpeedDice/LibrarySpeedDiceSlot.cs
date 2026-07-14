using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace Library.SpeedDice;

public sealed class LibrarySpeedDiceSlot
{
    internal LibrarySpeedDiceSlot(int index, int initialDisplayValue)
    {
        Index = index;
        DisplayValue = initialDisplayValue;
    }

    public int Index { get; }

    public int DisplayValue { get; internal set; }

    public int FinalValue { get; internal set; }

    public bool IsLocked { get; internal set; }

    public CardModel? Card { get; internal set; }

    public Creature? Target { get; internal set; }

    public bool RequiresTarget =>
        Card?.GetSpeedDiceTargetType() is TargetType.AnyEnemy or TargetType.AnyAlly;

    public bool HasValidTarget =>
        !RequiresTarget
        || Target is { IsAlive: true }
        && Card?.IsValidSpeedDiceTarget(Target) == true;

    public int ReservedEnergy { get; internal set; }

    public int ReservedStars { get; internal set; }

    internal void ClearReservation()
    {
        ReservedEnergy = 0;
        ReservedStars = 0;
    }

    internal void ClearCard()
    {
        Card = null;
        Target = null;
        ClearReservation();
    }
}
