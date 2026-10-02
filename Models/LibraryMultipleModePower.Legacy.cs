#if STS2_0_107_1
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
	public sealed override async Task AfterModifyingCardPlayResultPileOrPosition(CardModel card, PileType pileType, CardPilePosition position)
	{
        await Mode.AfterModifyingCardPlayResultPileOrPosition(card, pileType, position);
        await AfterModifyingCardPlayResultPileOrPosition(card, pileType, position, null);
	}

	public sealed override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(CardModel card, bool isAutoPlay, ResourceInfo resources, PileType pileType, CardPilePosition position)
	{
        (pileType, position) = Mode.ModifyCardPlayResultPileTypeAndPosition(card, isAutoPlay, resources, pileType, position);
        (pileType, position) = ModifyCardPlayResultPileTypeAndPosition(card, isAutoPlay, resources, pileType, position, null);
		return (pileType, position);
	}

	public virtual Task AfterModifyingCardPlayResultPileOrPosition(CardModel card, PileType pileType, CardPilePosition position, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
		CardModel card,
		bool isAutoPlay,
		ResourceInfo resources,
		PileType pileType, CardPilePosition position,
		object? _ = null)
	{
		return (pileType, position);
	}

	public sealed override async Task AfterBlockBroken(Creature target)
	{
        await Mode.AfterBlockBroken(target);
        await AfterBlockBroken(target, null);
	}

	public virtual Task AfterBlockBroken(
		Creature target,
		object? _ = null)
	{
		return Task.CompletedTask;
	}

	public sealed override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		decimal n = 0;
        n += Mode.ModifyDamageAdditive(target, amount+n, props, dealer, cardSource, null);
        n += ModifyDamageAdditive(target, amount+n, props, dealer, cardSource, null, null);
		return n;
	}

	public sealed override decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
        decimal cap = Mode.ModifyDamageCap(target, props, dealer, cardSource, null);
        cap = Math.Min(cap, ModifyDamageCap(target, props, dealer, cardSource, null, null));
		return cap;
	}

	public sealed override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		decimal n = 1m;
        n *= Mode.ModifyDamageMultiplicative(target, amount*n, props, dealer, cardSource, null);
        n *= ModifyDamageMultiplicative(target, amount*n, props, dealer, cardSource, null, null);
		return n;
	}
}
#endif
