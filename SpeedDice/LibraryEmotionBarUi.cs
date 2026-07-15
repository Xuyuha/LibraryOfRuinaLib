using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Library.SpeedDice;

internal static class LibraryEmotionBarUi
{
    private const string ContainerName = "LibraryEmotionBarContainer";
    private const string BorderPath =
        "res://LibraryOfRuinaLib/images/ui/library_emotion_level_border.png";
    private const string FontPath =
        "res://themes/kreon_bold_glyph_space_one.tres";
    private const float BarHeight = 14f;
    private const float BarGap = 2f;
    private const float ForegroundInset = 5f;
    private const float ForegroundInsetVertical = 3f;
    private const float ForegroundContainerInset = 10f;
    private const float MinFillWidth = 12f;
    private const float BadgeWidth = 44f;
    private const float BadgeHeight = 51f;

    private static readonly FieldInfo? CreatureField =
        AccessTools.Field(typeof(NHealthBar), "_creature");
    private static readonly ConditionalWeakTable<NHealthBar, UiState> States = new();
    private static readonly Color BackgroundColor = new("26183D");
    private static readonly Color FillColor = new("A84DE0");
    private static readonly Color LabelColor = new("F3D9FF");
    private static readonly Color OutlineColor = new("321341");

    private sealed class UiState
    {
        public Control? BarContainer;
        public NinePatchRect? Fill;
        public Label? ValueLabel;
        public TextureRect? Badge;
        public Label? BadgeLabel;
        public LibrarySpeedDiceCombatState? CombatState;
        public Action? ChangedHandler;
        public float MaxFillWidth;
    }

    public static void Refresh(NHealthBar healthBar)
    {
        Creature? creature = CreatureField?.GetValue(healthBar) as Creature;
        if (creature?.Player == null
            || !LibrarySpeedDiceService.TryGetState(
                creature.Player,
                out LibrarySpeedDiceCombatState? combatState)
            || combatState == null)
        {
            if (States.TryGetValue(healthBar, out UiState? hidden)
                && hidden.BarContainer != null)
            {
                hidden.BarContainer.Visible = false;
            }
            return;
        }

        UiState state = States.GetValue(healthBar, _ => new UiState());
        if (state.BarContainer == null)
            CreateNodes(healthBar, state);
        if (state.BarContainer == null)
            return;

        Subscribe(healthBar, state, combatState);
        state.BarContainer.Visible = creature.IsAlive;
        SyncLayout(healthBar, state);
        UpdateValues(state, combatState);
    }

    private static void Subscribe(
        NHealthBar healthBar,
        UiState state,
        LibrarySpeedDiceCombatState combatState)
    {
        if (state.CombatState == combatState)
            return;

        if (state.CombatState != null && state.ChangedHandler != null)
            state.CombatState.Changed -= state.ChangedHandler;

        state.CombatState = combatState;
        state.ChangedHandler = () => Callable.From(() =>
        {
            if (GodotObject.IsInstanceValid(healthBar))
                Refresh(healthBar);
        }).CallDeferred();
        combatState.Changed += state.ChangedHandler;
    }

