using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Library.SpeedDice;

internal sealed partial class LibrarySpeedDiceTargetLine : Node2D
{
    private const int SegmentCount = 50;
    private const string ArrowTexturePath =
        "res://images/vfx/targeted_intent/arrow.png";
    private const string ArrowStartTexturePath =
        "res://images/vfx/targeted_intent/arrowstart.png";

    private static readonly Color AllySoftColor =
        new(0.35f, 0.72f, 1f, 0.78f);
    private static readonly Color AllyStrongColor =
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
    private bool _usingController;
    private Vector2 _controllerTarget;
    private bool _hasControllerTarget;
    private bool _signalsConnected;
    private bool _stopped;

    private Callable _creatureHoveredCallable;
    private Callable _creatureUnhoveredCallable;

    private LibrarySpeedDiceTargetLine()
    {
        TopLevel = true;
        ZIndex = 120;
        GlobalPosition = Vector2.Zero;
    }

    public static LibrarySpeedDiceTargetLine Begin(
        NTargetManager targetManager,
        Control source,
        bool usingController)
    {
        var line = new LibrarySpeedDiceTargetLine();
        targetManager.AddChild(line);
        line.Initialize(targetManager, source, usingController);
        return line;
    }

    public override void _Process(double delta)
    {
        if (_stopped
            || _source == null
            || !GodotObject.IsInstanceValid(_source)
            || !_source.IsVisibleInTree()
            || NCombatUi.IsDebugHideTargetingUi)
        {
            Visible = false;
            return;
        }

        Rect2 sourceRect = _source.GetGlobalRect();
        Vector2 from = sourceRect.Position + sourceRect.Size * 0.5f;
        Vector2 to = _usingController
            ? _hasControllerTarget
                ? _controllerTarget
                : from
            : GetViewport().GetMousePosition();
        if (from.DistanceSquaredTo(to) <= 4f)
        {
            Visible = false;
            return;
        }

        Visible = true;
        float pulse =
            (Mathf.Sin((float)Time.GetTicksMsec() * 0.001f * Mathf.Tau)
                + 1f)
            * 0.5f;
        Color lineColor = LerpColor(
            AllySoftColor,
            AllyStrongColor,
            pulse);

        UpdateCurve(from, to);
        _dashes.SetLineColor(lineColor);
        _startMarker.Modulate = new Color(
            lineColor.R,
            lineColor.G,
            lineColor.B,
            lineColor.A * 0.72f);
        _arrowHead.Modulate = lineColor;
    }

    public void Stop()
    {
        if (_stopped)
            return;

        _stopped = true;
        DisconnectTargetSignals();
        QueueFree();
    }

    public override void _ExitTree()
    {
        DisconnectTargetSignals();
    }

    private void Initialize(
        NTargetManager targetManager,
        Control source,
        bool usingController)
    {
        _targetManager = targetManager;
        _source = source;
        _usingController = usingController;

        _arrowTexture ??= LoadTexture(ArrowTexturePath);
        _arrowStartTexture ??= LoadTexture(ArrowStartTexturePath);
        _arrowHead.Texture = _arrowTexture;
        _startMarker.Texture = _arrowStartTexture;

        AddChild(_dashes);
        AddChild(_startMarker);
        AddChild(_arrowHead);

        _creatureHoveredCallable =
            Callable.From<NCreature>(OnCreatureHovered);
        _creatureUnhoveredCallable =
            Callable.From<NCreature>(OnCreatureUnhovered);
        targetManager.Connect(
            NTargetManager.SignalName.CreatureHovered,
            _creatureHoveredCallable);
        targetManager.Connect(
            NTargetManager.SignalName.CreatureUnhovered,
            _creatureUnhoveredCallable);
        _signalsConnected = true;

        NTargetingArrow? vanillaArrow =
            targetManager.GetNodeOrNull<NTargetingArrow>("TargetingArrow");
        vanillaArrow?.Hide();
    }

    private void OnCreatureHovered(NCreature creature)
    {
        if (!_usingController)
            return;

        _controllerTarget = creature.VfxSpawnPosition;
        _hasControllerTarget = true;
    }

    private void OnCreatureUnhovered(NCreature creature)
    {
        if (_usingController)
            _hasControllerTarget = false;
    }

