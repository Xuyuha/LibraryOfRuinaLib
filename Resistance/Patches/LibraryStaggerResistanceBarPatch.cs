#nullable enable
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using Library.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Library.Resistance.Patches;

/// <summary>
///     在 NHealthBar 下方渲染混乱抗性条（黄色数值条），显示当前混乱抗性值/最大值。
///     晕眩状态下填充色变为红色。
/// </summary>
internal static class LibraryStaggerResistanceBarUi
{
    private const string ContainerName = "LibraryOfRuinaStaggerBarContainer";

    private const float BarHeight = 14f;
    private const float BarGap = 2f;
    private const float ForegroundInset = 5f;
    private const float ForegroundInsetVertical = 3f;
    private const float ForegroundContainerInset = 10f;
    private const float MinFillWidth = 12f;

    private static readonly Color BgModulate = new(0.45f, 0.35f, 0.08f, 0.75f);
    private static readonly Color FillColor = new(0.90f, 0.75f, 0.10f, 1.0f);
    private static readonly Color StunnedFillColor = new(0.50f, 0.12f, 0.12f, 1.0f);

    private static readonly Color LabelFontColor = new(1.0f, 0.965f, 0.886f, 1.0f);
    private static readonly Color LabelOutlineColor = new(0.40f, 0.30f, 0.0f, 1.0f);
    private static readonly Color LabelShadowColor = new(0.0f, 0.0f, 0.0f, 0.25f);
    private static readonly Color StunnedLabelOutlineColor = new(0.50f, 0.0f, 0.0f, 1.0f);

    private const string FontPath = "res://themes/kreon_bold_glyph_space_one.tres";

    private static readonly FieldInfo? CreatureField =
        AccessTools.Field(typeof(NHealthBar), "_creature");

    private sealed class State
    {
        public Control? BarContainer;
        public NinePatchRect? Background;
        public Control? FgContainer;
        public NinePatchRect? Mask;
        public NinePatchRect? Fill;
        public Label? ValueLabel;
        public float MaxFgWidth;
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

        bool shouldShow = libCreature != null && creature.IsAlive && libCreature.MaxChaoValue > 0;

        if (!shouldShow)
        {
            if (state.BarContainer != null)
                state.BarContainer.Visible = false;
            return;
        }

        if (state.BarContainer == null)
            CreateBarNodes(healthBar, state);

        if (state.BarContainer == null) return;

        state.BarContainer.Visible = true;
        SyncLayout(healthBar, state);
        UpdateFill(libCreature!, state);
        UpdateLabel(libCreature!, state);
    }

