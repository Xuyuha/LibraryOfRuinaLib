using Library.Utils;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

public static class LibraryDamageCmd//重置DamageCmd，仅用于创建LibraryCommand，不包含任何逻辑
{
	public static LibraryAttackCommand Attack(decimal damagePerHit)
	{
		return new LibraryAttackCommand(damagePerHit);
	}

	public static LibraryAttackCommand Attack(CalculatedDamageVar calculatedDamageVar)
	{
		return new LibraryAttackCommand(calculatedDamageVar); 
	}
	public static LibraryAttackCommand Attack(LibraryDice dice)
	{
		return new LibraryAttackCommand(dice); 
	}
}