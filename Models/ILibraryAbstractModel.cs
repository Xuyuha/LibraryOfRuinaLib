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

/// <summary>
///     库模型接口，定义库里的钩子，由 <see cref="Hooks.LibraryHooks"/> 派发。
///     每个 Library*Model 基类都以 public virtual 默认实现本接口（见 LibraryModelHookDefaults.cs）；
///     新增钩子时须同时在那里给每个基类补上默认实现。
/// </summary>
public interface ILibraryAbstractModel
{
    // ---- 骰子 ----

    /// <summary>自定义骰子最大值</summary>
    public decimal ModifyDiceMaxValue(LibraryDice dice, decimal maxValue);

    /// <summary>自定义骰子最小值</summary>
    public decimal ModifyDiceMinValue(LibraryDice dice, decimal minValue);

    /// <summary>骰子投出前触发</summary>
    public Task BeforeDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice);

    /// <summary>骰子投出后触发</summary>
    public Task AfterDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result);

    /// <summary>骰子投出后触发，询问是否需要重新投掷（不是重新使用），若返回 true 则触发 AfterRerolling</summary>
    public bool ShouldReroll(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result);

    /// <summary>本实例若在 ShouldReroll 中返回 true 则触发</summary>
    public Task AfterRerolling(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result);

    /// <summary>骰子投出后触发（在 reroll 后），询问是否需要重新使用，若返回 true 则触发 AfterReusing</summary>
    public bool ShouldReuse(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result);

    /// <summary>本实例若在 ShouldReuse 中返回 true 则触发</summary>
    public Task AfterReusing(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result);

    /// <summary>即将触发骰子效果，询问是否允许触发</summary>
    public bool TryDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result);

    /// <summary>骰子生效前触发</summary>
    public Task BeforeDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result);

    /// <summary>骰子生效后触发</summary>
    public Task AfterDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result);

    // ---- 攻击与目标 ----

    /// <summary>攻击前，仅使用 LibraryAttackCommand 才会触发</summary>
    public Task BeforeAttack(LibraryAttackCommand command);

    /// <summary>攻击后，仅使用 LibraryAttackCommand 才会触发</summary>
    public Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command);

    /// <summary>设置攻击命中次数，仅使用 LibraryAttackCommand 才会触发</summary>
    public int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num);

    /// <summary>改变攻击目标为...，原版怪物也可作用。</summary>
    public Creature ModifyDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type);

    /// <summary>改变混乱攻击目标为...</summary>
    public Creature ModifyChaoDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type);

    /// <summary>改变未被阻挡的伤害目标，仅使用 LibraryCreatureCmd 才会触发</summary>
    public Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type);

    // ---- 伤害（仅使用 LibraryCreatureCmd 才会触发） ----

    /// <summary>设置伤害加成</summary>
    public decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type);

    /// <summary>设置伤害乘区</summary>
    public decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type);

    /// <summary>设置伤害上限</summary>
    public decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type);

    /// <summary>伤害值改变后</summary>
    public Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type);

    /// <summary>受到伤害前</summary>
    public Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type);

    /// <summary>设置 Osty 伤害转移前的伤害值</summary>
    public decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type);

    /// <summary>设置 Osty 伤害转移前的伤害值（在 ModifyHpLostBeforeOsty 后）</summary>
    public decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type);

    /// <summary>确定 Osty 伤害转移前的伤害值后</summary>
    public Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type);

    /// <summary>设置 Osty 伤害转移后的伤害值</summary>
    public decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type);

    /// <summary>设置 Osty 伤害转移后的伤害值（在 ModifyHpLostAfterOsty 后）</summary>
    public decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type);

    /// <summary>确定 Osty 伤害转移后的伤害值后</summary>
    public Task AfterModifyingHpLostAfterOsty(LibraryDamageType type);

    /// <summary>击碎护盾后</summary>
    public Task AfterBlockBroken(Creature target, LibraryDamageType type);

    /// <summary>生命值改变后</summary>
    public Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type);

    /// <summary>造成伤害后</summary>
    public Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type);

    /// <summary>受到伤害后</summary>
    public Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type);

    /// <summary>受到伤害后（在 AfterDamageReceived 后）</summary>
    public Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type);

    // ---- 混乱 ----

    /// <summary>设置混乱伤害加成</summary>
    public decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type);

    /// <summary>设置混乱伤害乘区</summary>
    public decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type);

    /// <summary>设置混乱伤害上限</summary>
    public decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type);

    /// <summary>混乱伤害值改变后</summary>
    public Task AfterModifyingChaoDamageAmount(CardModel? cardSource, LibraryDamageType type);

    /// <summary>受到混乱伤害前</summary>
    public Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type);

    /// <summary>造成混乱伤害后</summary>
    public Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type);

    /// <summary>受到混乱伤害后</summary>
    public Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type);

    /// <summary>受到混乱伤害后（在 AfterChaoDamageReceived 后）</summary>
    public Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type);

    /// <summary>混乱值改变后</summary>
    public Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type);

    /// <summary>眩晕前</summary>
    public Task BeforeStun(Creature creature);

    /// <summary>眩晕后</summary>
    public Task AfterStun(Creature creature);

    // ---- 抗性 ----

    /// <summary>本次伤害抗性即将改变，询问是否允许改变</summary>
    public bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue);

    /// <summary>设置伤害抗性前</summary>
    public Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue);

    /// <summary>设置伤害抗性后</summary>
    public Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type);

    /// <summary>本次混乱抗性即将改变，询问是否允许改变</summary>
    public bool TrySetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue);

    /// <summary>设置混乱抗性前</summary>
    public Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue);

    /// <summary>设置混乱抗性后</summary>
    public Task AfterSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type);

    // ---- 能力（Power） ----

    /// <summary>设置 dot 触发层数加成</summary>
    public decimal ModifyEffectiveAmountAdditive(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource);

    /// <summary>设置 dot 触发层数乘区</summary>
    public decimal ModifyEffectiveAmountMultiplicative(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource);

    /// <summary>设置 dot 触发层数后，仅本实例改变了触发层数后才会触发</summary>
    public Task AfterModifyingEffectiveAmount(CardModel? cardSource, LibraryBasePowerModel power);

    /// <summary>询问是否触发能力效果</summary>
    public bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource);

    /// <summary>触发能力效果前</summary>
    public Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource);

    /// <summary>触发能力效果后</summary>
    public Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource);

    /// <summary>询问是否触发减少方法</summary>
    public bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource);

    /// <summary>能力层数减少方法被触发前</summary>
    public Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource);

    /// <summary>能力层数减少方法被触发后</summary>
    public Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource);

    /// <summary>设置能力模式前</summary>
    public Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode);

    /// <summary>设置能力模式后</summary>
    public Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode);
}
