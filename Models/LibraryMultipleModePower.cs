using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Library.Utils;
using MegaCrit.Sts2.Core.ValueProps;
using Library.Entities.Creatures;
using Library.Resistance;
using Library.Powers.Mode;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using Library.Hooks;
namespace Library.Models;
public abstract class LibraryMultipleModePowerModel : LibraryPowerModel
{
	protected LibraryPowerMode? _mode;
    protected abstract LibraryPowerMode DefaultMode{get;}
    public LibraryPowerMode Mode
	{
		get => _mode ?? DefaultMode;
		set
		{
			_mode = value;
            RefreshIcon();
		}
	}
    public override string Suffix => Mode.Name;
    public async Task SetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerMode mode, Creature? dealer, CardModel? cardSource)
    {
        ICombatState? combatState = Owner?.CombatState;
        if(combatState == null)
            return;
        await LibraryHooks.BeforeSetPowerMode(combatState, choiceContext, this, dealer, cardSource, mode);
        Mode = mode;
        await LibraryHooks.AfterSetPowerMode(combatState, choiceContext, this, dealer, cardSource, mode);
    }
    public sealed override bool ShouldReuse(IEnumerable<Creature>? targets, LibraryDice dice)
    {
		bool flag = Mode.ShouldReuse(targets,dice);
		flag |= ShouldReuse(targets,dice,null);
		return flag;
    }
    public sealed override async Task AfterReusing(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice)
    {
        await Mode.AfterReusing(choiceContext, targets, dice);
        await AfterReusing(choiceContext, targets, dice , null);
    }
    public sealed override async Task BeforeDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice)
    {
		await Mode.BeforeDiceRoll(choiceContext, targets, dice);
        await BeforeDiceRoll(choiceContext, targets, dice , null);
    }
    public sealed override bool ShouldReroll(IEnumerable<Creature>? targets, LibraryDice dice)
	{
		bool flag = Mode.ShouldReroll(targets,dice);
		flag |= ShouldReroll(targets,dice,null);
		return flag;
	}
    public sealed override async Task BeforeDiceEffect(PlayerChoiceContext choiceContext,  IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice)
    {
        await Mode.BeforeDiceEffect(choiceContext, targets, cardSource, dice);
        await BeforeDiceEffect(choiceContext, targets, cardSource, dice , null);
    }
    public sealed override async Task AfterDiceEffect(PlayerChoiceContext choiceContext,  IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice)
    {
        await Mode.AfterDiceEffect(choiceContext, targets, cardSource, dice);
        await AfterDiceEffect(choiceContext, targets, cardSource, dice , null);	
    }
    public sealed override async Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer, LibraryDamageType type,LibraryResistanceLevel resistanceValue)
    {
        await Mode.BeforeSetChaoResistance(choiceContext, target, dealer, type, resistanceValue);
        await BeforeSetChaoResistance(choiceContext, target, dealer, type, resistanceValue , null);
    }
    public sealed override async Task AfterSetChaoResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer, LibraryDamageType type)
    {
        await Mode.AfterSetChaoResistance(choiceContext, target, dealer, type);
        await AfterSetChaoResistance(choiceContext, target, dealer, type , null);
    }
    public sealed override bool TrySetChaoResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer, LibraryDamageType type,LibraryResistanceLevel resistanceValue)
    {
        bool flag = Mode.TrySetChaoResistance(choiceContext, target, dealer, type, resistanceValue);
        flag &= TrySetChaoResistance(choiceContext, target, dealer, type, resistanceValue , null);
        return flag;
    }
    public sealed override bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer,LibraryDamageType type,LibraryResistanceLevel resistanceValue)
    {
        bool flag = Mode.TrySetPhysicalResistance(choiceContext, target, dealer, type, resistanceValue);
        flag &= TrySetPhysicalResistance(choiceContext, target, dealer, type, resistanceValue , null);
        return flag;
    }
    public sealed override async Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer,LibraryDamageType type,LibraryResistanceLevel resistanceValue)
    {
        await Mode.BeforeSetPhysicalResistance(choiceContext, target, dealer, type, resistanceValue);
        await BeforeSetPhysicalResistance(choiceContext, target, dealer, type, resistanceValue , null);
    }
    public sealed override async Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer,LibraryDamageType type)
    {
        await Mode.AfterSetPhysicalResistance(choiceContext, target, dealer, type);
        await AfterSetPhysicalResistance(choiceContext, target, dealer, type , null);
    }
    public sealed override bool TryDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice)
    {
        bool flag = Mode.TryDiceEffect(choiceContext, targets, cardSource, dice);
        flag &= TryDiceEffect(choiceContext, targets, cardSource, dice , null);
        return flag;
    }
    public sealed override async Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command)
    {
        await Mode.AfterAttack(choiceContext, command);
        await AfterAttack(choiceContext, command , null);
    }
    public sealed override async Task AfterBlockBroken(Creature target, LibraryDamageType type)
    {
        await Mode.AfterBlockBroken(target, type);
        await AfterBlockBroken(target, type , null);
    }
    public sealed override async Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type) 
    {
        await Mode.AfterChaoDamageGiven(choiceContext, dealer, results, props, target, cardSource, type);
        await AfterChaoDamageGiven(choiceContext, dealer, results, props, target, cardSource, type , null);
    }
    public sealed override async Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType    type)
    {
        await Mode.AfterChaoDamageReceived(choiceContext, target, result, props, dealer, cardSource, type);
        await AfterChaoDamageReceived(choiceContext, target, result, props, dealer, cardSource, type , null);
    }
    public sealed override async Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType    type)
    {
        await Mode.AfterChaoDamageReceivedLate(choiceContext, target, result, props, dealer, cardSource, type);
        await AfterChaoDamageReceivedLate(choiceContext, target, result, props, dealer, cardSource, type , null);
    }
    public sealed override async Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type)
    {
        await Mode.AfterCurrentChaoValueChanged(target, amount, type);
        await AfterCurrentChaoValueChanged(target, amount, type , null);
    }
    public sealed override async Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type)
    {
        await Mode.AfterCurrentHpChanged(creature, delta, type);
        await AfterCurrentHpChanged(creature, delta, type , null);
    }
    public sealed override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type)
    {
        await Mode.AfterDamageGiven(choiceContext, dealer, results, props, target, cardSource, type);
        await AfterDamageGiven(choiceContext, dealer, results, props, target, cardSource, type , null);
    }
    public sealed override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        await Mode.AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource, type);
        await AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource, type , null);   
    }
    public sealed override async Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        await Mode.AfterDamageReceivedLate(choiceContext, target, result, props, dealer, cardSource, type);
        await AfterDamageReceivedLate(choiceContext, target, result, props, dealer, cardSource, type , null);
    }
    public sealed override async Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type)
    {
        await Mode.AfterModifyingDamageAmount(cardSource, type);
        await AfterModifyingDamageAmount(cardSource, type , null);
    }
    public sealed override async Task AfterModifyingHpLostAfterOsty(LibraryDamageType type)
    {
        await Mode.AfterModifyingHpLostAfterOsty(type);
        await AfterModifyingHpLostAfterOsty(type , null);
    }
    public sealed override async Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type)
    {
        await Mode.AfterModifyingHpLostBeforeOsty(type);
        await AfterModifyingHpLostBeforeOsty(type , null);
    }
    public sealed override async Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        await Mode.AfterPowerEffect(choiceContext, power, dealer, cardSource);
        await AfterPowerEffect(choiceContext, power, dealer, cardSource , null);
    }
    public sealed override async Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        await Mode.AfterPowerReduce(choiceContext, power, dealer, cardSource);
        await AfterPowerReduce(choiceContext, power, dealer, cardSource , null);    
    }
    public sealed override async Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode)
    {
        await Mode.AfterSetPowerMode(choiceContext, power, dealer, cardSource, mode);
        await AfterSetPowerMode(choiceContext, power, dealer, cardSource, mode , null);
    }
    public sealed override async Task AfterStun(Creature creature)
    {
        await Mode.AfterStun(creature);
        await AfterStun(creature , null);
    }
    public sealed override async Task BeforeAttack(LibraryAttackCommand command)
    {
        await Mode.BeforeAttack(command);
        await BeforeAttack(command , null); 
    }
    public sealed override async Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        await Mode.BeforeChaoDamageReceived(choiceContext, target, amount, props, dealer, cardSource, type);
        await BeforeChaoDamageReceived(choiceContext, target, amount, props, dealer, cardSource, type , null);
    }
    public sealed override async Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        await Mode.BeforeDamageReceived(choiceContext, target, amount, props, dealer, cardSource, type);
        await BeforeDamageReceived(choiceContext, target, amount, props, dealer, cardSource, type , null);
    }
    public sealed override async Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        await Mode.BeforePowerEffect(choiceContext, power, dealer, cardSource);
        await BeforePowerEffect(choiceContext, power, dealer, cardSource , null);
    }
    public sealed override async Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        await Mode.BeforePowerReduce(choiceContext, power, dealer, cardSource);
        await BeforePowerReduce(choiceContext, power, dealer, cardSource , null);
    }
    public sealed override async Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode)
    {
        await Mode.BeforeSetPowerMode(choiceContext, power, dealer, cardSource, mode);
        await BeforeSetPowerMode(choiceContext, power, dealer, cardSource, mode , null);
    }
    public sealed override async Task BeforeStun(Creature creature)
    {
        await Mode.BeforeStun(creature);
        await BeforeStun(creature , null);
    }
    public sealed override int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num)
    {
        num = Mode.ModifyAttackHitCount(attackCommand, num);
        num = ModifyAttackHitCount(attackCommand, num , null);
        return num;
    }
    public sealed override decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
		decimal n = 0;
        n += Mode.ModifyChaoDamageAdditive(target, num+n, props, dealer, cardSource, type);
        n += ModifyChaoDamageAdditive(target, num+n, props, dealer, cardSource, type , null);
        return n;
    }
    public sealed override decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        decimal num= Mode.ModifyChaoDamageCap(target, props, dealer, cardSource, type);
        num = Math.Min(num,ModifyChaoDamageCap(target, props, dealer, cardSource, type , null));
        return num;
    }
    public sealed override decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
		decimal n = 1;
        n *= Mode.ModifyChaoDamageMultiplicative(target, num*n, props, dealer, cardSource, type);
        n *= ModifyChaoDamageMultiplicative(target, num*n, props, dealer, cardSource, type , null);
        return n;
    }
    public sealed override decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
		decimal n = 0;
        n += Mode.ModifyDamageAdditive(target, num+n, props, dealer, cardSource, type);
        n += ModifyDamageAdditive(target, num+n, props, dealer, cardSource, type , null);
        return n;
    }
    public sealed override decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        decimal num= Mode.ModifyDamageCap(target, props, dealer, cardSource, type);
        num = Math.Min(num,ModifyDamageCap(target, props, dealer, cardSource, type , null));
        return num;
    }
    public sealed override decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
		decimal n = 1m;
        n *= Mode.ModifyDamageMultiplicative(target, num*n, props, dealer, cardSource, type);
        n *= ModifyDamageMultiplicative(target, num*n, props, dealer, cardSource, type , null);
        return n;
    }
    public sealed override decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        num= Mode.ModifyHpLostAfterOsty(target, num, props, dealer, cardSource, type);
        num = ModifyHpLostAfterOsty(target, num, props, dealer, cardSource, type , null);
        return num;
    }
    public sealed override decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        num= Mode.ModifyHpLostAfterOstyLate(target, num, props, dealer, cardSource, type);
        num = ModifyHpLostAfterOstyLate(target, num, props, dealer, cardSource, type , null);
        return num;
    }
    public sealed override decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        num= Mode.ModifyHpLostBeforeOsty(target, num, props, dealer, cardSource, type);
        num = ModifyHpLostBeforeOsty(target, num, props, dealer, cardSource, type , null);
        return num;
    }
    public sealed override decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        num= Mode.ModifyHpLostBeforeOstyLate(target, num, props, dealer, cardSource, type);
        num = ModifyHpLostBeforeOstyLate(target, num, props, dealer, cardSource, type , null);
        return num;
    }
    public sealed override Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type)
    {
        creature= Mode.ModifyUnblockedDamageTarget(creature, amount, props, dealer, type);
        creature = ModifyUnblockedDamageTarget(creature, amount, props, dealer, type , null);
        return creature;
    }
    public sealed override bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        bool flag= Mode.TryPowerEffect(choiceContext, power, dealer, cardSource);
        flag &= TryPowerEffect(choiceContext, power, dealer, cardSource , null) ;
        return flag;
    }
    public sealed override bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        bool flag= Mode.TryPowerReduce(choiceContext, power, dealer, cardSource);
        flag &= TryPowerReduce(choiceContext, power, dealer, cardSource , null) ;
        return flag;
    }
	public sealed override async Task AfterActEntered()
	{
        await Mode.AfterActEntered();
        await AfterActEntered(null);
	}

	public sealed override async Task AfterAddToDeckPrevented(CardModel card)
	{
        await Mode.AfterAddToDeckPrevented(card);
        await AfterAddToDeckPrevented(card , null);
	}

	public sealed override async Task BeforeAttack(AttackCommand command)
	{
        await Mode.BeforeAttack(command);
        await BeforeAttack(command , null);
	}

	public sealed override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
	{
        await Mode.AfterAttack(choiceContext, command);
        await AfterAttack(choiceContext, command , null);
	}

	public sealed override async Task AfterAutoPostPlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
	{
        await Mode.AfterAutoPostPlayPhaseEntered(choiceContext, player);
        await AfterAutoPostPlayPhaseEntered(choiceContext, player , null);
	}

	public sealed override async Task AfterAutoPrePlayPhaseEnteredEarly(PlayerChoiceContext choiceContext, Player player)
	{
        await Mode.AfterAutoPrePlayPhaseEnteredEarly(choiceContext, player);
        await AfterAutoPrePlayPhaseEnteredEarly(choiceContext, player , null);
	}

	public sealed override async Task AfterAutoPrePlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
	{
        await Mode.AfterAutoPrePlayPhaseEntered(choiceContext, player);
        await AfterAutoPrePlayPhaseEntered(choiceContext, player , null);
	}

	public sealed override async Task AfterAutoPrePlayPhaseEnteredLate(PlayerChoiceContext choiceContext, Player player)
	{
        await Mode.AfterAutoPrePlayPhaseEnteredLate(choiceContext, player);
        await AfterAutoPrePlayPhaseEnteredLate(choiceContext, player , null);
	}

	public sealed override async Task AfterBlockCleared(Creature creature)
	{
        await Mode.AfterBlockCleared(creature);
        await AfterBlockCleared(creature , null);
	}

	public sealed override async Task BeforeBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
	{
        await Mode.BeforeBlockGained(creature, amount, props, cardSource);
        await BeforeBlockGained(creature, amount, props, cardSource , null);
	}

	public sealed override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
	{
        await Mode.AfterBlockGained(creature, amount, props, cardSource);
        await AfterBlockGained(creature, amount, props, cardSource , null);
	}

	public sealed override async Task AfterBlockBroken(PlayerChoiceContext choiceContext, Creature target, Creature? breaker)
	{
        await Mode.AfterBlockBroken(choiceContext, target, breaker);
        await AfterBlockBroken(choiceContext, target, breaker, null);
	}

	public sealed override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
	{
        await Mode.AfterCardChangedPiles(card, oldPileType, clonedBy);
        await AfterCardChangedPiles(card, oldPileType, clonedBy , null);
	}

	public sealed override async Task AfterCardChangedPilesLate(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
	{
        await Mode.AfterCardChangedPilesLate(card, oldPileType, clonedBy);
        await AfterCardChangedPilesLate(card, oldPileType, clonedBy , null);
	}

	public sealed override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
	{
        await Mode.AfterCardDiscarded(choiceContext, card);
        await AfterCardDiscarded(choiceContext, card , null);
	}

	public sealed override async Task AfterCardDrawnEarly(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
        await Mode.AfterCardDrawnEarly(choiceContext, card, fromHandDraw);
        await AfterCardDrawnEarly(choiceContext, card, fromHandDraw , null);
	}

	public sealed override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
        await Mode.AfterCardDrawn(choiceContext, card, fromHandDraw);
        await AfterCardDrawn(choiceContext, card, fromHandDraw , null);
	}

	public sealed override async Task AfterCardEnteredCombat(CardModel card)
	{
        await Mode.AfterCardEnteredCombat(card);
        await AfterCardEnteredCombat(card , null);
	}

	public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
	{
        await Mode.AfterCardGeneratedForCombat(card, creator);
        await AfterCardGeneratedForCombat(card, creator , null);
	}

	public sealed override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
	{
        await Mode.AfterCardExhausted(choiceContext, card, causedByEthereal);
        await AfterCardExhausted(choiceContext, card, causedByEthereal , null);
	}

	public sealed override async Task BeforeCardAutoPlayed(CardModel card, Creature? target, AutoPlayType type)
	{
        await Mode.BeforeCardAutoPlayed(card, target, type);
        await BeforeCardAutoPlayed(card, target, type , null);
	}

	public sealed override async Task BeforeCardPlayed(CardPlay cardPlay)
	{
        await Mode.BeforeCardPlayed(cardPlay);
        await BeforeCardPlayed(cardPlay , null);        
	}

	public sealed override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{   
        await Mode.AfterCardPlayed(choiceContext, cardPlay);
        await AfterCardPlayed(choiceContext, cardPlay , null);
	}

	public sealed override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
        await Mode.AfterCardPlayedLate(choiceContext, cardPlay);
        await AfterCardPlayedLate(choiceContext, cardPlay , null);
	}

	public sealed override async Task BeforeCombatStart()
	{
        await Mode.BeforeCombatStart();
        await BeforeCombatStart(null);
	}

	public sealed override async Task BeforeCombatStartLate()
	{
        await Mode.BeforeCombatStartLate();
        await BeforeCombatStartLate(null);  
	}
	public sealed override async Task AfterCreatureAddedToCombat(Creature creature)
	{
        await Mode.AfterCreatureAddedToCombat(creature);
        await AfterCreatureAddedToCombat(creature , null); 
	}
	public sealed override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
	{
        await Mode.AfterCurrentHpChanged(creature, delta);
        await AfterCurrentHpChanged(creature, delta , null  );
	}

	public sealed override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
        await Mode.AfterDamageGiven(choiceContext, dealer, result, props, target, cardSource);
        await AfterDamageGiven(choiceContext, dealer, result, props, target, cardSource , null);
	}

	public sealed override async Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
        await Mode.BeforeDamageReceived(choiceContext, target, amount, props, dealer, cardSource);
        await BeforeDamageReceived(choiceContext, target, amount, props, dealer, cardSource , null);
	}

	public sealed override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
        await Mode.AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource);
        await AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource , null);
	}

	public sealed override async Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
        await Mode.AfterDamageReceivedLate(choiceContext, target, result, props, dealer, cardSource);
        await AfterDamageReceivedLate(choiceContext, target, result, props, dealer, cardSource , null);
	}

	public sealed override async Task BeforeDeath(Creature creature)
	{
        await Mode.BeforeDeath(creature);
        await BeforeDeath(creature , null);
	}

	public sealed override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
        await Mode.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
        await AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength , null);
	}

	public sealed override async Task AfterDiedToDoom(PlayerChoiceContext choiceContext, IReadOnlyList<Creature> creatures)
	{
        await Mode.AfterDiedToDoom(choiceContext, creatures);
        await AfterDiedToDoom(choiceContext, creatures , null);
	}

	public sealed override async Task AfterEnergyReset(Player player)
	{
        await Mode.AfterEnergyReset(player);
        await AfterEnergyReset(player , null);
	}

	public sealed override async Task AfterEnergyResetLate(Player player)
	{
        await Mode.AfterEnergyResetLate(player);
        await AfterEnergyResetLate(player , null);
	}

	public sealed override async Task AfterEnergySpent(CardModel card, int amount)
	{
        await Mode.AfterEnergySpent(card, amount);
        await AfterEnergySpent(card, amount , null);
	}

	public sealed override async Task BeforeCardRemoved(CardModel card)
	{
        await Mode.BeforeCardRemoved(card);
        await BeforeCardRemoved(card , null );
	}

	public sealed override async Task BeforeFlush(PlayerChoiceContext choiceContext, Player player)
	{
        await Mode.BeforeFlush(choiceContext, player);
        await BeforeFlush(choiceContext, player , null);
	}

	public sealed override async Task BeforeFlushLate(PlayerChoiceContext choiceContext, Player player)
	{
        await Mode.BeforeFlushLate(choiceContext, player);
        await BeforeFlushLate(choiceContext, player, null);
	}

	public sealed override async Task AfterFlush(PlayerChoiceContext choiceContext, Player player, IReadOnlyCollection<CardModel> flushedCards, IReadOnlyCollection<CardModel> retainedCards)
	{
        await Mode.AfterFlush(choiceContext, player, flushedCards, retainedCards);
        await AfterFlush(choiceContext, player, flushedCards, retainedCards , null);
	}

	public sealed override async Task AfterGoldGained(Player player)
	{
        await Mode.AfterGoldGained(player);
        await AfterGoldGained(player , null);
	}

	public sealed override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
	{
        await Mode.BeforeHandDraw(player, choiceContext, combatState);
        await BeforeHandDraw(player, choiceContext, combatState , null);
	}

	public sealed override async Task BeforeHandDrawLate(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
	{
        await Mode.BeforeHandDrawLate(player, choiceContext, combatState);
        await BeforeHandDrawLate(player, choiceContext, combatState, null);
	}

	public override async Task AfterHandEmptied(PlayerChoiceContext choiceContext, Player player)
	{
        await Mode.AfterHandEmptied(choiceContext, player);
        await AfterHandEmptied(choiceContext, player);
	}
	public override async Task AfterModifyingBlockAmount(decimal modifiedAmount, CardModel? cardSource, CardPlay? cardPlay)
	{
        await Mode.AfterModifyingBlockAmount(modifiedAmount, cardSource, cardPlay);
        await AfterModifyingBlockAmount(modifiedAmount, cardSource, cardPlay);
	}

	public override async Task AfterModifyingCardPlayCount(CardModel card)
	{
        await Mode.AfterModifyingCardPlayCount(card);
        await AfterModifyingCardPlayCount(card);
	}

	public sealed override async Task AfterModifyingCardPlayResultLocation(CardModel card, CardLocation cardLocation)
	{
        await Mode.AfterModifyingCardPlayResultLocation(card, cardLocation);
        await AfterModifyingCardPlayResultLocation(card, cardLocation, null);
	}

	public sealed override async Task AfterModifyingOrbPassiveTriggerCount(OrbModel orb)
	{
        await Mode.AfterModifyingOrbPassiveTriggerCount(orb);
        await AfterModifyingOrbPassiveTriggerCount(orb , null);
	}

	public sealed override async Task AfterModifyingCardRewardOptions()
	{
        await Mode.AfterModifyingCardRewardOptions();
        await AfterModifyingCardRewardOptions(null);
	}

	public sealed override async Task AfterModifyingDamageAmount(CardModel? cardSource)
	{
        await Mode.AfterModifyingDamageAmount(cardSource);
        await AfterModifyingDamageAmount(cardSource , null);
	}
    public sealed override async Task AfterModifyingChaoDamageAmount(CardModel? cardSource, LibraryDamageType type)
    {
        await Mode.AfterModifyingChaoDamageAmount(cardSource, type);
        await AfterModifyingChaoDamageAmount(cardSource, type , null);
    }
    public sealed override async Task AfterModifyingEffectiveAmount(CardModel? cardSource, LibraryBasePowerModel power)
    {
        await Mode.AfterModifyingEffectiveAmount(cardSource, power);
        await AfterModifyingEffectiveAmount(cardSource, power , null);
    }
	public sealed override async Task AfterModifyingEnergyGain()
	{
        await Mode.AfterModifyingEnergyGain();
        await AfterModifyingEnergyGain(null);
	}

	public sealed override async Task AfterModifyingHandDraw()
	{
        await Mode.AfterModifyingHandDraw();
        await AfterModifyingHandDraw(null);
	}

	public sealed  override async Task AfterPreventingDraw()
	{
        await Mode.AfterPreventingDraw();
        await AfterPreventingDraw(null);
	}

	public sealed override async Task AfterModifyingHpLostBeforeOsty()
	{
        await Mode.AfterModifyingHpLostBeforeOsty();
        await AfterModifyingHpLostBeforeOsty(null);
	}

	public sealed override async Task AfterModifyingHpLostAfterOsty()
	{
        await Mode.AfterModifyingHpLostAfterOsty();
        await AfterModifyingHpLostAfterOsty(null);
	}

	public sealed override async Task AfterModifyingPowerAmountReceived(PowerModel power)
	{
        await Mode.AfterModifyingPowerAmountReceived(power);
        await AfterModifyingPowerAmountReceived(power , null);
	}

	public sealed override async Task AfterModifyingPowerAmountGiven(PowerModel power)
	{
        await Mode.AfterModifyingPowerAmountGiven(power);
        await AfterModifyingPowerAmountGiven(power , null);
	}

	public sealed override async Task AfterModifyingRewards()
	{
        await Mode.AfterModifyingRewards();
        await AfterModifyingRewards(null);
	}

	public sealed override async Task AfterOrbChanneled(PlayerChoiceContext choiceContext, Player player, OrbModel orb)
	{
        await Mode.AfterOrbChanneled(choiceContext, player, orb);
        await AfterOrbChanneled(choiceContext, player, orb , null    );
	}

	public sealed override async Task AfterOrbEvoked(PlayerChoiceContext choiceContext, OrbModel orb, IEnumerable<Creature> targets)
	{
        await Mode.AfterOrbEvoked(choiceContext, orb, targets);
        await AfterOrbEvoked(choiceContext, orb, targets , null);
	}

	public sealed override async Task AfterOstyRevived(Creature osty)
	{
        await Mode.AfterOstyRevived(osty);
        await AfterOstyRevived(osty , null);
	}

	public sealed override async Task BeforePotionUsed(PotionModel potion, Creature? target)
	{
        await Mode.BeforePotionUsed(potion, target);
        await BeforePotionUsed(potion, target, null);
	}

	public sealed override async Task AfterPotionUsed(PotionModel potion, Creature? target)
	{
        await Mode.AfterPotionUsed(potion, target);
        await AfterPotionUsed(potion, target , null);
	}

	public sealed override async Task AfterPotionDiscarded(PotionModel potion)
	{
        await Mode.AfterPotionDiscarded(potion);
        await AfterPotionDiscarded(potion , null);
	}

	public sealed override async Task AfterPotionProcured(PotionModel potion)
	{
        await Mode.AfterPotionProcured(potion);
        await AfterPotionProcured(potion , null);
	}

	public sealed override async Task BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)
	{
        await Mode.BeforePowerAmountChanged(power, amount, target, applier, cardSource);
        await BeforePowerAmountChanged(power, amount, target, applier, cardSource , null);
	}

	public sealed override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
        await Mode.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);
        await AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource , null);
	}

	public sealed override async Task AfterPreventingBlockClear(AbstractModel preventer, Creature creature)
	{
        await Mode.AfterPreventingBlockClear(preventer, creature);
        await AfterPreventingBlockClear(preventer, creature , null);
	}

	public sealed override async Task AfterPreventingDeath(Creature creature)
	{
        await Mode.AfterPreventingDeath(creature);
        await AfterPreventingDeath(creature , null);
	}

	public sealed override async Task AfterRestSiteHeal(Player player, bool isMimicked)
	{
        await Mode.AfterRestSiteHeal(player, isMimicked);
        await AfterRestSiteHeal(player, isMimicked , null);
	}

	public sealed override async Task AfterRestSiteSmith(Player player)
	{
        await Mode.AfterRestSiteSmith(player);
        await AfterRestSiteSmith(player , null);
	}

	public sealed override async Task AfterRewardTaken(Player player, Reward reward)
	{
        await Mode.AfterRewardTaken(player, reward);
        await AfterRewardTaken(player, reward , null);
	}

	public sealed override async Task BeforeRoomEntered(AbstractRoom room)
	{
        await Mode.BeforeRoomEntered(room);
        await BeforeRoomEntered(room,null);
	}

	public sealed override async Task AfterRoomEntered(AbstractRoom room)
	{
        await Mode.AfterRoomEntered(room);
        await AfterRoomEntered(room,null    );
	}

	public sealed override async Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
	{
        await Mode.AfterShuffle(choiceContext, shuffler);
        await AfterShuffle(choiceContext, shuffler , null);
	}

	public sealed override async Task AfterStarsSpent(int amount, Player spender)
	{
        await Mode.AfterStarsSpent(amount, spender);
        await AfterStarsSpent(amount, spender , null);
	}

	public sealed override async Task AfterStarsGained(int amount, Player gainer)
	{
        await Mode.AfterStarsGained(amount, gainer);
        await AfterStarsGained(amount, gainer , null);
	}

	public sealed override async Task AfterForge(decimal amount, Player forger, AbstractModel? source)
	{
        await Mode.AfterForge(amount, forger, source);
        await AfterForge(amount, forger, source , null);
	}

	public sealed override async Task AfterSummon(PlayerChoiceContext choiceContext, Player summoner, decimal amount)
	{
        await Mode.AfterSummon(choiceContext, summoner, amount);
        await AfterSummon(choiceContext, summoner, amount , null);
	}

	public sealed override async Task AfterTakingExtraTurn(Player player)
	{
        await Mode.AfterTakingExtraTurn(player);
        await AfterTakingExtraTurn(player , null);
	}

	public sealed override async Task AfterTargetingBlockedVfx(Creature blocker)
	{
        await Mode.AfterTargetingBlockedVfx(blocker);
        await AfterTargetingBlockedVfx(blocker , null);
	}

	public sealed override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
        await Mode.BeforeSideTurnStart(choiceContext, side, participants, combatState);
        await BeforeSideTurnStart(choiceContext, side, participants, combatState , null);
	}

	public sealed override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
        await Mode.AfterSideTurnStart(side, participants, combatState);
        await AfterSideTurnStart(side, participants, combatState , null);
	}

	public sealed override async Task AfterSideTurnStartLate(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
        await Mode.AfterSideTurnStartLate(side, participants, combatState);
        await AfterSideTurnStartLate(side, participants, combatState , null);
	}

	public sealed override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
	{
        await Mode.AfterPlayerTurnStartEarly(choiceContext, player);
        await AfterPlayerTurnStartEarly(choiceContext, player , null);
	}

	public sealed override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
        await Mode.AfterPlayerTurnStart(choiceContext, player);
        await AfterPlayerTurnStart(choiceContext, player , null);
	}

	public sealed override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
	{
        await Mode.AfterPlayerTurnStartLate(choiceContext, player);
        await AfterPlayerTurnStartLate(choiceContext, player , null);
	}

	public sealed override async Task BeforeSideTurnEndVeryEarly(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
        await Mode.BeforeSideTurnEndVeryEarly(choiceContext, side, participants);
        await BeforeSideTurnEndVeryEarly(choiceContext, side, participants , null);
	}

	public sealed override async Task BeforeSideTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
        await Mode.BeforeSideTurnEndEarly(choiceContext, side, participants);
        await BeforeSideTurnEndEarly(choiceContext, side, participants , null);
	}

	public sealed override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
        await Mode.BeforeSideTurnEnd(choiceContext, side, participants);
        await BeforeSideTurnEnd(choiceContext, side, participants , null);
	}

	public sealed override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
        await Mode.AfterSideTurnEnd(choiceContext, side, participants);
        await AfterSideTurnEnd(choiceContext, side, participants , null);
	}

	public sealed override async Task AfterSideTurnEndLate(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
        await Mode.AfterSideTurnEndLate(choiceContext, side, participants);
        await AfterSideTurnEndLate(choiceContext, side, participants , null);
	}

	public sealed override int ModifyAttackHitCount(AttackCommand attack, int hitCount)
	{
        hitCount = Mode.ModifyAttackHitCount(attack, hitCount);
        hitCount = ModifyAttackHitCount(attack, hitCount , null);
		return hitCount;
	}

	public sealed override decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		decimal n = 0;
        n += Mode.ModifyBlockAdditive(target, block+n, props, cardSource, cardPlay);
        n += ModifyBlockAdditive(target, block+n, props, cardSource, cardPlay , null);
		return n;
	}

	public sealed override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		decimal n = 1m;
        n *= Mode.ModifyBlockMultiplicative(target, block*n, props, cardSource, cardPlay);
        n *= ModifyBlockMultiplicative(target, block*n, props, cardSource, cardPlay , null);
		return n;
	}

	public sealed override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
        playCount = Mode.ModifyCardPlayCount(card, target, playCount);
        playCount = ModifyCardPlayCount(card, target, playCount , null);
		return playCount;
	}

	public sealed override CardLocation ModifyCardPlayResultLocation(CardModel card, bool isAutoPlay, ResourceInfo resources, CardLocation cardLocation)
	{
        cardLocation = Mode.ModifyCardPlayResultLocation(card, isAutoPlay, resources, cardLocation);
        cardLocation = ModifyCardPlayResultLocation(card, isAutoPlay, resources, cardLocation, null);
		return cardLocation;
	}

	public sealed override int ModifyOrbPassiveTriggerCounts(OrbModel orb, int triggerCount)
	{
        triggerCount = Mode.ModifyOrbPassiveTriggerCounts(orb, triggerCount);
        triggerCount = ModifyOrbPassiveTriggerCounts(orb, triggerCount , null);
		return triggerCount;
	}

	public sealed override CardCreationOptions ModifyCardRewardCreationOptions(Player player, CardCreationOptions options)
	{
        options = Mode.ModifyCardRewardCreationOptions(player, options);
        options = ModifyCardRewardCreationOptions(player, options , null);
		return options;
	}

	public sealed override CardCreationOptions ModifyCardRewardCreationOptionsLate(Player player, CardCreationOptions options)
	{
        options = Mode.ModifyCardRewardCreationOptionsLate(player, options);
        options = ModifyCardRewardCreationOptionsLate(player, options , null);
		return options;
	}

	public sealed override decimal ModifyCardRewardUpgradeOdds(Player player, CardModel card, decimal odds)
	{
        odds = Mode.ModifyCardRewardUpgradeOdds(player, card, odds);
        odds = ModifyCardRewardUpgradeOdds(player, card, odds,null);
		return odds;
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


	public sealed override decimal ModifyEnergyGain(Player player, decimal amount)
	{
        amount = Mode?.ModifyEnergyGain(player, amount) ?? amount;
        amount = ModifyEnergyGain(player, amount , null);
		return amount;
	}
#if STS2_BETA
	public sealed override decimal ModifyGoldGained(Player player, decimal amount)
	{
		amount = Mode.ModifyGoldGained(player, amount);
		amount = ModifyGoldGained(player, amount , null);
		return amount;
	}
#endif

	public sealed override decimal ModifyHandDraw(Player player, decimal count)
	{
        count = Mode?.ModifyHandDraw(player, count) ?? count;
        count = ModifyHandDraw(player, count , null);
		return count;
	}

	public sealed override decimal ModifyHandDrawLate(Player player, decimal count)
	{
        count = Mode.ModifyHandDrawLate(player, count);
        count = ModifyHandDrawLate(player, count , null);
		return count;
	}

	public sealed override decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
        amount = Mode.ModifyHpLostBeforeOsty(target, amount, props, dealer, cardSource);
        amount = ModifyHpLostBeforeOsty(target, amount, props, dealer, cardSource , null);
		return amount;
	}

	public sealed override decimal ModifyHpLostBeforeOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
        amount = Mode.ModifyHpLostBeforeOstyLate(target, amount, props, dealer, cardSource);
        amount = ModifyHpLostBeforeOstyLate(target, amount, props, dealer, cardSource , null);
		return amount;
	}

	public sealed override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
        amount = Mode.ModifyHpLostAfterOsty(target, amount, props, dealer, cardSource);
        amount = ModifyHpLostAfterOsty(target, amount, props, dealer, cardSource , null);
		return amount;
	}

	public sealed override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
        amount = Mode.ModifyHpLostAfterOstyLate(target, amount, props, dealer, cardSource);
        amount = ModifyHpLostAfterOstyLate(target, amount, props, dealer, cardSource , null);
		return amount;
	}

	public sealed override decimal ModifyMaxEnergy(Player player, decimal amount)
	{
        amount = Mode.ModifyMaxEnergy(player, amount);
        amount = ModifyMaxEnergy(player, amount , null);
		return amount;
	}

    public sealed override decimal ModifyOrbValue(OrbModel orb, decimal value)
	{
        value = Mode.ModifyOrbValue(orb, value);
        value = ModifyOrbValue(orb, value , null);
		return value;
	}

