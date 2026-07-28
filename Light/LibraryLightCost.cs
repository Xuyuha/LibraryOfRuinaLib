using MegaCrit.Sts2.Core.Models;

namespace Library.Light;

public sealed class LibraryLightCost
{
    private readonly CardModel _card;
    private readonly Func<int> _xValueProvider;
    private readonly List<LibraryLightCostModifier> _localModifiers = [];
    private int _base;
    private int _capturedXValue;

    internal LibraryLightCost(
        CardModel card,
        int canonicalCost,
        bool costsX,
        Func<int>? xValueProvider = null)
    {
        ArgumentNullException.ThrowIfNull(card);
        _card = card;
        CostsX = costsX;
        Canonical = costsX ? 0 : canonicalCost;
        _base = Canonical;
        _xValueProvider = xValueProvider ?? (() =>
            LibraryLight.TryGetState(
                _card.Owner,
                out LibraryLightState? state)
                ? state!.Available
                : 0);
    }

    public int Canonical { get; }

    public bool CostsX { get; }

    public bool WasJustUpgraded { get; private set; }

    public bool HasLocalModifiers => _localModifiers.Count > 0;

    public int CapturedXValue
    {
        get
        {
            if (!CostsX)
            {
                throw new InvalidOperationException(
                    "Only X-cost Light cards have a captured value.");
            }

            return _capturedXValue;
        }
        set
        {
            _card.AssertMutable();
            if (!CostsX)
            {
                throw new InvalidOperationException(
                    "Only X-cost Light cards have a captured value.");
            }

            if (_capturedXValue == value)
                return;
            _capturedXValue = Math.Max(0, value);
            Changed?.Invoke();
        }
    }

    public event Action? Changed;

    public int GetWithModifiers(LibraryLightCostModifiers modifiers)
    {
        int value = _base;
        if (_card.IsCanonical || _base < 0 || CostsX)
            return value;

        if (modifiers.HasFlag(LibraryLightCostModifiers.Local))
        {
            foreach (LibraryLightCostModifier modifier in _localModifiers)
                value = modifier.Modify(value);
        }

        if (modifiers.HasFlag(LibraryLightCostModifiers.Global))
            value = LibraryLight.ModifyCost(_card, value);

        return Math.Max(0, value);
    }

    public int GetAmountToSpend()
    {
        return CostsX
            ? Math.Max(0, _xValueProvider())
            : Math.Max(
                0,
                GetWithModifiers(LibraryLightCostModifiers.All));
    }

    public int GetResolved()
    {
        return CostsX
            ? CapturedXValue
            : Math.Max(
                0,
                GetWithModifiers(LibraryLightCostModifiers.All));
    }

    public void SetUntilPlayed(int cost, bool reduceOnly = false) =>
        AddAbsolute(
            cost,
            LibraryLightCostModifierExpiration.WhenPlayed,
            reduceOnly);

    public void SetThisTurnOrUntilPlayed(
        int cost,
        bool reduceOnly = false) =>
        AddAbsolute(
            cost,
            LibraryLightCostModifierExpiration.EndOfTurn
            | LibraryLightCostModifierExpiration.WhenPlayed,
            reduceOnly);

    public void SetThisTurn(int cost, bool reduceOnly = false) =>
        AddAbsolute(
            cost,
            LibraryLightCostModifierExpiration.EndOfTurn,
            reduceOnly);

    public void SetThisCombat(int cost, bool reduceOnly = false) =>
        AddAbsolute(
            cost,
            LibraryLightCostModifierExpiration.EndOfCombat,
            reduceOnly);

    public void AddUntilPlayed(int amount, bool reduceOnly = false) =>
        AddRelative(
            amount,
            LibraryLightCostModifierExpiration.WhenPlayed,
            reduceOnly);

    public void AddThisTurnOrUntilPlayed(
        int amount,
        bool reduceOnly = false) =>
        AddRelative(
            amount,
            LibraryLightCostModifierExpiration.EndOfTurn
            | LibraryLightCostModifierExpiration.WhenPlayed,
            reduceOnly);

