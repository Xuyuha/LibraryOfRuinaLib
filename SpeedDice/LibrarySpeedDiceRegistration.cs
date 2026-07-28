using Godot;
using Library.Light;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace Library.SpeedDice;

internal sealed class LibrarySpeedDiceRegistration
{
    public LibrarySpeedDiceRegistration(
        string id,
        Func<Player, bool> isEnabledForPlayer,
        LibrarySpeedDiceOptions options,
        LibraryEmotionConfig emotion,
        LibraryLightOptions? light,
        LibraryLightStoreFactory? lightStoreFactory,
        IEnumerable<ILibrarySpeedDiceModule> modules,
        LibrarySpeedDiceParticipant compatibilityParticipant,
        LibrarySpeedDiceParticipant? legacyParticipant = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(isEnabledForPlayer);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(emotion);
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(compatibilityParticipant);

        options.Validate();
        LibrarySpeedDiceParticipant.ValidateEmotion(emotion);
        light?.Validate();

        Id = id;
        IsEnabledForPlayer = isEnabledForPlayer;
        Options = options;
        Emotion = FreezeEmotion(emotion);
        Light = light;
        LightStoreFactory = lightStoreFactory;
        Dispatcher = new LibrarySpeedDiceModuleDispatcher(modules);
        CompatibilityParticipant = compatibilityParticipant;
        LegacyParticipant = legacyParticipant;
    }

    public string Id { get; }

    public Func<Player, bool> IsEnabledForPlayer { get; }

    public LibrarySpeedDiceOptions Options { get; }

    public LibraryEmotionConfig Emotion { get; }

    public LibraryLightOptions? Light { get; }

    public LibraryLightStoreFactory? LightStoreFactory { get; }

    public LibrarySpeedDiceModuleDispatcher Dispatcher { get; }

    public LibrarySpeedDiceParticipant CompatibilityParticipant { get; }

    public LibrarySpeedDiceParticipant? LegacyParticipant { get; }

    internal static LibraryEmotionConfig FreezeEmotion(
        LibraryEmotionConfig emotion)
    {
        ArgumentNullException.ThrowIfNull(emotion);
        return new LibraryEmotionConfig
        {
            UnitThresholds = Array.AsReadOnly(
                emotion.UnitThresholds.ToArray()),
            GainEmotionFromDamage = emotion.GainEmotionFromDamage,
            DamageUnitFractionOfMaxHp =
                emotion.DamageUnitFractionOfMaxHp,
            ExtremeRollEmotionUnits =
                emotion.ExtremeRollEmotionUnits,
            KillEmotionUnits = emotion.KillEmotionUnits,
            AllyDeathEmotionUnits = emotion.AllyDeathEmotionUnits,
            MaxEnergyPerLevel = emotion.MaxEnergyPerLevel,
            ExtraSpeedDieLevel = emotion.ExtraSpeedDieLevel,
            ExtraSpeedDice = emotion.ExtraSpeedDice,
            BonusDrawLevel = emotion.BonusDrawLevel,
            BonusDrawRequiredTriggeredCards =
                emotion.BonusDrawRequiredTriggeredCards,
            BonusDrawAmount = emotion.BonusDrawAmount,
        };
    }
}

internal sealed class LibrarySpeedDiceModuleDispatcher
{
    private readonly ILibrarySpeedDicePolicy[] _policies;
    private readonly ILibrarySpeedDiceLifecycle[] _lifecycles;
    private readonly ILibrarySpeedDiceInputRouter[] _inputRouters;
    private readonly ILibrarySpeedDiceDeterminism[] _determinism;
    private readonly ILibrarySpeedDicePresentation[] _presentation;
    private readonly ILibraryLightPolicy[] _lightPolicies;

    public LibrarySpeedDiceModuleDispatcher(
        IEnumerable<ILibrarySpeedDiceModule> modules)
    {
        ILibrarySpeedDiceModule[] ordered = modules
            .Select(module => module
                ?? throw new ArgumentException(
                    "Speed-dice modules cannot contain null.",
                    nameof(modules)))
            .OrderBy(module => module.Order)
            .ThenBy(module => module.Id, StringComparer.Ordinal)
            .ToArray();

        foreach (ILibrarySpeedDiceModule module in ordered)
            ArgumentException.ThrowIfNullOrWhiteSpace(module.Id);

        string? duplicateId = ordered
            .GroupBy(module => module.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicateId != null)
        {
            throw new InvalidOperationException(
                $"Duplicate speed-dice module id '{duplicateId}'.");
        }

        Modules = ordered;
        _policies = ordered.OfType<ILibrarySpeedDicePolicy>().ToArray();
        _lifecycles = ordered.OfType<ILibrarySpeedDiceLifecycle>().ToArray();
        _inputRouters = ordered
            .OfType<ILibrarySpeedDiceInputRouter>()
            .ToArray();
        _determinism = ordered
            .OfType<ILibrarySpeedDiceDeterminism>()
            .ToArray();
        _presentation = ordered
            .OfType<ILibrarySpeedDicePresentation>()
            .ToArray();
        _lightPolicies = ordered.OfType<ILibraryLightPolicy>().ToArray();
    }

