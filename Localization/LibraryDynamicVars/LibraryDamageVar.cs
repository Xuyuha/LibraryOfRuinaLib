
// using MegaCrit.Sts2.Core.Entities.Cards;
// using MegaCrit.Sts2.Core.Entities.Creatures;
// using MegaCrit.Sts2.Core.Hooks;
// using MegaCrit.Sts2.Core.Localization.DynamicVars;
// using MegaCrit.Sts2.Core.Models;
// using MegaCrit.Sts2.Core.ValueProps;

// public class LibraryDamageVar : DynamicVar//准备用来做骰子的伤害展示
// {
// 	public const string defaultName = "LibraryDamage";

// 	public ValueProp Props { get; set; }

// 	public LibraryDamageVar(decimal damage, ValueProp props)
// 		: base("Damage", damage)
// 	{
// 		Props = props;
// 	}
//     public LibraryDamageKind DamageType { get; set; }

// 	public LibraryDamageVar(string name, decimal damage, ValueProp props,LibraryDamageKind damageType)
// 		: base(name, damage)
// 	{
// 		DamageType = damageType;
// 		Props = props;
// 	}

// 	public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
// 	{
// 		decimal num = base.BaseValue;//后面会加上抗性乘算
// 		EnchantmentModel enchantment = card.Enchantment;
// 		if (enchantment != null)
// 		{
// 			num += enchantment.EnchantDamageAdditive(num, Props);
// 			num *= enchantment.EnchantDamageMultiplicative(num, Props);
// 			if (!card.IsEnchantmentPreview)
// 			{
// 				base.EnchantedValue = num;
// 			}
// 		}
// 		if (runGlobalHooks)
// 		{
// 			num = Hook.ModifyDamage(card.Owner.RunState, card.CombatState, target, card.Owner.Creature, base.BaseValue, Props, card, ModifyDamageHookType.All, previewMode, out IEnumerable<AbstractModel> _);
// 		}
// 		base.PreviewValue = num;
// 	}
// }
