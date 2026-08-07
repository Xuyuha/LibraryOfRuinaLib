using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace LibraryLib.SpeedDice;

internal sealed partial class LibrarySpeedDiceHoverTargetLine : Node2D
{
    private const int PresentationZIndex = -8;
    private const int SegmentCount = 50;
    private const string ArrowTexturePath =
        "res://LibraryOfRuinaLib/LibraryOfRuinaLib/images/vfx/targeted_intent/arrow.png";
    private const string ArrowStartTexturePath =
        "res://LibraryOfRuinaLib/LibraryOfRuinaLib/images/vfx/targeted_intent/arrowstart.png";

    private static readonly Color SoftColor =
        new(0.35f, 0.72f, 1f, 0.78f);
    private static readonly Color StrongColor =
        new(0.62f, 0.90f, 1f, 0.96f);

    private static Texture2D? _arrowTexture;
    private static Texture2D? _arrowStartTexture;

    private readonly Control _source;
    private readonly Creature _target;
    private readonly bool _isPrimary;
    private readonly bool _showStartMarker;
    private readonly LibrarySpeedDiceTargetDashLine _dashes = new()
    {
        Name = "LineDashes",
    };
    private readonly Sprite2D _startMarker = new()
    {
        Name = "ArrowStart",
        Scale = Vector2.One * 0.16f,
        TextureFilter = CanvasItem.TextureFilterEnum.Linear,
    };
    private readonly Sprite2D _arrowHead = new()
    {
        Name = "ArrowHead",
        Scale = Vector2.One * 0.24f,
        TextureFilter = CanvasItem.TextureFilterEnum.Linear,
    };

    private bool _stopped;

    private LibrarySpeedDiceHoverTargetLine(
        Control source,
        Creature target,
        bool isPrimary,
        bool showStartMarker)
    {
        _source = source;
        _target = target;
        _isPrimary = isPrimary;
        _showStartMarker = showStartMarker;
        TopLevel = true;
        ZIndex = PresentationZIndex;
        ZAsRelative = false;
        GlobalPosition = Vector2.Zero;
        ProcessMode = ProcessModeEnum.Always;

        _arrowTexture ??= LoadTexture(ArrowTexturePath);
        _arrowStartTexture ??= LoadTexture(ArrowStartTexturePath);
        _arrowHead.Texture = _arrowTexture;
        _startMarker.Texture = _arrowStartTexture;
        _startMarker.Visible = showStartMarker;

        AddChild(_dashes);
        AddChild(_startMarker);
        AddChild(_arrowHead);
    }

    public static LibrarySpeedDiceHoverTargetLine? Begin(
        Control source,
        int slotIndex,
        int targetIndex,
        Creature? target,
        bool isPrimary,
        bool showStartMarker)
    {
        if (target is not { IsAlive: true })
            return null;

        var line = new LibrarySpeedDiceHoverTargetLine(
            source,
            target,
            isPrimary,
            showStartMarker)
        {
            Name =
                $"LibrarySpeedDiceTargetLine{slotIndex}_{targetIndex}",
        };
        NTargetManager.Instance.AddChildSafely(line);
        return line;
    }

    public override void _Process(double delta)
    {
        if (_stopped
            || !GodotObject.IsInstanceValid(_source)
            || !_source.IsVisibleInTree()
            || !TryGetTargetCenter(out Vector2 to))
        {
            Stop();
            return;
        }

        Rect2 sourceRect = _source.GetGlobalRect();
        Vector2 from =
            sourceRect.Position + sourceRect.Size * 0.5f;
        float pulse =
            (Mathf.Sin(
                (float)Time.GetTicksMsec()
                * 0.001f
                * Mathf.Tau)
                + 1f)
            * 0.5f;
        Color lineColor = LerpColor(
            SoftColor,
            StrongColor,
            pulse);
        if (!_isPrimary)
        {
            lineColor = new Color(
                lineColor.R,
                lineColor.G,
                lineColor.B,
                lineColor.A * 0.68f);
        }

        UpdateCurve(from, to);
        _dashes.SetLineColor(lineColor);
        if (_showStartMarker)
        {
            _startMarker.Modulate = new Color(
                lineColor.R,
                lineColor.G,
                lineColor.B,
                lineColor.A * 0.72f);
        }
        _arrowHead.Modulate = lineColor;
        Visible = true;
    }

    public void Stop()
    {
        if (_stopped)
            return;

        _stopped = true;
        QueueFree();
    }

    private bool TryGetTargetCenter(out Vector2 center)
    {
        center = Vector2.Zero;
        NCreature? targetNode =
            NCombatRoom.Instance?.GetCreatureNode(_target);
        if (targetNode == null
            || !GodotObject.IsInstanceValid(targetNode)
            || !GodotObject.IsInstanceValid(targetNode.Hitbox)
            || !_target.IsAlive)
        {
            return false;
        }

        Rect2 targetRect = targetNode.Hitbox.GetGlobalRect();
        center =
            targetRect.Position + targetRect.Size * 0.5f;
        return true;
    }

    private void UpdateCurve(Vector2 from, Vector2 to)
    {
        float distance = from.DistanceTo(to);
        float curveHeight = Mathf.Clamp(
            distance * 0.175f,
            36f,
            220f);
        Vector2 control =
            (from + to) * 0.5f + Vector2.Up * curveHeight;

        for (int i = 0; i <= SegmentCount; i++)
        {
            _dashes.SetCurvePoint(
                i,
                MathHelper.BezierCurve(
                    from,
                    to,
                    control,
                    i / (float)SegmentCount));
        }

        _dashes.Commit();
        Vector2 previous =
            _dashes.GetCurvePoint(SegmentCount - 1);
        _startMarker.GlobalPosition = from;
        _arrowHead.GlobalPosition = to;
        _arrowHead.GlobalRotation =
            (to - previous).Angle() + Mathf.Pi * 0.5f;
    }

    private static Texture2D? LoadTexture(string path)
    {
        return ResourceLoader.Exists(path)
            ? ResourceLoader.Load<Texture2D>(path)
            : null;
    }

    private static Color LerpColor(
        Color from,
        Color to,
        float weight)
    {
        return new Color(
            Mathf.Lerp(from.R, to.R, weight),
            Mathf.Lerp(from.G, to.G, weight),
            Mathf.Lerp(from.B, to.B, weight),
            Mathf.Lerp(from.A, to.A, weight));
    }
}
