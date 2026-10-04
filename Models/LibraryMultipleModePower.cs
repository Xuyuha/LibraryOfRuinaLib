using LibraryLib.Hooks;
using LibraryLib.Powers.LibraryPowerMode;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace LibraryLib.Models;
public abstract partial class LibraryMultipleModePowerModel : LibraryPowerModel
{
	protected LibraryPowerModeModel? _mode;
    protected abstract LibraryPowerModeModel DefaultMode{get;}
    public LibraryPowerModeModel Mode
	{
		get => _mode ?? DefaultMode;
		set
		{
			_mode = value;
            RefreshIcon();
		}
	}
    public override string Suffix{
		get => Mode.Name;
	}
    public override string PackedIconPath
    {
        get
        {
            string fileName = base.Id.Entry.ToLowerInvariant()
                + (IsDynamic ? $"_{LowSuffix}" : string.Empty)
                + ".png";

            if (Mode.GetType().Assembly != typeof(LibraryPowerModel).Assembly)
            {
                return ImageHelper.GetImagePath($"powers/{fileName}");
            }

            return $"res://LibraryOfRuinaLib/images/powers/{fileName}";
        }
    }
    public async Task SetPowerMode<T>(PlayerChoiceContext choiceContext, Creature? dealer, CardModel? cardSource)
    where T:LibraryPowerModeModel,new()
	{
        T mode = new()
        {
            SourcePower = this
        };
        await SetPowerMode(choiceContext, mode, dealer, cardSource);
    }	
    public async Task SetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModeModel mode, Creature? dealer, CardModel? cardSource)
    {
        ICombatState? combatState = Owner?.CombatState;
        if(combatState == null)
            return;
        LibraryHookSubscribers.UnsubscribeForCombatStateHooks(Mode);
        LibraryHookSubscribers.SubscribeForCombatStateHooks(mode);
        await LibraryHooks.BeforeSetPowerMode(combatState, choiceContext, this, dealer, cardSource, mode);
        Mode = mode;
        await LibraryHooks.AfterSetPowerMode(combatState, choiceContext, this, dealer, cardSource, mode);
    }
    public override Task AfterApplied(Creature applier, CardModel cardSource)
    {
        LibraryHookSubscribers.SubscribeForCombatStateHooks(Mode);
        return base.AfterApplied(applier, cardSource);
    }
    public override Task AfterRemoved(Creature oldOwner)
    {
        LibraryHookSubscribers.UnsubscribeForCombatStateHooks(Mode);
        return base.AfterRemoved(oldOwner);
    }
}
