#nullable enable
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using LibraryLib.Entities.Creatures;
using LibraryLib.Models;
using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace LibraryLib.Patches;

/// <summary>
///     在 NHealthBar 右侧显示斩/刺/打三个抗性小图标，带悬浮提示和受击预览闪烁。
///     <see cref="Chaos"/> 显示混乱抗性（与混乱条同高），<see cref="Physical"/> 显示物理抗性（叠在其上方）。
/// </summary>
internal sealed class LibraryResistanceIconsUi
{
    private const float IconSize = 28f;
    private const float IconSpacing = 1f;
    private const float RightOffset = 1f;
    private const float TopOffset = -52f;
    private const ulong PulseCooldownMs = 120;

    private static readonly LibraryDamageType[] DisplayOrder =
        [LibraryDamageType.Slash, LibraryDamageType.Pierce, LibraryDamageType.Blunt];

    public static readonly LibraryResistanceIconsUi Chaos = new(
        kind: "Chaos",
        iconInfix: "_chaos",
        rowsAboveStaggerBar: 0,
        getLevel: static (creature, type) => creature.GetChaosResistanceLevel(type),
        // Monsters without Chao still show chaos icons when some resistance differs from Normal.
        shouldShow: static creature => creature.MaxChaoValue > 0
            || DisplayOrder.Any(type => creature.GetChaosResistanceLevel(type) != LibraryResistanceLevel.Normal));

    public static readonly LibraryResistanceIconsUi Physical = new(
        kind: "Physical",
        iconInfix: "",
        rowsAboveStaggerBar: 3,
        getLevel: static (creature, type) => creature.GetPhysicalResistanceLevel(type),
        shouldShow: static _ => true);

    private sealed class State
    {
        public Control? Container;
        public readonly TextureRect?[] Icons = new TextureRect?[3];
        public readonly ColorRect?[] Highlights = new ColorRect?[3];
        public readonly Tween?[] PulseTweens = new Tween?[3];
        public readonly ulong[] LastPulseTicks = new ulong[3];
        public readonly LibraryResistanceLevel[] LastLevels =
            [LibraryResistanceLevel.Normal, LibraryResistanceLevel.Normal, LibraryResistanceLevel.Normal];
    }

    private readonly string _kind;
    private readonly string _iconInfix;
    private readonly float _verticalShift;
    private readonly Func<LibraryCreature, LibraryDamageType, LibraryResistanceLevel> _getLevel;
    private readonly Func<LibraryCreature, bool> _shouldShow;
    private readonly ConditionalWeakTable<NHealthBar, State> _states = new();

    private LibraryResistanceIconsUi(
        string kind,
        string iconInfix,
        int rowsAboveStaggerBar,
        Func<LibraryCreature, LibraryDamageType, LibraryResistanceLevel> getLevel,
        Func<LibraryCreature, bool> shouldShow)
    {
        _kind = kind;
        _iconInfix = iconInfix;
        _verticalShift = rowsAboveStaggerBar * (IconSize + IconSpacing);
        _getLevel = getLevel;
        _shouldShow = shouldShow;
    }

    public void Refresh(NHealthBar? healthBar)
    {
        if (healthBar == null || LibraryHealthBarAccess.GetCreature(healthBar) is not { } creature)
            return;

        State state = _states.GetValue(healthBar, static _ => new State());
        if (creature is not LibraryCreature { Monster: LibraryMonsterModel { ShowResistanceUi: true }, IsAlive: true } libraryCreature
            || !_shouldShow(libraryCreature))
        {
            if (state.Container != null)
                state.Container.Visible = false;
            return;
        }

        if (state.Container == null)
            CreateIconNodes(healthBar, state);
        if (state.Container == null)
            return;

        state.Container.Visible = true;
        SyncLayout(healthBar, state);
        UpdateIcons(libraryCreature, state);
    }

