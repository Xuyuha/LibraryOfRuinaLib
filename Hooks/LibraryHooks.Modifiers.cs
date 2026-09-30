using LibraryLib.Combat;
using LibraryLib.Commands;
using LibraryLib.Entities.Creatures;
using LibraryLib.Localization.LibraryDynamicVars;
using LibraryLib.Models;
using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Hooks;

public static partial class LibraryHooks
{
    public static decimal ModifyDiceMaxValue(ICombatState combatState, LibraryDice dice, decimal maxValue) =>
        ModifyDiceBound(combatState, dice, maxValue, static (m, d, value) => m.ModifyDiceMaxValue(d, value));

    public static decimal ModifyDiceMinValue(ICombatState combatState, LibraryDice dice, decimal minValue) =>
        ModifyDiceBound(combatState, dice, minValue, static (m, d, value) => m.ModifyDiceMinValue(d, value));

    public static List<Creature> ModifyDamageTarget(ICombatState combatState, List<Creature> originalTargets, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type = LibraryDamageType.None) =>
        originalTargets.Select(target => ModifyDamageTarget(combatState, target, amount, props, dealer, type)).ToList();

    public static Creature ModifyDamageTarget(ICombatState combatState, Creature originalTarget, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type = LibraryDamageType.None) =>
        ChainLibraryListeners(combatState.IterateHookListeners(), originalTarget, (m, target) => m.ModifyDamageTarget(target, amount, props, dealer, type));

    public static Creature ModifyChaoDamageTarget(ICombatState combatState, Creature originalTarget, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type = LibraryDamageType.None) =>
        ChainLibraryListeners(combatState.IterateHookListeners(), originalTarget, (m, target) => m.ModifyChaoDamageTarget(target, amount, props, dealer, type));

    public static Creature ModifyUnblockedDamageTarget(ICombatState combatState, Creature originalTarget, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type)
    {
        Creature creature = originalTarget;
        foreach (AbstractModel model in combatState.IterateHookListeners())
        {
            creature = model.ModifyUnblockedDamageTarget(creature, amount, props, dealer);
            if (model is ILibraryAbstractModel libraryModel)
                creature = libraryModel.ModifyUnblockedDamageTarget(creature, amount, props, dealer, type);
        }
        return creature;
    }

    public static decimal ModifyAttackHitCount(ICombatState combatState, LibraryAttackCommand attackCommand, int originalHitCount)
    {
        int hitCount = originalHitCount;
        foreach (AbstractModel model in CombatListeners(combatState))
        {
            hitCount = model.ModifyAttackHitCount(attackCommand.ToAttackCommand, hitCount);
            if (model is ILibraryAbstractModel libraryModel)
                hitCount = libraryModel.ModifyAttackHitCount(attackCommand, hitCount);
        }
        return hitCount;
    }

    public static decimal ModifyEffectiveAmount(ICombatState combatState, LibraryBasePowerModel power, Creature? dealer, decimal amount, CardModel? cardSource, out IEnumerable<AbstractModel> modifiers)
    {
        var changed = new List<AbstractModel>();
        decimal value = AdditivePass(CombatListeners(combatState), amount, changed, (model, current) =>
            model is ILibraryAbstractModel m ? m.ModifyEffectiveAmountAdditive(power, current, dealer, cardSource) : 0m);
        value = MultiplicativePass(CombatListeners(combatState), value, changed, (model, current) =>
            model is ILibraryAbstractModel m ? m.ModifyEffectiveAmountMultiplicative(power, current, dealer, cardSource) : 1m);
        modifiers = changed;
        return value;
    }

