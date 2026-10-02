#if STS2_0_107_1
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
internal static partial class LibraryVanillaDamageResolutionPatch
{
    private static bool Prefix(
        ICombatState? combatState,
        Creature? target,
        Creature? dealer,
        decimal damage,
        ValueProp props,
        CardModel? cardSource,
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
                null,
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
#endif
