#nullable enable
using System;
using System.Collections.Generic;

namespace LibraryLib.SpeedDice;

/// <summary>
/// Read-only emotion state that can reuse the Library emotion bar even when
/// the owning creature is not a player registered with the speed-dice system.
/// </summary>
public interface ILibraryEmotionBarSource
{
    int EmotionLevel { get; }

    int EmotionUnits { get; }

    IReadOnlyList<int> EmotionUnitThresholds { get; }

    event Action? EmotionChanged;
}
