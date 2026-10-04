using LibraryLib.Localization.LibraryDynamicVars;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace LibraryLib.Localization;
public static class DynamicVarExtension
{
    public static T ToDice<T>(this DynamicVar var) where T : LibraryDice
    {
        return (T)var;
    }
    public static LibraryDice ToDice(this DynamicVar var)
    {
        return (LibraryDice)var;
    }
}