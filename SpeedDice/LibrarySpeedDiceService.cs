using System.Runtime.CompilerServices;
using Godot;
using LibraryLib.Light;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace LibraryLib.SpeedDice;

/// <summary>
/// 速度骰子的运行时服务：每名玩家的战斗状态、注册与快照。其余职责按主题分在
/// LibrarySpeedDiceService.{Turn,Input,Reservation,Emotion}.cs。
/// </summary>
internal static partial class LibrarySpeedDiceService
{
    private readonly record struct ExplicitSelection(
        CardModel Card,
        string SourceId);

    private enum AdvanceAction
    {
        Roll,
        Resolve,
    }

    private static readonly Lock Sync = new();

    private static readonly List<LibrarySpeedDiceRegistration> Registrations =
        [];

    private static ConditionalWeakTable<Player, LibrarySpeedDiceCombatState> States = new();

    private static WeakReference<LibrarySpeedDiceCombatState>? _localState;

    private static ExplicitSelection? _explicitSelection;

    public static void RegisterParticipant(LibrarySpeedDiceParticipant participant)
    {
        ArgumentNullException.ThrowIfNull(participant);
        participant.Validate();

        var registration = new LibrarySpeedDiceRegistration(
            participant.Id,
            participant.IsEnabledForPlayer,
            new LibrarySpeedDiceOptions(
                participant.BaseSpeedDiceCount,
                participant.MinSpeed,
                participant.MaxSpeed),
            participant.Emotion,
            light: null,
            lightStoreFactory: null,
            [new LegacyParticipantAdapter(participant)],
            participant,
            participant);
        RegisterRegistration(registration, replaceExisting: true);
    }

    internal static void RegisterRegistration(
        LibrarySpeedDiceRegistration registration,
        bool replaceExisting)
    {
        ArgumentNullException.ThrowIfNull(registration);
        lock (Sync)
        {
            int existingIndex = Registrations.FindIndex(candidate =>
                string.Equals(
                    candidate.Id,
                    registration.Id,
                    StringComparison.Ordinal));
            if (existingIndex >= 0 && !replaceExisting)
            {
                throw new InvalidOperationException(
                    $"Speed-dice registration '{registration.Id}' already exists.");
            }

            if (existingIndex >= 0)
                Registrations.RemoveAt(existingIndex);
            Registrations.Add(registration);
        }
    }

    public static bool TryGetState(
        Player player,
        out LibrarySpeedDiceCombatState? state)
    {
        state = null;
        if (player.PlayerCombatState == null)
            return false;

        LibrarySpeedDiceRegistration? registration =
            FindRegistration(player);
        if (registration == null)
            return false;

        if (!States.TryGetValue(player, out state))
        {
            state = new LibrarySpeedDiceCombatState(
                player,
                registration);
            state.ReplaceSlots(GetDiceCount(state));
            States.Add(player, state);
            registration.Dispatcher.OnStateCreated(state);
        }

        if (LocalContext.IsMe(player))
            _localState = new WeakReference<LibrarySpeedDiceCombatState>(state);

        return true;
    }

    public static bool TryGetLocalState(out LibrarySpeedDiceCombatState? state)
    {
        state = null;
        return _localState != null
            && _localState.TryGetTarget(out state)
            && IsStateUsable(state);
    }

    public static bool TryGetEquippedSlot(
        CardModel card,
        out LibrarySpeedDiceSlot? slot)
    {
        slot = null;
        var owner = card.Owner;
        if (owner == null
            || !TryGetState(owner, out var state)
            || state == null)
        {
            return false;
        }

        slot = state.Slots.FirstOrDefault(candidate =>
            ReferenceEquals(candidate.Card, card));
        return slot != null;
    }

    public static bool TryGetResolvingSlot(
        CardModel card,
        out LibrarySpeedDiceSlot? slot)
    {
        slot = null;
        var owner = card.Owner;
        if (owner == null
            || !TryGetState(owner, out var state)
            || state?.ResolvingSlot == null
            || !ReferenceEquals(state.ResolvingSlot.Card, card))
        {
            return false;
        }

        slot = state.ResolvingSlot;
        return true;
    }

