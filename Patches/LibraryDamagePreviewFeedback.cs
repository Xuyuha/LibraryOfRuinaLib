#nullable enable

using Library.Entities.Creatures;
using Library.Resistance.Patches;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Library.Patches;

/// <summary>
/// 让卡牌伤害预览在基础伤害修正之后继续吃 Library 受击抗性，并提示对应抗性图标。
/// </summary>
internal static class LibraryDamagePreviewFeedback
{
    private const ValueProp FallbackPreviewProps = ValueProp.Move;

    private static readonly HashSet<string> FixedMultiHitCardIds =
    [
        "ASTRAL_PULSE",
        "DAGGER_SPRAY",
        "MAUL",
        "REFRACT",
        "RIP_AND_TEAR",
        "THRASH",
        "TWIN_STRIKE",
        "UPROAR",
    ];

    public static LibraryDamageType ResolveVanillaPreviewDamageType(CardModel card, Creature? target)
    {
        try
        {
            if (card.Type == CardType.Attack && IsPreviewMultiHit(card, target))
            {
                return LibraryDamageType.Pierce;
            }

            if (card.Type == CardType.Attack &&
                (card.TargetType == TargetType.AllEnemies ||
                 card.TargetType == TargetType.AllAllies))
            {
                return LibraryDamageType.Slash;
            }
        }
        catch
        {
            return LibraryDamageType.Blunt;
        }

        return LibraryDamageType.Blunt;
    }

    public static void PulsePhysicalResistancePreview(
        CardModel card,
        CardPreviewMode previewMode,
        Creature? target,
        ValueProp props,
        LibraryDamageType damageType)
    {
        try
        {
            if (!ResistancePreview.ShouldApplyResistance(props, damageType))
            {
                return;
            }

            if (target is LibraryCreature libraryTarget)
            {
                ResistancePreview.PulseResistanceIcons(libraryTarget, damageType);
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
                    ResistancePreview.PulseResistanceIcons(libraryEnemy, damageType);
                }
            }
        }
        catch
        {
        }
    }

    public static void SafePulseVanillaPreview(
        CardModel? card,
        CardPreviewMode previewMode,
        Creature? target,
        ValueProp props)
    {
        if (card == null)
        {
            return;
        }

        try
        {
            PulsePhysicalResistancePreview(
                card,
                previewMode,
                target,
                props,
                ResolveVanillaPreviewDamageType(card, target));
        }
        catch
        {
            try
            {
                PulsePhysicalResistancePreview(
                    card,
                    previewMode,
                    target,
                    props,
                    LibraryDamageType.Blunt);
            }
            catch
            {
            }
        }
    }

    public static ValueProp SafeGetProps(DynamicVar? dynamicVar)
    {
        try
        {
            return dynamicVar switch
            {
                DamageVar damageVar => damageVar.Props,
                CalculatedDamageVar calculatedDamageVar => calculatedDamageVar.Props,
                OstyDamageVar ostyDamageVar => ostyDamageVar.Props,
                _ => FallbackPreviewProps
            };
        }
        catch
        {
            return FallbackPreviewProps;
        }
    }

    private static bool IsPreviewMultiHit(CardModel card, Creature? target)
    {
        if (FixedMultiHitCardIds.Contains(card.Id.Entry))
        {
            return true;
        }

        if (card.Id.Entry == "DISMANTLE")
        {
            return target?.HasPower<VulnerablePower>() == true;
        }

        if (card.Id.Entry == "FIEND_FIRE")
        {
            return GetHandCardCount(card) > 1;
        }

        if (card.Id.Entry == "FOLLOW_THROUGH" &&
            card.DynamicVars.TryGetValue("CardCount", out DynamicVar? cardCount))
        {
            return GetOtherHandCardCount(card) >= cardCount.IntValue;
        }

        if (card.DynamicVars.TryGetValue("Repeat", out DynamicVar? repeat) && repeat.IntValue > 1)
        {
            if (card.Id.Entry == "SPITE")
            {
                return HasOwnerLostHpThisTurn(card);
            }

            return true;
        }

        if (card.DynamicVars.TryGetValue("CalculatedHits", out DynamicVar? calculatedHits) &&
            CalculateDynamicVar(calculatedHits, target) > 1m)
        {
            return true;
        }

        if ((card.EnergyCost.CostsX || card.HasStarCostX) &&
            (card.DynamicVars.ContainsKey("Damage") ||
             card.DynamicVars.ContainsKey("CalculatedDamage") ||
             card.DynamicVars.ContainsKey("OstyDamage")))
        {
            return true;
        }

        return false;
    }

    private static int GetHandCardCount(CardModel card)
    {
        try
        {
            return card.Owner.PlayerCombatState?.Hand.Cards.Count ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private static int GetOtherHandCardCount(CardModel card)
    {
        try
        {
            return card.Owner.PlayerCombatState?.Hand.Cards.Count(other => other != card) ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private static bool HasOwnerLostHpThisTurn(CardModel card)
    {
        try
        {
            Creature owner = card.Owner.Creature;
            return CombatManager.Instance.History.Entries.Any(entry =>
                entry.GetType().Name == "DamageReceivedEntry" &&
                AccessTools.PropertyGetter(entry.GetType(), "Receiver")?.Invoke(entry, null) == owner &&
                AccessTools.Method(entry.GetType(), "HappenedThisTurn")?.Invoke(entry, [owner.CombatState]) is true &&
                AccessTools.PropertyGetter(entry.GetType(), "Result")?.Invoke(entry, null) is DamageResult result &&
                result.UnblockedDamage > 0);
        }
        catch
        {
            return false;
        }
    }

    private static decimal CalculateDynamicVar(DynamicVar dynamicVar, Creature? target)
    {
        try
        {
            return AccessTools.Method(dynamicVar.GetType(), "Calculate")?.Invoke(dynamicVar, [target]) is decimal value
                ? value
                : dynamicVar.PreviewValue;
        }
        catch
        {
            return dynamicVar.PreviewValue;
        }
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
        LibraryDamagePreviewFeedback.SafePulseVanillaPreview(
            card,
            previewMode,
            target,
            LibraryDamagePreviewFeedback.SafeGetProps(__instance));
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
        LibraryDamagePreviewFeedback.SafePulseVanillaPreview(
            card,
            previewMode,
            target,
            LibraryDamagePreviewFeedback.SafeGetProps(__instance));
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
        LibraryDamagePreviewFeedback.SafePulseVanillaPreview(
            card,
            previewMode,
            target,
            LibraryDamagePreviewFeedback.SafeGetProps(__instance));
    }
}
