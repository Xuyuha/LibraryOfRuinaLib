using LibraryLib.Commands;
using LibraryLib.Entities.Creatures;
using LibraryLib.Localization.Dice;
using LibraryLib.Localization.LibraryDynamicVars;
using LibraryLib.Models;
using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Powers.LibraryPowerMode;

/// <summary>
///     <see cref="LibraryMultipleModePowerModel"/> 的一种模式。所属能力收到的每个钩子都会先交给当前模式；
///     这里全部是空操作默认实现，具体模式只重写需要的钩子。
/// </summary>
public abstract class LibraryPowerMode
{
    public LibraryMultipleModePowerModel? SourcePower;

    public LibraryPowerMode(LibraryMultipleModePowerModel sourcePower)
    {
        SourcePower = sourcePower;
    }

    public LibraryPowerMode()
    {
    }

    public abstract string Name { get; }

    // ---- Library 钩子 ----
    public virtual decimal ModifyDiceMaxValue(LibraryDice dice, decimal maxValue) => maxValue;
    public virtual decimal ModifyDiceMinValue(LibraryDice dice, decimal minValue) => minValue;
    public virtual Creature ModifyDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Creature ModifyChaoDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual Task BeforeDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice) => Task.CompletedTask;
    public virtual bool ShouldReroll(IEnumerable<Creature>? target, LibraryDice dice, DiceRollResult result) => false;
    public virtual bool ShouldReuse(IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => false;
    public virtual Task AfterReusing(PlayerChoiceContext choiceContext, IEnumerable<Creature>? target, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual Task BeforeDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual Task AfterDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;
    public virtual bool TrySetChaoResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => true;
    public virtual Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type, LibraryResistanceLevel resistanceValue) => Task.CompletedTask;
    public virtual Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext, LibraryCreature target, Creature? dealer, LibraryDamageType type) => Task.CompletedTask;    
    public virtual bool TryDiceEffect(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, CardModel cardSource, LibraryDice dice, DiceRollResult result) => true;
    public virtual Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command) => Task.CompletedTask;
    public virtual Task AfterBlockBroken(Creature target, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterModifyingHpLostAfterOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterModifyingEffectiveAmount(CardModel? cardSource, LibraryBasePowerModel power) => Task.CompletedTask;
    public virtual Task AfterModifyingChaoDamageAmount(CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
    public virtual Task AfterStun(Creature creature) => Task.CompletedTask;
    public virtual Task BeforeAttack(LibraryAttackCommand command) => Task.CompletedTask;
    public virtual Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => Task.CompletedTask;
    public virtual Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, decimal amount, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode) => Task.CompletedTask;
    public virtual Task BeforeStun(Creature creature) => Task.CompletedTask;
    public virtual int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num) => num;
    public virtual decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 0m;
    public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => decimal.MaxValue;
    public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, LibraryDamageType type) => 1m;
    public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type) => num;
    public virtual Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type) => creature;
    public virtual bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;
    public virtual bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource) => true;

    // ---- 原版钩子 ----
    public virtual Task AfterActEntered() => Task.CompletedTask;
    public virtual Task AfterAddToDeckPrevented(CardModel card) => Task.CompletedTask;
    public virtual Task BeforeAttack(AttackCommand command) => Task.CompletedTask;
    public virtual Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command) => Task.CompletedTask;
    public virtual Task AfterAutoPostPlayPhaseEntered(PlayerChoiceContext choiceContext, Player player) => Task.CompletedTask;
    public virtual Task AfterAutoPrePlayPhaseEnteredEarly(PlayerChoiceContext choiceContext, Player player) => Task.CompletedTask;
    public virtual Task AfterAutoPrePlayPhaseEntered(PlayerChoiceContext choiceContext, Player player) => Task.CompletedTask;
    public virtual Task AfterAutoPrePlayPhaseEnteredLate(PlayerChoiceContext choiceContext, Player player) => Task.CompletedTask;
    public virtual Task AfterBlockCleared(Creature creature) => Task.CompletedTask;
    public virtual Task BeforeBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterBlockBroken(PlayerChoiceContext choiceContext, Creature target, Creature? breaker) => Task.CompletedTask;
    public virtual Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy) => Task.CompletedTask;
    public virtual Task AfterCardChangedPilesLate(CardModel card, PileType oldPileType, AbstractModel? clonedBy) => Task.CompletedTask;
    public virtual Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card) => Task.CompletedTask;
    public virtual Task AfterCardDrawnEarly(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw) => Task.CompletedTask;
    public virtual Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw) => Task.CompletedTask;
    public virtual Task AfterCardEnteredCombat(CardModel card) => Task.CompletedTask;
    public virtual Task AfterCardGeneratedForCombat(CardModel card, Player? creator) => Task.CompletedTask;
    public virtual Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal) => Task.CompletedTask;
    public virtual Task BeforeCardAutoPlayed(CardModel card, Creature? target, AutoPlayType type) => Task.CompletedTask;
    public virtual Task BeforeCardPlayed(CardPlay cardPlay) => Task.CompletedTask;
    public virtual Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Task.CompletedTask;
    public virtual Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Task.CompletedTask;
    public virtual Task BeforeCombatStart() => Task.CompletedTask;
    public virtual Task BeforeCombatStartLate() => Task.CompletedTask;
    public virtual Task AfterCreatureAddedToCombat(Creature creature) => Task.CompletedTask;
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta) => Task.CompletedTask;
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task BeforeDeath(Creature creature) => Task.CompletedTask;
    public virtual Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength) => Task.CompletedTask;
    public virtual Task AfterDiedToDoom(PlayerChoiceContext choiceContext, IReadOnlyList<Creature> creatures) => Task.CompletedTask;
    public virtual Task AfterEnergyReset(Player player) => Task.CompletedTask;
    public virtual Task AfterEnergyResetLate(Player player) => Task.CompletedTask;
    public virtual Task AfterEnergySpent(CardModel card, int amount) => Task.CompletedTask;
    public virtual Task BeforeCardRemoved(CardModel card) => Task.CompletedTask;
    public virtual Task BeforeFlush(PlayerChoiceContext choiceContext, Player player) => Task.CompletedTask;
    public virtual Task BeforeFlushLate(PlayerChoiceContext choiceContext, Player player) => Task.CompletedTask;
    public virtual Task AfterFlush(PlayerChoiceContext choiceContext, Player player, IReadOnlyCollection<CardModel> flushedCards, IReadOnlyCollection<CardModel> retainedCards) => Task.CompletedTask;
    public virtual Task AfterGoldGained(Player player) => Task.CompletedTask;
    public virtual Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState) => Task.CompletedTask;
    public virtual Task BeforeHandDrawLate(Player player, PlayerChoiceContext choiceContext, ICombatState combatState) => Task.CompletedTask;
    public virtual Task AfterHandEmptied(PlayerChoiceContext choiceContext, Player player) => Task.CompletedTask;
    public virtual Task AfterModifyingBlockAmount(decimal modifiedAmount, CardModel? cardSource, CardPlay? cardPlay) => Task.CompletedTask;
    public virtual Task AfterModifyingCardPlayCount(CardModel card) => Task.CompletedTask;
    public virtual Task AfterModifyingCardPlayResultLocation(CardModel card, CardLocation cardLocation) => Task.CompletedTask;
    public virtual Task AfterModifyingOrbPassiveTriggerCount(OrbModel orb) => Task.CompletedTask;
    public virtual Task AfterModifyingCardRewardOptions() => Task.CompletedTask;
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterModifyingEnergyGain() => Task.CompletedTask;
    public virtual Task AfterModifyingHandDraw() => Task.CompletedTask;
    public virtual Task AfterPreventingDraw() => Task.CompletedTask;
    public virtual Task AfterModifyingHpLostBeforeOsty() => Task.CompletedTask;
    public virtual Task AfterModifyingHpLostAfterOsty() => Task.CompletedTask;
    public virtual Task AfterModifyingPowerAmountReceived(PowerModel power) => Task.CompletedTask;
    public virtual Task AfterModifyingPowerAmountGiven(PowerModel power) => Task.CompletedTask;
    public virtual Task AfterModifyingRewards() => Task.CompletedTask;
    public virtual Task AfterOrbChanneled(PlayerChoiceContext choiceContext, Player player, OrbModel orb) => Task.CompletedTask;
    public virtual Task AfterOrbEvoked(PlayerChoiceContext choiceContext, OrbModel orb, IEnumerable<Creature> targets) => Task.CompletedTask;
    public virtual Task AfterOstyRevived(Creature osty) => Task.CompletedTask;
    public virtual Task BeforePotionUsed(PotionModel potion, Creature? target) => Task.CompletedTask;
    public virtual Task AfterPotionUsed(PotionModel potion, Creature? target) => Task.CompletedTask;
    public virtual Task AfterPotionDiscarded(PotionModel potion) => Task.CompletedTask;
    public virtual Task AfterPotionProcured(PotionModel potion) => Task.CompletedTask;
    public virtual Task BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource) => Task.CompletedTask;
    public virtual Task AfterPreventingBlockClear(AbstractModel preventer, Creature creature) => Task.CompletedTask;
    public virtual Task AfterPreventingDeath(Creature creature) => Task.CompletedTask;
    public virtual Task AfterRestSiteHeal(Player player, bool isMimicked) => Task.CompletedTask;
    public virtual Task AfterRestSiteSmith(Player player) => Task.CompletedTask;
    public virtual Task AfterRewardTaken(Player player, Reward reward) => Task.CompletedTask;
    public virtual Task BeforeRoomEntered(AbstractRoom room) => Task.CompletedTask;
    public virtual Task AfterRoomEntered(AbstractRoom room) => Task.CompletedTask;
    public virtual Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler) => Task.CompletedTask;
    public virtual Task AfterStarsSpent(int amount, Player spender) => Task.CompletedTask;
    public virtual Task AfterStarsGained(int amount, Player gainer) => Task.CompletedTask;
    public virtual Task AfterForge(decimal amount, Player forger, AbstractModel? source) => Task.CompletedTask;
    public virtual Task AfterSummon(PlayerChoiceContext choiceContext, Player summoner, decimal amount) => Task.CompletedTask;
    public virtual Task AfterTakingExtraTurn(Player player) => Task.CompletedTask;
    public virtual Task AfterTargetingBlockedVfx(Creature blocker) => Task.CompletedTask;
    public virtual Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState) => Task.CompletedTask;
    public virtual Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState) => Task.CompletedTask;
    public virtual Task AfterSideTurnStartLate(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState) => Task.CompletedTask;
    public virtual Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player) => Task.CompletedTask;
    public virtual Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player) => Task.CompletedTask;
    public virtual Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player) => Task.CompletedTask;
    public virtual Task BeforeSideTurnEndVeryEarly(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants) => Task.CompletedTask;
    public virtual Task BeforeSideTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants) => Task.CompletedTask;
    public virtual Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants) => Task.CompletedTask;
    public virtual Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants) => Task.CompletedTask;
    public virtual Task AfterSideTurnEndLate(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants) => Task.CompletedTask;
    public virtual int ModifyAttackHitCount(AttackCommand attack, int hitCount) => hitCount;
    public virtual decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay) => 0m;
    public virtual decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay) => 1m;
    public virtual int ModifyCardPlayCount(CardModel card, Creature? target, int playCount) => playCount;
    public virtual CardLocation ModifyCardPlayResultLocation(CardModel card, bool isAutoPlay, ResourceInfo resources, CardLocation cardLocation) => cardLocation;
    public virtual int ModifyOrbPassiveTriggerCounts(OrbModel orb, int triggerCount) => triggerCount;
    public virtual CardCreationOptions ModifyCardRewardCreationOptions(Player player, CardCreationOptions options) => options;
    public virtual CardCreationOptions ModifyCardRewardCreationOptionsLate(Player player, CardCreationOptions options) => options;
    public virtual decimal ModifyCardRewardUpgradeOdds(Player player, CardModel card, decimal odds) => odds;
    public virtual decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) => 0m;
    public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) => decimal.MaxValue;
    public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) => 1m;
    public virtual decimal ModifyEnergyGain(Player player, decimal amount) => amount;
    public virtual decimal ModifyGoldGained(Player player, decimal amount) => amount;
    public virtual decimal ModifyHandDraw(Player player, decimal count) => count;
    public virtual decimal ModifyHandDrawLate(Player player, decimal count) => count;
    public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource) => amount;
    public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource) => amount;
    public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource) => amount;
    public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource) => amount;
    public virtual decimal ModifyMaxEnergy(Player player, decimal amount) => amount;
    public virtual decimal ModifyOrbValue(OrbModel orb, decimal value) => value;
    public virtual decimal ModifyEffectiveAmountAdditive(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 0m;
    public virtual decimal ModifyEffectiveAmountMultiplicative(LibraryBasePowerModel power, decimal num, Creature? dealer, CardModel? cardSource) => 1m;
    public virtual decimal ModifyPowerAmountGivenAdditive(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource) => 0m;
    public virtual decimal ModifyPowerAmountGivenMultiplicative(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource) => 1m;
    public virtual void ModifyShuffleOrder(Player player, List<CardModel> cards, bool isInitialShuffle) { }
    public virtual decimal ModifySummonAmount(Player summoner, decimal amount, AbstractModel? source) => amount;
    public virtual Creature ModifyUnblockedDamageTarget(Creature target, decimal amount, ValueProp props, Creature? dealer) => target;
    public virtual int ModifyXValue(CardModel card, int originalValue) => originalValue;
    public virtual bool TryModifyCardBeingAddedToDeck(CardModel card, out CardModel? newCard) { newCard = null; return false; }
    public virtual bool TryModifyCardBeingAddedToDeckLate(CardModel card, out CardModel? newCard) { newCard = null; return false; }
    public virtual bool TryModifyCardRewardAlternatives(Player player, CardReward cardReward, List<CardRewardAlternative> alternatives) => false;
    public virtual bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions) => false;
    public virtual bool TryModifyCardRewardOptionsLate(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions) => false;
    public virtual bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost) { modifiedCost = originalCost; return false; }
    public virtual bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost) { modifiedCost = originalCost; return false; }
    public virtual bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost) { modifiedCost = originalCost; return false; }
    public virtual bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount) { modifiedAmount = amount; return false; }
    public virtual bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room) => false;
    public virtual bool TryModifyRewardsLate(Player player, List<Reward> rewards, AbstractRoom? room) => false;
    public virtual bool ShouldAddToDeck(CardModel card) => true;
    public virtual bool ShouldAfflict(CardModel card, AfflictionModel affliction) => true;
    public virtual bool ShouldAllowHitting(Creature creature) => true;
    public virtual bool ShouldAllowTargeting(Creature target) => true;
    public virtual bool ShouldAllowSelectingMoreCardRewards(Player player, CardReward cardReward) => false;
    public virtual bool ShouldClearBlock(Creature creature) => true;
    public virtual bool ShouldDie(Creature creature) => true;
    public virtual bool ShouldDieLate(Creature creature) => true;
    public virtual bool ShouldDisableRemainingRestSiteOptions(Player player) => true;
    public virtual bool ShouldDraw(Player player, bool fromHandDraw) => true;
    public virtual bool ShouldEtherealTrigger(CardModel card) => true;
    public virtual bool ShouldFlush(Player player) => true;
    public virtual bool ShouldGainStars(decimal amount, Player player) => true;
    public virtual bool ShouldPayExcessEnergyCostWithStars(Player player) => false;
    public virtual bool ShouldPlay(CardModel card, AutoPlayType autoPlayType) => true;
    public virtual bool ShouldPlayerResetEnergy(Player player) => true;
    public virtual bool ShouldProcurePotion(PotionModel potion, Player player) => true;
    public virtual bool ShouldPowerBeRemovedOnDeath(PowerModel power) => true;
    public virtual bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature) => true;
    public virtual bool ShouldTakeExtraTurn(Player player) => false;
    public virtual bool ShouldForcePotionReward(Player player, RoomType roomType) => false;
    public virtual Task AfterModifyingGoldGained(Player player, decimal amount) => Task.CompletedTask;    
    public virtual bool TryModifyKeywordsInCombat(CardModel card, ISet<CardKeyword> keywords) => false;
    public virtual Task AfterDiceRoll(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
    public virtual Task AfterRerolling(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, LibraryDice dice, DiceRollResult result) => Task.CompletedTask;
}
