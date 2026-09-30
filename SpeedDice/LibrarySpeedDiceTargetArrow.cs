using Godot;
using MegaCrit.Sts2.Core.Helpers;

namespace LibraryLib.SpeedDice;

/// <summary>Drawing shared by the speed-dice target lines (equip, hover and right-click previews).</summary>
internal static class LibrarySpeedDiceTargetArrow
{
    internal const int SegmentCount = 50;

    /// <summary>Bends the dashes into an upward arc from <paramref name="from"/> to <paramref name="to"/> and places both markers.</summary>
    internal static void Update(
        LibrarySpeedDiceTargetDashLine dashes,
        Sprite2D startMarker,
        Sprite2D arrowHead,
        Vector2 from,
        Vector2 to)
    {
        float curveHeight = Mathf.Clamp(from.DistanceTo(to) * 0.175f, 36f, 220f);
        Vector2 control = (from + to) * 0.5f + Vector2.Up * curveHeight;
        for (int i = 0; i <= SegmentCount; i++)
            dashes.SetCurvePoint(i, MathHelper.BezierCurve(from, to, control, i / (float)SegmentCount));

        dashes.Commit();
        Vector2 previous = dashes.GetCurvePoint(SegmentCount - 1);
        startMarker.GlobalPosition = from;
        arrowHead.GlobalPosition = to;
        arrowHead.GlobalRotation = (to - previous).Angle() + Mathf.Pi * 0.5f;
    }

    internal static Texture2D? LoadTexture(string path) =>
        ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;
}
