using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Commands.Builders;
using Library.Utils;
using Library.Entities.Creatures;
using Library.Resistance;
using Library.Powers.Mode;

namespace Library.Models;
public interface ILibraryAbstractModel//库模型接口，定义了库里的钩子
{
    public Task BeforeDiceEffect(PlayerChoiceContext choiceContext, Creature? target, CardModel cardSource, LibraryDice dice)
    {
        return Task.CompletedTask;
    }
    public Task AfterDiceEffect(PlayerChoiceContext choiceContext, Creature? target, CardModel cardSource, LibraryDice dice)
    {
        return Task.CompletedTask;
    }
    public Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer, LibraryDamageType type,LibraryResistanceLevel resistanceValue)
    {
        return Task.CompletedTask;
    }
    public Task AfterSetChaoResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public bool TrySetChaoResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer, LibraryDamageType type,LibraryResistanceLevel resistanceValue)  
    {
        return true;
    }
    public bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer,LibraryDamageType type,LibraryResistanceLevel resistanceValue)
    {
        return true;
    }
    public Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer,LibraryDamageType type,LibraryResistanceLevel resistanceValue)
    {
        return Task.CompletedTask;
    }
    public Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer,LibraryDamageType type)
    {
        return Task.CompletedTask;
    }

    public bool TryDiceEffect(PlayerChoiceContext choiceContext,Creature? target, CardModel cardSource, LibraryDice dice)
    {
        return true;
    }
    public Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command)
    {
        return Task.CompletedTask;
    }
    public Task AfterBlockBroken(Creature target, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public Task AfterModifyingChaoDamageAmount(CardModel? cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public Task AfterModifyingHpLostAfterOsty(LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        return Task.CompletedTask;
    }
    public Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        return Task.CompletedTask;
    }
    public Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode)
    {
        return Task.CompletedTask;
    }
    public Task AfterStun(Creature creature)
    {
        return Task.CompletedTask;
    }
    public Task BeforeAttack(LibraryAttackCommand command)
    {
        return Task.CompletedTask;
    }
    public Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        return Task.CompletedTask;
    }
    public Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        return Task.CompletedTask;
    }
    public Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode)
    {
        return Task.CompletedTask;
    }
    public Task BeforeStun(Creature creature)
    {
        return Task.CompletedTask;
    }
    public int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num)
    {
        return num;
    }
    public decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return 0m;
    }
    public decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return decimal.MaxValue;
    }
    public decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return 1m;
    }
    public decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return 0m;
    }
    public decimal ModifyEffectiveAmountAdditive(Creature? target, decimal num, Creature? dealer, CardModel? cardSource)
    {
        return 0m;
    }
    public decimal ModifyEffectiveAmountMultiplicative(Creature? target, decimal num, Creature? dealer, CardModel? cardSource)
    {
        return 1m;
    }
    public Task AfterModifyingEffectiveAmount(CardModel? cardSource)
    {
        return Task.CompletedTask;
    }
    public decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return decimal.MaxValue;
    }
    public decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return 1m;
    }
    public decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return num;
    }
    public decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return num;
    }
    public decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return num;
    }
    public decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return num;
    }
    public Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type)
    {
        return creature;
    }
    public bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        return true;
    }
    public bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) 
    {
        return true;
    }
}