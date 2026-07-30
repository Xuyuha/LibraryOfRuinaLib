using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Cards;
using System.Text.RegularExpressions;

namespace Library.Patches;

[HarmonyPatch(
    typeof(NCard),
    nameof(NCard.UpdateVisuals),
    typeof(PileType),
    typeof(CardPreviewMode))]
internal static class LibraryDiceCardDescriptionLayoutPatch
{
    private const string DiceLabelNodeName =
        "LibraryDiceDescriptionLabel";
    private const string CenterOpen = "[center]";
    private const string CenterClose = "[/center]";
    private const char InlineDiceTimingSeparator = '\u001f';
    private const char DiceRowSeparator = '\u2028';
    private const int MaximumDiceFontSize = 24;
    private const int MinimumDiceFontSize = 12;
    private const int DefaultBodyFontSize = 21;
    private const int DiceIconSizeOffset = 1;
    private const float MaximumDiceBodyGap = 8f;
    private const float MinimumDiceBodyGap = 4f;
    private const float LayoutHeightTolerance = 1f;
    private const int LayoutMeasurementFrameCount = 2;

    private static readonly Regex DiceImageTagPattern = new(
        @"\[img(?:=\d+x\d+)?\]"
        + @"(?<path>res://[^\[]*/images/dice/[^\[]+)"
        + @"\[/img\]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex DiceTimingLinePattern = new(
        @"^\s*\[gold\](?:命中时|拼点开始|On Hit|On Clash Start)"
        + @"\s*[：:]?\[/gold\]",
        RegexOptions.Compiled
        | RegexOptions.CultureInvariant
        | RegexOptions.IgnoreCase);

    private static readonly Dictionary<ulong, long> FitRevisions = [];

    private static bool _loggedFailure;
    private static long _nextFitRevision;

    public static void Postfix(NCard __instance)
    {
        try
        {
            Apply(__instance);
        }
        catch (Exception exception)
        {
            if (_loggedFailure)
                return;

            _loggedFailure = true;
            Log.Warn(
                "[LibraryOfRuinaLib.CardDescription] "
                + $"Failed to apply dice card description layout: {exception}");
        }
    }

    private static void Apply(NCard card)
    {
        if (!card.IsNodeReady())
            return;

        MegaRichTextLabel? bodyLabel =
            card.GetNodeOrNull<MegaRichTextLabel>("%DescriptionLabel");
        MegaRichTextLabel? diceLabel =
            card.GetNodeOrNull<MegaRichTextLabel>(
                $"CardContainer/{DiceLabelNodeName}");
        if (bodyLabel == null)
            return;

        if (card.Visibility != ModelVisibility.Visible)
        {
            CancelDeferredFit(bodyLabel);
            if (diceLabel != null)
                RestoreDefaultLayout(bodyLabel, diceLabel);
            return;
        }

        string text = UnwrapCenteredText(bodyLabel.Text);
        string[] lines = MergeDiceTimingLines(
            SplitInlineDiceLines(
                text
                    .Replace("\r\n", "\n", StringComparison.Ordinal)
                    .Split('\n')));

        if (!lines.Any(IsDiceLine))
        {
            CancelDeferredFit(bodyLabel);
            if (diceLabel == null)
                return;

            RestoreBodyLayout(bodyLabel, diceLabel);
            HideDiceLabel(diceLabel);
            RestoreBodyAutoSize(bodyLabel);
            bodyLabel.Visible = true;
            bodyLabel.VerticalAlignment = VerticalAlignment.Center;
            bodyLabel.SetTextAutoSize(Center(text));
            return;
        }

        int firstDiceLine = Array.FindIndex(lines, IsDiceLine);
        int bodyStart = firstDiceLine;
        while (bodyStart < lines.Length
               && IsDiceLine(lines[bodyStart]))
        {
            bodyStart++;
        }

        while (bodyStart < lines.Length
               && string.IsNullOrWhiteSpace(lines[bodyStart]))
        {
            bodyStart++;
        }

        diceLabel ??= EnsureDiceLabel(bodyLabel);
        if (diceLabel == null)
            return;

        string[] keywordLines = lines
            .Take(firstDiceLine)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        string[] diceLines = lines
            .Skip(firstDiceLine)
            .Take(bodyStart - firstDiceLine)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        string[] bodyLines = lines.Skip(bodyStart).ToArray();
        ApplyAdaptiveLayout(
            bodyLabel,
            diceLabel,
            keywordLines,
            diceLines,
            bodyLines);
        ScheduleDeferredFit(
            bodyLabel,
            diceLabel,
            keywordLines,
            diceLines,
            bodyLines);
    }

