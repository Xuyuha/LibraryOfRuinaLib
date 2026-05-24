using System;
using System.Collections.Generic;
using Library.Hooks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

public class LibraryCalculatedDamageVar : CalculatedDamageVar
{
    public LibraryDamageType DamageType { get; set; }
	public LibraryCalculatedDamageVar(ValueProp props, LibraryDamageType damageType)
		: base(props)
	{
        DamageType = damageType;
	}

	public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
	{
		EnchantmentModel enchantment = card.Enchantment;
		if (enchantment != null)
		{
			decimal baseValue = GetBaseVar().BaseValue;
			baseValue += enchantment.EnchantDamageAdditive(baseValue, Props);
			baseValue *= enchantment.EnchantDamageMultiplicative(baseValue, Props);
			baseValue = Math.Max(baseValue, 0m);
			if (card.IsEnchantmentPreview)
			{
				base.PreviewValue = baseValue;
			}
			else
			{
				base.EnchantedValue = baseValue;
			}
		}
		decimal num = Calculate(target);
		if (runGlobalHooks)
		{
			ICombatState combatState = card.CombatState ?? card.Owner.Creature.CombatState;
			base.PreviewValue = LibraryHooks.ModifyDamage(card.Owner.RunState, combatState, target, IsFromOsty ? card.Owner.Osty : card.Owner.Creature, num, Props, card, ModifyDamageHookType.All, previewMode, out IEnumerable<AbstractModel> _, DamageType);
		}
		else if (!card.IsEnchantmentPreview)
		{
			if (enchantment != null)
			{
				num += enchantment.EnchantDamageAdditive(num, Props);
				num *= enchantment.EnchantDamageMultiplicative(num, Props);
			}
			base.PreviewValue = num;
		}
		base.PreviewValue = Math.Max(base.PreviewValue, 0m);
	}
}
