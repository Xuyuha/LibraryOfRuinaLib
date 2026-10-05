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
		get => _mode ??= BindMode(DefaultMode);
		set
		{
			_mode = BindMode(value);
            RefreshIcon();
		}
	}
    private LibraryPowerModeModel BindMode(LibraryPowerModeModel mode)
    {
        // 可变能力持有独立模式，避免克隆或图鉴实例共享战斗状态。
        if (IsMutable && (!mode.IsMutable || mode.SourcePower != this))
        {
            mode = (LibraryPowerModeModel)mode.MutableClone();
        }

        mode.SourcePower = this;
        return mode;
    }

    protected override void AfterCloned()
    {
        base.AfterCloned();
        if (_mode != null)
        {
            _mode = BindMode(_mode);
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
    where T:LibraryPowerModeModel
	{
        await SetPowerMode(choiceContext, LibraryPowerModeModel.Canonical<T>(), dealer, cardSource);
    }	
    public async Task SetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModeModel mode, Creature? dealer, CardModel? cardSource)
    {
        ICombatState? combatState = Owner?.CombatState;
        if (combatState == null)
        {
            return;
        }

        mode = BindMode(mode);
        await LibraryHooks.BeforeSetPowerMode(combatState, choiceContext, this, dealer, cardSource, mode);
        LibraryHookSubscribers.UnsubscribeForCombatStateHooks(Mode);
        Mode = mode;
        LibraryHookSubscribers.SubscribeForCombatStateHooks(Mode);
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
