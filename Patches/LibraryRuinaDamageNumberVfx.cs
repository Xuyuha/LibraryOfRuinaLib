#nullable enable

using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Godot;
using Library.Entities.Creatures;
using Library.Resistance;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.TestSupport;

namespace Library.Patches;

/// <summary>
/// Library of Ruina 风格的伤害跳字：抗性图标 + 实际数值，物理在左、混乱在右。
/// </summary>
internal sealed partial class LibraryRuinaDamageNumberVfx : Node2D
{
    private const float IconSize = 46f;
    private const float NameWidth = 96f;
    private const float NumberWidth = 116f;
    private const float TextHeight = 72f;
    private const float NameFontSize = 38f;
    private const float NumberFontSize = 58f;

    private static readonly Vector2 PositionOffset = new(0f, -118f);
    private static readonly Vector2 PhysicalSideOffset = new(-150f, 0f);
    private static readonly Vector2 ChaosSideOffset = new(150f, 0f);

    private static readonly Font? DamageFont =
        ResourceLoader.Load<Font>("res://themes/kreon_regular_glyph_space_two.tres", null, ResourceLoader.CacheMode.Reuse);
    private static readonly Regex LocColorTagRegex = new(@"\[/?(?:color(?:=[^\]]+)?|gray|blue|green|orange|red|gold)\]", RegexOptions.Compiled);
    private static readonly Regex ResistanceMultiplierRegex = new(@"\s*[\uFF08(]\s*[\u00D7xX]\s*[\d.]+\s*[\uFF09)]", RegexOptions.Compiled);

    private Tween? _tween;
    private string _text = "0";
    private string _resistanceName = "";
    private string _iconPath = "";
    private Color _fontColor = Colors.White;
    private Color _outlineColor = new("241F1B");
    private bool _isChaos;

    public static LibraryRuinaDamageNumberVfx? CreatePhysical(Creature target, DamageResult result, LibraryDamageType type)
    {
        if (type == LibraryDamageType.None)
        {
            return null;
        }

        LibraryResistanceLevel level = (target as LibraryCreature)?.GetPhysicalResistanceLevel(type)
            ?? LibraryResistanceLevel.Normal;
        int damage = result.UnblockedDamage;

        return Create(target, type, level, damage, false, PhysicalSideOffset);
    }

    public static LibraryRuinaDamageNumberVfx? CreateChaos(Creature target, LibraryChaoResult result, LibraryDamageType type)
    {
        if (type == LibraryDamageType.None)
        {
            return null;
        }

        LibraryResistanceLevel level = (target as LibraryCreature)?.GetChaosResistanceLevel(type)
            ?? LibraryResistanceLevel.Normal;
        int damage = result.ChaoValueAmount;

        return Create(target, type, level, damage, true, ChaosSideOffset);
    }

    private static LibraryRuinaDamageNumberVfx? Create(
        Creature target,
        LibraryDamageType type,
        LibraryResistanceLevel level,
        int damage,
        bool isChaos,
        Vector2 sideOffset)
    {
        if (TestMode.IsOn)
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
                + sideOffset
                + new Vector2(Rng.Chaotic.NextFloat(-8f, 8f), Rng.Chaotic.NextFloat(-5f, 5f));
        }

        var vfx = new LibraryRuinaDamageNumberVfx
        {
            GlobalPosition = globalPosition,
            _text = damage.ToString(),
            _resistanceName = GetResistanceName(level),
            _iconPath = GetIconPath(type, level, isChaos),
            _fontColor = GetFontColor(level),
            _outlineColor = GetOutlineColor(level),
            _isChaos = isChaos,
            RotationDegrees = Rng.Chaotic.NextFloat(-3f, 3f),
            Scale = Vector2.One * 1.35f
        };

