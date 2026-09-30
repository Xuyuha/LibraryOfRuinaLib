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

/// <summary>资源预定：装备卡牌时预留能量/光芒/Light 等资源，结算或卸下时释放。</summary>
internal static partial class LibrarySpeedDiceService
{
    /// <summary>
    /// 应用速度骰子资源预定的限制：如果其他速度骰子已经预定了部分能量/光芒，
    /// 则当前卡必须用剩余资源支付。能量不足时可用光芒补足缺口（1:2比率），
    /// 设置对应的UnplayableReason。
    /// </summary>
    public static void ApplyReservedResourceRestriction(
        CardModel card,
        ref UnplayableReason reason,
        ref bool result)
    {
        Player? owner = card.Owner;
        if (owner == null
            || !TryGetState(owner, out var state)
            || state == null
            || state.ReservedEnergy <= 0 && state.ReservedStars <= 0)
        {
            return;
        }

        var resources = owner.PlayerCombatState;
        if (resources == null)
            return;
        // 可用资源 = 总资源 - 已被其他速度骰子预定的部分
        var energyAvailable = Math.Max(0, resources.Energy - state.ReservedEnergy);
        var starsAvailable = Math.Max(0, resources.Stars - state.ReservedStars);
        var energyCost = Math.Max(0, card.EnergyCost.GetWithModifiers(CostModifiers.All));
        var starCost = Math.Max(0, card.GetStarCostWithModifiers());

        // 能量不足时，用光芒补足缺口
        // 兑换比率：1点能量缺口 = 2点光芒额外消耗
        if (energyCost > energyAvailable
            && card.CombatState != null
            && Hook.ShouldPayExcessEnergyCostWithStars(
                card.CombatState,
                owner))
        {
            starCost += (energyCost - energyAvailable) * 2;
            energyCost = energyAvailable;
        }

        if (energyCost > energyAvailable)
            reason |= UnplayableReason.EnergyCostTooHigh;
        if (starCost > starsAvailable)
            reason |= UnplayableReason.StarCostTooHigh;
        result = reason == UnplayableReason.None;
    }

