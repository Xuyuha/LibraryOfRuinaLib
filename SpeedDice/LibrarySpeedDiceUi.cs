using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Random;

namespace Library.SpeedDice;

internal sealed partial class LibrarySpeedDiceUi : Control
{
    private const string DiceAtlasPath =
        "res://LibraryOfRuinaLib/images/ui/library_speed_dice_atlas.png";
    private const string FontPath =
        "res://themes/kreon_bold_glyph_space_one.tres";
    private const float SlotWidth = 70f;
    private const float SlotHeight = 78f;
    private const float SlotGap = 5f;

    private static readonly Rect2 NormalDiceRegion =
        new(278f, 525f, 211f, 231f);
    private static readonly Rect2 GlowDiceRegion =
        new(522f, 512f, 235f, 256f);
    private static readonly Color EquippedGlowColor =
        new(1f, 0.82f, 0.25f, 1f);
    private static readonly Color MissingTargetGlowColor =
        new(1f, 0.22f, 0.12f, 1f);

    private readonly List<SlotView> _slotViews = [];
    private NCreature? _creatureNode;
    private LibrarySpeedDiceCombatState? _state;
    private double _rouletteTimer;

    private sealed class SlotView
    {
        public required Control Root { get; init; }
        public required TextureRect Glow { get; init; }
        public required Label ValueLabel { get; init; }
    }

    public void Initialize(
        NCreature creatureNode,
        LibrarySpeedDiceCombatState state)
    {
        _creatureNode = creatureNode;
        _state = state;
        Name = "LibrarySpeedDiceUi";
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 120;
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

        SyncPosition();
        _rouletteTimer += delta;
        if (!_state.IsLocked && _rouletteTimer >= 0.075)
        {
            _rouletteTimer = 0;
            foreach (LibrarySpeedDiceSlot slot in _state.Slots)
            {
                slot.DisplayValue = Rng.Chaotic.NextInt(
                    _state.Participant.MinSpeed,
                    _state.Participant.MaxSpeed + 1);
            }
        }

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
        Font? font = ResourceLoader.Load<Font>(FontPath);
        for (int i = 0; i < _state.Slots.Count; i++)
        {
            int slotIndex = i;
            var root = new Control
            {
                Name = $"SpeedDie{slotIndex + 1}",
                Position = new Vector2(i * (SlotWidth + SlotGap), 0f),
                Size = new Vector2(SlotWidth, SlotHeight),
                MouseFilter = MouseFilterEnum.Stop,
                FocusMode = FocusModeEnum.None,
            };
            AddChild(root);

            var normal = CreateDiceTextureRect(
                "Normal",
                atlas,
                NormalDiceRegion,
                Colors.White);
            root.AddChild(normal);

            var glow = CreateDiceTextureRect(
                "Glow",
                atlas,
                GlowDiceRegion,
                EquippedGlowColor);
            glow.Visible = false;
            root.AddChild(glow);

            var valueLabel = new Label
            {
                Name = "Value",
                AnchorRight = 1f,
                AnchorBottom = 1f,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                OffsetTop = -2f,
                OffsetBottom = -5f,
                MouseFilter = MouseFilterEnum.Ignore,
                Text = _state.Participant.MinSpeed.ToString(),
            };
            if (font != null)
                valueLabel.AddThemeFontOverride("font", font);
            valueLabel.AddThemeFontSizeOverride("font_size", 34);
            valueLabel.AddThemeColorOverride(
                "font_color",
                new Color("F7E9C2"));
            valueLabel.AddThemeColorOverride(
                "font_outline_color",
                new Color("25180F"));
            valueLabel.AddThemeConstantOverride("outline_size", 10);
            root.AddChild(valueLabel);

            root.GuiInput += input => OnSlotGuiInput(slotIndex, root, input);
            root.MouseEntered += () => ShowCardPreview(slotIndex, root);
            root.MouseExited += () => NHoverTipSet.Remove(root);

            _slotViews.Add(new SlotView
            {
                Root = root,
                Glow = glow,
                ValueLabel = valueLabel,
            });
        }

        Size = new Vector2(
            Math.Max(0f, _slotViews.Count * SlotWidth
                + Math.Max(0, _slotViews.Count - 1) * SlotGap),
            SlotHeight);
        SyncPosition();
        RefreshViews();
    }

    private static TextureRect CreateDiceTextureRect(
        string name,
        Texture2D? atlas,
        Rect2 region,
        Color modulate)
    {
        Texture2D? texture = atlas == null
            ? null
            : new AtlasTexture
            {
                Atlas = atlas,
                Region = region,
                FilterClip = true,
            };
        return new TextureRect
        {
            Name = name,
            Texture = texture,
            AnchorRight = 1f,
            AnchorBottom = 1f,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
            Modulate = modulate,
        };
    }

    private void RefreshViews()
    {
        if (_state == null)
            return;

        for (int i = 0; i < _slotViews.Count && i < _state.Slots.Count; i++)
        {
            SlotView view = _slotViews[i];
            LibrarySpeedDiceSlot slot = _state.Slots[i];
            view.ValueLabel.Text =
                (_state.IsLocked ? slot.FinalValue : slot.DisplayValue).ToString();
            view.Glow.Visible = slot.Card != null;
            if (view.Glow.Visible)
            {
                float pulse = 0.72f
                    + 0.28f * (Mathf.Sin((float)Time.GetTicksMsec() * 0.006f) + 1f) * 0.5f;
                Color color =
                    slot.RequiresTarget && !slot.HasValidTarget
                        ? MissingTargetGlowColor
                        : EquippedGlowColor;
                color.A = pulse;
                view.Glow.Modulate = color;
            }

            view.Root.MouseDefaultCursorShape =
                _state.IsLocked || _state.IsResolving
                    ? CursorShape.Forbidden
                    : CursorShape.PointingHand;
        }
    }

    private void SyncPosition()
    {
        if (_creatureNode == null)
            return;

        Control hitbox = _creatureNode.Hitbox;
        GlobalPosition = new Vector2(
            hitbox.GlobalPosition.X + hitbox.Size.X * 0.5f - Size.X * 0.5f,
            hitbox.GlobalPosition.Y - SlotHeight - 18f);
    }

    private static void OnSlotGuiInput(
        int slotIndex,
        Control root,
        InputEvent input)
    {
        if (input is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
            return;

        if (mouseButton.ButtonIndex == MouseButton.Left)
        {
            TaskHelper.RunSafely(
                LibrarySpeedDiceService.ActivateSlotAsync(slotIndex, root));
            root.AcceptEvent();
        }
        else if (mouseButton.ButtonIndex == MouseButton.Right)
        {
            TaskHelper.RunSafely(
                LibrarySpeedDiceService.UnequipCardAsync(slotIndex));
            root.AcceptEvent();
        }
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
