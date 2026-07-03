
using System.Collections.Generic;
using Library.Entities.Creatures;
using Library.Hooks;
using Library.Models;
using Library.Patches;
using Library.Resistance;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

public class LibraryDamageVar : DamageVar
{
    public LibraryDamageType DamageType { get; set; }
    public decimal DamageResistanceValue = 1m;
    public decimal ChaoResistanceValue = 0m;
	public decimal ChaoPreviewValue = 0m;
	public LibraryDamageVar(decimal damage, ValueProp props, LibraryDamageType damageType)
		: base(damage,props)
	{
        DamageType = damageType;
	}
	public LibraryDamageVar(string name, decimal damage, ValueProp props, LibraryDamageType damageType)
		: base(name, damage,props)
	{
        DamageType = damageType;
	}

	public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
	{
            decimal num = base.BaseValue;
            decimal num1 = base.BaseValue;
            EnchantmentModel enchantment = card.Enchantment;
            if (enchantment != null)
            {
                num += enchantment.EnchantDamageAdditive(num, Props);
                num *= enchantment.EnchantDamageMultiplicative(num, Props);
                if (!card.IsEnchantmentPreview)
                {
                    base.EnchantedValue = num;
                }
                if(enchantment is LibraryEnchantmentModel le){
                    num1 +=le.EnchantChaoDamageAdditive(num1,Props);
                    num1 *=le.EnchantChaoDamageMultiplicative(num1,Props);
                }
            }
            if (runGlobalHooks)
            {
                num = LibraryHooks.ModifyDamage(card.Owner.RunState, card.CombatState, target, card.Owner.Creature, base.BaseValue, Props, card, null, ModifyDamageHookType.All, previewMode, out IEnumerable<AbstractModel> _, DamageType);
                num1 = LibraryHooks.ModifyChaoDamage(card.Owner.RunState, card.CombatState, target, card.Owner.Creature, base.BaseValue, Props, card, ModifyChaoDamageHookType.All, previewMode, out IEnumerable<AbstractModel> _, DamageType);
            }
            if(target is LibraryCreature lc)
            {
                DamageResistanceValue = lc.GetPhysicalResistanceLevel(DamageType).GetMultiplier();
                if (lc.HasChaoResistance)
                    ChaoResistanceValue = lc.GetChaosResistanceLevel(DamageType).GetMultiplier();
            }
			PreviewValue = num;
			ChaoPreviewValue = num1;
	}
}
