#nullable enable
using MegaCrit.Sts2.Core.Entities.Players;

namespace Library.Models;

public readonly record struct LibraryRightClickContext(
    Player Player,
    LibraryRelicModel Relic,
    LibraryRightClickTrigger Trigger);
