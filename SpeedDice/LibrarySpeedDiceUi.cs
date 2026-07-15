using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace Library.SpeedDice;

internal sealed partial class LibrarySpeedDiceUi : Control
{
    private const string DiceAtlasPath =
        "res://LibraryOfRuinaLib/images/ui/library_speed_dice_atlas.png";
    private const string RoulettePath =
        "res://LibraryOfRuinaLib/images/ui/speed_dice_roulette.png";
    private const string BrokenDicePath =
        "res://LibraryOfRuinaLib/images/ui/speed_dice_broken.png";
    private const string BrokenLinePath =
        "res://LibraryOfRuinaLib/images/ui/speed_dice_broken_line.png";
    private const string FontPath =
        "res://themes/kreon_bold_glyph_space_one.tres";
    private const float SlotWidth = 68f;
    private const float SlotHeight = 74f;
    private const float SlotGap = 4f;
    private const float TopGap = 18f;
    private const float RouletteViewportWidth = 42f;
    private const float RouletteViewportHeight = 50f;
    private const float RouletteDisplayedStripHeight = 541f;
    private const float RouletteScrollSpeed = 240f;

    private static readonly Rect2 NormalDiceRegion =
        new(278f, 525f, 211f, 231f);
    private static readonly Rect2 LightDiceRegion =
        new(522f, 512f, 235f, 256f);
    private static readonly Rect2 HighlightDiceRegion =
        new(778f, 520f, 235f, 240f);
    private static readonly Color AllyDiceColor =
        new(0.937f, 0.761f, 0.506f, 1f);
    private static readonly Color AllyDiceLineColor =
        new(0.424f, 0.169f, 0f, 1f);
    private readonly List<SlotView> _slotViews = [];
    private NCreature? _creatureNode;
    private LibrarySpeedDiceCombatState? _state;
    private double _rouletteTimer;

    private sealed class SlotView
    {
        public required NButton Root { get; init; }
        public required TextureRect Normal { get; init; }
        public required TextureRect Light { get; init; }
        public required TextureRect Highlight { get; init; }
        public required TextureRect Roulette { get; init; }
        public AtlasTexture? RouletteAtlas { get; init; }
        public required Label RangeLabel { get; init; }
        public required Label ValueLabel { get; init; }
        public required TextureRect Broken { get; init; }
        public required TextureRect BrokenLine { get; init; }
        public bool IsFocused { get; set; }
    }

    public void Initialize(
        NCreature creatureNode,
        LibrarySpeedDiceCombatState state)
    {
        _creatureNode = creatureNode;
        _state = state;
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 0;
        ZAsRelative = true;
        ProcessMode = ProcessModeEnum.Always;
        state.Changed += OnStateChanged;
        RebuildSlots();
    }

    public override void _ExitTree()
    {
        if (_state != null)
            _state.Changed -= OnStateChanged;
        NHoverTipSet.Remove(this);
        base._ExitTree();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_creatureNode == null
            || _state == null
            || !GodotObject.IsInstanceValid(_creatureNode))
        {
            QueueFree();
            return;
        }

        Visible = _creatureNode.Entity.IsAlive;
        if (!Visible)
            return;

