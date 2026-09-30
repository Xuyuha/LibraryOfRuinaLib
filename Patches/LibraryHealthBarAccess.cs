#nullable enable
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace LibraryLib.Patches;

/// <summary>Reads the creature an <see cref="NHealthBar"/> displays (vanilla keeps it private).</summary>
internal static class LibraryHealthBarAccess
{
    private static readonly AccessTools.FieldRef<NHealthBar, Creature?> CreatureRef =
        AccessTools.FieldRefAccess<NHealthBar, Creature?>("_creature");

    internal static Creature? GetCreature(NHealthBar? healthBar) =>
        healthBar == null ? null : CreatureRef(healthBar);
}