    public IReadOnlyList<ILibrarySpeedDiceModule> Modules { get; }

    public bool HasInputRouter => _inputRouters.Any(router =>
        router is not LegacyParticipantAdapter legacy
        || legacy.HasInputRouter);

    public bool CanEquipCard(
        LibrarySpeedDiceCombatState state,
        CardModel card) =>
        _policies.All(policy => policy.CanEquipCard(state, card));

    public bool CanUnequipCard(
        LibrarySpeedDiceCombatState state,
        CardModel card) =>
        _policies.All(policy => policy.CanUnequipCard(state, card));

    public bool CanTargetCard(
        LibrarySpeedDiceCombatState state,
        CardModel card,
        Creature? target) =>
        _policies.All(policy => policy.CanTargetCard(state, card, target));

    public int ModifySpeedDiceCount(
        LibrarySpeedDiceCombatState state,
        int count)
    {
        foreach (ILibrarySpeedDicePolicy policy in _policies)
            count = policy.ModifySpeedDiceCount(state, count);
        return count;
    }

    public Rng? CreateGameplayRng(Player player) =>
        _determinism
            .Select(module => module.CreateGameplayRng(player))
            .FirstOrDefault(rng => rng != null);

    public Rng? CreateTargetRepairRng(Player player) =>
        _determinism
            .Select(module => module.CreateTargetRepairRng(player))
            .FirstOrDefault(rng => rng != null);

    public string? GetStableTargetKey(Creature target) =>
        _determinism
            .Select(module => module.GetStableTargetKey(target))
            .FirstOrDefault(key => !string.IsNullOrWhiteSpace(key));

    public async Task<bool> RouteInputAsync(
        LibrarySpeedDiceInputRequest request)
    {
        foreach (ILibrarySpeedDiceInputRouter router in _inputRouters)
        {
            if (await router.RouteAsync(request))
                return true;
        }

        return false;
    }

    public void ConfigureSlotUi(
        Control control,
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot)
    {
        foreach (ILibrarySpeedDicePresentation presentation in _presentation)
            presentation.ConfigureSlotUi(control, state, slot);
    }

    public void OnStateCreated(LibrarySpeedDiceCombatState state)
    {
        foreach (ILibrarySpeedDiceLifecycle lifecycle in _lifecycles)
            lifecycle.OnStateCreated(state);
    }

    public void BeforePlayerTurn(LibrarySpeedDiceCombatState state)
    {
        foreach (ILibrarySpeedDiceLifecycle lifecycle in _lifecycles)
            lifecycle.BeforePlayerTurn(state);
    }

    public void OnEmotionLevelChanged(LibraryEmotionLevelChanged change)
    {
        foreach (ILibrarySpeedDiceLifecycle lifecycle in _lifecycles)
            lifecycle.OnEmotionLevelChanged(change);
    }

    public async Task AfterRollAsync(
        PlayerChoiceContext choiceContext,
        LibrarySpeedDiceCombatState state)
    {
        foreach (ILibrarySpeedDiceLifecycle lifecycle in _lifecycles)
            await lifecycle.AfterRollAsync(choiceContext, state);
    }

    public async Task OnUseAsync(
        PlayerChoiceContext choiceContext,
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot,
        LibrarySpeedDiceCardLease lease)
    {
        foreach (ILibrarySpeedDiceLifecycle lifecycle in _lifecycles)
        {
            await lifecycle.OnUseAsync(
                choiceContext,
                state,
                slot,
                lease);
        }
    }

    public async Task OnTargetedUseAsync(
        PlayerChoiceContext choiceContext,
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot,
        LibrarySpeedDiceCardLease lease,
        Creature target)
    {
        foreach (ILibrarySpeedDiceLifecycle lifecycle in _lifecycles)
        {
            await lifecycle.OnTargetedUseAsync(
                choiceContext,
                state,
                slot,
                lease,
                target);
        }
    }

    public async Task AfterCardEquippedAsync(
        PlayerChoiceContext choiceContext,
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot)
    {
        foreach (ILibrarySpeedDiceLifecycle lifecycle in _lifecycles)
        {
            await lifecycle.AfterCardEquippedAsync(
                choiceContext,
                state,
                slot);
        }
    }

    public async Task BeforeResolutionBatchAsync(
        LibrarySpeedDiceResolutionBatchContext context)
    {
        foreach (ILibrarySpeedDiceLifecycle lifecycle in _lifecycles)
            await lifecycle.BeforeResolutionBatchAsync(context);
    }

    public async Task BeforeCardResolutionAsync(
        LibrarySpeedDiceCardResolutionContext context)
    {
        foreach (ILibrarySpeedDiceLifecycle lifecycle in _lifecycles)
            await lifecycle.BeforeCardResolutionAsync(context);
    }

