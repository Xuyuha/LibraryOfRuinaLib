using Library.Entities.Creatures;
using Library.Resistance;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

public static class LibraryDamageCalculate//计算伤害和Chao值
{
    public static decimal CalculateDamage(decimal amount,LibraryCreature? target, ValueProp props, LibraryDamageType type)
    {
        if(target == null || type == LibraryDamageType.None || !props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered))
            return amount;
        return target.GetPhysicalResistanceLevel(type).GetMultiplier()*amount;
    }
    public static decimal CalculateHpLoss(decimal amount, LibraryCreature? target, ValueProp props, LibraryDamageType type)
    {
        return CalculateDamage(amount, target, props, type);
    }
    public static decimal CalculateUnblockedDamage(decimal modifiedAmount, decimal blockedDamage, LibraryCreature? target, ValueProp props, LibraryDamageType type)
    {
        decimal unblockedDamage = Math.Max(modifiedAmount - blockedDamage, 0m);
        return CalculateHpLoss(unblockedDamage, target, props, type);
    }
    public static decimal CalculateChaoAmount(decimal amount,LibraryCreature? target, ValueProp props, LibraryDamageType type)
    {
        if(target == null || type == LibraryDamageType.None || !props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered))
            return amount;
        return target.GetChaosResistanceLevel(type).GetMultiplier()*amount;
    }
}
