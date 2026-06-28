#nullable enable
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;

namespace Library.Models;

public sealed class LibraryRelicRightClickAction : GameAction
{
    private readonly Player _player;
    private readonly ModelId _relicId;
    private readonly LibraryRightClickTrigger _trigger;
    private readonly GameActionType _actionType;

    public override ulong OwnerId => _player.NetId;

    public override GameActionType ActionType => _actionType;

    public LibraryRelicRightClickAction(
        Player player,
        ModelId relicId,
        LibraryRightClickTrigger trigger,
        GameActionType actionType)
    {
        _player = player;
        _relicId = relicId;
        _trigger = trigger;
        _actionType = NormalizeActionType(actionType);
    }

    internal static bool TryRequest(Player player, LibraryRelicModel relic, LibraryRightClickTrigger trigger)
    {
        GameActionType actionType = CombatManager.Instance.IsInProgress
            ? GameActionType.CombatPlayPhaseOnly
            : GameActionType.NonCombat;

        RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(
            new LibraryRelicRightClickAction(player, relic.Id, trigger, actionType));
        return true;
    }

    protected override async Task ExecuteAction()
    {
        if (!TryResolveRelic(out LibraryRelicModel relic))
        {
            return;
        }

        var choiceContext = new GameActionPlayerChoiceContext(this);
        var context = new LibraryRightClickExecutionContext(_player, relic, _trigger, choiceContext, this);
        bool shouldExecute;
        try
        {
            shouldExecute = relic.CanExecuteRightClick(context);
        }
        catch (Exception ex)
        {
            Log.Warn("[LibraryOfRuinaLib.RightClick] Relic execute guard failed. relic="
                + _relicId
                + " owner="
                + _player.NetId
                + " error="
                + ex.Message);
            return;
        }

        if (!shouldExecute)
        {
            relic.InvokeExecutionFinished();
            return;
        }

        try
        {
            await relic.OnRightClick(context);
        }
        catch (Exception ex)
        {
            Log.Warn("[LibraryOfRuinaLib.RightClick] Relic right-click action failed. relic="
                + _relicId
                + " owner="
                + _player.NetId
                + " error="
                + ex);
            return;
        }

        if (!relic.HasBeenRemovedFromState && relic.Owner == _player && _player.Relics.Contains(relic))
        {
            relic.InvokeExecutionFinished();
        }
    }

    public override INetAction ToNetAction()
    {
        return new NetLibraryRelicRightClickAction
        {
            RelicId = _relicId,
            IsController = _trigger.IsController,
            Metadata = _trigger.Metadata ?? string.Empty,
            HasMetadata = _trigger.Metadata != null,
            ActionType = _actionType
        };
    }

    public override string ToString()
    {
        return "LibraryRelicRightClickAction relic=" + _relicId + " player=" + _player.NetId;
    }

    private bool TryResolveRelic(out LibraryRelicModel relic)
    {
        relic = _player.Relics
            .OfType<LibraryRelicModel>()
            .FirstOrDefault(candidate => candidate.Id == _relicId)!;

        return relic != null
            && !relic.HasBeenRemovedFromState
            && relic.Owner == _player;
    }

    private static GameActionType NormalizeActionType(GameActionType actionType)
    {
        return actionType is GameActionType.Combat
            or GameActionType.CombatPlayPhaseOnly
            or GameActionType.NonCombat
            or GameActionType.Any
            ? actionType
            : GameActionType.CombatPlayPhaseOnly;
    }
}

public struct NetLibraryRelicRightClickAction : INetAction, IPacketSerializable
{
    public ModelId RelicId;
    public bool IsController;
    public bool HasMetadata;
    public string Metadata;
    public GameActionType ActionType;

    public GameAction ToGameAction(Player player)
    {
        return new LibraryRelicRightClickAction(
            player,
            RelicId,
            new LibraryRightClickTrigger(IsController, HasMetadata ? Metadata : null),
            ActionType);
    }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteFullModelId(RelicId);
        writer.WriteBool(IsController);
        writer.WriteBool(HasMetadata);
        if (HasMetadata)
        {
            writer.WriteString(Metadata ?? string.Empty);
        }
        writer.WriteEnum(ActionType);
    }

    public void Deserialize(PacketReader reader)
    {
        RelicId = reader.ReadFullModelId();
        IsController = reader.ReadBool();
        HasMetadata = reader.ReadBool();
        Metadata = HasMetadata ? reader.ReadString() : string.Empty;
        ActionType = reader.ReadEnum<GameActionType>();
    }

    public override string ToString()
    {
        return "NetLibraryRelicRightClickAction relic=" + RelicId;
    }
}