    public static decimal ModifyDamage(IRunState runState, ICombatState combatState, Creature? target, Creature? dealer, decimal damage, ValueProp props, CardModel? cardSource, CardPlay? cardPlay, ModifyDamageHookType modifyDamageHookType, CardPreviewMode previewMode, out IEnumerable<AbstractModel> modifiers, LibraryDamageType type)
    {
        LibraryCombatValueResolution resolution = LibraryCombatValueResolver.Resolve(
            combatState, LibraryCombatValueKind.PhysicalDamage, damage, target, dealer, props, cardSource, cardPlay, type, previewMode);
        if (resolution != LibraryCombatValueResolution.Default)
        {
            modifiers = Array.Empty<AbstractModel>();
            return LibraryCombatValueResolver.ResolveBaseValue(resolution, damage);
        }

        decimal enchanted = damage;
        if (cardSource?.Enchantment is { } enchantment)
        {
            if (modifyDamageHookType.HasFlag(ModifyDamageHookType.Additive))
                enchanted += enchantment.EnchantDamageAdditive(enchanted, props);
            if (modifyDamageHookType.HasFlag(ModifyDamageHookType.Multiplicative))
                enchanted *= enchantment.EnchantDamageMultiplicative(enchanted, props);
        }

        decimal value = ResolveForPreviewTargets(combatState, target, cardSource, previewMode, out List<AbstractModel> changed, creature =>
        {
            var creatureModifiers = new List<AbstractModel>();
            decimal result = enchanted;
            if (modifyDamageHookType.HasFlag(ModifyDamageHookType.Additive))
            {
                result = AdditivePass(runState.IterateHookListeners(combatState), result, creatureModifiers, (model, current) =>
                    model.ModifyDamageAdditive(creature, current, props, dealer, cardSource, cardPlay)
                    + (model is ILibraryAbstractModel m ? m.ModifyDamageAdditive(creature, current, props, dealer, cardSource, cardPlay, type) : 0m));
            }
            if (modifyDamageHookType.HasFlag(ModifyDamageHookType.Multiplicative))
            {
                result = MultiplicativePass(runState.IterateHookListeners(combatState), result, creatureModifiers, (model, current) =>
                    model.ModifyDamageMultiplicative(creature, current, props, dealer, cardSource, cardPlay)
                    * (model is ILibraryAbstractModel m ? m.ModifyDamageMultiplicative(creature, current, props, dealer, cardSource, cardPlay, type) : 1m));
            }
            result = CapPass(runState.IterateHookListeners(combatState), result, creatureModifiers, model =>
                model is ILibraryAbstractModel m
                    ? Math.Min(
                        model.ModifyDamageCap(creature, props, dealer, cardSource, cardPlay),
                        m.ModifyDamageCap(creature, props, dealer, cardSource, cardPlay, type))
                    : model.ModifyDamageCap(creature, props, dealer, cardSource, cardPlay));
            return (result, creatureModifiers);
        });
        modifiers = changed;
        return Math.Max(0m, value);
    }

    public static decimal ModifyChaoDamage(IRunState runState, ICombatState combatState, Creature? target, Creature? dealer, decimal chaoDamage, ValueProp props, CardModel? cardSource, CardPlay? cardPlay, ModifyChaoDamageHookType modifyChaoDamageHookType, CardPreviewMode previewMode, out IEnumerable<AbstractModel> modifiers, LibraryDamageType type)
    {
        LibraryCombatValueResolution resolution = LibraryCombatValueResolver.Resolve(
            combatState, LibraryCombatValueKind.ChaoDamage, chaoDamage, target, dealer, props, cardSource, cardPlay, type, previewMode);
        modifiers = Array.Empty<AbstractModel>();
        if (resolution != LibraryCombatValueResolution.Default)
        {
            // A policy decides the value even for a multi-target preview, which has no single target.
            bool isPolicyControlledMultiTargetPreview =
                target == null && previewMode == CardPreviewMode.MultiCreatureTargeting;
            return target is LibraryCreature || isPolicyControlledMultiTargetPreview
                ? LibraryCombatValueResolver.ResolveBaseValue(resolution, chaoDamage)
                : 0m;
        }

        // Only Library creatures take Chao damage, so a missing or vanilla target previews as zero.
        if (target is not LibraryCreature)
            return 0m;

        decimal value = chaoDamage;
        if (cardSource?.Enchantment is LibraryEnchantmentModel enchantment)
        {
            if (modifyChaoDamageHookType.HasFlag(ModifyChaoDamageHookType.Additive))
                value += enchantment.EnchantChaoDamageAdditive(value, props);
            if (modifyChaoDamageHookType.HasFlag(ModifyChaoDamageHookType.Multiplicative))
                value *= enchantment.EnchantChaoDamageMultiplicative(value, props);
        }

        var changed = new List<AbstractModel>();
        if (modifyChaoDamageHookType.HasFlag(ModifyChaoDamageHookType.Additive))
        {
            value = AdditivePass(runState.IterateHookListeners(combatState), value, changed, (model, current) =>
                model is ILibraryAbstractModel m ? m.ModifyChaoDamageAdditive(target, current, props, dealer, cardSource, cardPlay, type) : 0m);
        }
        if (modifyChaoDamageHookType.HasFlag(ModifyChaoDamageHookType.Multiplicative))
        {
            value = MultiplicativePass(runState.IterateHookListeners(combatState), value, changed, (model, current) =>
                model is ILibraryAbstractModel m ? m.ModifyChaoDamageMultiplicative(target, current, props, dealer, cardSource, cardPlay, type) : 1m);
        }
        value = CapPass(runState.IterateHookListeners(combatState), value, changed, model =>
            model is ILibraryAbstractModel m ? m.ModifyChaoDamageCap(target, props, dealer, cardSource, cardPlay, type) : decimal.MaxValue);
        modifiers = changed;
        return Math.Max(0m, value);
    }