        return vfx;
    }

    public override void _Ready()
    {
        Modulate = Colors.White;

        Texture2D? iconTexture = ResourceLoader.Load<Texture2D>(_iconPath, null, ResourceLoader.CacheMode.Reuse);
        _fontColor = GetIconTextColor(iconTexture, _fontColor);
        _outlineColor = GetOutlineColor(_fontColor);
        float rowWidth = IconSize + NameWidth + NumberWidth;

        var row = new Control
        {
            Name = "LibraryRuinaDamageNumber",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Size = new Vector2(rowWidth, TextHeight),
            Position = new Vector2(-rowWidth / 2f, -TextHeight / 2f),
        };
        AddChild(row);

        var icon = new TextureRect
        {
            Name = "TypeIcon",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Texture = iconTexture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.LinearWithMipmaps,
            Size = new Vector2(IconSize, IconSize),
            Position = new Vector2(0f, (TextHeight - IconSize) / 2f),
        };
        row.AddChild(icon);

        var nameLabel = CreateLabel(
            "ResistanceName",
            _resistanceName,
            new Vector2(NameWidth, TextHeight),
            new Vector2(IconSize, 0f),
            (int)NameFontSize);
        row.AddChild(nameLabel);

        var numberLabel = CreateLabel(
            "DamageLabel",
            _text,
            new Vector2(NumberWidth, TextHeight),
            new Vector2(IconSize + NameWidth, 0f),
            (int)NumberFontSize);
        row.AddChild(numberLabel);

        TaskHelper.RunSafely(AnimVfx());
    }

    private Label CreateLabel(string name, string text, Vector2 size, Vector2 position, int fontSize)
    {
        var label = new Label
        {
            Name = name,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Size = size,
            Position = position,
            PivotOffset = size / 2f,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", _fontColor);
        label.AddThemeColorOverride("font_outline_color", _outlineColor);
        label.AddThemeConstantOverride("outline_size", 14);
        label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.28f));
        label.AddThemeConstantOverride("shadow_offset_x", 4);
        label.AddThemeConstantOverride("shadow_offset_y", 4);
        if (DamageFont != null)
        {
            label.AddThemeFontOverride("font", DamageFont);
        }

        return label;
    }

    public override void _ExitTree()
    {
        _tween?.Kill();
    }

    private async Task AnimVfx()
    {
        _tween = CreateTween().SetParallel();
        _tween.TweenProperty(this, "scale", Vector2.One, 0.22).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);

        Vector2 drift = new(_isChaos ? 92f : -92f, -138f);
        _tween.TweenProperty(this, "position", Position + drift, 0.8).SetDelay(1.0).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        _tween.TweenProperty(this, "modulate:a", 0f, 0.8).SetDelay(1.0).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Sine);
        await ToSignal(_tween, Tween.SignalName.Finished);
        this.QueueFreeSafely();
    }

    private static string GetIconPath(LibraryDamageType type, LibraryResistanceLevel level, bool isChaos)
    {
        string levelStr = level.GetLocKeySuffix();
        string chaosPart = isChaos ? "_chaos" : "";
        return $"res://images/resistance/{type.String()}{chaosPart}_{levelStr}.png";
    }

    private static string GetResistanceName(LibraryResistanceLevel level)
    {
        string raw = new LocString("powers", $"DAMAGE_TYPE_RESISTANCE.{level.GetLocKeySuffix()}").GetRawText();
        string plainText = LocColorTagRegex.Replace(raw, "");
        plainText = ResistanceMultiplierRegex.Replace(plainText, "");
        return string.IsNullOrWhiteSpace(plainText) ? level.GetLocKeySuffix() : plainText;
    }

    private static Color GetFontColor(LibraryResistanceLevel level) => level switch
    {
        LibraryResistanceLevel.Immune => new Color("8FA7C8"),
        LibraryResistanceLevel.Resist => new Color("72B7FF"),
        LibraryResistanceLevel.Endure => new Color("B8C6D9"),
        LibraryResistanceLevel.Normal => new Color("FFF6E2"),
        LibraryResistanceLevel.Vulnerable => new Color("FFC15C"),
        LibraryResistanceLevel.Fatal => new Color("FF5A3D"),
        _ => new Color("FFF6E2")
    };

    private static Color GetOutlineColor(LibraryResistanceLevel level) => level switch
    {
        LibraryResistanceLevel.Immune => new Color("182435"),
        LibraryResistanceLevel.Resist => new Color("123459"),
        LibraryResistanceLevel.Endure => new Color("2C3440"),
        LibraryResistanceLevel.Normal => new Color("2B2823"),
        LibraryResistanceLevel.Vulnerable => new Color("4B2D0C"),
        LibraryResistanceLevel.Fatal => new Color("4A100A"),
        _ => new Color("2B2823")
    };

    private static Color GetIconTextColor(Texture2D? texture, Color fallback)
    {
        Image? image = texture?.GetImage();
        if (image == null)
        {
            return fallback;
        }

        double r = 0d;
        double g = 0d;
        double b = 0d;
        int count = 0;

        for (int y = 0; y < image.GetHeight(); y++)
        {
            for (int x = 0; x < image.GetWidth(); x++)
            {
                Color color = image.GetPixel(x, y);
                if (color.A < 0.2f)
                {
                    continue;
                }

                float max = Math.Max(color.R, Math.Max(color.G, color.B));
                float min = Math.Min(color.R, Math.Min(color.G, color.B));
                if (max < 0.08f || max - min < 0.03f)
                {
                    continue;
                }

                r += color.R;
                g += color.G;
                b += color.B;
                count++;
            }
        }

        if (count == 0)
        {
            return fallback;
        }

        var averaged = new Color((float)(r / count), (float)(g / count), (float)(b / count));
        float strongest = Math.Max(averaged.R, Math.Max(averaged.G, averaged.B));
        if (strongest > 0f)
        {
            averaged.R /= strongest;
            averaged.G /= strongest;
            averaged.B /= strongest;
        }

        return averaged.Lightened(0.12f);
    }

    private static Color GetOutlineColor(Color fontColor)
    {
        return fontColor.Darkened(0.78f);
    }
}
