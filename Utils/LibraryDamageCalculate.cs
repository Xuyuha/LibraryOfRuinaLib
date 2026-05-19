using Library.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.ValueProps;

public static class LibraryDamageCalculate//计算伤害和Chao值
{
    public static decimal CalculateDamage(decimal amount,LibraryCreature target, ValueProp props, LibraryDamageType type)
    {
        if(!props.IsPoweredAttack())
            return amount;
        return target.GetDamageResistance(type)*amount;
    }
    public static decimal CalculateChao(decimal amount,LibraryCreature target, ValueProp props, LibraryDamageType type)
    {
        if(!props.IsPoweredAttack())
            return amount;
        return target.GetChaoResistance(type)*amount;
    }
}