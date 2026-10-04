using LibraryLib.Commands;
using LibraryLib.Entities.Creatures;
using LibraryLib.Localization.LibraryDynamicVars;
using LibraryLib.Powers.LibraryPowerMode;
using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Entities.Cards;
using LibraryLib.Localization.Dice;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Models;

public class LibraryEnchantmentModel : EnchantmentModel
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