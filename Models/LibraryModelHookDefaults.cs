using LibraryLib.Commands;
using LibraryLib.Entities.Creatures;
using LibraryLib.Localization.Dice;
using LibraryLib.Localization.LibraryDynamicVars;
using LibraryLib.Powers.LibraryPowerMode;
using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Models;

// 每个 Library*Model 基类对 ILibraryAbstractModel 的默认实现（全部为空操作），供子类按需 override。
// 各基类继承自不同的原版模型，C# 无法共享实现，而已发布的下游模组又按这些虚方法二进制绑定，
// 因此每个基类都保留一份完全相同的块。新增钩子时：先加到 ILibraryAbstractModel，
// 再把同一行默认实现加到下面每个块（漏掉任何一个都会触发 CS0535）。
// 有自身逻辑的基类（Card/Enchantment/Monster/Power/Relic）在各自文件里声明基类型。

public abstract class LibraryAchievementModel : AchievementModel, ILibraryAbstractModel
{
    public virtual decimal ModifyDiceMaxValue(LibraryDice dice, decimal maxValue) => maxValue;
    public virtual decimal ModifyDiceMinValue(LibraryDice dice, decimal minValue) => minValue;
    public virtual Task BeforeDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice) => Task.CompletedTask;
    public virtual Task AfterDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReroll(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterRerolling(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReuse(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterReusing(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool TryDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => true;
    public virtual Task BeforeDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual Task AfterDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;

    public virtual Task BeforeAttack(LibraryAttackCommand command) => Task.CompletedTask;
    public virtual Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command) => Task.CompletedTask;
    public virtual int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num) => num;
    public virtual Creature ModifyDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyChaoDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;

    public virtual decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostAfterOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterBlockBroken(Creature target, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingChaoDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeStun(Creature creature) => Task.CompletedTask;
    public virtual Task AfterStun(Creature creature) => Task.CompletedTask;

    public virtual bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;
    public virtual bool TrySetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyEffectiveAmountAdditive(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 0m;
    public virtual decimal ModifyEffectiveAmountMultiplicative(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 1m;
    public virtual Task AfterModifyingEffectiveAmount(CardModel? cardSource, LibraryBasePowerModel power) => Task.CompletedTask;
    public virtual bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
    public virtual Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
}

public abstract class LibraryActModel : ActModel, ILibraryAbstractModel
{
    public virtual decimal ModifyDiceMaxValue(LibraryDice dice, decimal maxValue) => maxValue;
    public virtual decimal ModifyDiceMinValue(LibraryDice dice, decimal minValue) => minValue;
    public virtual Task BeforeDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice) => Task.CompletedTask;
    public virtual Task AfterDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReroll(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterRerolling(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReuse(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterReusing(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool TryDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => true;
    public virtual Task BeforeDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual Task AfterDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;

    public virtual Task BeforeAttack(LibraryAttackCommand command) => Task.CompletedTask;
    public virtual Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command) => Task.CompletedTask;
    public virtual int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num) => num;
    public virtual Creature ModifyDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyChaoDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;

    public virtual decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostAfterOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterBlockBroken(Creature target, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingChaoDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeStun(Creature creature) => Task.CompletedTask;
    public virtual Task AfterStun(Creature creature) => Task.CompletedTask;

    public virtual bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;
    public virtual bool TrySetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyEffectiveAmountAdditive(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 0m;
    public virtual decimal ModifyEffectiveAmountMultiplicative(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 1m;
    public virtual Task AfterModifyingEffectiveAmount(CardModel? cardSource, LibraryBasePowerModel power) => Task.CompletedTask;
    public virtual bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
    public virtual Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
}

public abstract class LibraryAfflictionModel : AfflictionModel, ILibraryAbstractModel
{
    public virtual decimal ModifyDiceMaxValue(LibraryDice dice, decimal maxValue) => maxValue;
    public virtual decimal ModifyDiceMinValue(LibraryDice dice, decimal minValue) => minValue;
    public virtual Task BeforeDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice) => Task.CompletedTask;
    public virtual Task AfterDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReroll(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterRerolling(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReuse(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterReusing(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool TryDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => true;
    public virtual Task BeforeDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual Task AfterDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;

    public virtual Task BeforeAttack(LibraryAttackCommand command) => Task.CompletedTask;
    public virtual Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command) => Task.CompletedTask;
    public virtual int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num) => num;
    public virtual Creature ModifyDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyChaoDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;

    public virtual decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostAfterOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterBlockBroken(Creature target, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingChaoDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeStun(Creature creature) => Task.CompletedTask;
    public virtual Task AfterStun(Creature creature) => Task.CompletedTask;

    public virtual bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;
    public virtual bool TrySetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyEffectiveAmountAdditive(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 0m;
    public virtual decimal ModifyEffectiveAmountMultiplicative(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 1m;
    public virtual Task AfterModifyingEffectiveAmount(CardModel? cardSource, LibraryBasePowerModel power) => Task.CompletedTask;
    public virtual bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
    public virtual Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
}

public abstract class LibraryAncientEventModel : AncientEventModel, ILibraryAbstractModel
{
    public virtual decimal ModifyDiceMaxValue(LibraryDice dice, decimal maxValue) => maxValue;
    public virtual decimal ModifyDiceMinValue(LibraryDice dice, decimal minValue) => minValue;
    public virtual Task BeforeDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice) => Task.CompletedTask;
    public virtual Task AfterDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReroll(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterRerolling(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReuse(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterReusing(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool TryDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => true;
    public virtual Task BeforeDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual Task AfterDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;

    public virtual Task BeforeAttack(LibraryAttackCommand command) => Task.CompletedTask;
    public virtual Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command) => Task.CompletedTask;
    public virtual int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num) => num;
    public virtual Creature ModifyDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyChaoDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;

    public virtual decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostAfterOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterBlockBroken(Creature target, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingChaoDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeStun(Creature creature) => Task.CompletedTask;
    public virtual Task AfterStun(Creature creature) => Task.CompletedTask;

    public virtual bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;
    public virtual bool TrySetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyEffectiveAmountAdditive(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 0m;
    public virtual decimal ModifyEffectiveAmountMultiplicative(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 1m;
    public virtual Task AfterModifyingEffectiveAmount(CardModel? cardSource, LibraryBasePowerModel power) => Task.CompletedTask;
    public virtual bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
    public virtual Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
}

public abstract class LibraryBadgeModel : BadgeModel, ILibraryAbstractModel
{
    public virtual decimal ModifyDiceMaxValue(LibraryDice dice, decimal maxValue) => maxValue;
    public virtual decimal ModifyDiceMinValue(LibraryDice dice, decimal minValue) => minValue;
    public virtual Task BeforeDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice) => Task.CompletedTask;
    public virtual Task AfterDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReroll(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterRerolling(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReuse(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterReusing(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool TryDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => true;
    public virtual Task BeforeDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual Task AfterDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;

    public virtual Task BeforeAttack(LibraryAttackCommand command) => Task.CompletedTask;
    public virtual Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command) => Task.CompletedTask;
    public virtual int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num) => num;
    public virtual Creature ModifyDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyChaoDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;

    public virtual decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostAfterOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterBlockBroken(Creature target, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingChaoDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeStun(Creature creature) => Task.CompletedTask;
    public virtual Task AfterStun(Creature creature) => Task.CompletedTask;

    public virtual bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;
    public virtual bool TrySetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyEffectiveAmountAdditive(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 0m;
    public virtual decimal ModifyEffectiveAmountMultiplicative(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 1m;
    public virtual Task AfterModifyingEffectiveAmount(CardModel? cardSource, LibraryBasePowerModel power) => Task.CompletedTask;
    public virtual bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
    public virtual Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
}

public abstract partial class LibraryCardModel
{
    public virtual decimal ModifyDiceMaxValue(LibraryDice dice, decimal maxValue) => maxValue;
    public virtual decimal ModifyDiceMinValue(LibraryDice dice, decimal minValue) => minValue;
    public virtual Task BeforeDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice) => Task.CompletedTask;
    public virtual Task AfterDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReroll(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterRerolling(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReuse(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterReusing(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool TryDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => true;
    public virtual Task BeforeDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual Task AfterDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;

    public virtual Task BeforeAttack(LibraryAttackCommand command) => Task.CompletedTask;
    public virtual Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command) => Task.CompletedTask;
    public virtual int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num) => num;
    public virtual Creature ModifyDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyChaoDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;

    public virtual decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostAfterOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterBlockBroken(Creature target, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingChaoDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeStun(Creature creature) => Task.CompletedTask;
    public virtual Task AfterStun(Creature creature) => Task.CompletedTask;

    public virtual bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;
    public virtual bool TrySetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyEffectiveAmountAdditive(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 0m;
    public virtual decimal ModifyEffectiveAmountMultiplicative(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 1m;
    public virtual Task AfterModifyingEffectiveAmount(CardModel? cardSource, LibraryBasePowerModel power) => Task.CompletedTask;
    public virtual bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
    public virtual Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
}

public abstract class LibraryCardPoolModel : CardPoolModel, ILibraryAbstractModel
{
    public virtual decimal ModifyDiceMaxValue(LibraryDice dice, decimal maxValue) => maxValue;
    public virtual decimal ModifyDiceMinValue(LibraryDice dice, decimal minValue) => minValue;
    public virtual Task BeforeDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice) => Task.CompletedTask;
    public virtual Task AfterDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReroll(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterRerolling(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReuse(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterReusing(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool TryDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => true;
    public virtual Task BeforeDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual Task AfterDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;

    public virtual Task BeforeAttack(LibraryAttackCommand command) => Task.CompletedTask;
    public virtual Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command) => Task.CompletedTask;
    public virtual int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num) => num;
    public virtual Creature ModifyDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyChaoDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;

    public virtual decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostAfterOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterBlockBroken(Creature target, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingChaoDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeStun(Creature creature) => Task.CompletedTask;
    public virtual Task AfterStun(Creature creature) => Task.CompletedTask;

    public virtual bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;
    public virtual bool TrySetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyEffectiveAmountAdditive(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 0m;
    public virtual decimal ModifyEffectiveAmountMultiplicative(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 1m;
    public virtual Task AfterModifyingEffectiveAmount(CardModel? cardSource, LibraryBasePowerModel power) => Task.CompletedTask;
    public virtual bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
    public virtual Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
}

public abstract class LibraryCharacterModel : CharacterModel, ILibraryAbstractModel
{
    public virtual decimal ModifyDiceMaxValue(LibraryDice dice, decimal maxValue) => maxValue;
    public virtual decimal ModifyDiceMinValue(LibraryDice dice, decimal minValue) => minValue;
    public virtual Task BeforeDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice) => Task.CompletedTask;
    public virtual Task AfterDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReroll(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterRerolling(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReuse(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterReusing(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool TryDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => true;
    public virtual Task BeforeDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual Task AfterDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;

    public virtual Task BeforeAttack(LibraryAttackCommand command) => Task.CompletedTask;
    public virtual Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command) => Task.CompletedTask;
    public virtual int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num) => num;
    public virtual Creature ModifyDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyChaoDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;

    public virtual decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostAfterOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterBlockBroken(Creature target, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingChaoDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeStun(Creature creature) => Task.CompletedTask;
    public virtual Task AfterStun(Creature creature) => Task.CompletedTask;

    public virtual bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;
    public virtual bool TrySetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyEffectiveAmountAdditive(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 0m;
    public virtual decimal ModifyEffectiveAmountMultiplicative(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 1m;
    public virtual Task AfterModifyingEffectiveAmount(CardModel? cardSource, LibraryBasePowerModel power) => Task.CompletedTask;
    public virtual bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
    public virtual Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
}

public abstract class LibraryEncounterModel : EncounterModel, ILibraryAbstractModel
{
    public virtual decimal ModifyDiceMaxValue(LibraryDice dice, decimal maxValue) => maxValue;
    public virtual decimal ModifyDiceMinValue(LibraryDice dice, decimal minValue) => minValue;
    public virtual Task BeforeDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice) => Task.CompletedTask;
    public virtual Task AfterDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReroll(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterRerolling(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReuse(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterReusing(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool TryDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => true;
    public virtual Task BeforeDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual Task AfterDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;

    public virtual Task BeforeAttack(LibraryAttackCommand command) => Task.CompletedTask;
    public virtual Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command) => Task.CompletedTask;
    public virtual int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num) => num;
    public virtual Creature ModifyDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyChaoDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;

    public virtual decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostAfterOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterBlockBroken(Creature target, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingChaoDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeStun(Creature creature) => Task.CompletedTask;
    public virtual Task AfterStun(Creature creature) => Task.CompletedTask;

    public virtual bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;
    public virtual bool TrySetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyEffectiveAmountAdditive(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 0m;
    public virtual decimal ModifyEffectiveAmountMultiplicative(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 1m;
    public virtual Task AfterModifyingEffectiveAmount(CardModel? cardSource, LibraryBasePowerModel power) => Task.CompletedTask;
    public virtual bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
    public virtual Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
}

public partial class LibraryEnchantmentModel
{
    public virtual decimal ModifyDiceMaxValue(LibraryDice dice, decimal maxValue) => maxValue;
    public virtual decimal ModifyDiceMinValue(LibraryDice dice, decimal minValue) => minValue;
    public virtual Task BeforeDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice) => Task.CompletedTask;
    public virtual Task AfterDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReroll(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterRerolling(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReuse(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterReusing(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool TryDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => true;
    public virtual Task BeforeDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual Task AfterDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;

    public virtual Task BeforeAttack(LibraryAttackCommand command) => Task.CompletedTask;
    public virtual Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command) => Task.CompletedTask;
    public virtual int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num) => num;
    public virtual Creature ModifyDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyChaoDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;

    public virtual decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostAfterOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterBlockBroken(Creature target, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingChaoDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeStun(Creature creature) => Task.CompletedTask;
    public virtual Task AfterStun(Creature creature) => Task.CompletedTask;

    public virtual bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;
    public virtual bool TrySetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyEffectiveAmountAdditive(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 0m;
    public virtual decimal ModifyEffectiveAmountMultiplicative(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 1m;
    public virtual Task AfterModifyingEffectiveAmount(CardModel? cardSource, LibraryBasePowerModel power) => Task.CompletedTask;
    public virtual bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
    public virtual Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
}

public abstract class LibraryEventModel : EventModel, ILibraryAbstractModel
{
    public virtual decimal ModifyDiceMaxValue(LibraryDice dice, decimal maxValue) => maxValue;
    public virtual decimal ModifyDiceMinValue(LibraryDice dice, decimal minValue) => minValue;
    public virtual Task BeforeDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice) => Task.CompletedTask;
    public virtual Task AfterDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReroll(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterRerolling(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReuse(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterReusing(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool TryDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => true;
    public virtual Task BeforeDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual Task AfterDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;

    public virtual Task BeforeAttack(LibraryAttackCommand command) => Task.CompletedTask;
    public virtual Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command) => Task.CompletedTask;
    public virtual int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num) => num;
    public virtual Creature ModifyDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyChaoDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;

    public virtual decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostAfterOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterBlockBroken(Creature target, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingChaoDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeStun(Creature creature) => Task.CompletedTask;
    public virtual Task AfterStun(Creature creature) => Task.CompletedTask;

    public virtual bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;
    public virtual bool TrySetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyEffectiveAmountAdditive(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 0m;
    public virtual decimal ModifyEffectiveAmountMultiplicative(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 1m;
    public virtual Task AfterModifyingEffectiveAmount(CardModel? cardSource, LibraryBasePowerModel power) => Task.CompletedTask;
    public virtual bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
    public virtual Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
}

public abstract class LibraryModifierModel : ModifierModel, ILibraryAbstractModel
{
    public virtual decimal ModifyDiceMaxValue(LibraryDice dice, decimal maxValue) => maxValue;
    public virtual decimal ModifyDiceMinValue(LibraryDice dice, decimal minValue) => minValue;
    public virtual Task BeforeDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice) => Task.CompletedTask;
    public virtual Task AfterDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReroll(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterRerolling(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReuse(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterReusing(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool TryDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => true;
    public virtual Task BeforeDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual Task AfterDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;

    public virtual Task BeforeAttack(LibraryAttackCommand command) => Task.CompletedTask;
    public virtual Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command) => Task.CompletedTask;
    public virtual int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num) => num;
    public virtual Creature ModifyDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyChaoDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;

    public virtual decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostAfterOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterBlockBroken(Creature target, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingChaoDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeStun(Creature creature) => Task.CompletedTask;
    public virtual Task AfterStun(Creature creature) => Task.CompletedTask;

    public virtual bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;
    public virtual bool TrySetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyEffectiveAmountAdditive(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 0m;
    public virtual decimal ModifyEffectiveAmountMultiplicative(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 1m;
    public virtual Task AfterModifyingEffectiveAmount(CardModel? cardSource, LibraryBasePowerModel power) => Task.CompletedTask;
    public virtual bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
    public virtual Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
}

public abstract partial class LibraryMonsterModel
{
    public virtual decimal ModifyDiceMaxValue(LibraryDice dice, decimal maxValue) => maxValue;
    public virtual decimal ModifyDiceMinValue(LibraryDice dice, decimal minValue) => minValue;
    public virtual Task BeforeDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice) => Task.CompletedTask;
    public virtual Task AfterDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReroll(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterRerolling(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReuse(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterReusing(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool TryDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => true;
    public virtual Task BeforeDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual Task AfterDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;

    public virtual Task BeforeAttack(LibraryAttackCommand command) => Task.CompletedTask;
    public virtual Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command) => Task.CompletedTask;
    public virtual int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num) => num;
    public virtual Creature ModifyDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyChaoDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;

    public virtual decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostAfterOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterBlockBroken(Creature target, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingChaoDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeStun(Creature creature) => Task.CompletedTask;
    public virtual Task AfterStun(Creature creature) => Task.CompletedTask;

    public virtual bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;
    public virtual bool TrySetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyEffectiveAmountAdditive(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 0m;
    public virtual decimal ModifyEffectiveAmountMultiplicative(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 1m;
    public virtual Task AfterModifyingEffectiveAmount(CardModel? cardSource, LibraryBasePowerModel power) => Task.CompletedTask;
    public virtual bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
    public virtual Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
}

public abstract class LibraryOrbModel : OrbModel, ILibraryAbstractModel
{
    public virtual decimal ModifyDiceMaxValue(LibraryDice dice, decimal maxValue) => maxValue;
    public virtual decimal ModifyDiceMinValue(LibraryDice dice, decimal minValue) => minValue;
    public virtual Task BeforeDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice) => Task.CompletedTask;
    public virtual Task AfterDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReroll(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterRerolling(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReuse(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterReusing(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool TryDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => true;
    public virtual Task BeforeDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual Task AfterDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;

    public virtual Task BeforeAttack(LibraryAttackCommand command) => Task.CompletedTask;
    public virtual Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command) => Task.CompletedTask;
    public virtual int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num) => num;
    public virtual Creature ModifyDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyChaoDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;

    public virtual decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostAfterOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterBlockBroken(Creature target, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingChaoDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeStun(Creature creature) => Task.CompletedTask;
    public virtual Task AfterStun(Creature creature) => Task.CompletedTask;

    public virtual bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;
    public virtual bool TrySetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyEffectiveAmountAdditive(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 0m;
    public virtual decimal ModifyEffectiveAmountMultiplicative(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 1m;
    public virtual Task AfterModifyingEffectiveAmount(CardModel? cardSource, LibraryBasePowerModel power) => Task.CompletedTask;
    public virtual bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
    public virtual Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
}

public abstract class LibraryPotionModel : PotionModel, ILibraryAbstractModel
{
    public virtual decimal ModifyDiceMaxValue(LibraryDice dice, decimal maxValue) => maxValue;
    public virtual decimal ModifyDiceMinValue(LibraryDice dice, decimal minValue) => minValue;
    public virtual Task BeforeDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice) => Task.CompletedTask;
    public virtual Task AfterDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReroll(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterRerolling(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReuse(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterReusing(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool TryDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => true;
    public virtual Task BeforeDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual Task AfterDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;

    public virtual Task BeforeAttack(LibraryAttackCommand command) => Task.CompletedTask;
    public virtual Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command) => Task.CompletedTask;
    public virtual int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num) => num;
    public virtual Creature ModifyDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyChaoDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;

    public virtual decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostAfterOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterBlockBroken(Creature target, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingChaoDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeStun(Creature creature) => Task.CompletedTask;
    public virtual Task AfterStun(Creature creature) => Task.CompletedTask;

    public virtual bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;
    public virtual bool TrySetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyEffectiveAmountAdditive(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 0m;
    public virtual decimal ModifyEffectiveAmountMultiplicative(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 1m;
    public virtual Task AfterModifyingEffectiveAmount(CardModel? cardSource, LibraryBasePowerModel power) => Task.CompletedTask;
    public virtual bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
    public virtual Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
}

public abstract class LibraryPotionPoolModel : PotionPoolModel, ILibraryAbstractModel
{
    public virtual decimal ModifyDiceMaxValue(LibraryDice dice, decimal maxValue) => maxValue;
    public virtual decimal ModifyDiceMinValue(LibraryDice dice, decimal minValue) => minValue;
    public virtual Task BeforeDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice) => Task.CompletedTask;
    public virtual Task AfterDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReroll(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterRerolling(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReuse(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterReusing(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool TryDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => true;
    public virtual Task BeforeDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual Task AfterDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;

    public virtual Task BeforeAttack(LibraryAttackCommand command) => Task.CompletedTask;
    public virtual Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command) => Task.CompletedTask;
    public virtual int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num) => num;
    public virtual Creature ModifyDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyChaoDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;

    public virtual decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostAfterOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterBlockBroken(Creature target, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingChaoDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeStun(Creature creature) => Task.CompletedTask;
    public virtual Task AfterStun(Creature creature) => Task.CompletedTask;

    public virtual bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;
    public virtual bool TrySetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyEffectiveAmountAdditive(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 0m;
    public virtual decimal ModifyEffectiveAmountMultiplicative(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 1m;
    public virtual Task AfterModifyingEffectiveAmount(CardModel? cardSource, LibraryBasePowerModel power) => Task.CompletedTask;
    public virtual bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
    public virtual Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
}

public abstract partial class LibraryPowerModel
{
    public virtual decimal ModifyDiceMaxValue(LibraryDice dice, decimal maxValue) => maxValue;
    public virtual decimal ModifyDiceMinValue(LibraryDice dice, decimal minValue) => minValue;
    public virtual Task BeforeDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice) => Task.CompletedTask;
    public virtual Task AfterDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReroll(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterRerolling(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReuse(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterReusing(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool TryDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => true;
    public virtual Task BeforeDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual Task AfterDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;

    public virtual Task BeforeAttack(LibraryAttackCommand command) => Task.CompletedTask;
    public virtual Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command) => Task.CompletedTask;
    public virtual int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num) => num;
    public virtual Creature ModifyDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyChaoDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;

    public virtual decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostAfterOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterBlockBroken(Creature target, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingChaoDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeStun(Creature creature) => Task.CompletedTask;
    public virtual Task AfterStun(Creature creature) => Task.CompletedTask;

    public virtual bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;
    public virtual bool TrySetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyEffectiveAmountAdditive(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 0m;
    public virtual decimal ModifyEffectiveAmountMultiplicative(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 1m;
    public virtual Task AfterModifyingEffectiveAmount(CardModel? cardSource, LibraryBasePowerModel power) => Task.CompletedTask;
    public virtual bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
    public virtual Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
}

public abstract partial class LibraryRelicModel
{
    public virtual decimal ModifyDiceMaxValue(LibraryDice dice, decimal maxValue) => maxValue;
    public virtual decimal ModifyDiceMinValue(LibraryDice dice, decimal minValue) => minValue;
    public virtual Task BeforeDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice) => Task.CompletedTask;
    public virtual Task AfterDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReroll(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterRerolling(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReuse(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterReusing(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool TryDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => true;
    public virtual Task BeforeDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual Task AfterDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;

    public virtual Task BeforeAttack(LibraryAttackCommand command) => Task.CompletedTask;
    public virtual Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command) => Task.CompletedTask;
    public virtual int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num) => num;
    public virtual Creature ModifyDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyChaoDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;

    public virtual decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostAfterOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterBlockBroken(Creature target, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingChaoDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeStun(Creature creature) => Task.CompletedTask;
    public virtual Task AfterStun(Creature creature) => Task.CompletedTask;

    public virtual bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;
    public virtual bool TrySetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyEffectiveAmountAdditive(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 0m;
    public virtual decimal ModifyEffectiveAmountMultiplicative(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 1m;
    public virtual Task AfterModifyingEffectiveAmount(CardModel? cardSource, LibraryBasePowerModel power) => Task.CompletedTask;
    public virtual bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
    public virtual Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
}

public abstract class LibraryRelicPoolModel : RelicPoolModel, ILibraryAbstractModel
{
    public virtual decimal ModifyDiceMaxValue(LibraryDice dice, decimal maxValue) => maxValue;
    public virtual decimal ModifyDiceMinValue(LibraryDice dice, decimal minValue) => minValue;
    public virtual Task BeforeDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice) => Task.CompletedTask;
    public virtual Task AfterDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReroll(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterRerolling(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReuse(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterReusing(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool TryDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => true;
    public virtual Task BeforeDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual Task AfterDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;

    public virtual Task BeforeAttack(LibraryAttackCommand command) => Task.CompletedTask;
    public virtual Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command) => Task.CompletedTask;
    public virtual int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num) => num;
    public virtual Creature ModifyDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyChaoDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;

    public virtual decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostAfterOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterBlockBroken(Creature target, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingChaoDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeStun(Creature creature) => Task.CompletedTask;
    public virtual Task AfterStun(Creature creature) => Task.CompletedTask;

    public virtual bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;
    public virtual bool TrySetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyEffectiveAmountAdditive(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 0m;
    public virtual decimal ModifyEffectiveAmountMultiplicative(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 1m;
    public virtual Task AfterModifyingEffectiveAmount(CardModel? cardSource, LibraryBasePowerModel power) => Task.CompletedTask;
    public virtual bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
    public virtual Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
}

public abstract class LibrarySingletonModel : SingletonModel, ILibraryAbstractModel
{
    public virtual decimal ModifyDiceMaxValue(LibraryDice dice, decimal maxValue) => maxValue;
    public virtual decimal ModifyDiceMinValue(LibraryDice dice, decimal minValue) => minValue;
    public virtual Task BeforeDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice) => Task.CompletedTask;
    public virtual Task AfterDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReroll(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterRerolling(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool ShouldReuse(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterReusing(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual bool TryDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => true;
    public virtual Task BeforeDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual Task AfterDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;

    public virtual Task BeforeAttack(LibraryAttackCommand command) => Task.CompletedTask;
    public virtual Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command) => Task.CompletedTask;
    public virtual int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num) => num;
    public virtual Creature ModifyDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyChaoDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;

    public virtual decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Task AfterModifyingHpLostAfterOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterBlockBroken(Creature target, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual Task AfterModifyingChaoDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeStun(Creature creature) => Task.CompletedTask;
    public virtual Task AfterStun(Creature creature) => Task.CompletedTask;

    public virtual bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;
    public virtual bool TrySetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;

    public virtual decimal ModifyEffectiveAmountAdditive(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 0m;
    public virtual decimal ModifyEffectiveAmountMultiplicative(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 1m;
    public virtual Task AfterModifyingEffectiveAmount(CardModel? cardSource, LibraryBasePowerModel power) => Task.CompletedTask;
    public virtual bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
    public virtual Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
}