#if STS2_BETA
	public sealed override decimal ModifyPowerAmountGivenAdditive(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
	{
		decimal n = 0;
        n += Mode.ModifyPowerAmountGivenAdditive(power, giver, amount+n, target, cardSource);
        n += ModifyPowerAmountGivenAdditive(power, giver, amount+n, target, cardSource , null);
		return n;
	}

	public sealed override decimal ModifyPowerAmountGivenMultiplicative(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
	{
		decimal n = 1m;
        n *= Mode.ModifyPowerAmountGivenMultiplicative(power, giver, amount*n, target, cardSource);
        n *= ModifyPowerAmountGivenMultiplicative(power, giver, amount*n, target, cardSource , null);
		return n;
	}
#endif
    public sealed override decimal ModifyEffectiveAmountAdditive(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource)
    {
		decimal n = 0;
        n += Mode.ModifyEffectiveAmountAdditive(power, num, dealer, cardSource);
        n += ModifyEffectiveAmountAdditive(power, num, dealer, cardSource , null);
		return n;
    }
    public sealed override decimal ModifyEffectiveAmountMultiplicative(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource)
    {
		decimal n = 1m;
        n *= Mode.ModifyEffectiveAmountMultiplicative(power, num, dealer, cardSource);
        n *= ModifyEffectiveAmountMultiplicative(power, num, dealer, cardSource , null);
		return n;
    }

	public sealed override void ModifyShuffleOrder(Player player, List<CardModel> cards, bool isInitialShuffle)
	{
        Mode.ModifyShuffleOrder(player, cards, isInitialShuffle);
        ModifyShuffleOrder(player, cards, isInitialShuffle , null);
	}

	public sealed override decimal ModifySummonAmount(Player summoner, decimal amount, AbstractModel? source)
	{
        amount = Mode.ModifySummonAmount(summoner, amount, source);
        amount = ModifySummonAmount(summoner, amount, source , null);
		return amount;
	}

	public sealed override Creature ModifyUnblockedDamageTarget(Creature target, decimal amount, ValueProp props, Creature? dealer)
	{
        target = Mode.ModifyUnblockedDamageTarget(target, amount, props, dealer);
        target = ModifyUnblockedDamageTarget(target, amount, props, dealer , null    );
		return target;
	}
	public sealed override int ModifyXValue(CardModel card, int originalValue)
	{
        originalValue = Mode.ModifyXValue(card, originalValue);
        originalValue = ModifyXValue(card, originalValue , null);
		return originalValue;
	}

	public sealed override bool TryModifyCardBeingAddedToDeck(CardModel card, out CardModel? cardModel)
	{
		if (Mode.TryModifyCardBeingAddedToDeck(card, out CardModel newCard) && newCard != null)
		{
			cardModel = newCard;
			return true;
		}
        if(TryModifyCardBeingAddedToDeck(card, out CardModel newCard1 , null) && newCard1 != null){
            cardModel = newCard1;
            return true;
        }
        cardModel = null;
        return false;
	}

	public sealed override bool TryModifyCardBeingAddedToDeckLate(CardModel card, out CardModel? cardModel)
	{
		if (Mode.TryModifyCardBeingAddedToDeckLate(card, out CardModel newCard) && newCard != null)
		{
			cardModel = newCard;
			return true;
		}
        if(TryModifyCardBeingAddedToDeckLate(card, out CardModel newCard1 , null) && newCard1 != null){
            cardModel = newCard1;
            return true;
        }
        cardModel = null;
        return false;
	}

	public sealed override bool TryModifyCardRewardAlternatives(Player player, CardReward cardReward, List<CardRewardAlternative> alternatives)
	{
        bool flag =Mode.TryModifyCardRewardAlternatives(player, cardReward, alternatives);
        flag |= TryModifyCardRewardAlternatives(player, cardReward, alternatives , null);
        return flag;
	}

	public sealed override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
	{
        bool flag =Mode.TryModifyCardRewardOptions(player, cardRewardOptions, creationOptions);
        flag |= TryModifyCardRewardOptions(player, cardRewardOptions, creationOptions , null);
        return flag;
	}

	public sealed override bool TryModifyCardRewardOptionsLate(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
	{
        bool flag =Mode.TryModifyCardRewardOptionsLate(player, cardRewardOptions, creationOptions);
        flag |= TryModifyCardRewardOptionsLate(player, cardRewardOptions, creationOptions , null);
        return flag;
	}

	public sealed override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
        modifiedCost = originalCost;
		bool flag = Mode.TryModifyEnergyCostInCombat(card, modifiedCost,out  modifiedCost) ;
		flag |=TryModifyEnergyCostInCombat(card, modifiedCost, out  modifiedCost , null);
        return flag;
	}

	public sealed override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
        modifiedCost = originalCost;
		bool flag = Mode.TryModifyEnergyCostInCombatLate(card, modifiedCost,out  modifiedCost) ;
		flag |=TryModifyEnergyCostInCombatLate(card, modifiedCost, out  modifiedCost , null);
        return flag;
	}

	public sealed override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
        modifiedCost = originalCost;
		bool flag = Mode.TryModifyStarCost(card, modifiedCost,out  modifiedCost) ;
		flag |=TryModifyStarCost(card, modifiedCost, out  modifiedCost , null);
        return flag;
	}

	public sealed override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
	{
		modifiedAmount = amount;
        bool flag = Mode.TryModifyPowerAmountReceived(canonicalPower, target, modifiedAmount, applier, out modifiedAmount) ;
		flag |=TryModifyPowerAmountReceived(canonicalPower, target, modifiedAmount, applier, out modifiedAmount , null);
		return flag;
	}
	public sealed override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
	{
        bool flag = Mode.TryModifyRewards(player, rewards, room);
        flag |= TryModifyRewards(player, rewards, room , null);
		return flag;
	}

	public sealed override bool TryModifyRewardsLate(Player player, List<Reward> rewards, AbstractRoom? room)
	{
        bool flag = Mode.TryModifyRewardsLate(player, rewards, room);
        flag |= TryModifyRewardsLate(player, rewards, room , null);
		return flag;
	}

	public sealed override bool ShouldAddToDeck(CardModel card)
	{
        bool flag = Mode.ShouldAddToDeck(card);
        flag &= ShouldAddToDeck(card , null);
		return flag;
	}

	public sealed override bool ShouldAfflict(CardModel card, AfflictionModel affliction)
	{
        bool flag = Mode.ShouldAfflict(card, affliction);
        flag &= ShouldAfflict(card, affliction , null);
		return flag;
	}

	public sealed override bool ShouldAllowHitting(Creature creature)
	{
        bool flag = Mode.ShouldAllowHitting(creature);
        flag &= ShouldAllowHitting(creature , null);
		return flag;
	}

	public sealed override bool ShouldAllowTargeting(Creature target)
	{
        bool flag = Mode.ShouldAllowTargeting(target);
        flag &= ShouldAllowTargeting(target , null);
		return flag;
	}

	public sealed override bool ShouldAllowSelectingMoreCardRewards(Player player, CardReward cardReward)
	{
        bool flag = Mode.ShouldAllowSelectingMoreCardRewards(player, cardReward);
        flag |= ShouldAllowSelectingMoreCardRewards(player, cardReward , null);
		return flag;
	}

	public sealed override bool ShouldClearBlock(Creature creature)
	{
        bool flag = Mode.ShouldClearBlock(creature);
        flag &= ShouldClearBlock(creature , null);
		return flag;
	}

	public sealed override bool ShouldDie(Creature creature)
	{
        bool flag = Mode.ShouldDie(creature);
        flag &= ShouldDie(creature , null);
		return flag;
	}

	public sealed override bool ShouldDieLate(Creature creature)
	{
        bool flag = Mode.ShouldDieLate(creature);
        flag &= ShouldDieLate(creature , null);
		return flag;
	}

	public sealed override bool ShouldDisableRemainingRestSiteOptions(Player player)
	{
        bool flag = Mode.ShouldDisableRemainingRestSiteOptions(player);
        flag &= ShouldDisableRemainingRestSiteOptions(player , null);
		return flag;
	}

	public sealed override bool ShouldDraw(Player player, bool fromHandDraw)
	{
        bool flag = Mode.ShouldDraw(player, fromHandDraw);
        flag &= ShouldDraw(player, fromHandDraw , null);
		return flag;
	}

	public sealed override bool ShouldEtherealTrigger(CardModel card)
	{
        bool flag = Mode.ShouldEtherealTrigger(card);
        flag &= ShouldEtherealTrigger(card , null);
		return flag;
	}

	public sealed override bool ShouldFlush(Player player)
	{
        bool flag = Mode.ShouldFlush(player);
        flag &= ShouldFlush(player , null);
		return flag;
	}

	public sealed override bool ShouldGainStars(decimal amount, Player player)
	{
        bool flag = Mode.ShouldGainStars(amount, player);
        flag &= ShouldGainStars(amount, player , null);
		return flag;
	}
	public sealed override bool ShouldPayExcessEnergyCostWithStars(Player player)
	{
        bool flag = Mode.ShouldPayExcessEnergyCostWithStars(player);
        flag |= ShouldPayExcessEnergyCostWithStars(player , null);
		return flag;
	}

	public sealed override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
	{
        bool flag = Mode.ShouldPlay(card, autoPlayType);
        flag &= ShouldPlay(card, autoPlayType , null);
		return flag;
	}

	public sealed override bool ShouldPlayerResetEnergy(Player player)
	{
        bool flag = Mode.ShouldPlayerResetEnergy(player);
        flag &= ShouldPlayerResetEnergy(player,null);
		return flag;
	}
	public sealed override bool ShouldProcurePotion(PotionModel potion, Player player)
	{
        bool flag = Mode.ShouldProcurePotion(potion, player);
        flag &= ShouldProcurePotion(potion, player , null);
		return flag;
	}

	public sealed override bool ShouldPowerBeRemovedOnDeath(PowerModel power)
	{
        bool flag = Mode.ShouldPowerBeRemovedOnDeath(power);
        flag &= ShouldPowerBeRemovedOnDeath(power , null);
		return flag;
	}
	public sealed override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature)
	{
        bool flag = Mode.ShouldCreatureBeRemovedFromCombatAfterDeath(creature);
        flag &= ShouldCreatureBeRemovedFromCombatAfterDeath(creature , null);
		return flag;
	}
	public sealed override bool ShouldTakeExtraTurn(Player player)
	{
        bool flag = Mode.ShouldTakeExtraTurn(player);
        flag |= ShouldTakeExtraTurn(player , null);
		return flag;
	}

	public sealed override bool ShouldForcePotionReward(Player player, RoomType roomType)
	{
        bool flag = Mode.ShouldForcePotionReward(player, roomType);
        flag |= ShouldForcePotionReward(player, roomType , null);   
		return flag;
	}
