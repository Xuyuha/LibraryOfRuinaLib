using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using LibraryLib.Combat.HealthBars;
using LibraryLib.Hooks;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Patches;

internal static class LibraryStatusDamageHealthBar
{
    private const string ContainerName = "LibraryStatusDamageForecastContainer";

    private static readonly string[] ForeignRightContainerNames =
    [
        "RitsuForecastRightContainer",
        "BaseLibForecastRightContainer"
    ];

    private static readonly Color DoomLethalTextColor = new("FB8DFF");

    private static readonly ConditionalWeakTable<NHealthBar, UiState> States = new();

    internal static void RefreshForeground(
        NHealthBar healthBar,
        Creature creature,
        Control hpForeground,
        Control hpForegroundContainer,
        Control poisonForeground,
        Control doomForeground,
        float expectedMaxForegroundWidth)
    {
        if (creature == null || creature.CurrentHp <= 0 || creature.HpDisplay.IsInfinite())
        {
            Hide(healthBar);
            return;
        }

        float maxWidth = GetMaxForegroundWidth(
            hpForegroundContainer,
            expectedMaxForegroundWidth);
        int poisonDamage = creature.GetPower<PoisonPower>()?
            .CalculateTotalDamageNextTurn() ?? 0;
        int remainingHp = Math.Max(0, creature.CurrentHp - poisonDamage);

        if (HasVisibleForeignRightForecast(poisonForeground))
        {
            remainingHp = hpForeground.Visible
                ? Math.Min(
                    remainingHp,
                    HpFromForegroundWidth(
                        creature,
                        hpForeground,
                        maxWidth))
                : 0;
        }

        IReadOnlyList<ForecastSegment> forecasts = CalculateForecasts(
            creature,
            remainingHp);
        if (forecasts.Count == 0 || remainingHp <= 0)
        {
            Hide(healthBar);
            return;
        }

        if (poisonForeground is not NinePatchRect
            || poisonForeground.GetParent() is not Control)
        {
            Hide(healthBar);
            return;
        }

        UiState state = States.GetValue(
            healthBar,
            _ => CreateState(poisonForeground));
        if (!GodotObject.IsInstanceValid(state.Container))
        {
            States.Remove(healthBar);
            state = States.GetValue(
                healthBar,
                _ => CreateState(poisonForeground));
        }

        state.Container.Visible = true;
        EnsureOverlayOrder(
            state.Container,
            poisonForeground,
            hpForeground);

        int segmentIndex = 0;
        Color? lethalColor = null;
        float originalRightEdge = hpForeground.OffsetRight;

        foreach (ForecastSegment forecast in forecasts)
        {
            int visibleAmount = Math.Min(forecast.Amount, remainingHp);
            if (visibleAmount <= 0)
            {
                continue;
            }

            EnsureSegmentCount(state, segmentIndex + 1);
            NinePatchRect segment = state.Segments[segmentIndex++];
            int previousHp = remainingHp;
            remainingHp -= visibleAmount;

            float leftWidth = GetForegroundWidth(
                creature,
                remainingHp,
                maxWidth);
            float rightWidth = GetForegroundWidth(
                creature,
                previousHp,
                maxWidth);

            segment.Visible = true;
            segment.SelfModulate = forecast.Color;
            segment.OffsetLeft = remainingHp > 0
                ? Math.Max(0f, leftWidth - segment.PatchMarginLeft)
                : 0f;
            segment.OffsetRight = rightWidth - maxWidth;

            if (remainingHp <= 0)
            {
                lethalColor = forecast.Color;
            }
        }

        HideSegments(state, segmentIndex);
        if (segmentIndex == 0)
        {
            Hide(healthBar);
            return;
        }

        if (remainingHp > 0)
        {
            hpForeground.Visible = true;
            hpForeground.OffsetRight = GetForegroundWidth(
                creature,
                remainingHp,
                maxWidth) - maxWidth;
        }
        else
        {
            hpForeground.Visible = false;
            doomForeground.Visible = false;
        }

        if (remainingHp > 0 && doomForeground.Visible)
        {
            doomForeground.OffsetRight = Math.Min(
                doomForeground.OffsetRight,
                hpForeground.OffsetRight);
        }

        state.LastRender = new(
            true,
            originalRightEdge,
            lethalColor,
            remainingHp);
    }

