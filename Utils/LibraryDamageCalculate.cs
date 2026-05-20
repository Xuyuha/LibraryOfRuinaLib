using Library.Entities.Creatures;
using Library.Resistance;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

public static class LibraryDamageCalculate//计算伤害和Chao值
{
    public static decimal CalculateDamage(decimal amount,LibraryCreature target, ValueProp props, LibraryDamageType type)
    {
        if(!props.IsPoweredAttack())
            return amount;
        return target.GetDamageResistance(type)*amount;
    }
    public static decimal CalculateChaoAmount(decimal amount,LibraryCreature target, ValueProp props, LibraryDamageType type)
    {
        if(!props.IsPoweredAttack())
            return amount;
        return target.GetChaoResistance(type)*amount;
    }

    public static int CalculateAttackCardResistanceLoss(int singleAttackDamage, int hitCount, int ownerMaxHp) //暂定的计算方法
    {
        decimal baseRatio = Math.Max(0m, (singleAttackDamage - 1) / (Math.Max(1m, ownerMaxHp) * 2m));
        decimal multiplier = 1m + (Math.Max(1, hitCount) - 1) * 1.25m;
        return (int)Math.Ceiling(baseRatio * multiplier * 100m);
    }

    public static int CalculateOtherDamageResistanceLoss(int unblockedDamage, int ownerMaxHp)
    {
        return (int)Math.Ceiling(unblockedDamage / Math.Max(1m, ownerMaxHp) * 50m);
    }

    public static int ApplyChaosResistanceMultiplier(int baseLoss, CardModel? cardSource, LibraryCreature target)
    {
        if (!LibraryAttackDamageKindRegistry.TryGetKind(cardSource, out LibraryDamageKind kind))
            return baseLoss;
        if (kind == LibraryDamageKind.None)
            return baseLoss;
        decimal multiplier = target.GetChaosMultiplier(kind);
        if (multiplier == 1m) return baseLoss;
        return Math.Max(0, (int)Math.Ceiling(baseLoss * multiplier));
    }
}