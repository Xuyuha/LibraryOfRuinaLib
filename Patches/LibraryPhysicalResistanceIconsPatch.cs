#nullable enable
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using Library.Entities.Creatures;
using Library.Models;
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
    private const ulong PulseCooldownMs = 120;

    private static readonly LibraryDamageType[] DisplayOrder =
        [LibraryDamageType.Slash, LibraryDamageType.Pierce, LibraryDamageType.Blunt];

    private static readonly FieldInfo? CreatureField =
        AccessTools.Field(typeof(NHealthBar), "_creature");

    private sealed class State
    {
        public Control? Container;
        public TextureRect?[] Icons = new TextureRect?[3];
        public ColorRect?[] Highlights = new ColorRect?[3];
        public Control?[] Hitboxes = new Control?[3];
        public Tween?[] PulseTweens = new Tween?[3];
        public ulong[] LastPulseTicks = new ulong[3];
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

    private static Creature? GetCreature(NHealthBar? healthBar)
    {
        if (healthBar == null) return null;

        return CreatureField?.GetValue(healthBar) as Creature;
    }

    public static void Refresh(NHealthBar? healthBar)
    {
        if (healthBar == null) return;

        Creature? creature = GetCreature(healthBar);
        if (creature == null) return;

        var libCreature = creature as LibraryCreature;
        State state = GetOrCreateState(healthBar);

        bool shouldShow = libCreature?.Monster is LibraryMonsterModel { ShowResistanceUi: true }
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

    public static void Pulse(LibraryCreature creature, LibraryDamageType damageType)
    {
        NHealthBar? healthBar = creature.HealthBar;
        if (healthBar == null) return;

        Refresh(healthBar);

        State state = GetOrCreateState(healthBar);
        int index = Array.IndexOf(DisplayOrder, damageType);
        if (index < 0) return;

        TextureRect? icon = state.Icons[index];
        if (icon == null || !GodotObject.IsInstanceValid(icon)) return;

        ulong now = Time.GetTicksMsec();
        if (state.LastPulseTicks[index] != 0 && now - state.LastPulseTicks[index] < PulseCooldownMs) return;
        state.LastPulseTicks[index] = now;

        Tween? runningTween = state.PulseTweens[index];
        if (runningTween != null && GodotObject.IsInstanceValid(runningTween))
            runningTween.Kill();

        ColorRect? highlight = state.Highlights[index];
        icon.PivotOffset = icon.Size / 2f;
        icon.Position = Vector2.Zero;
        icon.Scale = Vector2.One * 1.04f;
        icon.Modulate = new Color(1f, 0.98f, 0.72f, 1f);
        if (highlight != null && GodotObject.IsInstanceValid(highlight))
        {
            highlight.PivotOffset = highlight.Size / 2f;
            highlight.Scale = Vector2.One * 1.08f;
            highlight.Color = new Color(1f, 0.72f, 0.02f, 0.32f);
            highlight.Visible = true;
        }

        Tween tween = icon.CreateTween();
        AddFlashStep(tween, icon, highlight, 1.16f, 0.42f);
        AddFlashStep(tween, icon, highlight, 1.09f, 0.24f);
        tween.TweenCallback(Callable.From(() =>
        {
            if (GodotObject.IsInstanceValid(icon))
            {
                icon.Scale = Vector2.One;
                icon.Modulate = Colors.White;
            }

            if (highlight != null && GodotObject.IsInstanceValid(highlight))
            {
                highlight.Scale = Vector2.One * 0.85f;
                highlight.Color = new Color(1f, 0.78f, 0.08f, 0f);
                highlight.Visible = false;
            }
        }));
        state.PulseTweens[index] = tween;
    }

    private static void AddFlashStep(Tween tween, TextureRect icon, ColorRect? highlight, float peakScale, float peakAlpha)
    {
        float highlightScale = peakScale + 0.08f;

        tween.TweenProperty(icon, "scale", Vector2.One * peakScale, 0.08)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.Parallel().TweenProperty(icon, "modulate", new Color(1f, 0.98f, 0.72f, 1f), 0.08)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);

        if (highlight != null && GodotObject.IsInstanceValid(highlight))
        {
            tween.Parallel().TweenProperty(highlight, "scale", Vector2.One * highlightScale, 0.08)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
            tween.Parallel().TweenProperty(highlight, "color", new Color(1f, 0.70f, 0.02f, peakAlpha), 0.08)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
        }

        tween.TweenInterval(0.03);

        tween.TweenProperty(icon, "scale", Vector2.One, 0.12)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
        tween.Parallel().TweenProperty(icon, "modulate", Colors.White, 0.12)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);

        if (highlight != null && GodotObject.IsInstanceValid(highlight))
        {
            tween.Parallel().TweenProperty(highlight, "scale", Vector2.One * 0.92f, 0.12)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Back);
            tween.Parallel().TweenProperty(highlight, "color", new Color(1f, 0.78f, 0.08f, 0.06f), 0.12)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Sine);
        }
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
        healthBarNode.MoveChild(container, 0);

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

            var highlight = new ColorRect
            {
                Name = $"PhysicalResistPulse_{damageType}",
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Size = new Vector2(IconSize + 10f, IconSize + 10f),
                Position = new Vector2(-5f, -5f),
                Color = new Color(1f, 0.78f, 0.08f, 0f),
                Visible = false,
            };
            hitbox.AddChild(highlight);

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
            state.Highlights[i] = highlight;
            state.Hitboxes[i] = hitbox;
        }

        state.Container = container;
    }

    private static void SyncLayout(NHealthBar healthBar, State state)
    {
        if (state.Container == null) return;

        Control hpBarContainer = healthBar.HpBarContainer;
        float barWidth = hpBarContainer.Size.X;

        state.Container.ZIndex = 0;
        state.Container.ZAsRelative = true;
        Node? healthBarNode = hpBarContainer.GetParent();
        if (healthBarNode != null && state.Container.GetIndex() != 0)
            healthBarNode.MoveChild(state.Container, 0);

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