    internal static void RefreshMiddleground(
        NHealthBar healthBar,
        Creature creature,
        Control hpMiddleground,
        ref Tween? middlegroundTween)
    {
        if (!States.TryGetValue(healthBar, out UiState? state)
            || !state.LastRender.HasForecast
            || creature.CurrentHp <= 0
            || creature.HpDisplay.IsInfinite())
        {
            return;
        }

        float targetOffsetRight = state.LastRender.OriginalRightEdge;
        bool hpChanged = creature.CurrentHp != state.MiddlegroundHp
            || creature.MaxHp != state.MiddlegroundMaxHp;
        bool targetChanged = state.MiddlegroundTarget is not { } previousTarget
            || !Mathf.IsEqualApprox(previousTarget, targetOffsetRight);
        if (!hpChanged && !targetChanged)
        {
            return;
        }

        state.MiddlegroundHp = creature.CurrentHp;
        state.MiddlegroundMaxHp = creature.MaxHp;
        state.MiddlegroundTarget = targetOffsetRight;

        bool animateImmediately = targetOffsetRight >= hpMiddleground.OffsetRight;
        hpMiddleground.OffsetRight += 1f;
        middlegroundTween?.Kill();
        middlegroundTween = healthBar.CreateTween();
        middlegroundTween
            .TweenProperty(
                hpMiddleground,
                "offset_right",
                targetOffsetRight - 2f,
                1.0)
            .SetDelay(animateImmediately ? 0.0 : 1.0)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
    }

    internal static void SnapMiddleground(
        NHealthBar healthBar,
        Creature creature,
        Control hpMiddleground,
        ref Tween? middlegroundTween)
    {
        if (!States.TryGetValue(healthBar, out UiState? state)
            || !state.LastRender.HasForecast
            || creature.CurrentHp <= 0
            || creature.HpDisplay.IsInfinite())
        {
            return;
        }

        middlegroundTween?.Kill();
        hpMiddleground.OffsetRight = state.LastRender.OriginalRightEdge - 2f;
        state.MiddlegroundHp = creature.CurrentHp;
        state.MiddlegroundMaxHp = creature.MaxHp;
        state.MiddlegroundTarget = state.LastRender.OriginalRightEdge;
    }

    internal static void RefreshText(
        NHealthBar healthBar,
        Creature creature,
        MegaLabel hpLabel)
    {
        if (!States.TryGetValue(healthBar, out UiState? state)
            || !state.LastRender.HasForecast
            || creature.CurrentHp <= 0
            || creature.HpDisplay.IsInfinite())
        {
            return;
        }

        Color? lethalColor = state.LastRender.LethalColor;
        if (!lethalColor.HasValue
            && state.LastRender.RemainingHp > 0
            && creature.GetPowerAmount<DoomPower>()
                >= state.LastRender.RemainingHp)
        {
            lethalColor = DoomLethalTextColor;
        }

        if (!lethalColor.HasValue)
        {
            return;
        }

        hpLabel.AddThemeColorOverride(
            ThemeConstants.Label.FontColor,
            lethalColor.Value);
        hpLabel.AddThemeColorOverride(
            ThemeConstants.Label.FontOutlineColor,
            DarkenForOutline(lethalColor.Value));
    }

    private static IReadOnlyList<ForecastSegment> CalculateForecasts(
        Creature creature,
        int remainingHp)
    {
        if (remainingHp <= 0 || creature.CombatState == null)
        {
            return [];
        }

        int remainingBlock = Math.Max(
            0,
            (creature.PetOwner?.Creature ?? creature).Block);
        var context = new LibraryHealthBarForecastContext(creature);
        List<OrderedDamageForecast> pending = [];
        long sourceSequence = 0;

        foreach (ILibraryHealthBarDamageForecastSource source in
                 creature.Powers.OfType<ILibraryHealthBarDamageForecastSource>())
        {
            try
            {
                int contributionSequence = 0;
                foreach (LibraryHealthBarDamageForecast forecast in
                         source.GetLibraryHealthBarDamageForecasts(context))
                {
                    if (forecast.Damage > 0)
                    {
                        pending.Add(new(
                            forecast,
                            sourceSequence,
                            contributionSequence));
                    }
                    contributionSequence++;
                }
            }
            catch
            {
                // UI forecasting must never interrupt combat if a third-party
                // source cannot produce a contribution for the current state.
            }

            sourceSequence++;
        }

        List<ForecastSegment> result = [];
        foreach (OrderedDamageForecast ordered in pending
                     .OrderBy(entry => entry.Forecast.Order)
                     .ThenBy(entry => entry.SourceSequence)
                     .ThenBy(entry => entry.ContributionSequence))
        {
            if (remainingHp <= 0)
            {
                break;
            }

            int hpLost = CalculateActualHpLoss(
                ordered.Forecast,
                creature,
                ref remainingBlock,
                remainingHp);
            if (hpLost <= 0)
            {
                continue;
            }

            hpLost = Math.Min(hpLost, remainingHp);
            result.Add(new(hpLost, ordered.Forecast.Color));
            remainingHp -= hpLost;
        }

        return result;
    }