    public static decimal ModifyHpLostBeforeOsty(IRunState runState, ICombatState combatState, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, out IEnumerable<AbstractModel> modifiers, LibraryDamageType type) =>
        ModifyHpLost(
            runState, combatState, target, amount, props, dealer, cardSource, out modifiers,
            (model, current) => model.ModifyHpLostBeforeOsty(target, current, props, dealer, cardSource),
            (m, current) => m.ModifyHpLostBeforeOsty(target, current, props, dealer, cardSource, type),
            (model, current) => model.ModifyHpLostBeforeOstyLate(target, current, props, dealer, cardSource),
            (m, current) => m.ModifyHpLostBeforeOstyLate(target, current, props, dealer, cardSource, type),
            type);

    public static decimal ModifyHpLostAfterOsty(IRunState runState, ICombatState combatState, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, out IEnumerable<AbstractModel> modifiers, LibraryDamageType type) =>
        ModifyHpLost(
            runState, combatState, target, amount, props, dealer, cardSource, out modifiers,
            (model, current) => model.ModifyHpLostAfterOsty(target, current, props, dealer, cardSource),
            (m, current) => m.ModifyHpLostAfterOsty(target, current, props, dealer, cardSource, type),
            (model, current) => model.ModifyHpLostAfterOstyLate(target, current, props, dealer, cardSource),
            (m, current) => m.ModifyHpLostAfterOstyLate(target, current, props, dealer, cardSource, type),
            type);

    private static decimal ModifyHpLost(
        IRunState runState,
        ICombatState combatState,
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        out IEnumerable<AbstractModel> modifiers,
        Func<AbstractModel, decimal, decimal> vanillaHook,
        Func<ILibraryAbstractModel, decimal, decimal> libraryHook,
        Func<AbstractModel, decimal, decimal> vanillaLateHook,
        Func<ILibraryAbstractModel, decimal, decimal> libraryLateHook,
        LibraryDamageType type)
    {
        LibraryCombatValueResolution resolution = LibraryCombatValueResolver.Resolve(
            combatState, LibraryCombatValueKind.HpLoss, amount, target, dealer, props, cardSource, null, type);
        if (resolution != LibraryCombatValueResolution.Default)
        {
            modifiers = Array.Empty<AbstractModel>();
            return LibraryCombatValueResolver.ResolveBaseValue(resolution, amount);
        }

        var changed = new List<AbstractModel>();
        decimal value = TransformPass(runState.IterateHookListeners(combatState), amount, changed, (model, current) =>
            Paired(model, vanillaHook(model, current), libraryHook));
        value = TransformPass(runState.IterateHookListeners(combatState), value, changed, (model, current) =>
            Paired(model, vanillaLateHook(model, current), libraryLateHook));
        modifiers = changed;
        return value;

        static decimal Paired(AbstractModel model, decimal vanillaResult, Func<ILibraryAbstractModel, decimal, decimal> library) =>
            model is ILibraryAbstractModel m ? library(m, vanillaResult) : vanillaResult;
    }

