#nullable enable

using System.Threading.Tasks;
using Godot;
using Library.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.TestSupport;

namespace Library.Patches;

/// <summary>
/// 混乱抗性恢复跳字：图标 + 实际恢复量。
/// </summary>
internal sealed partial class LibraryChaoHealNumberVfx : Node2D
{
    private const float IconSize = 48f;
    private const float NumberWidth = 120f;
    private const float Height = 76f;
    private const float FontSize = 58f;
    private const string IconPath = "res://images/vfx/library_chao_heal.png";
    private const string FontPath = "res://themes/kreon_regular_glyph_space_two.tres";

    private static readonly Vector2 PositionOffset = new(0f, -105f);
    private static readonly Vector2 IconOffset = new(-7f, 0f);
    private static readonly Color FontColor = new("F7C85A");
    private static readonly Color OutlineColor = new("3C2608");
    private static readonly Font? NumberFont =
        ResourceLoader.Load<Font>(FontPath, null, ResourceLoader.CacheMode.Reuse);

    private Tween? _tween;
    private string _text = "0";
    private Vector2 _velocity;

    public static LibraryChaoHealNumberVfx? Create(LibraryCreature target, decimal amount)
    {
        if (TestMode.IsOn || amount <= 0m)
        {
            return null;
        }

        Vector2 globalPosition;
        NCreature? creatureNode = NCombatRoom.Instance?.GetCreatureNode(target);
        if (creatureNode == null || !creatureNode.IsInteractable)
        {
            if (!LocalContext.IsMe(target))
            {
                return null;
            }

            Vector2 size = ((SceneTree)Engine.GetMainLoop()).Root.GetViewport().GetVisibleRect().Size;
            globalPosition = size * new Vector2(0.25f, 0.5f);
        }
        else
        {
            globalPosition = creatureNode.VfxSpawnPosition
                + PositionOffset
                + new Vector2(Rng.Chaotic.NextFloat(-10f, 10f), Rng.Chaotic.NextFloat(-5f, 5f));
        }

        return new LibraryChaoHealNumberVfx
        {
            GlobalPosition = globalPosition,
            _text = ((int)amount).ToString(),
            Scale = Vector2.One * Rng.Chaotic.NextFloat(1.15f, 1.25f),
            RotationDegrees = Rng.Chaotic.NextFloat(-5f, 5f),
        };
    }

    public static void Show(LibraryCreature creature, decimal amount)
    {
        if (amount <= 0m || !creature.IsMonster || !CombatManager.Instance.IsInProgress)
        {
            return;
        }

        LibraryChaoHealNumberVfx? vfx = Create(creature, amount);
        if (vfx == null)
        {
            return;
        }

        Node? vfxContainer = creature.GetVfxContainer();
        if (vfxContainer != null)
        {
            vfxContainer.AddChildSafely(vfx);
        }
        else
        {
            NRun.Instance?.GlobalUi.AddChildSafely(vfx);
        }
    }

    public override void _Ready()
    {
        float rowWidth = IconSize + NumberWidth;
        var row = new Control
        {
            Name = "LibraryChaoHealNumber",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Size = new Vector2(rowWidth, Height),
            Position = new Vector2(-rowWidth / 2f, -Height / 2f),
        };
        AddChild(row);

        row.AddChild(new TextureRect
        {
            Name = "ChaoHealIcon",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Texture = ResourceLoader.Load<Texture2D>(IconPath, null, ResourceLoader.CacheMode.Reuse),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.LinearWithMipmaps,
            Size = new Vector2(IconSize, IconSize),
            Position = new Vector2(IconOffset.X, (Height - IconSize) / 2f + IconOffset.Y),
        });

        Label label = CreateNumberLabel(new Vector2(NumberWidth, Height), new Vector2(IconSize, 0f));
        row.AddChild(label);

        _velocity = new Vector2(Rng.Chaotic.NextFloat(-90f, 90f), Rng.Chaotic.NextFloat(-520f, -280f));
        TaskHelper.RunSafely(AnimVfx());
    }

    public override void _Process(double delta)
    {
        float seconds = (float)delta;
        Position += _velocity * seconds;
        if (_velocity.LengthSquared() > 1E-06f)
        {
            _velocity -= (_velocity.Normalized() * 1800f * seconds).LimitLength(_velocity.Length());
        }
    }

    public override void _ExitTree()
    {
        _tween?.Kill();
    }

    private Label CreateNumberLabel(Vector2 size, Vector2 position)
    {
        var label = new Label
        {
            Name = "ChaoHealAmount",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Text = _text,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Size = size,
            Position = position,
        };
        label.AddThemeFontSizeOverride("font_size", (int)FontSize);
        label.AddThemeColorOverride("font_color", FontColor);
        label.AddThemeColorOverride("font_outline_color", OutlineColor);
        label.AddThemeConstantOverride("outline_size", 14);
        label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.28f));
        label.AddThemeConstantOverride("shadow_offset_x", 4);
        label.AddThemeConstantOverride("shadow_offset_y", 4);
        if (NumberFont != null)
        {
            label.AddThemeFontOverride("font", NumberFont);
        }

        return label;
    }

    private async Task AnimVfx()
    {
        _tween = CreateTween().SetParallel();
        _tween.TweenProperty(this, "modulate:a", 0f, 0.3).SetDelay(1.0).SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Quad);
        _tween.TweenProperty(this, "scale", Vector2.One, 0.5).SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad).From(Vector2.One * 2.2f);
        await ToSignal(_tween, Tween.SignalName.Finished);
        this.QueueFreeSafely();
    }
}