    private static MegaRichTextLabel? EnsureDiceLabel(
        MegaRichTextLabel bodyLabel)
    {
        if (bodyLabel.GetParent() is not Control parent
            || bodyLabel.Duplicate() is not MegaRichTextLabel diceLabel)
        {
            return null;
        }

        diceLabel.Name = DiceLabelNodeName;
        diceLabel.UniqueNameInOwner = false;
        diceLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        diceLabel.VerticalAlignment = VerticalAlignment.Top;
        diceLabel.MaxFontSize = MaximumDiceFontSize;
        diceLabel.SetTextAutoSize(string.Empty);
        parent.AddChild(diceLabel);
        parent.MoveChild(
            diceLabel,
            Math.Min(bodyLabel.GetIndex() + 1, parent.GetChildCount() - 1));
        return diceLabel;
    }

    private static bool IsDiceLine(string line)
    {
        string trimmed = line.TrimStart();
        Match match = DiceImageTagPattern.Match(trimmed);
        return match.Success && match.Index == 0;
    }

    private static IEnumerable<string> SplitInlineDiceLines(
        IEnumerable<string> lines)
    {
        foreach (string line in lines)
        {
            Match diceImage = DiceImageTagPattern.Match(line);
            if (!diceImage.Success
                || string.IsNullOrWhiteSpace(line[..diceImage.Index]))
            {
                yield return line;
                continue;
            }

            yield return line[..diceImage.Index].TrimEnd();
            yield return line[diceImage.Index..].TrimStart();
        }
    }

    private static string[] MergeDiceTimingLines(
        IEnumerable<string> lines)
    {
        List<string> mergedLines = [];
        foreach (string line in lines)
        {
            if (DiceTimingLinePattern.IsMatch(line)
                && mergedLines.Count > 0
                && IsDiceLine(mergedLines[^1])
                && !mergedLines[^1].Contains(
                    InlineDiceTimingSeparator))
            {
                mergedLines[^1] +=
                    $"{InlineDiceTimingSeparator}{line.Trim()}";
                continue;
            }

            mergedLines.Add(line);
        }

        return [..mergedLines];
    }

    private static string UnwrapCenteredText(string text)
    {
        if (!text.StartsWith(CenterOpen, StringComparison.Ordinal)
            || !text.EndsWith(CenterClose, StringComparison.Ordinal))
        {
            return text;
        }

        return text[
            CenterOpen.Length
            ..^CenterClose.Length];
    }

    private static string Center(string text)
    {
        return $"{CenterOpen}{text}{CenterClose}";
    }

    private static void ApplyAdaptiveLayout(
        MegaRichTextLabel bodyLabel,
        MegaRichTextLabel? diceLabel,
        IReadOnlyList<string> keywordLines,
        IReadOnlyList<string> diceLines,
        IReadOnlyList<string> bodyLines)
    {
        if (diceLabel != null)
            RestoreBodyLayout(bodyLabel, diceLabel);

        float fullTop = bodyLabel.OffsetTop;
        float fullHeight = Math.Max(
            0f,
            bodyLabel.OffsetBottom - bodyLabel.OffsetTop);

        bodyLabel.AutoSizeEnabled = false;
        bodyLabel.Visible = bodyLines.Count > 0;
        bodyLabel.VerticalAlignment = VerticalAlignment.Top;

        if (diceLabel != null)
        {
            diceLabel.AutoSizeEnabled = false;
            diceLabel.Visible =
                keywordLines.Count > 0 || diceLines.Count > 0;
            diceLabel.VerticalAlignment = VerticalAlignment.Top;
        }

        float selectedTopHeight = 0f;
        float selectedGap = 0f;

        for (int diceFontSize = MaximumDiceFontSize;
             diceFontSize >= MinimumDiceFontSize;
             diceFontSize--)
        {
            int bodyFontSize = Math.Clamp(
                diceFontSize - 3,
                MinimumDiceFontSize,
                DefaultBodyFontSize);
            int iconSize = diceFontSize + DiceIconSizeOffset;

            if (diceLabel != null)
            {
                diceLabel.SetTextAutoSize(
                    FormatTopLines(
                        keywordLines,
                        diceLines,
                        diceFontSize,
                        bodyFontSize,
                        iconSize));
            }

            bodyLabel.SetTextAutoSize(
                FormatDescriptionLines(
                    bodyLines,
                    diceFontSize,
                    bodyFontSize,
                    iconSize));

            float topHeight = diceLabel != null
                && (keywordLines.Count > 0 || diceLines.Count > 0)
                ? diceLabel.GetContentHeight()
                : 0f;
            float bodyHeight = bodyLines.Count > 0
                ? bodyLabel.GetContentHeight()
                : 0f;
            float gap = topHeight > 0f && bodyHeight > 0f
                ? GetDiceBodyGap(diceFontSize)
                : 0f;

            selectedTopHeight = topHeight;
            selectedGap = gap;

            bool measurementReady =
                ((keywordLines.Count == 0
                  && diceLines.Count == 0)
                 || topHeight > 0f)
                && (bodyLines.Count == 0 || bodyHeight > 0f);
            if (measurementReady
                && topHeight + gap + bodyHeight
                    <= fullHeight + LayoutHeightTolerance)
            {
                break;
            }
        }

        if (bodyLines.Count > 0)
        {
            bodyLabel.OffsetTop =
                fullTop + selectedTopHeight + selectedGap;
        }
        else
        {
            bodyLabel.Visible = false;
            bodyLabel.SetTextAutoSize(string.Empty);
        }
    }