    private static int CalculateActualHpLoss(
        LibraryHealthBarDamageForecast forecast,
        Creature originalTarget,
        ref int remainingBlock,
        int remainingHp)
    {
        var combatState = originalTarget.CombatState;
        if (combatState == null)
        {
            return 0;
        }

        IRunState runState = IRunState.GetFrom([originalTarget]);
        ValueProp props = forecast.Props;

        decimal modifiedDamage = Hook.ModifyDamage(
            runState,
            combatState,
            originalTarget,
            forecast.Dealer,
            forecast.Damage,
            props,
            forecast.CardSource,
            forecast.CardPlay,
            ModifyDamageHookType.All,
            CardPreviewMode.None,
            out _);

        decimal blockedDamage = props.HasFlag(ValueProp.Unblockable)
            ? 0m
            : Math.Min(remainingBlock, modifiedDamage);
        remainingBlock = Math.Max(0, remainingBlock - (int)blockedDamage);

        decimal hpLossBeforeRedirect = Hook.ModifyHpLost(
            runState,
            combatState,
            originalTarget,
            Math.Max(modifiedDamage - blockedDamage, 0m),
            props,
            forecast.Dealer,
            forecast.CardSource,
            HpLossHookPhase.BeforeOsty,
            out _);
        Creature hpLossTarget = Hook.ModifyUnblockedDamageTarget(
            combatState,
            originalTarget,
            hpLossBeforeRedirect,
            props,
            forecast.Dealer);

        decimal hpLossAfterRedirect = Hook.ModifyHpLost(
            runState,
            combatState,
            hpLossTarget,
            hpLossBeforeRedirect,
            props,
            forecast.Dealer,
            forecast.CardSource,
            HpLossHookPhase.AfterOsty,
            out _);

        if (ReferenceEquals(hpLossTarget, originalTarget))
        {
            return Math.Min(ToDamageInt(hpLossAfterRedirect), remainingHp);
        }

        int redirectedDamage = ToDamageInt(hpLossAfterRedirect);
        int redirectedOverkill = Math.Max(
            redirectedDamage - Math.Max(0, hpLossTarget.CurrentHp),
            0);
        if (redirectedOverkill <= 0)
        {
            return 0;
        }

        decimal originalTargetHpLoss = Hook.ModifyHpLost(
            runState,
            combatState,
            originalTarget,
            redirectedOverkill,
            props,
            forecast.Dealer,
            forecast.CardSource,
            HpLossHookPhase.AfterOsty,
            out _);
        return Math.Min(ToDamageInt(originalTargetHpLoss), remainingHp);
    }

    private static int ToDamageInt(decimal amount) =>
        (int)Math.Clamp(amount, 0m, 999999999m);

