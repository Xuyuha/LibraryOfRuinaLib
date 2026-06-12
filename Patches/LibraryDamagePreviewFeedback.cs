#nullable enable

using Library.Entities.Creatures;
using Library.Resistance.Patches;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Library.Patches;

/// <summary>
/// 让卡牌伤害预览在基础伤害修正之后继续吃 Library 受击抗性，并提示对应抗性图标。
/// </summary>
internal static class LibraryDamagePreviewFeedback
{
    public static void PulsePhysicalResistancePreview(
        CardModel card,
        CardPreviewMode previewMode,
        Creature? target,
        ValueProp props,
        LibraryDamageType damageType)
    {
        if (!ShouldApplyResistance(props, damageType))
        {
            return;
        }

        if (target is LibraryCreature libraryTarget)
        {
            PulseResistanceIcons(libraryTarget, damageType);
            return;
        }

        if (previewMode != CardPreviewMode.MultiCreatureTargeting || card.CombatState == null)
        {
            return;
        }

        foreach (Creature enemy in card.CombatState.HittableEnemies)
        {
            if (enemy is LibraryCreature libraryEnemy)
            {
                PulseResistanceIcons(libraryEnemy, damageType);
            }
        }
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
            return LibraryDamageCalculate.CalculateDamage(amount, libraryTarget, props, damageType);
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
            decimal previewValue = LibraryDamageCalculate.CalculateDamage(amount, libraryEnemy, props, damageType);
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

    private static bool ShouldApplyResistance(ValueProp props, LibraryDamageType damageType)
    {
        return damageType != LibraryDamageType.None
            && props.HasFlag(ValueProp.Move)
            && !props.HasFlag(ValueProp.Unpowered);
    }

    private static void PulseResistanceIcons(LibraryCreature creature, LibraryDamageType damageType)
    {
        LibraryPhysicalResistanceIconsUi.Pulse(creature, damageType);
        LibraryChaosResistanceIconsUi.Pulse(creature, damageType);
    }
}

/// <summary>
/// 原版 DamageVar 没有 LibraryDamageType；按当前兼容规则，预览时只闪打击抗性。
/// </summary>
[HarmonyPatch(typeof(DamageVar), nameof(DamageVar.UpdateCardPreview))]
internal static class LibraryVanillaDamageVarPreviewPulsePatch
{
    private static void Postfix(
        DamageVar __instance,
        CardModel card,
        CardPreviewMode previewMode,
        Creature? target)
    {
        LibraryDamagePreviewFeedback.PulsePhysicalResistancePreview(
            card,
            previewMode,
            target,
            __instance.Props,
            LibraryDamageType.Blunt);
    }
}

/// <summary>
/// 原版 CalculatedDamageVar 预览同样按打击抗性提示。
/// </summary>
[HarmonyPatch(typeof(CalculatedDamageVar), nameof(CalculatedDamageVar.UpdateCardPreview))]
internal static class LibraryVanillaCalculatedDamageVarPreviewPulsePatch
{
    private static void Postfix(
        CalculatedDamageVar __instance,
        CardModel card,
        CardPreviewMode previewMode,
        Creature? target)
    {
        LibraryDamagePreviewFeedback.PulsePhysicalResistancePreview(
            card,
            previewMode,
            target,
            __instance.Props,
            LibraryDamageType.Blunt);
    }
}

/// <summary>
/// 原版 OstyDamageVar 预览同样按打击抗性提示。
/// </summary>
[HarmonyPatch(typeof(OstyDamageVar), nameof(OstyDamageVar.UpdateCardPreview))]
internal static class LibraryVanillaOstyDamageVarPreviewPulsePatch
{
    private static void Postfix(
        OstyDamageVar __instance,
        CardModel card,
        CardPreviewMode previewMode,
        Creature? target)
    {
        LibraryDamagePreviewFeedback.PulsePhysicalResistancePreview(
            card,
            previewMode,
            target,
            __instance.Props,
            LibraryDamageType.Blunt);
    }
}