    private static string FormatTopLines(
        IEnumerable<string> keywordLines,
        IEnumerable<string> diceLines,
        int diceFontSize,
        int bodyFontSize,
        int iconSize)
    {
        List<string> sections = [];
        string keywordText = string.Join(
            ' ',
            keywordLines.Select(line => line.Trim()));
        if (!string.IsNullOrWhiteSpace(keywordText))
        {
            sections.Add(
                $"[center][font_size={bodyFontSize}]"
                + $"{keywordText}[/font_size][/center]");
        }

        string diceText = FormatDiceRows(
            diceLines,
            diceFontSize,
            bodyFontSize,
            iconSize);
        if (!string.IsNullOrWhiteSpace(diceText))
        {
            sections.Add($"[left]{diceText}[/left]");
        }

        return string.Join('\n', sections);
    }

    private static string FormatDescriptionLines(
        IEnumerable<string> lines,
        int diceFontSize,
        int bodyFontSize,
        int iconSize)
    {
        List<string> formattedSections = [];
        List<string> sectionLines = [];
        bool? sectionIsDice = null;

        void FlushSection()
        {
            if (sectionLines.Count == 0
                || sectionIsDice == null)
            {
                return;
            }

            string alignment = sectionIsDice.Value
                ? "left"
                : "center";
            string sectionText = sectionIsDice.Value
                ? FormatDiceRows(
                    sectionLines,
                    diceFontSize,
                    bodyFontSize,
                    iconSize)
                : string.Join('\n', sectionLines);

            formattedSections.Add(sectionIsDice.Value
                ? $"[{alignment}]{sectionText}[/{alignment}]"
                : $"[{alignment}][font_size={bodyFontSize}]"
                  + $"{sectionText}[/font_size][/{alignment}]");
            sectionLines.Clear();
        }

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                FlushSection();
                formattedSections.Add(string.Empty);
                sectionIsDice = null;
                continue;
            }

            bool isDice = IsDiceLine(line);
            if (sectionIsDice != null
                && sectionIsDice.Value != isDice)
            {
                FlushSection();
            }