        SyncPosition();
        _rouletteTimer += delta;
        RefreshViews();
    }

    private void OnStateChanged()
    {
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(this) || _state == null)
                return;
            if (_slotViews.Count != _state.Slots.Count)
                RebuildSlots();
            RefreshViews();
        }).CallDeferred();
    }

    private void RebuildSlots()
    {
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }
        _slotViews.Clear();

        if (_state == null)
            return;

        Texture2D? atlas = ResourceLoader.Load<Texture2D>(DiceAtlasPath);
        Texture2D? rouletteTexture =
            ResourceLoader.Load<Texture2D>(RoulettePath);
        Texture2D? brokenTexture =
            ResourceLoader.Load<Texture2D>(BrokenDicePath);
        Texture2D? brokenLineTexture =
            ResourceLoader.Load<Texture2D>(BrokenLinePath);
        Font? font = ResourceLoader.Load<Font>(FontPath);

        for (int i = 0; i < _state.Slots.Count; i++)
        {
            int slotIndex = i;
            var root = new NButton
            {
                Name = $"SpeedDie{slotIndex + 1}",
                Position = new Vector2(i * (SlotWidth + SlotGap), 0f),
                Size = new Vector2(SlotWidth, SlotHeight),
                MouseFilter = MouseFilterEnum.Stop,
                MouseDefaultCursorShape = CursorShape.Arrow,
                FocusMode = FocusModeEnum.All,
            };
            AddChild(root);

            TextureRect normal = CreateFrameTexture(
                "Normal",
                atlas,
                NormalDiceRegion);
            root.AddChild(normal);

            TextureRect light = CreateFrameTexture(
                "Light",
                atlas,
                LightDiceRegion);
            light.Visible = false;
            root.AddChild(light);

            TextureRect roulette = CreateRouletteTexture(
                "Roulette",
                rouletteTexture,
                out AtlasTexture? rouletteAtlas);
            root.AddChild(roulette);

            Label rangeLabel = CreateLabel(
                "Range",
                font,
                17,
                $"{_state.Participant.MinSpeed}~{_state.Participant.MaxSpeed}");
            rangeLabel.OffsetTop = 47f;
            rangeLabel.OffsetBottom = 72f;
            root.AddChild(rangeLabel);

            Label valueLabel = CreateLabel(
                "Value",
                font,
                32,
                _state.Participant.MinSpeed.ToString());
            valueLabel.OffsetTop = 4f;
            valueLabel.OffsetBottom = 68f;
            valueLabel.Visible = false;
            root.AddChild(valueLabel);

            TextureRect broken = CreateFullTexture(
                "Broken",
                brokenTexture,
                new Vector2(3f, 3f),
                new Vector2(62f, 66f),
                AllyDiceColor.Darkened(0.36f));
            broken.Visible = false;
            root.AddChild(broken);

            TextureRect brokenLine = CreateFullTexture(
                "BrokenLine",
                brokenLineTexture,
                new Vector2(7f, 7f),
                new Vector2(54f, 58f),
                AllyDiceLineColor.Darkened(0.18f));
            brokenLine.Visible = false;
            root.AddChild(brokenLine);

            TextureRect highlight = CreateFrameTexture(
                "Highlight",
                atlas,
                HighlightDiceRegion);
            highlight.Visible = false;
            root.AddChild(highlight);

            var view = new SlotView
            {
                Root = root,
                Normal = normal,
                Light = light,
                Highlight = highlight,
                Roulette = roulette,
                RouletteAtlas = rouletteAtlas,
                RangeLabel = rangeLabel,
                ValueLabel = valueLabel,
                Broken = broken,
                BrokenLine = brokenLine,
            };
            root.Connect(
                NClickableControl.SignalName.Released,
                Callable.From<NClickableControl>(_ =>
                    ActivateSlot(slotIndex, root)));
            root.Connect(
                NClickableControl.SignalName.MouseReleased,
                Callable.From<InputEvent>(input =>
                    OnSlotMouseReleased(slotIndex, input)));
            root.Connect(
                NClickableControl.SignalName.Focused,
                Callable.From<NClickableControl>(_ =>
                    OnSlotFocused(slotIndex, view)));
            root.Connect(
                NClickableControl.SignalName.Unfocused,
                Callable.From<NClickableControl>(_ =>
                    OnSlotUnfocused(view)));

            _slotViews.Add(view);
        }

        ConfigureFocusNeighbors();
        Size = new Vector2(
            Math.Max(
                0f,
                _slotViews.Count * SlotWidth
                + Math.Max(0, _slotViews.Count - 1) * SlotGap),
            SlotHeight);
        SyncPosition();
        RefreshViews();
    }

    private static TextureRect CreateFrameTexture(
        string name,
        Texture2D? atlas,
        Rect2 region)
    {
        Texture2D? texture = atlas == null
            ? null
            : new AtlasTexture
            {
                Atlas = atlas,
                Region = region,
                FilterClip = true,
            };
        return CreateFullTexture(
            name,
            texture,
            Vector2.Zero,
            new Vector2(SlotWidth, SlotHeight),
            Colors.White);
    }

    private static TextureRect CreateRouletteTexture(
        string name,
        Texture2D? texture,
        out AtlasTexture? rouletteAtlas)
    {
        rouletteAtlas = texture == null
            ? null
            : new AtlasTexture
            {
                Atlas = texture,
                Region = new Rect2(
                    0f,
                    0f,
                    texture.GetWidth(),
                    GetRouletteSourceWindowHeight(texture)),
                FilterClip = true,
            };
        return CreateFullTexture(
            name,
            rouletteAtlas,
            new Vector2(
                (SlotWidth - RouletteViewportWidth) * 0.5f + 1f,
                10f),
            new Vector2(
                RouletteViewportWidth - 2f,
                RouletteViewportHeight),
            AllyDiceColor);
    }

    private static float GetRouletteSourceWindowHeight(Texture2D texture)
    {
        return texture.GetHeight()
            * RouletteViewportHeight
            / RouletteDisplayedStripHeight;
    }

    private static TextureRect CreateFullTexture(
        string name,
        Texture2D? texture,
        Vector2 position,
        Vector2 size,
        Color modulate)
    {
        var textureRect = new TextureRect
        {
            Name = name,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = MouseFilterEnum.Ignore,
            Modulate = modulate,
        };
        textureRect.Texture = texture;
        textureRect.Position = position;
        textureRect.Size = size;
        return textureRect;
    }

    private static Label CreateLabel(
        string name,
        Font? font,
        int fontSize,
        string text)
    {
        var label = new Label
        {
            Name = name,
            AnchorRight = 1f,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
            Text = text,
        };
        if (font != null)
            label.AddThemeFontOverride("font", font);
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", AllyDiceColor);
        label.AddThemeColorOverride(
            "font_outline_color",
            new Color("24170E"));
        label.AddThemeConstantOverride("outline_size", 8);
        return label;
    }

    private void RefreshViews()
    {
        if (_state == null)
            return;

        for (int i = 0; i < _slotViews.Count && i < _state.Slots.Count; i++)
        {
            SlotView view = _slotViews[i];
            LibrarySpeedDiceSlot slot = _state.Slots[i];
            bool isBroken = slot.IsSpent;
            bool isRolling = !_state.HasRolled && !isBroken;
            bool canInteract =
                LibrarySpeedDiceService.CanInteractWithSlot(
                    _state,
                    i,
                    out bool canAcceptSelectedCard);

            view.Normal.Visible = !isBroken;
            view.Roulette.Visible = isRolling;
            view.RangeLabel.Visible = isRolling;
            view.ValueLabel.Visible = !isRolling && !isBroken;
            view.Broken.Visible = isBroken;
            view.BrokenLine.Visible = isBroken;
            view.ValueLabel.Text = slot.FinalValue.ToString();

            if (isRolling)
            {
                UpdateRouletteRegion(view, i);
            }

            view.Root.MouseDefaultCursorShape = CursorShape.Arrow;
            view.Light.Visible =
                !isBroken
                && (slot.Card != null || canAcceptSelectedCard);
            if (view.Light.Visible)
            {
                float lightPulse = 0.72f
                    + 0.28f
                    * (Mathf.Sin(
                        (float)Time.GetTicksMsec() * 0.006f)
                        + 1f)
                    * 0.5f;
                view.Light.Modulate =
                    new Color(1f, 1f, 1f, lightPulse);
            }

            view.Highlight.Visible =
                !isBroken && canInteract && view.IsFocused;
            if (view.Highlight.Visible)
            {
                float highlightPulse = 0.82f
                    + 0.18f
                    * (Mathf.Sin(
                        (float)Time.GetTicksMsec() * 0.009f)
                        + 1f)
                    * 0.5f;
                view.Highlight.Modulate =
                    new Color(1f, 1f, 1f, highlightPulse);
            }

            view.Root.SetEnabled(canInteract);
        }
    }

    private void UpdateRouletteRegion(SlotView view, int slotIndex)
    {
        if (view.RouletteAtlas?.Atlas is not Texture2D texture)
            return;

        float windowHeight = GetRouletteSourceWindowHeight(texture);
        float maxOffset = Math.Max(0f, texture.GetHeight() - windowHeight);
        float displayOffset = (float)(
            _rouletteTimer * RouletteScrollSpeed
            + slotIndex * 97d);
        float sourceOffset = maxOffset <= 0f
            ? 0f
            : Mathf.PosMod(
                displayOffset
                * texture.GetHeight()
                / RouletteDisplayedStripHeight,
                maxOffset);
        view.RouletteAtlas.Region = new Rect2(
            0f,
            sourceOffset,
            texture.GetWidth(),
            windowHeight);
    }

    private void SyncPosition()
    {
        if (_creatureNode == null)
            return;

        Control hitbox = _creatureNode.Hitbox;
        Vector2 viewportSize = GetViewportRect().Size;
        float x = hitbox.GlobalPosition.X
            + hitbox.Size.X * 0.5f
            - Size.X * 0.5f;
        float y = hitbox.GlobalPosition.Y - SlotHeight - TopGap;
        x = Mathf.Clamp(
            x,
            12f,
            Math.Max(12f, viewportSize.X - Size.X - 12f));
        y = Math.Max(12f, y);
        GlobalPosition = new Vector2(x, y);
    }

    private void ConfigureFocusNeighbors()
    {
        if (_slotViews.Count == 0)
            return;

        for (int i = 0; i < _slotViews.Count; i++)
        {
            NButton root = _slotViews[i].Root;
            root.FocusNeighborLeft =
                _slotViews[(i - 1 + _slotViews.Count) % _slotViews.Count]
                    .Root
                    .GetPath();
            root.FocusNeighborRight =
                _slotViews[(i + 1) % _slotViews.Count]
                    .Root
                    .GetPath();
        }
    }

    private static void ActivateSlot(
        int slotIndex,
        Control root)
    {
        TaskHelper.RunSafely(
            LibrarySpeedDiceService.ActivateSlotAsync(
                slotIndex,
                root));
    }

    private static void OnSlotMouseReleased(
        int slotIndex,
        InputEvent input)
    {
        if (input is InputEventMouseButton
            {
                ButtonIndex: MouseButton.Right,
                Pressed: false,
            })
        {
            TaskHelper.RunSafely(
                LibrarySpeedDiceService.UnequipCardAsync(slotIndex));
        }
    }

    private void OnSlotFocused(
        int slotIndex,
        SlotView view)
    {
        view.IsFocused = true;
        ShowCardPreview(slotIndex, view.Root);
        RefreshViews();
    }

    private void OnSlotUnfocused(SlotView view)
    {
        view.IsFocused = false;
        NHoverTipSet.Remove(view.Root);
        RefreshViews();
    }

    private void ShowCardPreview(int slotIndex, Control owner)
    {
        if (_state == null
            || slotIndex < 0
            || slotIndex >= _state.Slots.Count)
        {
            return;
        }

        CardModel? card = _state.Slots[slotIndex].Card;
        if (card != null)
        {
            NHoverTipSet.CreateAndShow(
                owner,
                HoverTipFactory.FromCard(card),
                HoverTipAlignment.Center);
        }
    }
}