    public static LibrarySpeedDiceStateSnapshot CreateSnapshot(
        LibrarySpeedDiceCombatState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return new LibrarySpeedDiceStateSnapshot(
            state.Player.PlayerCombatState?.TurnNumber ?? -1,
            state.Revision,
            state.HasRolled,
            state.IsLocked,
            state.CurrentTurnTriggeredCards,
            state.PreviousTurnTriggeredCards,
            state.BonusDrawPending,
            state.DamageGivenAccumulator,
            state.DamageReceivedAccumulator,
            state.Emotion.Level,
            state.Emotion.Units,
            state.Slots.Select(slot =>
            {
                LibrarySpeedDiceCardLease? lease = slot.Lease;
                return new LibrarySpeedDiceSlotSnapshot(
                    slot.Index,
                    slot.DisplayValue,
                    slot.FinalValue,
                    slot.IsLocked,
                    slot.IsSpent,
                    slot.Card,
                    slot.Target,
                    slot.ReservedEnergy,
                    slot.ReservedStars,
                    slot.ReservedSecondaryResources.ToDictionary(
                        pair => pair.Key,
                        pair => pair.Value,
                        StringComparer.Ordinal))
                {
                    Lease = lease == null
                        ? null
                        : new LibrarySpeedDiceLeaseSnapshot(
                            lease.Id,
                            lease.ReservationPlan.Resources.ToArray(),
                            lease.IsUseTriggered,
                            lease.IsTargetedUseTriggered,
                            lease.PreventUnequip,
                            lease.IsCommitted),
                };
            }).ToArray())
        {
            Extension = new LibrarySpeedDiceSnapshotExtension
            {
                LeaseSequence = state.LeaseSequence,
                PendingEmotionPreviousLevel =
                    state.PendingEmotionPreviousLevel,
                PendingEmotionCurrentLevel =
                    state.PendingEmotionCurrentLevel,
                DamageGivenAccumulatorThreshold =
                    state.DamageGivenAccumulatorThreshold,
                DamageReceivedAccumulatorThreshold =
                    state.DamageReceivedAccumulatorThreshold,
                Light = state.Light?.CreateSnapshot(),
            },
        };
    }

    public static bool TryRestoreSnapshot(
        Player player,
        LibrarySpeedDiceStateSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!TryGetState(player, out LibrarySpeedDiceCombatState? state)
            || state == null
            || player.PlayerCombatState?.TurnNumber != snapshot.TurnNumber
            || state.IsLifecycleBusy
            || !state.Gate.Wait(0))
        {
            return false;
        }

