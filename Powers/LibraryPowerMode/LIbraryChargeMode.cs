using Library.Models;
using Library.Powers.Mode;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Relics;

public abstract class LibraryChargeMode : LibraryPowerMode
{
    public LibraryChargeMode(LibraryMultipleModePowerModel sourcePower) : base(sourcePower)
    {
    }
    public virtual Task Effect(PlayerChoiceContext choiceContext, decimal effectiveAmount)
    {
        return Task.CompletedTask;
    }
}