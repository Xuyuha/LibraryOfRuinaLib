using LibraryLib.Entities.Creatures;
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
		(decimal damage, decimal chao) = LibraryDamagePreview.Calculate(this, card, previewMode, target, runGlobalHooks, Props, DamageType);
		// Multipliers describe the current target only; reset them so a previous target's values never linger.
		DamageResistanceValue = 1m;
		ChaoResistanceValue = 0m;
		if (target is LibraryCreature libraryTarget)
		{
			DamageResistanceValue = libraryTarget.GetPhysicalResistanceLevel(DamageType).GetMultiplier();
			if (libraryTarget.HasChaoResistance)
				ChaoResistanceValue = libraryTarget.GetChaosResistanceLevel(DamageType).GetMultiplier();
		}
		PreviewValue = ResistancePreview.ApplyPhysicalResistancePreview(card, previewMode, target, damage, Props, DamageType);
		ChaoPreviewValue = chao;
	}
}