    private static decimal ModifyDiceBound(ICombatState combatState, LibraryDice dice, decimal value, Func<ILibraryAbstractModel, LibraryDice, decimal, decimal> modify)
    {
        LibraryCombatValueResolution resolution = LibraryCombatValueResolver.Resolve(
            combatState,
            dice.DiceType == LibraryDiceType.Block ? LibraryCombatValueKind.Block : LibraryCombatValueKind.PhysicalDamage,
            value,
            null,
            null,
            LibraryDice.Props,
            dice.SourceCard,
            null,
            dice.DamageType);
        if (resolution != LibraryCombatValueResolution.Default)
            return LibraryCombatValueResolver.ResolveBaseValue(resolution, value);
        return ChainLibraryListeners(CombatListeners(combatState), value, (m, current) => modify(m, dice, current));
    }

    /// <summary>
    /// A card that hits every enemy previews against all of them without a single target. The preview
    /// shows the per-enemy value only when every hittable enemy resolves to the same whole number;
    /// otherwise it falls back to resolving without a target, like vanilla <c>Hook.ModifyDamage</c>.
    /// </summary>
    private static decimal ResolveForPreviewTargets(
        ICombatState? combatState,
        Creature? target,
        CardModel? cardSource,
        CardPreviewMode previewMode,
        out List<AbstractModel> modifiers,
        Func<Creature?, (decimal Value, List<AbstractModel> Modifiers)> resolve)
    {
        bool isMultiTargetPreview =
            target == null
            && previewMode == CardPreviewMode.MultiCreatureTargeting
            && cardSource is { TargetType: TargetType.AllEnemies or TargetType.RandomEnemy, Pile.Type: PileType.Hand or PileType.Play };
        if (isMultiTargetPreview)
        {
            var merged = new List<AbstractModel>();
            decimal? shared = null;
            bool isUniform = true;
            foreach (Creature enemy in combatState?.HittableEnemies ?? Array.Empty<Creature>())
            {
                (decimal value, List<AbstractModel> enemyModifiers) = resolve(enemy);
                if (shared == null)
                {
                    shared = value;
                }
                else if ((int)value != (int)shared.Value)
                {
                    isUniform = false;
                    break;
                }
                merged.AddRange(enemyModifiers);
            }

            if (shared.HasValue && isUniform)
            {
                modifiers = merged.Distinct().ToList();
                return shared.Value;
            }
        }

        (decimal result, modifiers) = resolve(target);
        return result;
    }

    private static decimal AdditivePass(IEnumerable<AbstractModel> listeners, decimal value, List<AbstractModel> modifiers, Func<AbstractModel, decimal, decimal> contribution)
    {
        foreach (AbstractModel model in listeners)
        {
            decimal delta = contribution(model, value);
            value += delta;
            if (delta != 0m)
                modifiers.Add(model);
        }
        return value;
    }

    private static decimal MultiplicativePass(IEnumerable<AbstractModel> listeners, decimal value, List<AbstractModel> modifiers, Func<AbstractModel, decimal, decimal> factor)
    {
        foreach (AbstractModel model in listeners)
        {
            decimal multiplier = factor(model, value);
            value *= multiplier;
            if (multiplier != 1m)
                modifiers.Add(model);
        }
        return value;
    }

    /// <summary>Applies the lowest cap; only a listener that lowers the value counts as a modifier.</summary>
    private static decimal CapPass(IEnumerable<AbstractModel> listeners, decimal value, List<AbstractModel> modifiers, Func<AbstractModel, decimal> cap)
    {
        decimal lowestCap = decimal.MaxValue;
        foreach (AbstractModel model in listeners)
        {
            decimal candidate = cap(model);
            if (candidate >= lowestCap)
                continue;
            lowestCap = candidate;
            if (value > candidate)
            {
                value = candidate;
                modifiers.Add(model);
            }
        }
        return value;
    }

    /// <summary>A listener counts as a modifier when it changes the whole-number part of the value.</summary>
    private static decimal TransformPass(IEnumerable<AbstractModel> listeners, decimal value, List<AbstractModel> modifiers, Func<AbstractModel, decimal, decimal> transform)
    {
        foreach (AbstractModel model in listeners)
        {
            decimal before = value;
            value = transform(model, value);
            if (decimal.Truncate(before) != decimal.Truncate(value))
                modifiers.Add(model);
        }
        return value;
    }

    private static T ChainLibraryListeners<T>(IEnumerable<AbstractModel> listeners, T value, Func<ILibraryAbstractModel, T, T> modify)
    {
        foreach (AbstractModel model in listeners)
        {
            if (model is ILibraryAbstractModel libraryModel)
                value = modify(libraryModel, value);
        }
        return value;
    }
}
