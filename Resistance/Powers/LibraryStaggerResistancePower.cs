#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Library.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Helpers;
using Library.Resistance.Patches;

namespace Library.Resistance.Powers;

/// <summary>
///     混乱抗性能力——受击时消耗抗性值替代伤害，归零时晕眩。
///     支持物理/混乱抗性倍率，以及充能球伤害的特殊处理。
/// </summary>
public sealed class LibraryStaggerResistancePower : LibraryPowerModel
{
    private sealed class Data
    {
        public int MaxResistance;
        public bool RestoreOnNextOwnerTurn;
        public AttackCommand? CurrentAttack;
        public LibraryCreatureResistanceData ResistanceData = new();
    }

    private static readonly string StaggerIconPath = ImageHelper.GetImagePath("powers/library_stagger_resistance.png");

    protected override string LegacyPowerId => "LIBRARY_OF_RUINA_STAGGER_RESISTANCE_POWER";

    public override PowerType Type => PowerType.None;

    protected override bool IsVisibleInternal => false;

    public override string PackedIconPath => StaggerIconPath;

    public override string ResolvedBigIconPath => StaggerIconPath;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        GetInternalData<Data>().MaxResistance = Math.Max(1, (int)amount);
        return Task.CompletedTask;
    }

    public override Task BeforeAttack(AttackCommand command)
    {
        Data data = GetInternalData<Data>();
        data.CurrentAttack = null;

        if (command.ModelSource is CardModel card
            && card.Type == CardType.Attack
            && command.Attacker?.Side != Owner.Side
            && command.DamageProps.IsPoweredAttack())
        {
            data.CurrentAttack = command;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterAttack(
        PlayerChoiceContext choiceContext,
        AttackCommand command)
    {
        Data data = GetInternalData<Data>();
        if (data.CurrentAttack != command)
        {
            return;
        }

        try
        {
            IEnumerable<DamageResult> ownerResults = command.Results
                .SelectMany(static hitResults => hitResults)
                .Where(result => result.Receiver == Owner && result.UnblockedDamage > 0);

            int loss = 0;
            foreach (DamageResult result in ownerResults)
            {
                loss += CalculateAttackCardResistanceLoss(result.UnblockedDamage, 1, Owner.MaxHp);
            }

            if (loss > 0)
            {
                loss = ApplyChaosResistanceMultiplier(loss, command.ModelSource as CardModel);
                await ReduceResistance(loss, command.Attacker, command.ModelSource as CardModel);
            }
        }
        finally
        {
            data.CurrentAttack = null;
        }
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || result.TotalDamage <= 0)
        {
            return;
        }

        Data data = GetInternalData<Data>();
        if (data.CurrentAttack != null
            && data.CurrentAttack.ModelSource == cardSource
            && data.CurrentAttack.Attacker == dealer
            && props.IsPoweredAttack())
        {
            return;
        }

        if (result.UnblockedDamage > 0
            && (!props.HasFlag(ValueProp.Unpowered)
                || LibraryStaggerResistanceOrbDamageContext.IsOrbDamage(dealer)))
        {
            int loss = CalculateOtherDamageResistanceLoss(result.UnblockedDamage, Owner.MaxHp);
            if (loss > 0)
            {
                loss = ApplyChaosResistanceMultiplier(loss, cardSource);
                await ReduceResistance(loss, dealer, null);
            }
        }
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        ICombatState combatState)
    {
        Data data = GetInternalData<Data>();
        if (side != Owner.Side || !data.RestoreOnNextOwnerTurn)
        {
            return;
        }

        data.RestoreOnNextOwnerTurn = false;
        int maxResistance = ResolveMaxResistance();
        if (Amount != maxResistance)
        {
            SetAmount(maxResistance, silent: true);
        }
    }

    public int MaxResistanceValue => GetInternalData<Data>().MaxResistance;

    public bool IsStunPending => GetInternalData<Data>().RestoreOnNextOwnerTurn;

    public LibraryCreatureResistanceData ResistanceData => GetInternalData<Data>().ResistanceData;

    public LibraryResistanceLevel GetChaosResistance(LibraryDamageKind kind) =>
        ResistanceData.GetChaosResistance(kind);

    public decimal GetChaosMultiplier(LibraryDamageKind kind)
    {
        if (kind == LibraryDamageKind.None) return 1m;
        return ResistanceData.GetChaosResistance(kind).GetMultiplier();
    }

    public Task ReduceResistance(int amount, Creature? applier, CardModel? cardSource)
    {
        if (amount <= 0 || Owner.IsDead)
        {
            return Task.CompletedTask;
        }

        return ReduceResistanceInternal(amount, applier, cardSource);
    }

    public void SetMaxResistance(int maxResistance, bool clampCurrent)
    {
        AssertMutable();
        Data data = GetInternalData<Data>();
        int oldMax = data.MaxResistance;
        data.MaxResistance = Math.Max(1, maxResistance);

        if (clampCurrent && Amount > data.MaxResistance)
        {
            SetAmount(data.MaxResistance, silent: true);
        }
        else if (data.MaxResistance > oldMax && Amount < data.MaxResistance)
        {
            SetAmount(data.MaxResistance, silent: true);
        }
    }

    private async Task ReduceResistanceInternal(int amount, Creature? applier, CardModel? cardSource)
    {
        Data data = GetInternalData<Data>();
        int nextAmount = Math.Max(0, Amount - amount);
        if (nextAmount == Amount)
        {
            return;
        }

        Flash();
        SetAmount(nextAmount);
        if (nextAmount <= 0 && !data.RestoreOnNextOwnerTurn && Owner.IsAlive)
        {
            data.RestoreOnNextOwnerTurn = true;
            await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), Owner, 2m, Owner, null);
            await CreatureCmd.Stun(Owner, Owner?.Monster?.NextMove.Id);
        }
    }

    private int ResolveMaxResistance()
    {
        Data data = GetInternalData<Data>();
        if (data.MaxResistance <= 0)
        {
            data.MaxResistance = Math.Max(1, Amount);
        }

        return data.MaxResistance;
    }

    private static int CalculateAttackCardResistanceLoss(int singleAttackDamage, int hitCount, int ownerMaxHp)
    {
        decimal baseRatio = Math.Max(0m, (singleAttackDamage - 1) / (Math.Max(1m, ownerMaxHp) * 2m));
        decimal multiplier = 1m + (Math.Max(1, hitCount) - 1) * 1.5m;
        return (int)Math.Ceiling(baseRatio * multiplier * 100m);
    }

    private static int CalculateOtherDamageResistanceLoss(int unblockedDamage, int ownerMaxHp)
    {
        return (int)Math.Ceiling(unblockedDamage / Math.Max(1m, ownerMaxHp) * 50m);
    }

    private int ApplyChaosResistanceMultiplier(int baseLoss, CardModel? cardSource)
    {
        if (!LibraryAttackDamageKindRegistry.TryGetKind(cardSource, out LibraryDamageKind kind))
            return baseLoss;
        if (kind == LibraryDamageKind.None)
            return baseLoss;

        decimal multiplier = GetChaosMultiplier(kind);
        if (multiplier == 1m) return baseLoss;

        return Math.Max(0, (int)Math.Ceiling(baseLoss * multiplier));
    }
}
