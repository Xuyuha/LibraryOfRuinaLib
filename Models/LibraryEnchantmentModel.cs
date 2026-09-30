using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Models;

/// <summary>
///     在原版附魔的伤害修正之外，提供混乱伤害的附魔修正。
///     库钩子的默认实现见 LibraryModelHookDefaults.cs。
/// </summary>
public partial class LibraryEnchantmentModel : EnchantmentModel, ILibraryAbstractModel
{
    public virtual decimal EnchantChaoDamageAdditive(decimal originalDamage, ValueProp props) => 0m;

    public virtual decimal EnchantChaoDamageMultiplicative(decimal originalDamage, ValueProp props) => 1m;
}
