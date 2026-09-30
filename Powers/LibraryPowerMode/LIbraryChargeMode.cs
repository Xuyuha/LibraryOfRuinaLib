using LibraryLib.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace LibraryLib.Powers.LibraryPowerMode;

public abstract class LibraryChargeMode : LibraryPowerMode
{
    public virtual int MaxAmount => 10;
    public LibraryChargeMode(LibraryMultipleModePowerModel sourcePower) : base(sourcePower)
    {
    }
    public LibraryChargeMode()
    {
    }
    public virtual Task Effect(PlayerChoiceContext choiceContext, decimal effectiveAmount)
    {
        return Task.CompletedTask;
    }
}