        try
        {
            state.Restore(snapshot);
            if (snapshot.Extension?.Light != null)
                state.Light?.Restore(snapshot.Extension.Light);

            foreach (LibrarySpeedDiceSlotSnapshot savedSlot
                     in snapshot.Slots.OrderBy(slot => slot.Index))
            {
                LibrarySpeedDiceSlot? slot = state.Slots.FirstOrDefault(
                    candidate => candidate.Index == savedSlot.Index);
                if (slot?.Card != null)
                    RestoreLease(state, slot, savedSlot);
            }

            state.Revision = Math.Max(0, snapshot.Revision);
            state.PublishGameplayChangedWithoutRevision();
            return true;
        }
        finally
        {
            state.Gate.Release();
        }
    }

    public static void ClearCombat()
    {
        _explicitSelection = null;
        States = new ConditionalWeakTable<Player, LibrarySpeedDiceCombatState>();
        _localState = null;
        LibraryLight.ClearCombatCosts();
    }

    private static void RestoreLease(
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot,
        LibrarySpeedDiceSlotSnapshot snapshot)
    {
        CardModel card = slot.Card!;
        LibrarySpeedDiceResourceReservation[] resources;
        string leaseId;
        bool useTriggered = false;
        bool targetedUseTriggered = false;
        bool preventUnequip = false;
        bool committed = false;

        if (snapshot.Lease != null)
        {
            resources = snapshot.Lease.Resources.ToArray();
            leaseId = snapshot.Lease.Id;
            useTriggered = snapshot.Lease.IsUseTriggered;
            targetedUseTriggered =
                snapshot.Lease.IsTargetedUseTriggered;
            preventUnequip = snapshot.Lease.PreventUnequip;
            committed = snapshot.Lease.IsCommitted;
        }
        else
        {
            var restored =
                new List<LibrarySpeedDiceResourceReservation>();
            if (snapshot.ReservedEnergy > 0)
            {
                restored.Add(
                    new LibrarySpeedDiceResourceReservation(
                        "energy",
                        snapshot.ReservedEnergy,
                        LibrarySpeedDiceResourceKind.Energy));
            }
            if (snapshot.ReservedStars > 0)
            {
                restored.Add(
                    new LibrarySpeedDiceResourceReservation(
                        "stars",
                        snapshot.ReservedStars,
                        LibrarySpeedDiceResourceKind.Stars));
            }
            foreach ((string resourceId, int amount)
                     in snapshot.ReservedSecondaryResources)
            {
                LibrarySpeedDiceResourceKind kind =
                    state.Light != null
                    && card is ILibraryLightCard
                    && string.Equals(
                        resourceId,
                        state.Light.ReservationResourceId,
                        StringComparison.Ordinal)
                        ? LibrarySpeedDiceResourceKind.Light
                        : LibrarySpeedDiceResourceKind.LegacySecondary;
                restored.Add(
                    new LibrarySpeedDiceResourceReservation(
                        resourceId,
                        Math.Max(0, amount),
                        kind));
            }

            resources = restored.ToArray();
            leaseId =
                $"{state.Registration.Id}:{state.Player.NetId}:"
                + $"{snapshot.Index}:restored";
        }

        var plan = new LibrarySpeedDiceReservationPlan(resources);
        int light = plan.GetAmount(
            LibrarySpeedDiceResourceKind.Light);
        if (!committed
            && card is ILibraryLightCard
            && state.Light != null
            && !state.Light.TryReserve(leaseId, light))
        {
            throw new InvalidOperationException(
                $"Unable to restore Light reservation for lease '{leaseId}'.");
        }

        IReadOnlyDictionary<string, int> legacySecondary =
            plan.Resources
                .Where(resource =>
                    resource.Kind
                        == LibrarySpeedDiceResourceKind.LegacySecondary)
                .ToDictionary(
                    resource => resource.ResourceId,
                    resource => resource.Amount,
                    StringComparer.Ordinal);
        var lease = new LibrarySpeedDiceCardLease(
            leaseId,
            card,
            plan,
            CreateReservationTransaction(
                state,
                card,
                leaseId,
                plan,
                light,
                legacySecondary))
        {
            IsUseTriggered = useTriggered,
            IsTargetedUseTriggered = targetedUseTriggered,
            IsCommitted = committed,
        };
        if (preventUnequip)
            lease.LockUnequip();
        slot.SetLease(lease);
        ApplyLegacyReservationProjection(state, card, slot);
    }

    public static void NotifyParticipantStateChanged(
        LibrarySpeedDiceCombatState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        state.Light?.RefreshMaximum();
        state.NotifyGameplayChanged();
    }

    private static int GetDiceCount(LibrarySpeedDiceCombatState state)
    {
        var extra = state.Emotion.Level
                    >= state.Registration.Emotion.ExtraSpeedDieLevel
            ? state.Registration.Emotion.ExtraSpeedDice
            : 0;
        int count = state.Registration.Options.BaseCount + extra;
        count = state.Registration.Dispatcher.ModifySpeedDiceCount(
            state,
            count);

        return Math.Max(0, count);
    }

    public static void RefreshSlotCount(Player player, bool rollNewSlots)
    {
        if (!TryGetState(player, out LibrarySpeedDiceCombatState? state)
            || state == null
            || state.IsResolving)
        {
            Log.Info(
                "[LibraryOfRuinaLib] [DEBUG-speed-ui-v3] refresh-skipped "
                + $"hasState={state != null} "
                + $"resolving={state?.IsResolving ?? false}");
            return;
        }

        int requested = GetDiceCount(state);
        Log.Info(
            "[LibraryOfRuinaLib] [DEBUG-speed-ui-v3] refresh "
            + $"requested={requested} before={state.Slots.Count} "
            + $"rollNew={rollNewSlots}");
        state.EnsureSlotCount(requested, rollNewSlots);
    }

    private static LibrarySpeedDiceRegistration? FindRegistration(
        Player player)
    {
        lock (Sync)
        {
            return Registrations.FirstOrDefault(registration =>
            {
                try
                {
                    return registration.IsEnabledForPlayer(player);
                }
                catch (Exception exception)
                {
                    Log.Error(
                        $"[LibraryOfRuinaLib] Participant {registration.Id} predicate failed: {exception}");
                    return false;
                }
            });
        }
    }

    /// <summary>In multiplayer, a registration with an input router sends player input over the network.</summary>
    private static bool RoutesInputThroughNetwork(LibrarySpeedDiceCombatState state) =>
        state.Registration.Dispatcher.HasInputRouter
        && RunManager.Instance.NetService.Type != NetGameType.Singleplayer;

    private static bool IsStateUsable(LibrarySpeedDiceCombatState state)
    {
        return state.Player.PlayerCombatState != null
            && state.Player.Creature.CombatState != null
            && CombatManager.Instance.IsInProgress;
    }
}
