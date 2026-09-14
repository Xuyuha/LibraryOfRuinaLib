using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace LibraryLib.Models;

/// <summary>明确标记复活、转阶段行动，使其能越过混乱锁且免于被混乱覆盖。</summary>
public sealed class LibraryPhaseTransitionMoveState(
    string stateId,
    Func<IReadOnlyList<Creature>, Task> onPerform,
    params AbstractIntent[] intents)
    : MoveState(stateId, onPerform, intents);