    private static UiState CreateState(Control poisonForeground)
    {
        var poisonTemplate = (NinePatchRect)poisonForeground;
        var mask = (Control)poisonForeground.GetParent();

        var container = new Control
        {
            Name = ContainerName,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        container.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        mask.AddChild(container);

        var template = (NinePatchRect)poisonTemplate.Duplicate();
        template.Name = "LibraryStatusDamageForecastTemplate";
        template.Visible = false;
        template.Modulate = Colors.White;
        template.SelfModulate = Colors.White;
        template.Material = null;
        template.ZIndex = 0;
        template.MouseFilter = Control.MouseFilterEnum.Ignore;
        template.OffsetLeft = 0f;
        template.OffsetRight = 0f;
        container.AddChild(template);

        return new(container, template);
    }

    private static void EnsureSegmentCount(UiState state, int requiredCount)
    {
        while (state.Segments.Count < requiredCount)
        {
            var segment = (NinePatchRect)state.Template.Duplicate();
            segment.Name = $"LibraryStatusDamageForecastSegment{state.Segments.Count}";
            segment.Visible = false;
            state.Container.AddChild(segment);
            state.Segments.Add(segment);
        }
    }

    private static void EnsureOverlayOrder(
        Control container,
        Control poisonForeground,
        Control hpForeground)
    {
        if (container.GetParent() is not Control mask
            || poisonForeground.GetParent() != mask
            || hpForeground.GetParent() != mask)
        {
            return;
        }

        if (poisonForeground.GetIndex() < hpForeground.GetIndex())
        {
            MoveChildAfter(mask, container, poisonForeground);
        }
        else
        {
            MoveChildBefore(mask, container, hpForeground);
        }
    }

    private static void MoveChildAfter(
        Control parent,
        Control node,
        Control anchor)
    {
        int nodeIndex = node.GetIndex();
        int anchorIndex = anchor.GetIndex();
        int targetIndex = nodeIndex > anchorIndex
            ? anchorIndex + 1
            : anchorIndex;
        if (nodeIndex != targetIndex)
        {
            parent.MoveChild(node, targetIndex);
        }
    }

    private static void MoveChildBefore(
        Control parent,
        Control node,
        Control anchor)
    {
        int nodeIndex = node.GetIndex();
        int anchorIndex = anchor.GetIndex();
        int targetIndex = nodeIndex > anchorIndex
            ? anchorIndex
            : Math.Max(0, anchorIndex - 1);
        if (nodeIndex != targetIndex)
        {
            parent.MoveChild(node, targetIndex);
        }
    }

    private static bool HasVisibleForeignRightForecast(Control poisonForeground)
    {
        Node? mask = poisonForeground.GetParent();
        if (mask == null)
        {
            return false;
        }

        foreach (string name in ForeignRightContainerNames)
        {
            if (mask.GetNodeOrNull<Control>(name) is not { Visible: true } container)
            {
                continue;
            }

            foreach (Node child in container.GetChildren())
            {
                if (child is CanvasItem { Visible: true })
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static int HpFromForegroundWidth(
        Creature creature,
        Control hpForeground,
        float maxWidth)
    {
        if (maxWidth <= 0f || creature.MaxHp <= 0)
        {
            return 0;
        }

        float visibleWidth = Math.Clamp(
            hpForeground.OffsetRight + maxWidth,
            0f,
            maxWidth);
        return Math.Max(
            0,
            (int)MathF.Round(
                visibleWidth / maxWidth * creature.MaxHp,
                MidpointRounding.AwayFromZero));
    }

    private static float GetMaxForegroundWidth(
        Control hpForegroundContainer,
        float expectedMaxForegroundWidth) =>
        expectedMaxForegroundWidth > 0f
            ? expectedMaxForegroundWidth
            : hpForegroundContainer.Size.X;

    private static float GetForegroundWidth(
        Creature creature,
        int amount,
        float maxWidth)
    {
        if (creature.MaxHp <= 0 || amount <= 0)
        {
            return 0f;
        }

        float width = (float)amount / creature.MaxHp * maxWidth;
        return Math.Max(width, creature.CurrentHp > 0 ? 12f : 0f);
    }

    private static Color DarkenForOutline(Color color) => new(
        Math.Clamp(color.R * 0.3f, 0f, 1f),
        Math.Clamp(color.G * 0.3f, 0f, 1f),
        Math.Clamp(color.B * 0.3f, 0f, 1f));

    private static void Hide(NHealthBar healthBar)
    {
        if (!States.TryGetValue(healthBar, out UiState? state))
        {
            return;
        }

        state.Container.Visible = false;
        HideSegments(state, 0);
        state.LastRender = RenderResult.Empty;
        state.MiddlegroundTarget = null;
    }

    private static void HideSegments(UiState state, int startIndex)
    {
        for (int i = startIndex; i < state.Segments.Count; i++)
        {
            state.Segments[i].Visible = false;
            state.Segments[i].SelfModulate = Colors.White;
        }
    }

    private readonly record struct ForecastSegment(int Amount, Color Color);

    private readonly record struct OrderedDamageForecast(
        LibraryHealthBarDamageForecast Forecast,
        long SourceSequence,
        int ContributionSequence);

    private readonly record struct RenderResult(
        bool HasForecast,
        float OriginalRightEdge,
        Color? LethalColor,
        int RemainingHp)
    {
        internal static RenderResult Empty => new(false, 0f, null, 0);
    }

    private sealed class UiState(Control container, NinePatchRect template)
    {
        internal Control Container { get; } = container;
        internal NinePatchRect Template { get; } = template;
        internal List<NinePatchRect> Segments { get; } = [];
        internal RenderResult LastRender { get; set; } = RenderResult.Empty;
        internal float? MiddlegroundTarget { get; set; }
        internal int MiddlegroundHp { get; set; } = -1;
        internal int MiddlegroundMaxHp { get; set; } = -1;
    }
}

[HarmonyPatch(typeof(NHealthBar), "RefreshForeground")]
[HarmonyAfter("BaseLib", "com.ritsukage.sts2-RitsuLib.framework-core")]
[HarmonyBefore("FYY.LibraryOfRuina")]
[HarmonyPriority(Priority.Last)]
internal static class LibraryStatusDamageHealthBarForegroundPatch
{
    private static void Postfix(
        NHealthBar __instance,
        Creature ____creature,
        Control ____hpForeground,
        Control ____hpForegroundContainer,
        Control ____poisonForeground,
        Control ____doomForeground,
        float ____expectedMaxFgWidth)
    {
        LibraryStatusDamageHealthBar.RefreshForeground(
            __instance,
            ____creature,
            ____hpForeground,
            ____hpForegroundContainer,
            ____poisonForeground,
            ____doomForeground,
            ____expectedMaxFgWidth);
    }
}

[HarmonyPatch(typeof(NHealthBar), "RefreshMiddleground")]
[HarmonyAfter("BaseLib", "com.ritsukage.sts2-RitsuLib.framework-core")]
[HarmonyBefore("FYY.LibraryOfRuina")]
[HarmonyPriority(Priority.Last)]
internal static class LibraryStatusDamageHealthBarMiddlegroundPatch
{
    private static void Postfix(
        NHealthBar __instance,
        Creature ____creature,
        Control ____hpMiddleground,
        ref Tween? ____middlegroundTween)
    {
        LibraryStatusDamageHealthBar.RefreshMiddleground(
            __instance,
            ____creature,
            ____hpMiddleground,
            ref ____middlegroundTween);
    }
}

[HarmonyPatch(typeof(NHealthBar), "RefreshText")]
[HarmonyAfter("BaseLib", "com.ritsukage.sts2-RitsuLib.framework-core")]
[HarmonyBefore("FYY.LibraryOfRuina")]
[HarmonyPriority(Priority.Last)]
internal static class LibraryStatusDamageHealthBarTextPatch
{
    private static void Postfix(
        NHealthBar __instance,
        Creature ____creature,
        MegaLabel ____hpLabel)
    {
        LibraryStatusDamageHealthBar.RefreshText(
            __instance,
            ____creature,
            ____hpLabel);
    }
}

[HarmonyPatch(typeof(NHealthBar), "SetHpBarContainerSizeWithOffsetsImmediately")]
[HarmonyAfter("BaseLib", "com.ritsukage.sts2-RitsuLib.framework-core")]
[HarmonyBefore("FYY.LibraryOfRuina")]
[HarmonyPriority(Priority.Last)]
internal static class LibraryStatusDamageHealthBarResizePatch
{
    private static void Postfix(
        NHealthBar __instance,
        Creature ____creature,
        Control ____hpForeground,
        Control ____hpForegroundContainer,
        Control ____poisonForeground,
        Control ____doomForeground,
        Control ____hpMiddleground,
        float ____expectedMaxFgWidth,
        ref Tween? ____middlegroundTween)
    {
        if (____creature == null)
        {
            return;
        }

        LibraryStatusDamageHealthBar.RefreshForeground(
            __instance,
            ____creature,
            ____hpForeground,
            ____hpForegroundContainer,
            ____poisonForeground,
            ____doomForeground,
            ____expectedMaxFgWidth);
        LibraryStatusDamageHealthBar.SnapMiddleground(
            __instance,
            ____creature,
            ____hpMiddleground,
            ref ____middlegroundTween);
    }
}