    private static bool TryGetLegacySecondaryResourceReservations(
        LibrarySpeedDiceCombatState state,
        CardModel card,
        out IReadOnlyDictionary<string, int> reservations)
    {
        reservations = new Dictionary<string, int>(
            StringComparer.Ordinal);
        LibrarySpeedDiceParticipant? legacy =
            state.Registration.LegacyParticipant;
        if (legacy?.GetSecondaryResourceReservations == null)
            return true;

        try
        {
            IReadOnlyDictionary<string, int>? result =
                legacy.GetSecondaryResourceReservations(
                    state,
                    card);
            if (result == null)
                return false;

            reservations = result
                .Where(pair =>
                    !string.IsNullOrWhiteSpace(pair.Key)
                    && pair.Value > 0)
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value,
                    StringComparer.Ordinal);
            return true;
        }
        catch (Exception exception)
        {
            Log.Error(
                $"[LibraryOfRuinaLib] Participant {state.Registration.Id} secondary-resource reservation failed: {exception}");
            return false;
        }
    }

    private static bool CanReserveCard(
        LibrarySpeedDiceCombatState state,
        CardModel card)
    {
        return TryCalculateReservation(
                state,
                card,
                out _,
                out _)
            && TryGetLegacySecondaryResourceReservations(
                state,
                card,
                out _)
            && TryCalculateLightReservation(
                state,
                card,
                out _);
    }

    private static bool TryCreateReservationLease(
        LibrarySpeedDiceCombatState state,
        CardModel card,
        LibrarySpeedDiceSlot slot,
        out LibrarySpeedDiceCardLease? lease)
    {
        lease = null;
        if (!TryCalculateReservation(
                state,
                card,
                out int energy,
                out int stars)
            || !TryGetLegacySecondaryResourceReservations(
                state,
                card,
                out IReadOnlyDictionary<string, int> legacySecondary)
            || !TryCalculateLightReservation(
                state,
                card,
                out int light))
        {
            return false;
        }

        string leaseId = state.CreateLeaseId(slot.Index);
        var resources = new List<LibrarySpeedDiceResourceReservation>();
        if (energy > 0)
        {
            resources.Add(
                new LibrarySpeedDiceResourceReservation(
                    "energy",
                    energy,
                    LibrarySpeedDiceResourceKind.Energy));
        }
        if (stars > 0)
        {
            resources.Add(
                new LibrarySpeedDiceResourceReservation(
                    "stars",
                    stars,
                    LibrarySpeedDiceResourceKind.Stars));
        }

        if (card is ILibraryLightCard)
        {
            if (state.Light == null
                || !state.Light.TryReserve(leaseId, light))
            {
                return false;
            }

            if (light > 0)
            {
                resources.Add(
                    new LibrarySpeedDiceResourceReservation(
                        state.Light.ReservationResourceId,
                        light,
                        LibrarySpeedDiceResourceKind.Light));
            }
        }

        foreach ((string resourceId, int amount) in legacySecondary)
        {
            resources.Add(
                new LibrarySpeedDiceResourceReservation(
                    resourceId,
                    amount,
                    LibrarySpeedDiceResourceKind.LegacySecondary));
        }

        try
        {
            var plan = new LibrarySpeedDiceReservationPlan(resources);
            LibrarySpeedDiceReservationTransaction transaction =
                CreateReservationTransaction(
                    state,
                    card,
                    leaseId,
                    plan,
                    light,
                    legacySecondary);
            lease = new LibrarySpeedDiceCardLease(
                leaseId,
                card,
                plan,
                transaction);
            return true;
        }
        catch (Exception exception)
        {
            state.Light?.ReleaseReservation(leaseId);
            Log.Error(
                $"[LibraryOfRuinaLib] Participant {state.Registration.Id} could not freeze a reservation plan: {exception}");
            return false;
        }
    }

    private static bool TryCalculateLightReservation(
        LibrarySpeedDiceCombatState state,
        CardModel card,
        out int amount)
    {
        amount = 0;
        if (card is not ILibraryLightCard)
            return true;
        if (state.Light == null)
            return false;

        try
        {
            amount = Math.Max(
                0,
                LibraryLight.GetCost(card).GetAmountToSpend());
            return state.Light.HasEnoughAvailable(amount);
        }
        catch (Exception exception)
        {
            Log.Error(
                $"[LibraryOfRuinaLib] Participant {state.Registration.Id} Light reservation failed: {exception}");
            amount = 0;
            return false;
        }
    }

    private static LibrarySpeedDiceReservationTransaction
        CreateReservationTransaction(
            LibrarySpeedDiceCombatState state,
            CardModel card,
            string leaseId,
            LibrarySpeedDiceReservationPlan plan,
            int frozenLight,
            IReadOnlyDictionary<string, int> legacySecondary)
    {
        int energy = plan.ReservedEnergy;
        int stars = plan.ReservedStars;
        bool energyCommitted = false;
        bool starsCommitted = false;
        int previousLastStarsSpent = card.LastStarsSpent;
        var commitments =
            new List<LibrarySpeedDiceReservationCommitment>
            {
                new()
                {
                    ResourceId = "20.energy",
                    PreflightAsync = () => Task.FromResult(
                        state.Player.PlayerCombatState!.Energy >= energy),
                    CommitAsync = () =>
                    {
                        if (energy > 0)
                        {
                            if (state.Player.PlayerCombatState!.Energy
                                < energy)
                            {
                                return Task.FromResult(false);
                            }

                            state.Player.PlayerCombatState!
                                .LoseEnergy(energy);
                            energyCommitted = true;
                        }
                        return Task.FromResult(true);
                    },
                    RollbackAsync = () =>
                    {
                        if (energyCommitted)
                        {
                            state.Player.PlayerCombatState!
                                .GainEnergy(energy);
                            energyCommitted = false;
                        }
                        return Task.CompletedTask;
                    },
                    FinalizeAsync = async () =>
                    {
                        if (energy > 0)
                        {
                            CombatManager.Instance.History.EnergySpent(
                                card.CombatState!,
                                energy,
                                card.Owner);
                        }
                        await Hook.AfterEnergySpent(
                            card.CombatState!,
                            card,
                            energy);
                    },
                },
                new()
                {
                    ResourceId = "30.stars",
                    PreflightAsync = () => Task.FromResult(
                        state.Player.PlayerCombatState!.Stars >= stars),
                    CommitAsync = () =>
                    {
                        if (stars > 0
                            && state.Player.PlayerCombatState!.Stars
                            < stars)
                        {
                            return Task.FromResult(false);
                        }

                        card.LastStarsSpent = stars;
                        if (stars > 0)
                        {
                            state.Player.PlayerCombatState!
                                .LoseStars(stars);
                        }
                        starsCommitted = true;
                        return Task.FromResult(true);
                    },
                    RollbackAsync = () =>
                    {
                        if (starsCommitted)
                        {
                            if (stars > 0)
                            {
                                state.Player.PlayerCombatState!
                                    .GainStars(stars);
                            }
                            card.LastStarsSpent =
                                previousLastStarsSpent;
                            starsCommitted = false;
                        }
                        return Task.CompletedTask;
                    },
                    FinalizeAsync = () => stars > 0
                        ? Hook.AfterStarsSpent(
                            card.Owner.Creature.CombatState!,
                            stars,
                            card.Owner)
                        : Task.CompletedTask,
                },
            };

        if (card is ILibraryLightCard && state.Light != null)
        {
            LibraryLightState lightState = state.Light;
            bool lightCommitted = false;
            commitments.Add(
                new LibrarySpeedDiceReservationCommitment
                {
                    ResourceId = "80.light",
                    PreflightAsync = () => Task.FromResult(
                        lightState.HasReservation(
                            leaseId,
                            frozenLight)
                        && lightState.Current >= frozenLight),
                    CommitAsync = async () =>
                    {
                        lightCommitted =
                            await lightState.CommitReservation(
                                leaseId,
                                frozenLight,
                                card);
                        return lightCommitted;
                    },
                    RollbackAsync = async () =>
                    {
                        if (!lightCommitted)
                            return;

                        if (!await lightState.RestoreCommittedReservation(
                                leaseId,
                                frozenLight,
                                card))
                        {
                            throw new InvalidOperationException(
                                "Failed to restore a committed Light reservation.");
                        }
                        lightCommitted = false;
                    },
                    FinalizeAsync = () =>
                    {
                        LibraryLightCost cost =
                            LibraryLight.GetCost(card);
                        if (cost.CostsX)
                            cost.CapturedXValue = frozenLight;
                        return Task.CompletedTask;
                    },
                    Release = () =>
                        lightState.ReleaseReservation(leaseId),
                });
        }

        LibrarySpeedDiceParticipant? legacy =
            state.Registration.LegacyParticipant;
        if (legacy?.CommitSecondaryResourcesAsync != null)
        {
            commitments.Add(
                new LibrarySpeedDiceReservationCommitment
                {
                    ResourceId = "90.legacy-secondary",
                    PreflightAsync = () => Task.FromResult(true),
                    CommitAsync = () =>
                        legacy.CommitSecondaryResourcesAsync(
                            state,
                            card,
                            legacySecondary),
                    RollbackAsync = static () => Task.CompletedTask,
                });
        }

        return new LibrarySpeedDiceReservationTransaction(commitments);
    }

    private static void ApplyLegacyReservationProjection(
        LibrarySpeedDiceCombatState state,
        CardModel card,
        LibrarySpeedDiceSlot slot)
    {
        LibrarySpeedDiceParticipant? legacy =
            state.Registration.LegacyParticipant;
        if (legacy?.OnSecondaryResourcesReserved == null)
            return;

        IReadOnlyDictionary<string, int> reservations =
            slot.Lease?.ReservationPlan.Resources
                .Where(resource =>
                    resource.Kind
                        == LibrarySpeedDiceResourceKind.LegacySecondary)
                .ToDictionary(
                    resource => resource.ResourceId,
                    resource => resource.Amount,
                    StringComparer.Ordinal)
            ?? new Dictionary<string, int>(StringComparer.Ordinal);
        legacy.OnSecondaryResourcesReserved(card, slot, reservations);
    }

    private static void ReleaseSlotReservation(
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot)
    {
        state.Registration.LegacyParticipant?
            .OnSecondaryResourceReservationsReleased?.Invoke(slot);
        slot.ClearReservation();
    }

    private static void ReleaseSlotCard(
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot)
    {
        CardModel? card = slot.Card;
        LibrarySpeedDiceCardLease? lease = slot.Lease;
        ReleaseSlotReservation(state, slot);
        if (card != null)
        {
            state.Registration.Dispatcher.OnCardReleased(
                state,
                slot,
                card,
                lease);
        }
        slot.ClearCard();
    }

    /// <summary>
    /// 计算一张卡在速度骰子系统中所需的能量和光芒，并返回当前资源是否足够
    /// </summary>
    /// <param name="state">当前速度骰子战斗状态</param>
    /// <param name="card">要计算的卡牌</param>
    /// <param name="energy">输出：实际需要的能量（可能被光芒补足后降低）</param>
    /// <param name="stars">输出：实际需要的光芒（可能因补足能量而增加）</param>
    /// <returns>当前可用资源是否足够</returns>
    private static bool TryCalculateReservation(
        LibrarySpeedDiceCombatState state,
        CardModel card,
        out int energy,
        out int stars)
    {
        LibrarySpeedDiceParticipant? legacy =
            state.Registration.LegacyParticipant;
        if (legacy?.GetPrimaryResourceReservation != null)
        {
            try
            {
                LibrarySpeedDicePrimaryResourceReservation? reservation =
                    legacy.GetPrimaryResourceReservation(
                        state,
                        card);
                if (reservation == null)
                {
                    energy = 0;
                    stars = 0;
                    return false;
                }

                energy = Math.Max(0, reservation.Value.Energy);
                stars = Math.Max(0, reservation.Value.Stars);
                int availableEnergy = Math.Max(
                    0,
                    state.Player.PlayerCombatState!.Energy
                    - state.ReservedEnergy);
                int availableStars = Math.Max(
                    0,
                    state.Player.PlayerCombatState!.Stars
                    - state.ReservedStars);
                return energy <= availableEnergy
                    && stars <= availableStars;
            }
            catch (Exception exception)
            {
                Log.Error(
                    $"[LibraryOfRuinaLib] Participant {state.Registration.Id} primary-resource reservation failed: {exception}");
                energy = 0;
                stars = 0;
                return false;
            }
        }

        // ReSharper disable once SuspiciousTypeConversion.Global
        var hasCustomCost = card is ILibrarySpeedDiceCard;
        // ReSharper disable once SuspiciousTypeConversion.Global
        // 获取卡的费用：如果实现了ILibrarySpeedDiceCard则用自定义速度骰子费用，否则用标准修饰符计算
        if (card is ILibrarySpeedDiceCard speedDiceCard)
        {
            energy = Math.Max(0, speedDiceCard.SpeedDiceResourceCost.Energy);
            stars = Math.Max(0, speedDiceCard.SpeedDiceResourceCost.Stars);
        }
        else
        {
            energy = Math.Max(0, card.EnergyCost.GetWithModifiers(CostModifiers.All));
            stars = Math.Max(0, card.GetStarCostWithModifiers());
        }

        // 当前可用资源 = 总资源 - 已被其他速度骰子预定的部分
        var energyAvailable = Math.Max(
            0,
            state.Player.PlayerCombatState!.Energy - state.ReservedEnergy);
        var starsAvailable = Math.Max(
            0,
            state.Player.PlayerCombatState!.Stars - state.ReservedStars);

        // 能量不足时，用星光补足缺口（仅非自定义费用卡，且钩子允许时）
        // 兑换比率：1点能量缺口 = 2点额外消耗
        if (!hasCustomCost
            && energy > energyAvailable
            && card.CombatState != null
            && Hook.ShouldPayExcessEnergyCostWithStars(card.CombatState, card.Owner))
        {
            stars += (energy - energyAvailable) * 2;
            energy = energyAvailable;
        }

        return energy <= energyAvailable && stars <= starsAvailable;
    }
}
