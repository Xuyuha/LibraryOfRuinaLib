#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Library.Models;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Library.Resistance.Powers;

/// <summary>
/// 怪物侧斩 / 打 / 刺抗性。仅对已登记攻击牌、且伤害来源为已注册参与方玩家时，在
/// <see cref="ModifyDamageMultiplicative"/> 中乘以系数。系数<strong>不会</strong>因攻击命中或怪物行动自动变化。
/// </summary>
public abstract class LibraryResistancePowerBase : LibraryPowerModel
{
    public const decimal InitialCoefficient = 1m;

    public const decimal MinCoefficient = 0.25m;

    public const decimal MaxCoefficient = 2m;

    private sealed class Data
    {
        public decimal Coefficient = InitialCoefficient;
    }

    protected abstract LibraryDamageKind Kind { get; }

    protected abstract string IconFileName { get; }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public new virtual string PackedIconPath =>
        ImageHelper.GetImagePath($"powers/{IconFileName}");

    protected override object? InitInternalData() => new Data();

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new StringVar("CoeffText") };

    public decimal Coefficient => GetInternalData<Data>().Coefficient;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        var data = GetInternalData<Data>();
        data.Coefficient = ClampCoefficient(Amount / 100m);
        RefreshCoeffVar();
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || !LibraryResistance.IsRelevantDamageDealer(dealer))
        {
            return 1m;
        }

        if (!props.IsPoweredAttack())
        {
            return 1m;
        }

        if (!LibraryAttackDamageKindRegistry.TryGetKind(cardSource, out var kind) || kind != Kind)
        {
            return 1m;
        }

        return GetInternalData<Data>().Coefficient;
    }

    public void ApplyCoefficientDelta(decimal delta)
    {
        var data = GetInternalData<Data>();
        data.Coefficient = ClampCoefficient(data.Coefficient + delta);
        SetAmount((int)Math.Round(data.Coefficient * 100m, MidpointRounding.AwayFromZero), silent: true);
        RefreshCoeffVar();
        InvokeDisplayAmountChanged();
        if (Owner.CombatState != null)
        {
            LibraryResistance.NotifyCoefficientsChanged(Owner.CombatState);
        }
    }

    public void SetCoefficientAbsolute(decimal coefficient)
    {
        var data = GetInternalData<Data>();
        data.Coefficient = ClampCoefficient(coefficient);
        SetAmount((int)Math.Round(data.Coefficient * 100m, MidpointRounding.AwayFromZero), silent: true);
        RefreshCoeffVar();
        InvokeDisplayAmountChanged();
        if (Owner.CombatState != null)
        {
            LibraryResistance.NotifyCoefficientsChanged(Owner.CombatState);
        }
    }

    private static decimal ClampCoefficient(decimal value) =>
        Math.Max(MinCoefficient, Math.Min(MaxCoefficient, value));

    private void RefreshCoeffVar()
    {
        decimal c = GetInternalData<Data>().Coefficient;
        ((StringVar)DynamicVars["CoeffText"]).StringValue = $"{c:0.##}";
    }
}

public sealed class LibrarySlashResistancePower : LibraryResistancePowerBase
{
    protected override LibraryDamageKind Kind => LibraryDamageKind.Slash;

    protected override string IconFileName => "library_resistance_slash.png";
}

public sealed class LibraryBluntResistancePower : LibraryResistancePowerBase
{
    protected override LibraryDamageKind Kind => LibraryDamageKind.Blunt;

    protected override string IconFileName => "library_resistance_blunt.png";
}

public sealed class LibraryPierceResistancePower : LibraryResistancePowerBase
{
    protected override LibraryDamageKind Kind => LibraryDamageKind.Pierce;

    protected override string IconFileName => "library_resistance_pierce.png";
}
