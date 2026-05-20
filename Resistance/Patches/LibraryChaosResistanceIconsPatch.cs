#nullable enable
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using Library.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace Library.Resistance.Patches;

/// <summary>
///     在 NHealthBar 上显示混乱抗性图标（斩/刺/打三个小图标）。
///     根据生物当前的 <see cref="LibraryStaggerResistancePower"/> 动态刷新图标和悬浮提示。
/// </summary>
internal static class LibraryChaosResistanceIconsUi
{
    private const float IconSize = 28f;
    private const float IconSpacing = 1f;
    private const float RightOffset = 1f;
    private const float TopOffset = -52f;

    private static readonly LibraryDamageKind[] DisplayOrder =
        [LibraryDamageKind.Slash, LibraryDamageKind.Pierce, LibraryDamageKind.Blunt];

    private static readonly FieldInfo? CreatureField =
        AccessTools.Field(typeof(NHealthBar), "_creature");

    private sealed class State
    {
        public Control? Container;
        public TextureRect?[] Icons = new TextureRect?[3];
        public Control?[] Hitboxes = new Control?[3];
        public LibraryResistanceLevel[] LastLevels =
            [LibraryResistanceLevel.Normal, LibraryResistanceLevel.Normal, LibraryResistanceLevel.Normal];
        public bool WasVisible;
    }

    private static readonly ConditionalWeakTable<NHealthBar, State> States = new();

    private static State GetOrCreateState(NHealthBar healthBar)
    {
        if (!States.TryGetValue(healthBar, out State? state))
        {
            state = new State();
            States.AddOrUpdate(healthBar, state);
        }
        return state;
    }

    private static Creature? GetCreature(NHealthBar healthBar)
    {
        return CreatureField?.GetValue(healthBar) as Creature;
    }

    public static void Refresh(NHealthBar healthBar)
    {
        Creature? creature = GetCreature(healthBar);
        if (creature == null) return;

        var libCreature = creature as LibraryCreature;
        State state = GetOrCreateState(healthBar);

        bool shouldShow = libCreature != null
            && creature.IsAlive && libCreature.MaxChaoValue > 0;

        if (!shouldShow)
        {
            if (state.Container != null)
                state.Container.Visible = false;
            state.WasVisible = false;
            return;
        }

        if (state.Container == null)
            CreateIconNodes(healthBar, state);

        if (state.Container == null) return;

        state.Container.Visible = true;
        state.WasVisible = true;
        SyncLayout(healthBar, state);
        UpdateIcons(libCreature!, state);
    }