    public void Pulse(LibraryCreature creature, LibraryDamageType damageType)
    {
        NHealthBar? healthBar = creature.HealthBar;
        if (healthBar == null)
            return;

        Refresh(healthBar);

        State state = _states.GetValue(healthBar, static _ => new State());
        int index = Array.IndexOf(DisplayOrder, damageType);
        if (index < 0)
            return;

        TextureRect? icon = state.Icons[index];
        if (icon == null || !GodotObject.IsInstanceValid(icon))
            return;

        ulong now = Time.GetTicksMsec();
        if (state.LastPulseTicks[index] != 0 && now - state.LastPulseTicks[index] < PulseCooldownMs)
            return;
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
        bool hasHighlight = highlight != null && GodotObject.IsInstanceValid(highlight);

        tween.TweenProperty(icon, "scale", Vector2.One * peakScale, 0.08)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.Parallel().TweenProperty(icon, "modulate", new Color(1f, 0.98f, 0.72f, 1f), 0.08)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        if (hasHighlight)
        {
            tween.Parallel().TweenProperty(highlight, "scale", Vector2.One * (peakScale + 0.08f), 0.08)
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
        if (hasHighlight)
        {
            tween.Parallel().TweenProperty(highlight, "scale", Vector2.One * 0.92f, 0.12)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Back);
            tween.Parallel().TweenProperty(highlight, "color", new Color(1f, 0.78f, 0.08f, 0.06f), 0.12)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Sine);
        }
    }

    private void CreateIconNodes(NHealthBar healthBar, State state)
    {
        Control hpBarContainer = healthBar.HpBarContainer;
        if (hpBarContainer?.GetParent() is not { } healthBarNode)
            return;

        var container = new Control
        {
            Name = $"LibraryOfRuina{_kind}ResistIcons",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        healthBarNode.AddChild(container);
        healthBarNode.MoveChild(container, 0);

        for (int i = 0; i < DisplayOrder.Length; i++)
        {
            LibraryDamageType damageType = DisplayOrder[i];

            var hitbox = new Control
            {
                Name = $"{_kind}ResistHitbox_{damageType}",
                MouseFilter = Control.MouseFilterEnum.Stop,
                Size = new Vector2(IconSize, IconSize),
                Position = new Vector2(0f, i * (IconSize + IconSpacing)),
            };
            container.AddChild(hitbox);

            var highlight = new ColorRect
            {
                Name = $"{_kind}ResistPulse_{damageType}",
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Size = new Vector2(IconSize + 10f, IconSize + 10f),
                Position = new Vector2(-5f, -5f),
                Color = new Color(1f, 0.78f, 0.08f, 0f),
                Visible = false,
            };
            hitbox.AddChild(highlight);

            var icon = new TextureRect
            {
                Name = $"{_kind}ResistIcon_{damageType}",
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps,
                Size = new Vector2(IconSize, IconSize),
                Position = Vector2.Zero,
            };
            hitbox.AddChild(icon);

            hitbox.Connect(Control.SignalName.MouseEntered, Callable.From(() => OnIconHovered(healthBar, damageType, hitbox)));
            hitbox.Connect(Control.SignalName.MouseExited, Callable.From(() => NHoverTipSet.Remove(hitbox)));

            state.Icons[i] = icon;
            state.Highlights[i] = highlight;
        }

        state.Container = container;
    }

    private void SyncLayout(NHealthBar healthBar, State state)
    {
        if (state.Container == null)
            return;

        Control hpBarContainer = healthBar.HpBarContainer;
        state.Container.ZIndex = 0;
        state.Container.ZAsRelative = true;
        Node? healthBarNode = hpBarContainer.GetParent();
        if (healthBarNode != null && state.Container.GetIndex() != 0)
            healthBarNode.MoveChild(state.Container, 0);

        float staggerBarY = hpBarContainer.Position.Y - 14f - 2f;
        state.Container.Position = new Vector2(
            hpBarContainer.Position.X + hpBarContainer.Size.X + RightOffset,
            staggerBarY + TopOffset - _verticalShift);
    }

    private void UpdateIcons(LibraryCreature creature, State state)
    {
        for (int i = 0; i < DisplayOrder.Length; i++)
        {
            TextureRect? icon = state.Icons[i];
            if (icon == null)
                continue;

            LibraryDamageType damageType = DisplayOrder[i];
            LibraryResistanceLevel level = _getLevel(creature, damageType);
            if (level == state.LastLevels[i] && icon.Texture != null)
                continue;

            string path = $"res://LibraryOfRuinaLib/images/resistance/{damageType.String()}{_iconInfix}_{level.GetLocKeySuffix()}.png";
            icon.Texture = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
            state.LastLevels[i] = level;
        }
    }

    private void OnIconHovered(NHealthBar healthBar, LibraryDamageType damageType, Control hitbox)
    {
        if (LibraryHealthBarAccess.GetCreature(healthBar) is not LibraryCreature creature)
            return;

        LibraryResistanceLevel level = _getLevel(creature, damageType);
        string typeName = new LocString("powers", $"DAMAGE_TYPE_RESISTANCE.{damageType.String()}_{_kind.ToLowerInvariant()}").GetRawText();

        var levelLoc = new LocString("powers", $"DAMAGE_TYPE_RESISTANCE.{level.GetLocKeySuffix()}");
        levelLoc.Add("Multiplier", level.GetMultiplierText());
        string format = new LocString("powers", "DAMAGE_TYPE_RESISTANCE.tooltip_format").GetRawText();
        string description = string.Format(format, typeName, levelLoc.GetFormattedText());

        var tip = new HoverTip(new LocString("powers", "DAMAGE_TYPE_RESISTANCE_POWER.title"), description);
        NHoverTipSet.CreateAndShow(hitbox, tip, HoverTip.GetHoverTipAlignment(hitbox));
    }
}

/// <summary>NHealthBar.RefreshForeground 后刷新混乱与物理抗性图标。</summary>
[HarmonyPatch(typeof(NHealthBar), "RefreshForeground")]
internal static class LibraryResistanceIconsRefreshPatch
{
    private static void Postfix(NHealthBar __instance)
    {
        Refresh(LibraryResistanceIconsUi.Chaos, __instance);
        Refresh(LibraryResistanceIconsUi.Physical, __instance);
    }

    private static void Refresh(LibraryResistanceIconsUi ui, NHealthBar healthBar)
    {
        try
        {
            ui.Refresh(healthBar);
        }
        catch (Exception)
        {
            // UI refresh must never break the vanilla health bar.
        }
    }
}