    private static void CreateBarNodes(NHealthBar healthBar, State state)
    {
        Control hpBarContainer = healthBar.HpBarContainer;
        if (hpBarContainer == null) return;

        Node healthBarNode = hpBarContainer.GetParent();
        if (healthBarNode == null) return;

        NinePatchRect? srcBg = hpBarContainer.GetNodeOrNull<NinePatchRect>("HpBackground");
        NinePatchRect? srcMask = hpBarContainer.GetNodeOrNull<NinePatchRect>(
            "HpForegroundContainer/Mask");
        NinePatchRect? srcFill = hpBarContainer.GetNodeOrNull<NinePatchRect>(
            "HpForegroundContainer/Mask/HpForeground");

        if (srcBg == null || srcMask == null || srcFill == null) return;

        var barContainer = new Control
        {
            Name = ContainerName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        healthBarNode.AddChild(barContainer);

        var background = (NinePatchRect)srcBg.Duplicate(15);
        background.Name = "StaggerBackground";
        background.Modulate = BgModulate;
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
        barContainer.AddChild(background);

        var fgContainer = new Control
        {
            Name = "StaggerForegroundContainer",
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
        barContainer.AddChild(fgContainer);

        var mask = (NinePatchRect)srcMask.Duplicate(15);
        mask.Name = "StaggerMask";
        mask.ClipChildren = CanvasItem.ClipChildrenMode.Only;
        mask.Visible = true;
        mask.MouseFilter = Control.MouseFilterEnum.Ignore;
        mask.AnchorLeft = 0f;
        mask.AnchorTop = 0f;
        mask.AnchorRight = 1f;
        mask.AnchorBottom = 1f;
        mask.OffsetLeft = 0f;
        mask.OffsetTop = 0f;
        mask.OffsetRight = 0f;
        mask.OffsetBottom = 0f;
        foreach (Node child in mask.GetChildren())
            child.QueueFree();
        fgContainer.AddChild(mask);

        var fill = (NinePatchRect)srcFill.Duplicate(15);
        fill.Name = "StaggerFill";
        fill.SelfModulate = FillColor;
        fill.Visible = true;
        fill.MouseFilter = Control.MouseFilterEnum.Ignore;
        fill.Material = null;
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
        mask.AddChild(fill);

        var label = new Label
        {
            Name = "StaggerValueLabel",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AnchorLeft = 0f,
            AnchorTop = 0f,
            AnchorRight = 1f,
            AnchorBottom = 1f,
            OffsetLeft = 0f,
            OffsetTop = -6f,
            OffsetRight = 0f,
            OffsetBottom = 7f,
            Text = "",
        };

        Font? font = ResourceLoader.Load<Font>(FontPath);
        if (font != null)
            label.AddThemeFontOverride("font", font);
        label.AddThemeFontSizeOverride("font_size", 20);
        label.AddThemeColorOverride("font_color", LabelFontColor);
        label.AddThemeColorOverride("font_outline_color", LabelOutlineColor);
        label.AddThemeColorOverride("font_shadow_color", LabelShadowColor);
        label.AddThemeConstantOverride("outline_size", 12);
        label.AddThemeConstantOverride("shadow_offset_x", 4);
        label.AddThemeConstantOverride("shadow_offset_y", 3);
        label.AddThemeConstantOverride("shadow_outline_size", 0);
        barContainer.AddChild(label);

        state.BarContainer = barContainer;
        state.Background = background;
        state.FgContainer = fgContainer;
        state.Mask = mask;
        state.Fill = fill;
        state.ValueLabel = label;
    }

    private static void SyncLayout(NHealthBar healthBar, State state)
    {
        if (state.BarContainer == null) return;

        Control hpBarContainer = healthBar.HpBarContainer;
        float barWidth = hpBarContainer.Size.X;

        state.BarContainer.Position = new Vector2(
            hpBarContainer.Position.X,
            hpBarContainer.Position.Y - BarHeight - BarGap
        );
        state.BarContainer.Size = new Vector2(barWidth, BarHeight);

        state.MaxFgWidth = barWidth - ForegroundContainerInset;
    }

    private static void UpdateFill(
        LibraryCreature creature,
        State state)
    {
        if (state.Fill == null) return;

        int currentAmount = creature.CurrentChaoValue;
        int maxResistance = creature.MaxChaoValue;
        if (maxResistance <= 0) maxResistance = Math.Max(1, currentAmount);

        bool isStunned = creature.IsStunPending;
        float maxFgWidth = state.MaxFgWidth;

        if (maxFgWidth <= 0f || maxResistance <= 0)
        {
            state.Fill.Visible = false;
            return;
        }

        state.Fill.Visible = currentAmount > 0;

        if (currentAmount > 0)
        {
            float fillWidth = (float)currentAmount / maxResistance * maxFgWidth;
            fillWidth = Math.Max(fillWidth, MinFillWidth);
            state.Fill.OffsetRight = fillWidth - maxFgWidth;
        }

        state.Fill.SelfModulate = isStunned ? StunnedFillColor : FillColor;
    }

    private static void UpdateLabel(
        LibraryCreature creature,
        State state)
    {
        if (state.ValueLabel == null) return;

        int currentAmount = creature.CurrentChaoValue;
        int maxResistance = creature.MaxChaoValue;
        if (maxResistance <= 0) maxResistance = Math.Max(1, currentAmount);

        bool isStunned = creature.IsStunPending;

        state.ValueLabel.Text = $"{currentAmount}/{maxResistance}";

        state.ValueLabel.AddThemeColorOverride("font_outline_color",
            isStunned ? StunnedLabelOutlineColor : LabelOutlineColor);
    }
}

/// <summary>
///     NHealthBar.RefreshForeground 后刷新混乱抗性条。
/// </summary>
[HarmonyPatch(typeof(NHealthBar), "RefreshForeground")]
internal static class LibraryStaggerResistanceBarRefreshPatch
{
    private static void Postfix(NHealthBar __instance)
    {
        try
        {
            LibraryStaggerResistanceBarUi.Refresh(__instance);
        }
        catch (Exception)
        {
        }
    }
}