    private static void CreateIconNodes(NHealthBar healthBar, State state)
    {
        Control hpBarContainer = healthBar.HpBarContainer;
        if (hpBarContainer == null) return;

        Node healthBarNode = hpBarContainer.GetParent();
        if (healthBarNode == null) return;

        var container = new Control
        {
            Name = "LibraryOfRuinaChaosResistIcons",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        healthBarNode.AddChild(container);

        for (int i = 0; i < 3; i++)
        {
            int index = i;
            LibraryDamageKind damageKind = DisplayOrder[i];

            var hitbox = new Control
            {
                Name = $"ChaosResistHitbox_{damageKind}",
                MouseFilter = Control.MouseFilterEnum.Stop,
                Size = new Vector2(IconSize, IconSize),
                Position = new Vector2(0f, i * (IconSize + IconSpacing)),
            };
            container.AddChild(hitbox);

            var icon = new TextureRect
            {
                Name = $"ChaosResistIcon_{damageKind}",
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps,
                Size = new Vector2(IconSize, IconSize),
                Position = Vector2.Zero,
            };
            hitbox.AddChild(icon);

            hitbox.Connect(Control.SignalName.MouseEntered, Callable.From(() =>
                OnIconHovered(healthBar, damageKind, hitbox)));
            hitbox.Connect(Control.SignalName.MouseExited, Callable.From(() =>
                OnIconUnhovered(hitbox)));

            state.Icons[i] = icon;
            state.Hitboxes[i] = hitbox;
        }

        state.Container = container;
    }

    private static void SyncLayout(NHealthBar healthBar, State state)
    {
        if (state.Container == null) return;

        Control hpBarContainer = healthBar.HpBarContainer;
        float barWidth = hpBarContainer.Size.X;

        float staggerBarY = hpBarContainer.Position.Y - 14f - 2f;

        state.Container.Position = new Vector2(
            hpBarContainer.Position.X + barWidth + RightOffset,
            staggerBarY + TopOffset
        );
    }

    private static void UpdateIcons(LibraryCreature creature, State state)
    {
        for (int i = 0; i < 3; i++)
        {
            LibraryDamageKind damageKind = DisplayOrder[i];
            LibraryResistanceLevel level = creature.GetChaosResistanceLevel(damageKind);

            if (state.Icons[i] == null) continue;

            if (level != state.LastLevels[i] || state.Icons[i]!.Texture == null)
            {
                string texPath = GetChaosIconPath(damageKind, level);
                state.Icons[i]!.Texture = ResourceLoader.Load<Texture2D>(texPath, null,
                    ResourceLoader.CacheMode.Reuse);
                state.LastLevels[i] = level;
            }
        }
    }

    private static string GetChaosIconPath(LibraryDamageKind kind, LibraryResistanceLevel level)
    {
        string typeStr = kind switch
        {
            LibraryDamageKind.Slash => "slash",
            LibraryDamageKind.Pierce => "pierce",
            LibraryDamageKind.Blunt => "blunt",
            _ => "slash"
        };
        string levelStr = level.GetLocKeySuffix();
        return $"res://images/resistance/{typeStr}_chaos_{levelStr}.png";
    }

    private static void OnIconHovered(NHealthBar healthBar, LibraryDamageKind damageKind, Control hitbox)
    {
        Creature? creature = GetCreature(healthBar);
        if (creature == null) return;

        var libCreature = creature as LibraryCreature;
        if (libCreature == null) return;

        LibraryResistanceLevel level = libCreature.GetChaosResistanceLevel(damageKind);

        string typeName = damageKind switch
        {
            LibraryDamageKind.Slash => new LocString("powers", "DAMAGE_TYPE_RESISTANCE.slash_chaos").GetRawText(),
            LibraryDamageKind.Pierce => new LocString("powers", "DAMAGE_TYPE_RESISTANCE.pierce_chaos").GetRawText(),
            LibraryDamageKind.Blunt => new LocString("powers", "DAMAGE_TYPE_RESISTANCE.blunt_chaos").GetRawText(),
            _ => ""
        };

        string levelText = new LocString("powers",
            $"DAMAGE_TYPE_RESISTANCE.{level.GetLocKeySuffix()}").GetRawText();

        string format = new LocString("powers", "DAMAGE_TYPE_RESISTANCE.tooltip_format").GetRawText();
        string description = string.Format(format, typeName, levelText);

        var tip = new HoverTip(new LocString("powers", "DAMAGE_TYPE_RESISTANCE_POWER.title"), description);

        NHoverTipSet.CreateAndShow(hitbox, tip, HoverTip.GetHoverTipAlignment(hitbox));
    }

    private static void OnIconUnhovered(Control hitbox)
    {
        NHoverTipSet.Remove(hitbox);
    }
}

/// <summary>
///     NHealthBar.RefreshForeground 后刷新混乱抗性图标。
/// </summary>
[HarmonyPatch(typeof(NHealthBar), "RefreshForeground")]
internal static class LibraryChaosResistanceIconsRefreshPatch
{
    private static void Postfix(NHealthBar __instance)
    {
        try
        {
            LibraryChaosResistanceIconsUi.Refresh(__instance);
        }
        catch (Exception)
        {
        }
    }
}
