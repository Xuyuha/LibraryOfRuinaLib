using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace LibraryLib.SpeedDice;

internal sealed partial class LibrarySpeedDiceHoverTargetLine : Node2D
{
    private const int PresentationZIndex = -8;
    private const string ArrowTexturePath =
        "res://LibraryOfRuinaLib/images/vfx/targeted_intent/arrow.png";
    private const string ArrowStartTexturePath =
        "res://LibraryOfRuinaLib/images/vfx/targeted_intent/arrowstart.png";

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

        _arrowTexture ??= LibrarySpeedDiceTargetArrow.LoadTexture(ArrowTexturePath);
        _arrowStartTexture ??= LibrarySpeedDiceTargetArrow.LoadTexture(ArrowStartTexturePath);
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
        Color lineColor = SoftColor.Lerp(StrongColor, pulse);
        if (!_isPrimary)
        {
            lineColor = new Color(
                lineColor.R,
                lineColor.G,
                lineColor.B,
                lineColor.A * 0.68f);
        }

        LibrarySpeedDiceTargetArrow.Update(_dashes, _startMarker, _arrowHead, from, to);
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
}