    private static void CreateNodes(NHealthBar healthBar, UiState state)
    {
        Control hpBarContainer = healthBar.HpBarContainer;
        NinePatchRect? sourceBackground =
            hpBarContainer.GetNodeOrNull<NinePatchRect>("HpBackground");
        NinePatchRect? sourceFill =
            hpBarContainer.GetNodeOrNull<NinePatchRect>(
                "HpForegroundContainer/Mask/HpForeground");
        if (sourceBackground == null || sourceFill == null)
            return;

        var bar = new Control
        {
            Name = ContainerName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 1,
            ZAsRelative = true,
            AnchorLeft = 0f,
            AnchorTop = 0f,
            AnchorRight = 1f,
            AnchorBottom = 0f,
            OffsetLeft = 0f,
            OffsetTop = -(BarHeight + BarGap) * 2f,
            OffsetRight = 0f,
            OffsetBottom = -(BarHeight + BarGap) * 2f + BarHeight,
        };
        hpBarContainer.AddChild(bar);

        var background = (NinePatchRect)sourceBackground.Duplicate(15);
        background.Name = "EmotionBackground";
        background.Modulate = BackgroundColor;
        background.Visible = true;
        background.MouseFilter = Control.MouseFilterEnum.Ignore;
        background.AnchorLeft = 0f;
        background.AnchorTop = 0f;
        background.AnchorRight = 1f;
        background.AnchorBottom = 1f;
        background.OffsetLeft = 1f;
        background.OffsetTop = 0f;
        background.OffsetRight = -1f;
        background.OffsetBottom = 0f;
        bar.AddChild(background);

        var foregroundContainer = new Control
        {
            Name = "EmotionForegroundContainer",
            ClipChildren = CanvasItem.ClipChildrenMode.Only,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AnchorLeft = 0f,
            AnchorTop = 0f,
            AnchorRight = 1f,
            AnchorBottom = 1f,
            OffsetLeft = ForegroundInset,
            OffsetTop = ForegroundInsetVertical,
            OffsetRight = -ForegroundInset,
            OffsetBottom = -ForegroundInsetVertical,
        };
        bar.AddChild(foregroundContainer);

        var fill = (NinePatchRect)sourceFill.Duplicate(15);
        fill.Name = "EmotionFill";
        fill.Visible = false;
        fill.Material = null;
        fill.SelfModulate = FillColor;
        fill.MouseFilter = Control.MouseFilterEnum.Ignore;
        fill.AnchorLeft = 0f;
        fill.AnchorTop = 0f;
        fill.AnchorRight = 1f;
        fill.AnchorBottom = 1f;
        fill.OffsetLeft = 0f;
        fill.OffsetTop = -4f;
        fill.OffsetRight = 0f;
        fill.OffsetBottom = 4f;
        foreach (Node child in fill.GetChildren())
            child.QueueFree();
        foregroundContainer.AddChild(fill);

        Font? font = ResourceLoader.Load<Font>(FontPath);
        var valueLabel = new Label
        {
            Name = "EmotionValueLabel",
            AnchorLeft = 0f,
            AnchorTop = 0f,
            AnchorRight = 1f,
            AnchorBottom = 1f,
            OffsetLeft = 0f,
            OffsetTop = -6f,
            OffsetRight = 0f,
            OffsetBottom = 7f,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        ApplyLabelTheme(valueLabel, font, 20);
        bar.AddChild(valueLabel);

        var badge = new TextureRect
        {
            Name = "EmotionLevelBadge",
            Texture = ResourceLoader.Load<Texture2D>(BorderPath),
            AnchorLeft = 0f,
            AnchorTop = 0f,
            AnchorRight = 0f,
            AnchorBottom = 0f,
            OffsetLeft = -BadgeWidth + 2f,
            OffsetTop = (BarHeight - BadgeHeight) * 0.5f,
            OffsetRight = 2f,
            OffsetBottom = (BarHeight + BadgeHeight) * 0.5f,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false,
        };
        bar.AddChild(badge);

        var badgeLabel = new Label
        {
            Name = "EmotionLevelLabel",
            AnchorLeft = 0f,
            AnchorTop = 0f,
            AnchorRight = 1f,
            AnchorBottom = 0f,
            OffsetLeft = 0f,
            OffsetTop = 1f,
            OffsetRight = 0f,
            OffsetBottom = 20f,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        ApplyBadgeLabelTheme(badgeLabel, font);
        badge.AddChild(badgeLabel);

        state.BarContainer = bar;
        state.Fill = fill;
        state.ValueLabel = valueLabel;
        state.Badge = badge;
        state.BadgeLabel = badgeLabel;
    }

    private static void ApplyLabelTheme(Label label, Font? font, int size)
    {
        if (font != null)
            label.AddThemeFontOverride("font", font);
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", LabelColor);
        label.AddThemeColorOverride("font_outline_color", OutlineColor);
        label.AddThemeConstantOverride("outline_size", 9);
    }

    private static void ApplyBadgeLabelTheme(Label label, Font? font)
    {
        if (font != null)
            label.AddThemeFontOverride("font", font);
        label.AddThemeFontSizeOverride("font_size", 10);
        label.AddThemeColorOverride("font_color", FillColor);
        label.AddThemeColorOverride("font_outline_color", Colors.White);
        label.AddThemeConstantOverride("outline_size", 1);
    }

    private static void SyncLayout(NHealthBar healthBar, UiState state)
    {
        if (state.BarContainer == null)
            return;

        Control hpBar = healthBar.HpBarContainer;
        state.MaxFillWidth = Math.Max(0f, hpBar.Size.X - ForegroundContainerInset);
    }

    private static void UpdateValues(
        UiState state,
        LibrarySpeedDiceCombatState combatState)
    {
        int value = combatState.Emotion.Value;
        if (state.Fill != null)
        {
            state.Fill.Visible = value > 0;
            if (value > 0)
            {
                float width = Math.Max(
                    MinFillWidth,
                    value / 100f * state.MaxFillWidth);
                state.Fill.OffsetRight = width - state.MaxFillWidth;
            }
        }

        if (state.ValueLabel != null)
            state.ValueLabel.Text = $"{value}/100";
        if (state.Badge != null)
            state.Badge.Visible = combatState.Emotion.Level > 0;
        if (state.BadgeLabel != null)
            state.BadgeLabel.Text = ToRoman(combatState.Emotion.Level);
    }

    private static string ToRoman(int level)
    {
        return level switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            5 => "V",
            _ => "",
        };
    }
}
