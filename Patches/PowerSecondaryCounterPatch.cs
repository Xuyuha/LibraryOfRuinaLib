using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using Library.Models;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Library.Patches;

/// <summary>
///     为实现了 <see cref="ISecondaryDisplayAmountPower"/> 的能力，
///     管理 NPower 节点上次要数值标签的创建与刷新。
/// </summary>
internal static class PowerSecondaryCounterUi
{
    private const string SecondaryAmountLabelName = "LibrarySecondaryAmountLabel";
    private const string FallbackLabelFontPath = "res://themes/kreon_bold_glyph_space_one.tres";

    private static readonly StringName ThemeFontName = "font";
    private static readonly StringName ThemeFontSizeName = "font_size";
    private static readonly StringName ThemeFontColorName = "font_color";
    private static readonly StringName ThemeFontOutlineColorName = "font_outline_color";
    private static readonly StringName ThemeFontShadowColorName = "font_shadow_color";

    private static readonly FieldInfo? ModelField = AccessTools.Field(typeof(NPower), "_model");
    private static readonly FieldInfo? ActiveHoverTipsField =
        typeof(NHoverTipSet).GetField("_activeHoverTips", BindingFlags.Static | BindingFlags.NonPublic);

    public static void EnsureAndRefresh(NPower powerNode)
    {
        EnsureSecondaryLabel(powerNode);
        SyncPowerNodeVisibility(powerNode);
        RefreshSecondaryLabel(powerNode);
        RefreshHoveredCreaturePowerTips(powerNode);
    }

    public static void RefreshSecondaryLabel(NPower powerNode)
    {
        MegaLabel? label = GetSecondaryLabel(powerNode);
        if (label == null)
            return;

        PowerModel? model = GetModel(powerNode);
        if (model is not ISecondaryDisplayAmountPower secondaryPower
            || !secondaryPower.ShowSecondaryDisplayAmount
            || !model.IsVisible)
        {
            label.Visible = false;
            label.SetTextAutoSize(string.Empty);
            return;
        }

        label.Visible = true;
        label.AddThemeColorOverride(
            ThemeFontColorName,
            secondaryPower.SecondaryDisplayAmountLabelColor);
        label.SetTextAutoSize(secondaryPower.SecondaryDisplayAmount.ToString());
    }

    public static void SyncPowerNodeVisibility(NPower powerNode)
    {
        PowerModel? model = GetModel(powerNode);
        bool isVisible = model?.IsVisible ?? false;

        powerNode.Visible = isVisible;
        powerNode.MouseFilter = isVisible
            ? Control.MouseFilterEnum.Stop
            : Control.MouseFilterEnum.Ignore;

        if (!isVisible && model?.Owner != null)
            NCombatRoom.Instance?.GetCreatureNode(model.Owner)?.HideHoverTips();
    }

    private static MegaLabel? EnsureSecondaryLabel(NPower powerNode)
    {
        MegaLabel? existing = GetSecondaryLabel(powerNode);
        if (existing != null)
            return existing;

        MegaLabel? amountLabel = powerNode.GetNodeOrNull<MegaLabel>("%AmountLabel");
        MegaLabel label = amountLabel?.Duplicate() as MegaLabel ?? CreateFallbackLabel();
        EnsureThemeFontOverride(label, amountLabel);
        EnsureFallbackThemeFontOverride(label);
        label.Name = SecondaryAmountLabelName;
        label.UniqueNameInOwner = false;
        label.Visible = false;
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.AutoSizeEnabled = false;
        label.OffsetLeft = -56f;
        label.OffsetTop = -4f;
        label.OffsetRight = 44f;
        label.OffsetBottom = 19f;
        label.HorizontalAlignment = HorizontalAlignment.Right;
        label.VerticalAlignment = VerticalAlignment.Top;
        label.SetTextAutoSize(string.Empty);

        powerNode.AddChild(label);
        label.Owner = powerNode;
        return label;
    }

