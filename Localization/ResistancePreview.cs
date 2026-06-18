using Library.Entities.Creatures;
using Library.Resistance.Patches;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

public static class ResistancePreview
{
    public static void PulseResistanceIcons(LibraryCreature creature, LibraryDamageType damageType)
    {
        LibraryPhysicalResistanceIconsUi.Pulse(creature, damageType);
        LibraryChaosResistanceIconsUi.Pulse(creature, damageType);
    }
    public static bool ShouldApplyResistance(ValueProp props, LibraryDamageType damageType)
    {
        return damageType != LibraryDamageType.None
            && props.HasFlag(ValueProp.Move)
            && !props.HasFlag(ValueProp.Unpowered);
    }
    public static decimal ApplyPhysicalResistancePreview(
        CardModel card,
        CardPreviewMode previewMode,
        Creature? target,
        decimal amount,
        ValueProp props,
        LibraryDamageType damageType)
    {
        if (!ShouldApplyResistance(props, damageType))
        {
            return amount;
        }

        if (target is LibraryCreature libraryTarget)
        {
            PulseResistanceIcons(libraryTarget, damageType);
            return CalculatePhysicalResistancePreview(amount, libraryTarget, props, damageType);
        }

        if (previewMode != CardPreviewMode.MultiCreatureTargeting || card.CombatState == null)
        {
            return amount;
        }

        decimal? firstValue = null;
        bool allSame = true;
        foreach (Creature enemy in card.CombatState.HittableEnemies)
        {
            if (enemy is not LibraryCreature libraryEnemy)
            {
                continue;
            }

            PulseResistanceIcons(libraryEnemy, damageType);
            decimal previewValue = CalculatePhysicalResistancePreview(amount, libraryEnemy, props, damageType);
            if (!firstValue.HasValue)
            {
                firstValue = previewValue;
            }
            else if ((int)previewValue != (int)firstValue.Value)
            {
                allSame = false;
            }
        }

        return allSame && firstValue.HasValue ? firstValue.Value : amount;
    }
    private static decimal CalculatePhysicalResistancePreview(
        decimal amount,
        LibraryCreature target,
        ValueProp props,
        LibraryDamageType damageType)
    {
        if (props.HasFlag(ValueProp.Unblockable))
        {
            return LibraryDamageCalculate.CalculateHpLoss(amount, target, props, damageType);
        }
        decimal blockedDamage = Math.Min(target.Block, Math.Max(amount, 0m));
        return blockedDamage + LibraryDamageCalculate.CalculateUnblockedDamage(amount, blockedDamage, target, props, damageType);
    }
}