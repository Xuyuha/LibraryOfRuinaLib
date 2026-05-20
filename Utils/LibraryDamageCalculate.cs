using Library.Entities.Creatures;
using Library.Resistance;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.ValueProps;

public static class LibraryDamageCalculate
{
    public static decimal CalculateDamage(decimal amount,LibraryCreature target, ValueProp props, LibraryDamageKind type)
    {
        if(!props.IsPoweredAttack())
            return amount;
        return target.GetDamageResistance(type)*amount;
    }
    public static decimal CalculateChao(decimal amount,LibraryCreature target, ValueProp props, LibraryDamageKind type)
    {
        if(!props.IsPoweredAttack())
            return amount;
        return target.GetChaoResistance(type)*amount;
    }
}