    private static MegaLabel CreateFallbackLabel()
    {
        MegaLabel label = new MegaLabel();
        EnsureFallbackThemeFontOverride(label);

        label.AddThemeFontSizeOverride(ThemeFontSizeName, 18);
        label.AddThemeColorOverride(
            ThemeFontOutlineColorName,
            new Color(0.12f, 0.10208f, 0.0816f, 1f));
        label.AddThemeColorOverride(
            ThemeFontShadowColorName,
            new Color(0f, 0f, 0f, 0.1882353f));
        label.AddThemeConstantOverride("shadow_offset_x", 3);
        label.AddThemeConstantOverride("shadow_offset_y", 3);
        label.AddThemeConstantOverride("outline_size", 10);
        return label;
    }

    private static void EnsureThemeFontOverride(MegaLabel targetLabel, MegaLabel? sourceLabel)
    {
        if (targetLabel.HasThemeFontOverride(ThemeFontName))
            return;

        Font? font = null;
        if (sourceLabel != null)
            font = sourceLabel.GetThemeFont(ThemeFontName, "Label");

        font ??= targetLabel.GetThemeFont(ThemeFontName, "Label");

        if (font != null)
            targetLabel.AddThemeFontOverride(ThemeFontName, font);
    }

    private static void EnsureFallbackThemeFontOverride(MegaLabel label)
    {
        if (label.HasThemeFontOverride(ThemeFontName))
            return;

        Font? fallbackFont = label.GetThemeFont(ThemeFontName, "Label")
                             ?? ResourceLoader.Load<Font>(FallbackLabelFontPath);

        if (fallbackFont != null)
            label.AddThemeFontOverride(ThemeFontName, fallbackFont);
    }

    private static PowerModel? GetModel(NPower powerNode)
    {
        return ModelField?.GetValue(powerNode) as PowerModel;
    }

    private static void RefreshHoveredCreaturePowerTips(NPower powerNode)
    {
        PowerModel? model = GetModel(powerNode);
        if (model?.Owner == null)
            return;

        NCreature? creatureNode = NCombatRoom.Instance?.GetCreatureNode(model.Owner);
        Control? hitbox = creatureNode?.Hitbox;
        if (hitbox == null || !HasActiveHoverTips(hitbox))
            return;

        creatureNode!.ShowHoverTips(model.Owner.HoverTips);
    }

    private static bool HasActiveHoverTips(Control owner)
    {
        object? activeObj = ActiveHoverTipsField?.GetValue(null);
        if (activeObj is IDictionary<Control, NHoverTipSet> activeTyped)
            return activeTyped.ContainsKey(owner);

        return activeObj is IDictionary active && active.Contains(owner);
    }

    private static MegaLabel? GetSecondaryLabel(NPower powerNode)
    {
        return powerNode.GetNodeOrNull<MegaLabel>(SecondaryAmountLabelName);
    }
}

/// <summary>
///     使 NPower.Model 的 getter 在模型尚未设置时安全调用。
///     返回 null 而不是抛出 InvalidOperationException。
/// </summary>
[HarmonyPatch(typeof(NPower), "get_Model")]
internal static class NPowerModelSafeGetterPatch
{
    private static readonly FieldInfo? ModelBackingField = AccessTools.Field(typeof(NPower), "_model");

    [HarmonyPrefix]
    public static bool Prefix(NPower __instance, ref PowerModel? __result)
    {
        __result = ModelBackingField?.GetValue(__instance) as PowerModel;
        return false;
    }
}

[HarmonyPatch(typeof(NPower), nameof(NPower._Ready))]
internal static class PowerSecondaryCounterReadyPatch
{
    [HarmonyPostfix]
    public static void Postfix(NPower __instance)
    {
        try
        {
            PowerSecondaryCounterUi.EnsureAndRefresh(__instance);
        }
        catch (System.Exception ex)
        {
            GD.PushError($"[LibraryOfRuinaLib] PowerSecondaryCounterReadyPatch failed: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(NPower), "RefreshAmount")]
internal static class PowerSecondaryCounterRefreshPatch
{
    [HarmonyPostfix]
    public static void Postfix(NPower __instance)
    {
        try
        {
            PowerSecondaryCounterUi.EnsureAndRefresh(__instance);
        }
        catch (System.Exception ex)
        {
            GD.PushError($"[LibraryOfRuinaLib] PowerSecondaryCounterRefreshPatch failed: {ex.Message}");
        }
    }
}
