using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace LibraryLib.SpeedDice;

internal sealed partial class LibrarySpeedDiceRightClickTargetLine : Node2D
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

    private NTargetManager? _targetManager;
    private Control? _source;
    private Node? _hoveredNode;
    private bool _usingController;
    private bool _signalsConnected;
    private bool _stopped;

    private Callable _nodeHoveredCallable;
    private Callable _nodeUnhoveredCallable;
    private Callable _targetingEndedCallable;

    private LibrarySpeedDiceRightClickTargetLine()
    {
        TopLevel = true;
        ZIndex = PresentationZIndex;
        ZAsRelative = false;
        GlobalPosition = Vector2.Zero;
        ProcessMode = ProcessModeEnum.Always;

        _arrowTexture ??= LibrarySpeedDiceTargetArrow.LoadTexture(ArrowTexturePath);
        _arrowStartTexture ??= LibrarySpeedDiceTargetArrow.LoadTexture(ArrowStartTexturePath);
        _arrowHead.Texture = _arrowTexture;
        _startMarker.Texture = _arrowStartTexture;

        AddChild(_dashes);
        AddChild(_startMarker);
        AddChild(_arrowHead);
    }

    public static LibrarySpeedDiceRightClickTargetLine Begin(
        NTargetManager targetManager,
        Control source,
        bool usingController)
    {
        var line = new LibrarySpeedDiceRightClickTargetLine
        {
            Name = "LibrarySpeedDiceRightClickTargetLine",
        };
        targetManager.AddChildSafely(line);
        line.Initialize(targetManager, source, usingController);
        return line;
    }

    public override void _Process(double delta)
    {
        if (_stopped
            || _targetManager == null
            || !_targetManager.IsInSelection
            || _source == null
            || !GodotObject.IsInstanceValid(_source)
            || !_source.IsVisibleInTree()
            || NCombatUi.IsDebugHideTargetingUi)
        {
            Visible = false;
            return;
        }

        Rect2 sourceRect = _source.GetGlobalRect();
        Vector2 from =
            sourceRect.Position + sourceRect.Size * 0.5f;
        Vector2 to;
        if (_usingController)
        {
            if (!TryGetNodeCenter(_hoveredNode, out to))
            {
                Visible = false;
                return;
            }
        }
        else
        {
            to = GetViewport().GetMousePosition();
        }

        if (from.DistanceSquaredTo(to) <= 4f)
        {
            Visible = false;
            return;
        }

        LibrarySpeedDiceTargetArrow.Update(_dashes, _startMarker, _arrowHead, from, to);
        float pulse =
            (Mathf.Sin(
                (float)Time.GetTicksMsec()
                * 0.001f
                * Mathf.Tau)
                + 1f)
            * 0.5f;
        Color lineColor = SoftColor.Lerp(StrongColor, pulse);
        _dashes.SetLineColor(lineColor);
        _startMarker.Modulate = new Color(
            lineColor.R,
            lineColor.G,
            lineColor.B,
            lineColor.A * 0.72f);
        _arrowHead.Modulate = lineColor;
        Visible = true;
    }

    public override void _ExitTree()
    {
        DisconnectSignals();
    }

    public void Stop()
    {
        if (_stopped)
            return;

        _stopped = true;
        DisconnectSignals();
        QueueFree();
    }

    private void Initialize(
        NTargetManager targetManager,
        Control source,
        bool usingController)
    {
        _targetManager = targetManager;
        _source = source;
        _usingController = usingController;

        _nodeHoveredCallable =
            Callable.From<Node>(OnNodeHovered);
        _nodeUnhoveredCallable =
            Callable.From<Node>(OnNodeUnhovered);
        _targetingEndedCallable =
            Callable.From(Stop);
        targetManager.Connect(
            NTargetManager.SignalName.NodeHovered,
            _nodeHoveredCallable);
        targetManager.Connect(
            NTargetManager.SignalName.NodeUnhovered,
            _nodeUnhoveredCallable);
        targetManager.Connect(
            NTargetManager.SignalName.TargetingEnded,
            _targetingEndedCallable);
        _signalsConnected = true;

        targetManager
            .GetNodeOrNull<NTargetingArrow>("TargetingArrow")
            ?.Hide();
    }

    private void OnNodeHovered(Node node)
    {
        _hoveredNode = node;
    }

    private void OnNodeUnhovered(Node node)
    {
        if (ReferenceEquals(_hoveredNode, node))
            _hoveredNode = null;
    }

    private void DisconnectSignals()
    {
        if (!_signalsConnected
            || _targetManager == null
            || !GodotObject.IsInstanceValid(_targetManager))
        {
            return;
        }

        DisconnectIfConnected(
            NTargetManager.SignalName.NodeHovered,
            _nodeHoveredCallable);
        DisconnectIfConnected(
            NTargetManager.SignalName.NodeUnhovered,
            _nodeUnhoveredCallable);
        DisconnectIfConnected(
            NTargetManager.SignalName.TargetingEnded,
            _targetingEndedCallable);
        _signalsConnected = false;
    }

    private void DisconnectIfConnected(
        StringName signal,
        Callable callable)
    {
        if (_targetManager!.IsConnected(signal, callable))
            _targetManager.Disconnect(signal, callable);
    }

    private static bool TryGetNodeCenter(
        Node? node,
        out Vector2 center)
    {
        center = Vector2.Zero;
        if (node == null || !GodotObject.IsInstanceValid(node))
            return false;

        if (node is Control control)
        {
            Rect2 rect = control.GetGlobalRect();
            center = rect.Position + rect.Size * 0.5f;
            return true;
        }

        if (node is Node2D node2D)
        {
            center = node2D.GlobalPosition;
            return true;
        }

        return false;
    }
}