#if STS2_BETA
	public sealed override async Task AfterModifyingGoldGained(Player player, decimal amount)
	{
		await Mode.AfterModifyingGoldGained(player, amount);
		await AfterModifyingGoldGained(player, amount , null);
	}	
	public sealed override bool TryModifyKeywordsInCombat(CardModel card, ISet<CardKeyword> keywords)
	{
        bool flag = Mode.TryModifyKeywordsInCombat(card, keywords);
        flag |= TryModifyKeywordsInCombat(card, keywords , null);
		return flag;
	}
#endif
    public sealed override async Task AfterDiceRoll(PlayerChoiceContext choiceContext,  IEnumerable<Creature>? targets, LibraryDice dice)
    {
		await Mode.AfterDiceRoll(choiceContext, targets , dice);
		await AfterDiceRoll(choiceContext, targets , dice);
    }
    public sealed override async Task AfterRerolling(PlayerChoiceContext choiceContext,  IEnumerable<Creature>? targets, LibraryDice dice)
    {
		await Mode.AfterRerolling(choiceContext, targets , dice);
		await AfterRerolling(choiceContext, targets , dice);
    }
    public sealed override bool ShouldReroll(PlayerChoiceContext choiceContext,  IEnumerable<Creature>? targets, LibraryDice dice)
    {
        bool flag = Mode.ShouldReroll(choiceContext, targets, dice);
        flag |= ShouldReroll(choiceContext, targets, dice);
		return flag;
    }
    public virtual Task BeforeDiceEffect(PlayerChoiceContext choiceContext,  IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? target, CardModel cardSource, LibraryDice dice, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer, LibraryDamageType type,LibraryResistanceLevel resistanceValue, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterSetChaoResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer, LibraryDamageType type, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual bool TrySetChaoResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer, LibraryDamageType type,LibraryResistanceLevel resistanceValue, object? _ = null)
    {
        return true;
    }
    public virtual bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer,LibraryDamageType type,LibraryResistanceLevel resistanceValue, object? _ = null)
    {
        return true;
    }
    public virtual Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer,LibraryDamageType type,LibraryResistanceLevel resistanceValue, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer,LibraryDamageType type, object? _ = null)
    {
        return Task.CompletedTask;
    }

    public virtual bool TryDiceEffect(PlayerChoiceContext choiceContext,IEnumerable<Creature>? target, CardModel cardSource, LibraryDice dice, object? _ = null)
    {
        return true;
    }
    public virtual Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterBlockBroken(Creature target, LibraryDamageType type, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterModifyingHpLostAfterOsty(LibraryDamageType type, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterStun(Creature creature, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforeAttack(LibraryAttackCommand command, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforeStun(Creature creature, object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num, object? _ = null)
    {
        return num;
    }
    public virtual decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type, object? _ = null)
    {
        return 0m;
    }
    public virtual decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type, object? _ = null)
    {
        return decimal.MaxValue;
    }
    public virtual decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type, object? _ = null)
    {
        return 1m;
    }
    public virtual decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type, object? _ = null)
    {
        return 0m;
    }
    public virtual decimal ModifyEffectiveAmountAdditive(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource, object? _ = null)
    {
        return 0m;
    }
    public virtual decimal ModifyEffectiveAmountMultiplicative(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource, object? _ = null)
    {
        return 1m;
    }
    public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type, object? _ = null)
    {
        return decimal.MaxValue;
    }
    public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type, object? _ = null)
    {
        return 1m;
    }
    public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type, object? _ = null)
    {
        return num;
    }
    public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type, object? _ = null)
    {
        return num;
    }
    public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type, object? _ = null)
    {
        return num;
    }
    public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type, object? _ = null)
    {
        return num;
    }
    public virtual Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type, object? _ = null)
    {
        return creature;
    }
    public virtual bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, object? _ = null)
    {
        return true;
    }
    public virtual bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, object? _ = null)
    {
        return true;
    }
	public virtual Task AfterActEntered(object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterAddToDeckPrevented(CardModel card, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task BeforeAttack(AttackCommand command, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterAutoPostPlayPhaseEntered(PlayerChoiceContext choiceContext, Player player, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterAutoPrePlayPhaseEnteredEarly(PlayerChoiceContext choiceContext, Player player, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterAutoPrePlayPhaseEntered(PlayerChoiceContext choiceContext, Player player, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterAutoPrePlayPhaseEnteredLate(PlayerChoiceContext choiceContext, Player player, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterBlockCleared(Creature creature, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task BeforeBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterBlockBroken(
		PlayerChoiceContext choiceContext,
		Creature target,
		Creature? breaker,
		object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterCardChangedPilesLate(CardModel card, PileType oldPileType, AbstractModel? clonedBy, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterCardDrawnEarly(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterCardEnteredCombat(CardModel card, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterCardGeneratedForCombat(CardModel card, Player? creator, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task BeforeCardAutoPlayed(CardModel card, Creature? target, AutoPlayType type, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task BeforeCardPlayed(CardPlay cardPlay, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task BeforeCombatStart(object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task BeforeCombatStartLate(object? _ = null)
	{
		return Task.CompletedTask;
	}


	public virtual Task AfterCreatureAddedToCombat(Creature creature, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task BeforeDeath(Creature creature, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterDiedToDoom(PlayerChoiceContext choiceContext, IReadOnlyList<Creature> creatures, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterEnergyReset(Player player, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterEnergyResetLate(Player player, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterEnergySpent(CardModel card, int amount, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task BeforeCardRemoved(CardModel card, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task BeforeFlush(PlayerChoiceContext choiceContext, Player player, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task BeforeFlushLate(PlayerChoiceContext choiceContext, Player player, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterFlush(PlayerChoiceContext choiceContext, Player player, IReadOnlyCollection<CardModel> flushedCards, IReadOnlyCollection<CardModel> retainedCards, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterGoldGained(Player player, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task BeforeHandDrawLate(Player player, PlayerChoiceContext choiceContext, ICombatState combatState, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterHandEmptied(PlayerChoiceContext choiceContext, Player player, object? _ = null)
	{
		return Task.CompletedTask;
	}
	public virtual Task AfterModifyingBlockAmount(decimal modifiedAmount, CardModel? cardSource, CardPlay? cardPlay, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterModifyingCardPlayCount(CardModel card, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterModifyingCardPlayResultLocation(CardModel card, CardLocation cardLocation, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterModifyingOrbPassiveTriggerCount(OrbModel orb, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterModifyingCardRewardOptions(object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterModifyingDamageAmount(CardModel? cardSource, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterModifyingEnergyGain(object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterModifyingHandDraw(object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterPreventingDraw(object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterModifyingHpLostBeforeOsty(object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterModifyingHpLostAfterOsty(object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterModifyingPowerAmountReceived(PowerModel power, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterModifyingPowerAmountGiven(PowerModel power, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterModifyingRewards(object? _ = null)
	{
		return Task.CompletedTask;
	}
    public virtual Task AfterModifyingChaoDamageAmount(CardModel? cardSource, LibraryDamageType type,object? _ =null)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterModifyingEffectiveAmount(CardModel? cardSource, LibraryBasePowerModel power, object? _ =null)
    {
        return Task.CompletedTask;
    }
	public virtual Task AfterOrbChanneled(PlayerChoiceContext choiceContext, Player player, OrbModel orb, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterOrbEvoked(PlayerChoiceContext choiceContext, OrbModel orb, IEnumerable<Creature> targets, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterOstyRevived(Creature osty, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task BeforePotionUsed(PotionModel potion, Creature? target, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterPotionUsed(PotionModel potion, Creature? target, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterPotionDiscarded(PotionModel potion, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterPotionProcured(PotionModel potion, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterPreventingBlockClear(AbstractModel preventer, Creature creature, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterPreventingDeath(Creature creature, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterRestSiteHeal(Player player, bool isMimicked, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterRestSiteSmith(Player player, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterRewardTaken(Player player, Reward reward, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task BeforeRoomEntered(AbstractRoom room, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterRoomEntered(AbstractRoom room, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterStarsSpent(int amount, Player spender, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterStarsGained(int amount, Player gainer, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterForge(decimal amount, Player forger, AbstractModel? source, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterSummon(PlayerChoiceContext choiceContext, Player summoner, decimal amount, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterTakingExtraTurn(Player player, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterTargetingBlockedVfx(Creature blocker, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterSideTurnStartLate(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task BeforeSideTurnEndVeryEarly(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task BeforeSideTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterSideTurnEndLate(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants, object? _ = null)
	{
		return Task.CompletedTask;
	}

	public virtual int ModifyAttackHitCount(AttackCommand attack, int hitCount, object? _ = null)
	{
		return hitCount;
	}

	public virtual decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay, object? _ = null)
	{
		return 0m;
	}

	public virtual decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay, object? _ = null)
	{
		return 1m;
	}

	public virtual int ModifyCardPlayCount(CardModel card, Creature? target, int playCount, object? _ = null)
	{
		return playCount;
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

	public virtual int ModifyOrbPassiveTriggerCounts(OrbModel orb, int triggerCount, object? _ = null)
	{
		return triggerCount;
	}

	public virtual CardCreationOptions ModifyCardRewardCreationOptions(Player player, CardCreationOptions options, object? _ = null)
	{
		return options;
	}

	public virtual CardCreationOptions ModifyCardRewardCreationOptionsLate(Player player, CardCreationOptions options, object? _ = null)
	{
		return options;
	}

	public virtual decimal ModifyCardRewardUpgradeOdds(Player player, CardModel card, decimal odds, object? _ = null)
	{
		return odds;
	}

	public virtual decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, object? _ = null)
	{
		return 0m;
	}

	public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, object? _ = null)
	{
		return decimal.MaxValue;
	}

	public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, object? _ = null)
	{
		return 1m;
	}

	public virtual decimal ModifyEnergyGain(Player player, decimal amount, object? _ = null)
	{
		return amount;
	}
	public virtual decimal ModifyGoldGained(Player player, decimal amount, object? _ = null)
	{
		return amount;
	}

	public virtual decimal ModifyHandDraw(Player player, decimal count, object? _ = null)
	{
		return count;
	}

	public virtual decimal ModifyHandDrawLate(Player player, decimal count, object? _ = null)
	{
		return count;
	}

	public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, object? _ = null)
	{
		return amount;
	}

	public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, object? _ = null)
	{
		return amount;
	}

	public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, object? _ = null)
	{
		return amount;
	}

	public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, object? _ = null)
	{
		return amount;
	}

	public virtual decimal ModifyMaxEnergy(Player player, decimal amount, object? _ = null)
	{
		return amount;
	}
	public virtual decimal ModifyOrbValue(OrbModel orb, decimal value, object? _ = null)
	{
		return value;
	}

	public virtual decimal ModifyPowerAmountGivenAdditive(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource, object? _ = null)
	{
		return 0m;
	}
	public virtual decimal ModifyPowerAmountGivenMultiplicative(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource, object? _ = null)
	{
		return 1m;
	}

	public virtual void ModifyShuffleOrder(Player player, List<CardModel> cards, bool isInitialShuffle, object? _ = null)
	{
	}

	public virtual decimal ModifySummonAmount(Player summoner, decimal amount, AbstractModel? source, object? _ = null)
	{
		return amount;
	}

	public virtual Creature ModifyUnblockedDamageTarget(Creature target, decimal amount, ValueProp props, Creature? dealer, object? _ = null)
	{
		return target;
	}

	public virtual int ModifyXValue(CardModel card, int originalValue, object? _ = null)
	{
		return originalValue;
	}

	public virtual bool TryModifyCardBeingAddedToDeck(CardModel card, out CardModel? newCard, object? _ = null)
	{
		newCard = null;
		return false;
	}

	public virtual bool TryModifyCardBeingAddedToDeckLate(CardModel card, out CardModel? newCard, object? _ = null)
	{
		newCard = null;
		return false;
	}

	public virtual bool TryModifyCardRewardAlternatives(Player player, CardReward cardReward, List<CardRewardAlternative> alternatives, object? _ = null)
	{
		return false;
	}

	public virtual bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions, object? _ = null)
	{
		return false;
	}

	public virtual bool TryModifyCardRewardOptionsLate(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions, object? _ = null)
	{
		return false;
	}

	public virtual bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost, object? _ = null)
	{
		modifiedCost = originalCost;
		return false;
	}

	public virtual bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost, object? _ = null)
	{
		modifiedCost = originalCost;
		return false;
	}

	public virtual bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost, object? _ = null)
	{
		modifiedCost = originalCost;
		return false;
	}

	public virtual bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount, object? _ = null)
	{
		modifiedAmount = amount;
		return false;
	}


	public virtual bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room, object? _ =null)
	{
		return false;
	}

	public virtual bool TryModifyRewardsLate(Player player, List<Reward> rewards, AbstractRoom? room, object? _ =null)
	{
		return false;
	}

	public virtual bool ShouldAddToDeck(CardModel card, object? _ =null)
	{
		return true;
	}

	public virtual bool ShouldAfflict(CardModel card, AfflictionModel affliction, object? _ =null)
	{
		return true;
	}

	public virtual bool ShouldAllowHitting(Creature creature, object? _ =null)
	{
		return true;
	}

	public virtual bool ShouldAllowTargeting(Creature target, object? _ =null)
	{
		return true;
	}

	public virtual bool ShouldAllowSelectingMoreCardRewards(Player player, CardReward cardReward, object? _ =null)
	{
		return false;
	}

	public virtual bool ShouldClearBlock(Creature creature, object? _ =null)
	{
		return true;
	}

	public virtual bool ShouldDie(Creature creature, object? _ =null)
	{
		return true;
	}

	public virtual bool ShouldDieLate(Creature creature, object? _ =null)
	{
		return true;
	}

	public virtual bool ShouldDisableRemainingRestSiteOptions(Player player, object? _ =null)
	{
		return true;
	}

	public virtual bool ShouldDraw(Player player, bool fromHandDraw, object? _ =null)
	{
		return true;
	}

	public virtual bool ShouldEtherealTrigger(CardModel card, object? _ =null)
	{
		return true;
	}

	public virtual bool ShouldFlush(Player player, object? _ =null)
	{
		return true;
	}

	public virtual bool ShouldGainGold(decimal amount, Player player, object? _ =null)
	{
		return true;
	}

	public virtual bool ShouldGainStars(decimal amount, Player player, object? _ =null)
	{
		return true;
	}

	public virtual bool ShouldPayExcessEnergyCostWithStars(Player player, object? _ =null)
	{
		return false;
	}

	public virtual bool ShouldPlay(CardModel card, AutoPlayType autoPlayType, object? _ =null)
	{
		return true;
	}

	public virtual bool ShouldPlayerResetEnergy(Player player, object? _ =null)
	{
		return true;
	}

	public virtual bool ShouldProcurePotion(PotionModel potion, Player player, object? _ =null)
	{
		return true;
	}

	public virtual bool ShouldPowerBeRemovedOnDeath(PowerModel power, object? _ =null)
	{
		return true;
	}


	public virtual bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature, object? _ =null)
	{
		return true;
	}

	public virtual bool ShouldTakeExtraTurn(Player player, object? _ =null)
	{
		return false;
	}

	public virtual bool ShouldForcePotionReward(Player player, RoomType roomType, object? _ =null)
	{
		return false;
	}
	public virtual Task AfterModifyingGoldGained(Player player, decimal amount, object? _ =null)
	{
		return Task.CompletedTask;
	}	
	public virtual bool TryModifyKeywordsInCombat(CardModel card, ISet<CardKeyword> keywords, object? _ =null)
	{
		return false;
	}
    public virtual Task AfterDiceRoll(PlayerChoiceContext choiceContext,  IEnumerable<Creature>? targets, LibraryDice dice, object? _ =null)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterRerolling(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, object? _ =null)
    {
        return Task.CompletedTask;
    }
    public virtual bool ShouldReroll( IEnumerable<Creature>? targets, LibraryDice dice, object? _ =null)
    {
        return false;
    }
    public virtual bool ShouldReuse(IEnumerable<Creature>? targets, LibraryDice dice,object? _ = null)
    {
        return false;
    }
    public virtual Task AfterReusing(PlayerChoiceContext choiceContext, IEnumerable<Creature>? target, LibraryDice dice,object? _ = null)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforeDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice,object? _ = null)
    {
        return Task.CompletedTask;
    }
}
