using LibraryLib.Hooks;
using LibraryLib.Models;
using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Localization.LibraryDynamicVars;

/// <summary>Shared card-preview math for Library damage variables (damage dice and <see cref="LibraryDamageVar"/>).</summary>
internal static class LibraryDamagePreview
{
    /// <summary>
    ///     Damage and Chao damage before resistance, like vanilla DamageVar.UpdateCardPreview: the enchantment is
    ///     applied (and stored in <see cref="DynamicVar.EnchantedValue"/> outside enchantment previews); with
    ///     <paramref name="runGlobalHooks"/> the Library hooks recompute both from the base value.
    /// </summary>
    internal static (decimal Damage, decimal Chao) Calculate(
        DynamicVar dynamicVar,
        CardModel card,
        CardPreviewMode previewMode,
        Creature? target,
        bool runGlobalHooks,
        ValueProp props,
        LibraryDamageType type)
    {
        decimal baseValue = dynamicVar.BaseValue;
        decimal damage = baseValue;
        decimal chao = baseValue;
        if (card.Enchantment is { } enchantment)
        {
            damage += enchantment.EnchantDamageAdditive(damage, props);
            damage *= enchantment.EnchantDamageMultiplicative(damage, props);
            if (!card.IsEnchantmentPreview)
                dynamicVar.EnchantedValue = damage;
            if (enchantment is LibraryEnchantmentModel libraryEnchantment)
            {
                chao += libraryEnchantment.EnchantChaoDamageAdditive(chao, props);
                chao *= libraryEnchantment.EnchantChaoDamageMultiplicative(chao, props);
            }
        }

        if (runGlobalHooks)
        {
            damage = LibraryHooks.ModifyDamage(card.Owner.RunState, card.CombatState, target, card.Owner.Creature, baseValue, props, card, null, ModifyDamageHookType.All, previewMode, out _, type);
            chao = LibraryHooks.ModifyChaoDamage(card.Owner.RunState, card.CombatState, target, card.Owner.Creature, baseValue, props, card, null, ModifyChaoDamageHookType.All, previewMode, out _, type);
        }

        return (damage, chao);
    }
}
