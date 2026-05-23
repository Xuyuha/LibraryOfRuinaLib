using Library.Models;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

public class LibraryEnchantmentModel : EnchantmentModel , ILibraryAbstractModel
{
    public virtual decimal EnchantChaoDamageAdditive(decimal originalDamage, ValueProp props)
    {
        return 0m;
    }
    public virtual decimal EnchantChaoDamageMultiplicative(decimal originalDamage, ValueProp props)
    {
        return 1m;
    }
}