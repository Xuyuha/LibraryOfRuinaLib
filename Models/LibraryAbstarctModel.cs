using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Commands.Builders;
using Library.Utils;
using Library.Entities.Creatures;
using Library.Resistance;

namespace Library.Models;
public interface ILibraryAbstractModel//库模型接口，定义了库里的钩子，还有很多钩子没写
{
    public virtual Task BeforeDiceEffect(PlayerChoiceContext choiceContext, Creature? target, CardModel cardSource, LibraryDice dice)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterDiceEffect(PlayerChoiceContext choiceContext, Creature? target, CardModel cardSource, LibraryDice dice)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer, LibraryDamageType type,LibraryResistanceLevel resistanceValue)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterSetChaoResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual bool TrySetChaoResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer, LibraryDamageType type,LibraryResistanceLevel resistanceValue)
    {
        return true;
    }
    public virtual bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer,LibraryDamageType type,LibraryResistanceLevel resistanceValue)
    {
        return true;
    }
    public virtual Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer,LibraryDamageType type,LibraryResistanceLevel resistanceValue)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer,LibraryDamageType type)
    {
        return Task.CompletedTask;
    }

    public virtual bool TryDiceEffect(PlayerChoiceContext choiceContext,Creature? target, CardModel cardSource, LibraryDice dice)
    {
        return true;
    }
    public virtual Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterBlockBroken(Creature target, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterModifyingHpLostAfterOsty(LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, int mode)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterStun(Creature creature)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforeAttack(LibraryAttackCommand command)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforePowerSetMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, int mode)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, int mode)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforeStun(Creature creature)
    {
        return Task.CompletedTask;
    }
    public virtual int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num)
    {
        return num;
    }
    public virtual decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return 0m;
    }
    public virtual decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return decimal.MaxValue;
    }
    public virtual decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return 1m;
    }
    public virtual decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return 0m;
    }
    public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return decimal.MaxValue;
    }
    public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return 1m;
    }
    public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return num;
    }
    public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return num;
    }
    public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return num;
    }
    public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return num;
    }
    public virtual Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type)
    {
        return creature;
    }
    public virtual bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        return true;
    }
    public virtual bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        return true;
    }
}