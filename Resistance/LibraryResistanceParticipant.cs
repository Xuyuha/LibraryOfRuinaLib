#nullable enable
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;

namespace Library.Resistance;

/// <summary>接入抗性的角色 mod：判定「是否为本 mod 玩家」及系数变更后的可选 UI 刷新。</summary>
public sealed class LibraryResistanceParticipant
{
    public required string Id { get; init; }

    public required Func<Player?, bool> IsRelevantPlayer { get; init; }

    /// <summary>抗性系数被卡牌等效果修改后调用（如手牌伤害预览）；可为 null。</summary>
    public Action<ICombatState>? OnCoefficientsChanged { get; init; }
}
