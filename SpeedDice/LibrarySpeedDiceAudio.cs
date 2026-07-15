using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes;

namespace Library.SpeedDice;

internal static class LibrarySpeedDiceAudio
{
    private const string FingerSnapPath =
        "res://LibraryOfRuinaLib/audio/sfx/finger_snap.ogg";

    private static AudioStream? _fingerSnap;
    private static bool _missingAssetLogged;
    private static int _nextPlayerId;

    public static void PlayAdvance()
    {
        _fingerSnap ??= ResourceLoader.Load<AudioStream>(
            FingerSnapPath,
            null,
            ResourceLoader.CacheMode.Reuse);
        if (_fingerSnap == null)
        {
            if (!_missingAssetLogged)
            {
                Log.Error(
                    "[LibraryOfRuinaLib] Unable to load speed-dice "
                    + $"finger snap SFX: {FingerSnapPath}");
                _missingAssetLogged = true;
            }

            return;
        }

        Node? host = NRun.Instance ?? (Node?)NGame.Instance;
        if (host == null)
            return;

        var player = new AudioStreamPlayer
        {
            Name = $"LibrarySpeedDiceFingerSnap{_nextPlayerId++}",
            Bus = "SFX",
            Stream = _fingerSnap,
        };
        host.AddChild(player);
        player.Finished += () =>
        {
            if (GodotObject.IsInstanceValid(player))
                player.QueueFree();
        };
        player.Play();
    }
}
