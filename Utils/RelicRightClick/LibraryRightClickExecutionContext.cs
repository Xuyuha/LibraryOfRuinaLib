#nullable enable
using LibraryLib.Models;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace LibraryLib.Utils.RelicRightClick;

public readonly record struct LibraryRightClickExecutionContext(
    Player Player,
    LibraryRelicModel Relic,
    LibraryRightClickTrigger Trigger,
    GameActionPlayerChoiceContext ChoiceContext,
    GameAction Action);
