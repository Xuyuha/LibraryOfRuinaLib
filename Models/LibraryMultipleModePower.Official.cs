#if STS2_0_111_0
using LibraryLib.Commands;
using LibraryLib.Entities.Creatures;
using LibraryLib.Hooks;
using LibraryLib.Localization.Dice;
using LibraryLib.Localization.LibraryDynamicVars;
using LibraryLib.Powers.LibraryPowerMode;
using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Models;
public abstract partial class LibraryMultipleModePowerModel
{
	public sealed override async Task AfterModifyingCardPlayResultLocation(CardModel card, CardLocation cardLocation)
	{
        await Mode.AfterModifyingCardPlayResultLocation(card, cardLocation);
        await AfterModifyingCardPlayResultLocation(card, cardLocation, null);
	}

	public sealed override CardLocation ModifyCardPlayResultLocation(CardModel card, bool isAutoPlay, ResourceInfo resources, CardLocation cardLocation)
	{
        cardLocation = Mode.ModifyCardPlayResultLocation(card, isAutoPlay, resources, cardLocation);
        cardLocation = ModifyCardPlayResultLocation(card, isAutoPlay, resources, cardLocation, null);
		return cardLocation;
	}

	public virtual Task AfterModifyingCardPlayResultLocation(CardModel card, CardLocation cardLocation, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual CardLocation ModifyCardPlayResultLocation(
		CardModel card,
		bool isAutoPlay,
		ResourceInfo resources,
		CardLocation cardLocation,
		object? _ = null)
	{
		return cardLocation;
	}

	public sealed override async Task AfterBlockBroken(PlayerChoiceContext choiceContext, Creature target, Creature? breaker)
	{
        await Mode.AfterBlockBroken(choiceContext, target, breaker);
        await AfterBlockBroken(choiceContext, target, breaker, null);
	}

	public virtual Task AfterBlockBroken(
		PlayerChoiceContext choiceContext,
		Creature target,
		Creature? breaker,
		object? _ = null)
	{
		return Task.CompletedTask;
	}

	public sealed override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		decimal n = 0;
        n += Mode.ModifyDamageAdditive(target, amount+n, props, dealer, cardSource, cardPlay);
        n += ModifyDamageAdditive(target, amount+n, props, dealer, cardSource, cardPlay, null);
		return n;
	}

	public sealed override decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
        decimal cap = Mode.ModifyDamageCap(target, props, dealer, cardSource, cardPlay);
        cap = Math.Min(cap, ModifyDamageCap(target, props, dealer, cardSource, cardPlay, null));
		return cap;
	}

	public sealed override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		decimal n = 1m;
        n *= Mode.ModifyDamageMultiplicative(target, amount*n, props, dealer, cardSource, cardPlay);
        n *= ModifyDamageMultiplicative(target, amount*n, props, dealer, cardSource, cardPlay, null);
		return n;
	}
}
#endif