            sectionIsDice = isDice;
            sectionLines.Add(line);
        }

        FlushSection();
        return string.Join('\n', formattedSections);
    }

    private static string FormatDiceRows(
        IEnumerable<string> lines,
        int diceFontSize,
        int bodyFontSize,
        int iconSize)
    {
        return string.Join(
            DiceRowSeparator,
            lines.Select(
                line => FormatDiceLine(
                    line,
                    diceFontSize,
                    bodyFontSize,
                    iconSize)));
    }

    private static string FormatDiceLine(
        string line,
        int diceFontSize,
        int bodyFontSize,
        int iconSize)
    {
        int timingSeparatorIndex =
            line.IndexOf(InlineDiceTimingSeparator);
        string diceText = timingSeparatorIndex >= 0
            ? line[..timingSeparatorIndex]
            : line;
        string formattedDice =
            $"[font_size={diceFontSize}]"
            + $"{ResizeDiceIcons(diceText, iconSize)}[/font_size]";
        if (timingSeparatorIndex < 0)
            return formattedDice;

        string timingText =
            line[(timingSeparatorIndex + 1)..].Trim();
        return $"{formattedDice}  "
            + $"[font_size={bodyFontSize}]"
            + $"{timingText}[/font_size]";
    }

    private static string ResizeDiceIcons(
        string text,
        int iconSize)
    {
        return DiceImageTagPattern.Replace(
            text,
            match =>
                $"[img={iconSize}x{iconSize}]"
                + $"{match.Groups["path"].Value}[/img]");
    }

    private static float GetDiceBodyGap(int diceFontSize)
    {
        float scale =
            (float)diceFontSize / MaximumDiceFontSize;
        return Math.Clamp(
            MaximumDiceBodyGap * scale,
            MinimumDiceBodyGap,
            MaximumDiceBodyGap);
    }

    private static void ScheduleDeferredFit(
        MegaRichTextLabel bodyLabel,
        MegaRichTextLabel? diceLabel,
        string[] keywordLines,
        string[] diceLines,
        string[] bodyLines)
    {
        ulong instanceId = bodyLabel.GetInstanceId();
        long revision = ++_nextFitRevision;
        FitRevisions[instanceId] = revision;

        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(bodyLabel)
                || bodyLabel.IsQueuedForDeletion()
                || !FitRevisions.TryGetValue(
                    instanceId,
                    out long currentRevision)
                || currentRevision != revision)
            {
                return;
            }

            MegaRichTextLabel? validDiceLabel =
                diceLabel != null
                && GodotObject.IsInstanceValid(diceLabel)
                && !diceLabel.IsQueuedForDeletion()
                    ? diceLabel
                    : null;
            _ = FitAfterLayoutAsync(
                bodyLabel,
                validDiceLabel,
                keywordLines,
                diceLines,
                bodyLines,
                instanceId,
                revision);
        }).CallDeferred();
    }

    private static async Task FitAfterLayoutAsync(
        MegaRichTextLabel bodyLabel,
        MegaRichTextLabel? diceLabel,
        string[] keywordLines,
        string[] diceLines,
        string[] bodyLines,
        ulong instanceId,
        long revision)
    {
        try
        {
            if (!IsFitCurrent(bodyLabel, instanceId, revision)
                || !bodyLabel.IsInsideTree())
                return;

            if (diceLabel != null)
                RestoreBodyLayout(bodyLabel, diceLabel);

            float fullTop = bodyLabel.OffsetTop;
            float fullHeight = Math.Max(
                0f,
                bodyLabel.OffsetBottom - bodyLabel.OffsetTop);
            ConfigureLabels(
                bodyLabel,
                diceLabel,
                keywordLines,
                diceLines,
                bodyLines);

            int low = MinimumDiceFontSize;
            int high = MaximumDiceFontSize;
            int bestFontSize = MinimumDiceFontSize;
            float bestTopHeight = 0f;
            float bestBodyHeight = 0f;

            while (low <= high)
            {
                int candidateFontSize = low + (high - low) / 2;
                ApplyFontSizeCandidate(
                    bodyLabel,
                    diceLabel,
                    keywordLines,
                    diceLines,
                    bodyLines,
                    candidateFontSize);

                if (!await WaitForLayoutAsync(
                        bodyLabel,
                        instanceId,
                        revision))
                {
                    return;
                }

                (float topHeight, float bodyHeight) =
                    MeasureContentHeights(
                        bodyLabel,
                        diceLabel,
                        keywordLines,
                        diceLines,
                        bodyLines);
                float gap = GetMeasuredGap(
                    candidateFontSize,
                    topHeight,
                    bodyHeight);
                bool fits =
                    IsMeasurementReady(
                        keywordLines,
                        diceLines,
                        bodyLines,
                        topHeight,
                        bodyHeight)
                    && topHeight + gap + bodyHeight
                        <= fullHeight + LayoutHeightTolerance;

                if (fits)
                {
                    bestFontSize = candidateFontSize;
                    bestTopHeight = topHeight;
                    bestBodyHeight = bodyHeight;
                    low = candidateFontSize + 1;
                }
                else
                {
                    high = candidateFontSize - 1;
                }
            }

            ApplyFontSizeCandidate(
                bodyLabel,
                diceLabel,
                keywordLines,
                diceLines,
                bodyLines,
                bestFontSize);
            if (!await WaitForLayoutAsync(
                    bodyLabel,
                    instanceId,
                    revision))
            {
                return;
            }

            (bestTopHeight, bestBodyHeight) =
                MeasureContentHeights(
                    bodyLabel,
                    diceLabel,
                    keywordLines,
                    diceLines,
                    bodyLines);
            float bestGap = GetMeasuredGap(
                bestFontSize,
                bestTopHeight,
                bestBodyHeight);

            while (bestFontSize > MinimumDiceFontSize
                   && (!IsMeasurementReady(
                           keywordLines,
                           diceLines,
                           bodyLines,
                           bestTopHeight,
                           bestBodyHeight)
                       || bestTopHeight + bestGap + bestBodyHeight
                           > fullHeight + LayoutHeightTolerance))
            {
                bestFontSize--;
                ApplyFontSizeCandidate(
                    bodyLabel,
                    diceLabel,
                    keywordLines,
                    diceLines,
                    bodyLines,
                    bestFontSize);
                if (!await WaitForLayoutAsync(
                        bodyLabel,
                        instanceId,
                        revision))
                {
                    return;
                }

                (bestTopHeight, bestBodyHeight) =
                    MeasureContentHeights(
                        bodyLabel,
                        diceLabel,
                        keywordLines,
                        diceLines,
                        bodyLines);
                bestGap = GetMeasuredGap(
                    bestFontSize,
                    bestTopHeight,
                    bestBodyHeight);
            }

            if (!IsFitCurrent(bodyLabel, instanceId, revision))
                return;

            if (bodyLines.Length > 0)
            {
                bodyLabel.OffsetTop =
                    fullTop + bestTopHeight + bestGap;
            }
            else
            {
                bodyLabel.Visible = false;
                bodyLabel.SetTextAutoSize(string.Empty);
            }
        }
        catch (Exception exception)
        {
            if (_loggedFailure)
                return;

            _loggedFailure = true;
            Log.Warn(
                "[LibraryOfRuinaLib.CardDescription] "
                + "Failed to finish dice card description layout: "
                + $"{exception}");
        }
        finally
        {
            CompleteFitRevision(instanceId, revision);
        }
    }

    private static void ConfigureLabels(
        MegaRichTextLabel bodyLabel,
        MegaRichTextLabel? diceLabel,
        IReadOnlyList<string> keywordLines,
        IReadOnlyList<string> diceLines,
        IReadOnlyList<string> bodyLines)
    {
        bodyLabel.AutoSizeEnabled = false;
        bodyLabel.Visible = bodyLines.Count > 0;
        bodyLabel.VerticalAlignment = VerticalAlignment.Top;

        if (diceLabel == null)
            return;

        diceLabel.AutoSizeEnabled = false;
        diceLabel.Visible =
            keywordLines.Count > 0 || diceLines.Count > 0;
        diceLabel.VerticalAlignment = VerticalAlignment.Top;
    }

    private static void ApplyFontSizeCandidate(
        MegaRichTextLabel bodyLabel,
        MegaRichTextLabel? diceLabel,
        IReadOnlyList<string> keywordLines,
        IReadOnlyList<string> diceLines,
        IReadOnlyList<string> bodyLines,
        int diceFontSize)
    {
        int bodyFontSize = Math.Clamp(
            diceFontSize - 3,
            MinimumDiceFontSize,
            DefaultBodyFontSize);
        int iconSize = diceFontSize + DiceIconSizeOffset;

        if (diceLabel != null)
        {
            diceLabel.SetTextAutoSize(
                FormatTopLines(
                    keywordLines,
                    diceLines,
                    diceFontSize,
                    bodyFontSize,
                    iconSize));
        }

        bodyLabel.SetTextAutoSize(
            FormatDescriptionLines(
                bodyLines,
                diceFontSize,
                bodyFontSize,
                iconSize));
    }

    private static (
        float TopHeight,
        float BodyHeight) MeasureContentHeights(
        MegaRichTextLabel bodyLabel,
        MegaRichTextLabel? diceLabel,
        IReadOnlyList<string> keywordLines,
        IReadOnlyList<string> diceLines,
        IReadOnlyList<string> bodyLines)
    {
        float topHeight = diceLabel != null
            && (keywordLines.Count > 0 || diceLines.Count > 0)
                ? diceLabel.GetContentHeight()
                : 0f;
        float bodyHeight = bodyLines.Count > 0
            ? bodyLabel.GetContentHeight()
            : 0f;
        return (topHeight, bodyHeight);
    }

    private static float GetMeasuredGap(
        int diceFontSize,
        float topHeight,
        float bodyHeight)
    {
        return topHeight > 0f && bodyHeight > 0f
            ? GetDiceBodyGap(diceFontSize)
            : 0f;
    }

    private static bool IsMeasurementReady(
        IReadOnlyList<string> keywordLines,
        IReadOnlyList<string> diceLines,
        IReadOnlyList<string> bodyLines,
        float topHeight,
        float bodyHeight)
    {
        return ((keywordLines.Count == 0 && diceLines.Count == 0)
                || topHeight > 0f)
            && (bodyLines.Count == 0 || bodyHeight > 0f);
    }

    private static async Task<bool> WaitForLayoutAsync(
        MegaRichTextLabel bodyLabel,
        ulong instanceId,
        long revision)
    {
        if (!IsFitCurrent(bodyLabel, instanceId, revision)
            || !bodyLabel.IsInsideTree())
        {
            return false;
        }

        SceneTree? tree = Engine.GetMainLoop() as SceneTree;
        if (tree == null || !GodotObject.IsInstanceValid(tree))
            return false;

        for (int frame = 0;
             frame < LayoutMeasurementFrameCount;
             frame++)
        {
            await tree.ToSignal(
                tree,
                SceneTree.SignalName.ProcessFrame);
            if (!IsFitCurrent(bodyLabel, instanceId, revision)
                || !bodyLabel.IsInsideTree())
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsFitCurrent(
        MegaRichTextLabel bodyLabel,
        ulong instanceId,
        long revision)
    {
        return GodotObject.IsInstanceValid(bodyLabel)
            && !bodyLabel.IsQueuedForDeletion()
            && FitRevisions.TryGetValue(
                instanceId,
                out long currentRevision)
            && currentRevision == revision;
    }

    private static void CompleteFitRevision(
        ulong instanceId,
        long revision)
    {
        if (FitRevisions.TryGetValue(
                instanceId,
                out long currentRevision)
            && currentRevision == revision)
        {
            FitRevisions.Remove(instanceId);
        }
    }

    private static void CancelDeferredFit(
        MegaRichTextLabel bodyLabel)
    {
        FitRevisions.Remove(bodyLabel.GetInstanceId());
    }

    private static void HideDiceLabel(
        MegaRichTextLabel diceLabel)
    {
        diceLabel.Visible = false;
        diceLabel.SetTextAutoSize(string.Empty);
    }

    private static void RestoreBodyAutoSize(
        MegaRichTextLabel bodyLabel)
    {
        bool needsRefresh = !bodyLabel.AutoSizeEnabled;
        bodyLabel.AutoSizeEnabled = true;
        bodyLabel.MaxFontSize = DefaultBodyFontSize;
        if (!needsRefresh)
            return;

        string currentText = bodyLabel.Text;
        bodyLabel.SetTextAutoSize(string.Empty);
        bodyLabel.SetTextAutoSize(currentText);
    }

    private static void RestoreDefaultLayout(
        MegaRichTextLabel bodyLabel,
        MegaRichTextLabel? diceLabel)
    {
        if (diceLabel != null)
        {
            RestoreBodyLayout(bodyLabel, diceLabel);
            HideDiceLabel(diceLabel);
        }

        RestoreBodyAutoSize(bodyLabel);
        bodyLabel.Visible = true;
        bodyLabel.VerticalAlignment = VerticalAlignment.Center;
    }

    private static void RestoreBodyLayout(
        MegaRichTextLabel bodyLabel,
        MegaRichTextLabel diceLabel)
    {
        bodyLabel.AnchorLeft = diceLabel.AnchorLeft;
        bodyLabel.AnchorTop = diceLabel.AnchorTop;
        bodyLabel.AnchorRight = diceLabel.AnchorRight;
        bodyLabel.AnchorBottom = diceLabel.AnchorBottom;
        bodyLabel.OffsetLeft = diceLabel.OffsetLeft;
        bodyLabel.OffsetTop = diceLabel.OffsetTop;
        bodyLabel.OffsetRight = diceLabel.OffsetRight;
        bodyLabel.OffsetBottom = diceLabel.OffsetBottom;
        bodyLabel.GrowHorizontal = diceLabel.GrowHorizontal;
        bodyLabel.GrowVertical = diceLabel.GrowVertical;
        bodyLabel.PivotOffset = diceLabel.PivotOffset;
        bodyLabel.Scale = diceLabel.Scale;
        bodyLabel.Rotation = diceLabel.Rotation;
    }
}
