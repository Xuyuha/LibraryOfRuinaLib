using LibraryLib.Commands;
using LibraryLib.Entities.Creatures;
using LibraryLib.Localization.LibraryDynamicVars;
using LibraryLib.Powers.LibraryPowerMode;
using LibraryLib.Utils.RelicRightClick;
using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using LibraryLib.Localization.Dice;

namespace LibraryLib.Models;

public abstract class LibraryRelicModel : RelicModel
{
    public virtual bool HasRightClick => false;

    public virtual bool CanHandleRightClickLocal(LibraryRightClickContext context)
    {
        return HasRightClick;
    }

    public virtual bool CanExecuteRightClick(LibraryRightClickExecutionContext context)
    {
        return HasRightClick;
    }

    public virtual Task OnRightClick(LibraryRightClickExecutionContext context)
    {
        return Task.CompletedTask;
    }
}
