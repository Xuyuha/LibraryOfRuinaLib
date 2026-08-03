#nullable enable
using LibraryLib.Models;
using MegaCrit.Sts2.Core.Entities.Players;

namespace LibraryLib.Utils.RelicRightClick;

public readonly record struct LibraryRightClickContext(
    Player Player,
    LibraryRelicModel Relic,
    LibraryRightClickTrigger Trigger);
