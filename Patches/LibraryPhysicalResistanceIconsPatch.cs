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
///     在 NHealthBar 左侧显示物理抗性图标（斩/刺/打），与混乱抗性图标对称。
/// </summary>
internal static class LibraryPhysicalResistanceIconsUi//TODO:你们来做物理抗性相关的方法。
{
    private const float IconSize = 28f;
    private const float IconSpacing = 1f;
    private const float LeftOffset = 1f;
    private const float TopOffset = -52f;

    private static readonly LibraryDamageType[] DisplayOrder =
        [LibraryDamageType.Slash, LibraryDamageType.Pierce, LibraryDamageType.Blunt];

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
            && creature.IsAlive;

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
            Name = "LibraryOfRuinaPhysicalResistIcons",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        healthBarNode.AddChild(container);

        for (int i = 0; i < 3; i++)
        {
            LibraryDamageType damageType = DisplayOrder[i];

            var hitbox = new Control
            {
                Name = $"PhysicalResistHitbox_{damageType}",
                MouseFilter = Control.MouseFilterEnum.Stop,
                Size = new Vector2(IconSize, IconSize),
                Position = new Vector2(0f, i * (IconSize + IconSpacing)),
            };
            container.AddChild(hitbox);

            var icon = new TextureRect
            {
                Name = $"PhysicalResistIcon_{damageType}",
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps,
                Size = new Vector2(IconSize, IconSize),
                Position = Vector2.Zero,
            };
            hitbox.AddChild(icon);

            hitbox.Connect(Control.SignalName.MouseEntered, Callable.From(() =>
                OnIconHovered(healthBar, damageType, hitbox)));
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
        float chaosIconsHeight = 3 * (IconSize + IconSpacing);
        state.Container.Position = new Vector2(
            hpBarContainer.Position.X + barWidth + LeftOffset,
            staggerBarY + TopOffset - chaosIconsHeight
        );
    }

    private static void UpdateIcons(LibraryCreature creature, State state)
    {
        for (int i = 0; i < 3; i++)
        {
            LibraryDamageType damageType = DisplayOrder[i];
            LibraryResistanceLevel level = creature.GetPhysicalResistanceLevel(damageType);

            if (state.Icons[i] == null) continue;

            if (level != state.LastLevels[i] || state.Icons[i]!.Texture == null)
            {
                string texPath = GetPhysicalIconPath(damageType, level);
                state.Icons[i]!.Texture = ResourceLoader.Load<Texture2D>(texPath, null,
                    ResourceLoader.CacheMode.Reuse);
                state.LastLevels[i] = level;
            }
        }
    }

    private static string GetPhysicalIconPath(LibraryDamageType type, LibraryResistanceLevel level)
    {
        string levelStr = level.GetLocKeySuffix();
        return $"res://images/resistance/{type.String()}_{levelStr}.png";
    }

    private static void OnIconHovered(NHealthBar healthBar, LibraryDamageType damageType, Control hitbox)
    {
        Creature? creature = GetCreature(healthBar);
        if (creature == null) return;

        var libCreature = creature as LibraryCreature;
        if (libCreature == null) return;

        LibraryResistanceLevel level = libCreature.GetPhysicalResistanceLevel(damageType);

        string typeName = damageType == LibraryDamageType.None?"" :new LocString("powers", $"DAMAGE_TYPE_RESISTANCE.{damageType.String()}_physical").GetRawText();

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
///     NHealthBar.RefreshForeground 后刷新物理抗性图标。
/// </summary>
[HarmonyPatch(typeof(NHealthBar), "RefreshForeground")]
internal static class LibraryPhysicalResistanceIconsRefreshPatch
{
    private static void Postfix(NHealthBar __instance)
    {
        try
        {
            LibraryPhysicalResistanceIconsUi.Refresh(__instance);
        }
        catch (Exception)
        {
        }
    }
}