    public async Task AfterCardResolutionAsync(
        LibrarySpeedDiceCardResolutionContext context,
        bool wasPlayed)
    {
        foreach (ILibrarySpeedDiceLifecycle lifecycle in _lifecycles)
            await lifecycle.AfterCardResolutionAsync(context, wasPlayed);
    }

    public async Task AfterResolutionBatchAsync(
        LibrarySpeedDiceResolutionBatchContext context)
    {
        foreach (ILibrarySpeedDiceLifecycle lifecycle in _lifecycles)
            await lifecycle.AfterResolutionBatchAsync(context);
    }

    public void OnCardReleased(
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot,
        CardModel card,
        LibrarySpeedDiceCardLease? lease)
    {
        foreach (ILibrarySpeedDiceLifecycle lifecycle in _lifecycles)
            lifecycle.OnCardReleased(state, slot, card, lease);
    }

    public int ModifyLightCost(CardModel card, int cost)
    {
        foreach (ILibraryLightPolicy policy in _lightPolicies)
            cost = policy.ModifyLightCost(card, cost);
        return cost;
    }

    public int ModifyMaximumLight(LibraryLightState state, int maximum)
    {
        foreach (ILibraryLightPolicy policy in _lightPolicies)
            maximum = policy.ModifyMaximumLight(state, maximum);
        return maximum;
    }

    public int ModifyTurnRecovery(LibraryLightState state, int recovery)
    {
        foreach (ILibraryLightPolicy policy in _lightPolicies)
            recovery = policy.ModifyTurnRecovery(state, recovery);
        return recovery;
    }

    public bool ShouldRecoverLightForTurn(LibraryLightState state) =>
        _lightPolicies.All(policy =>
            policy.ShouldRecoverForTurn(state));

    public bool AllowLightOverflow(LibraryLightState state) =>
        _lightPolicies.Any(policy => policy.AllowOverflow(state));
}

internal sealed class LegacyParticipantAdapter :
    ILibrarySpeedDicePolicy,
    ILibrarySpeedDiceLifecycle,
    ILibrarySpeedDiceInputRouter,
    ILibrarySpeedDiceDeterminism,
    ILibrarySpeedDicePresentation
{
    private readonly LibrarySpeedDiceParticipant _participant;

    public LegacyParticipantAdapter(LibrarySpeedDiceParticipant participant)
    {
        _participant = participant;
    }

    public string Id => $"legacy:{_participant.Id}";

    public int Order => 0;

    internal bool HasInputRouter =>
        _participant.RequestInputAsync != null;

    public bool CanEquipCard(
        LibrarySpeedDiceCombatState state,
        CardModel card) => _participant.CanEquipCard(card);

    public bool CanUnequipCard(
        LibrarySpeedDiceCombatState state,
        CardModel card) => _participant.CanUnequipCard(card);

    public bool CanTargetCard(
        LibrarySpeedDiceCombatState state,
        CardModel card,
        Creature? target) => _participant.CanTargetCard(card, target);

    public int ModifySpeedDiceCount(
        LibrarySpeedDiceCombatState state,
        int currentCount) =>
        _participant.ModifySpeedDiceCount?.Invoke(state, currentCount)
        ?? currentCount;

    public void OnStateCreated(LibrarySpeedDiceCombatState state) =>
        _participant.OnStateCreated?.Invoke(state);

    public void BeforePlayerTurn(LibrarySpeedDiceCombatState state) =>
        _participant.BeforePlayerTurn?.Invoke(state.Player);

    public Task AfterRollAsync(
        PlayerChoiceContext choiceContext,
        LibrarySpeedDiceCombatState state) =>
        _participant.AfterSpeedRollAsync?.Invoke(choiceContext, state)
        ?? Task.CompletedTask;

    public Task AfterCardEquippedAsync(
        PlayerChoiceContext choiceContext,
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot) =>
        _participant.AfterCardEquippedAsync?.Invoke(
            choiceContext,
            state,
            slot)
        ?? Task.CompletedTask;

    public Task AfterResolutionBatchAsync(
        LibrarySpeedDiceResolutionBatchContext context) =>
        Task.CompletedTask;

    public void OnCardReleased(
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot,
        CardModel card,
        LibrarySpeedDiceCardLease? lease) =>
        _participant.OnCardReleased?.Invoke(card, slot);

    public async Task<bool> RouteAsync(
        LibrarySpeedDiceInputRequest request)
    {
        if (_participant.RequestInputAsync == null)
            return false;

        await _participant.RequestInputAsync(request);
        return true;
    }

    public Rng? CreateGameplayRng(Player player) =>
        _participant.GameplayRngFactory?.Invoke(player);

    public Rng? CreateTargetRepairRng(Player player) =>
        _participant.TargetRepairRngFactory?.Invoke(player);

    public string? GetStableTargetKey(Creature target) =>
        _participant.GetStableTargetKey?.Invoke(target);

    public void ConfigureSlotUi(
        Control control,
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot) =>
        _participant.ConfigureSlotUi?.Invoke(control, state, slot);
}
