using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

public abstract class LibraryDamageExtensions : AttackCommand
{
    protected LibraryDamageExtensions(decimal damagePerHit) : base(damagePerHit)
    {
    }
    protected LibraryDamageExtensions(CalculatedDamageVar calculatedDamageVar):base(calculatedDamageVar)
    {
    }
}