    public void AddThisTurn(int amount, bool reduceOnly = false) =>
        AddRelative(
            amount,
            LibraryLightCostModifierExpiration.EndOfTurn,
            reduceOnly);

    public void AddThisCombat(int amount, bool reduceOnly = false) =>
        AddRelative(
            amount,
            LibraryLightCostModifierExpiration.EndOfCombat,
            reduceOnly);

    public bool EndOfTurnCleanup()
    {
        _card.AssertMutable();
        return RemoveExpired(
            LibraryLightCostModifierExpiration.EndOfTurn);
    }

    public bool AfterCardPlayedCleanup()
    {
        _card.AssertMutable();
        return RemoveExpired(
            LibraryLightCostModifierExpiration.WhenPlayed);
    }

    internal bool EndOfCombatCleanup() =>
        RemoveExpired(
            LibraryLightCostModifierExpiration.EndOfCombat);

    public void UpgradeBy(int addend)
    {
        _card.AssertMutable();
        if (CostsX || addend == 0)
            return;

        int previous = _base;
        int upgraded = Math.Max(0, checked(_base + addend));
        WasJustUpgraded = true;
        if (upgraded < previous)
        {
            foreach (LibraryLightCostModifier modifier in _localModifiers)
            {
                if (modifier.Type
                        == LibraryLightCostModifierType.Absolute
                    && modifier.Amount > upgraded)
                {
                    modifier.Amount = upgraded;
                }
            }
        }

        SetCustomBaseCost(upgraded);
    }

    public void FinalizeUpgrade()
    {
        _card.AssertMutable();
        if (!WasJustUpgraded)
            return;
        WasJustUpgraded = false;
        Changed?.Invoke();
    }

    public void ResetForDowngrade()
    {
        _card.AssertMutable();
        _base = Canonical;
        WasJustUpgraded = false;
        Changed?.Invoke();
    }

    public void SetCustomBaseCost(int newBaseCost)
    {
        _card.AssertMutable();
        if (_base == newBaseCost)
            return;
        _base = newBaseCost;
        Changed?.Invoke();
    }

    internal LibraryLightCost Clone(CardModel newCard)
    {
        var clone = new LibraryLightCost(
            newCard,
            Canonical,
            CostsX)
        {
            _base = _base,
            _capturedXValue = _capturedXValue,
            WasJustUpgraded = WasJustUpgraded,
        };
        clone._localModifiers.AddRange(
            _localModifiers.Select(modifier => modifier.Clone()));
        return clone;
    }

    internal IReadOnlyList<LibraryLightCostModifier> CloneModifiers() =>
        _localModifiers.Select(modifier => modifier.Clone()).ToArray();

    private void AddAbsolute(
        int cost,
        LibraryLightCostModifierExpiration expiration,
        bool reduceOnly)
    {
        _card.AssertMutable();
        if (cost == 0 && Canonical < 0)
            return;
        _localModifiers.Add(
            new LibraryLightCostModifier(
                cost,
                LibraryLightCostModifierType.Absolute,
                expiration,
                reduceOnly));
        Changed?.Invoke();
    }

    private void AddRelative(
        int amount,
        LibraryLightCostModifierExpiration expiration,
        bool reduceOnly)
    {
        _card.AssertMutable();
        if (amount == 0)
            return;
        _localModifiers.Add(
            new LibraryLightCostModifier(
                amount,
                LibraryLightCostModifierType.Relative,
                expiration,
                reduceOnly));
        Changed?.Invoke();
    }

    private bool RemoveExpired(
        LibraryLightCostModifierExpiration expiration)
    {
        int removed = _localModifiers.RemoveAll(
            modifier => modifier.Expiration.HasFlag(expiration));
        if (removed > 0)
            Changed?.Invoke();
        return removed > 0;
    }
}
