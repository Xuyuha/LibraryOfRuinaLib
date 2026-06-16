#nullable enable

using HarmonyLib;
using Library.Entities.Creatures;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace Library.Patches;

/// <summary>
/// 临时把原版 AttackCommand 的伤害跳字接入 Library 打击类型显示。
/// </summary>
[HarmonyPatch(typeof(NDamageNumVfx), nameof(NDamageNumVfx.Create), typeof(Creature), typeof(DamageResult))]
internal static class LibraryDamageNumberPatch
{
    private static bool Prefix(Creature target, DamageResult result, ref NDamageNumVfx? __result)
    {
        if (!AttackExecuteContext.IsInAttackExecute.Value || target is not LibraryCreature)
        {
            return true;
        }

        LibraryRuinaDamageNumberVfx? vfx = LibraryRuinaDamageNumberVfx.CreatePhysical(
            target,
            result,
            AttackExecuteContext.CurrentDamageType);

        if (vfx == null)
        {
            return true;
        }

        Node? vfxContainer = target.GetVfxContainer();
        if (vfxContainer != null)
        {
            vfxContainer.AddChildSafely(vfx);
        }
        else
        {
            NRun.Instance.GlobalUi.AddChildSafely(vfx);
        }

        __result = null;
        return false;
    }
}
