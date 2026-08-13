using HarmonyLib;
using LibraryLib.Combat;
using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Patches;

/// <summary>
/// Makes the reusable Library policy authoritative for vanilla damage in both
/// DynamicVar preview and CreatureCmd execution.
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyDamage))]
internal static class LibraryVanillaDamageResolutionPatch
{
    private static bool Prefix(
        ICombatState? combatState,
        Creature? target,
        Creature? dealer,
        decimal damage,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay,
        CardPreviewMode previewMode,
        ref IEnumerable<AbstractModel> modifiers,
        ref decimal __result)
    {
        LibraryCombatValueResolution resolution =
            LibraryCombatValueResolver.Resolve(
                combatState,
                LibraryCombatValueKind.PhysicalDamage,
                damage,
                target,
                dealer,
                props,
                cardSource,
                cardPlay,
                LibraryDamageType.None,
                previewMode);
        if (resolution == LibraryCombatValueResolution.Default)
        {
            return true;
        }

        modifiers = Array.Empty<AbstractModel>();
        __result = LibraryCombatValueResolver.ResolveBaseValue(
            resolution,
            damage);
        return false;
    }
}

/// <summary>
/// Skips Vulnerable, Intangible, protection and other HP-loss hooks after the
/// base-only damage value has passed core block absorption.
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyHpLost))]
internal static class LibraryVanillaHpLossResolutionPatch
{
    private static bool Prefix(
        ICombatState? combatState,
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        ref IEnumerable<AbstractModel> modifiers,
        ref decimal __result)
    {
        LibraryCombatValueResolution resolution =
            LibraryCombatValueResolver.Resolve(
                combatState,
                LibraryCombatValueKind.HpLoss,
                amount,
                target,
                dealer,
                props,
                cardSource);
        if (resolution == LibraryCombatValueResolution.Default)
        {
            return true;
        }

        modifiers = Array.Empty<AbstractModel>();
        __result = LibraryCombatValueResolver.ResolveBaseValue(
            resolution,
            amount);
        return false;
    }
}

/// <summary>
/// Applies the same policy to BlockVar preview and CreatureCmd.GainBlock.
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyBlock))]
internal static class LibraryVanillaBlockResolutionPatch
{
    private static bool Prefix(
        ICombatState combatState,
        Creature target,
        decimal block,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay,
        ref IEnumerable<AbstractModel> modifiers,
        ref decimal __result)
    {
        LibraryCombatValueResolution resolution =
            LibraryCombatValueResolver.Resolve(
                combatState,
                LibraryCombatValueKind.Block,
                block,
                target,
                target,
                props,
                cardSource,
                cardPlay);
        if (resolution == LibraryCombatValueResolution.Default)
        {
            return true;
        }

        modifiers = Array.Empty<AbstractModel>();
        __result = LibraryCombatValueResolver.ResolveBaseValue(
            resolution,
            block);
        return false;
    }
}