    private void DisconnectTargetSignals()
    {
        if (!_signalsConnected
            || _targetManager == null
            || !GodotObject.IsInstanceValid(_targetManager))
        {
            return;
        }

        if (_targetManager.IsConnected(
                NTargetManager.SignalName.CreatureHovered,
                _creatureHoveredCallable))
        {
            _targetManager.Disconnect(
                NTargetManager.SignalName.CreatureHovered,
                _creatureHoveredCallable);
        }

        if (_targetManager.IsConnected(
                NTargetManager.SignalName.CreatureUnhovered,
                _creatureUnhoveredCallable))
        {
            _targetManager.Disconnect(
                NTargetManager.SignalName.CreatureUnhovered,
                _creatureUnhoveredCallable);
        }
        _signalsConnected = false;
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
        Vector2 previous = _dashes.GetCurvePoint(SegmentCount - 1);
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

internal sealed partial class LibrarySpeedDiceTargetDashLine : Node2D
{
    private const int PointCount = 51;
    private const float DashLength = 92f;
    private const float GapLength = 28f;
    private const float DashSampleStep = 8f;
    private const float FlowCyclesPerSecond = 1.5f;
    private const float GlowWidth = 14f;
    private const float FrontWidth = 8f;

    private readonly Vector2[] _curvePoints = new Vector2[PointCount];
    private readonly float[] _cumulativeLengths = new float[PointCount];
    private Color _lineColor = Colors.White;
    private float _phase;

    public override void _Process(double delta)
    {
        float patternLength = DashLength + GapLength;
        _phase =
            (_phase
                + (float)delta
                * patternLength
                * FlowCyclesPerSecond)
            % patternLength;
        QueueRedraw();
    }

    public override void _Draw()
    {
        _cumulativeLengths[0] = 0f;
        for (int i = 1; i < PointCount; i++)
        {
            _cumulativeLengths[i] =
                _cumulativeLengths[i - 1]
                + _curvePoints[i - 1].DistanceTo(_curvePoints[i]);
        }

        float totalLength = _cumulativeLengths[^1];
        if (totalLength <= 0.01f)
            return;

        float patternLength = DashLength + GapLength;
        for (float dashStart = _phase - patternLength;
             dashStart < totalLength;
             dashStart += patternLength)
        {
            float visibleStart = Math.Max(0f, dashStart);
            float visibleEnd = Math.Min(
                totalLength,
                dashStart + DashLength);
            if (visibleEnd - visibleStart <= 1f)
                continue;

            float centerRatio =
                (visibleStart + visibleEnd)
                * 0.5f
                / totalLength;
            float fade = Mathf.Lerp(
                0.15f,
                1f,
                Mathf.Clamp(centerRatio / 0.20f, 0f, 1f));
            Color glowColor = new(
                _lineColor.R,
                _lineColor.G,
                _lineColor.B,
                _lineColor.A * 0.32f * fade);
            Color frontColor = new(
                _lineColor.R,
                _lineColor.G,
                _lineColor.B,
                _lineColor.A * fade);
            Vector2[] dashPoints =
                SampleRange(visibleStart, visibleEnd);
            DrawPolyline(
                dashPoints,
                glowColor,
                GlowWidth,
                antialiased: true);
            DrawPolyline(
                dashPoints,
                frontColor,
                FrontWidth,
                antialiased: true);
        }
    }

    public void SetCurvePoint(int index, Vector2 position)
    {
        _curvePoints[index] = position;
    }

    public Vector2 GetCurvePoint(int index)
    {
        return _curvePoints[index];
    }

    public void Commit()
    {
        QueueRedraw();
    }

    public void SetLineColor(Color color)
    {
        _lineColor = color;
    }

    private Vector2[] SampleRange(
        float startDistance,
        float endDistance)
    {
        int pointCount = Math.Max(
            2,
            Mathf.CeilToInt(
                (endDistance - startDistance) / DashSampleStep)
            + 1);
        var points = new Vector2[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            float weight = i / (float)(pointCount - 1);
            points[i] = SampleAtDistance(
                Mathf.Lerp(
                    startDistance,
                    endDistance,
                    weight));
        }

        return points;
    }

    private Vector2 SampleAtDistance(float distance)
    {
        if (distance <= 0f)
            return _curvePoints[0];

        float totalLength = _cumulativeLengths[^1];
        if (distance >= totalLength)
            return _curvePoints[^1];

        int segment = 1;
        while (segment < PointCount
               && _cumulativeLengths[segment] < distance)
        {
            segment++;
        }

        float segmentStart = _cumulativeLengths[segment - 1];
        float segmentLength =
            _cumulativeLengths[segment] - segmentStart;
        float weight = segmentLength <= 0.001f
            ? 0f
            : (distance - segmentStart) / segmentLength;
        return _curvePoints[segment - 1].Lerp(
            _curvePoints[segment],
            weight);
    }
}
