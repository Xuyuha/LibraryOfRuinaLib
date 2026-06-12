using System.Collections.Generic;
using Library.Hooks;
using Library.Patches;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

public class LibraryOstyDamageVar : OstyDamageVar
{
    public LibraryDamageType DamageType { get; set; }

	public LibraryOstyDamageVar(decimal damage, ValueProp props, LibraryDamageType damageType)
    : base(damage, props)
	{
        DamageType = damageType;
	}

	public LibraryOstyDamageVar(string name, decimal damage, ValueProp props, LibraryDamageType damageType)
    : base(name, damage, props)
	{
        DamageType = damageType;
	}

	public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
	{
		decimal num = base.BaseValue;
		EnchantmentModel enchantment = card.Enchantment;
		if (enchantment != null)
		{
			num += enchantment.EnchantDamageAdditive(num, Props);
			num *= enchantment.EnchantDamageMultiplicative(num, Props);
			if (!card.IsEnchantmentPreview)
			{
				base.EnchantedValue = num;
			}
		}
		if (runGlobalHooks)
		{
			ICombatState combatState = card.CombatState ?? card.Owner.Creature.CombatState;
			num = LibraryHooks.ModifyDamage(card.Owner.RunState, combatState, target, card.Owner.Osty, base.BaseValue, Props, card, ModifyDamageHookType.All, previewMode, out IEnumerable<AbstractModel> _, DamageType);
		}
		base.PreviewValue = LibraryDamagePreviewFeedback.ApplyPhysicalResistancePreview(
			card,
			previewMode,
			target,
			num,
			Props,
			DamageType);
	}
}
