using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Library.SpeedDice;

internal static class LibrarySpeedDiceInputMode
{
    public static bool ShouldUseControllerTargeting(
        bool controllerRequested)
    {
        return controllerRequested
            && (OS.GetName() != "Android"
                || Input.GetConnectedJoypads().Count > 0);
    }

    public static TargetMode ResolveTargetMode(
        bool controllerRequested)
    {
        return ShouldUseControllerTargeting(controllerRequested)
            ? TargetMode.Controller
            : TargetMode.ClickMouseToTarget;
    }
}
