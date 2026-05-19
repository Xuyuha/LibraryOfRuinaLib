using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Library.Models;
public abstract class LibraryMonsterModel : MonsterModel,LibraryAbstractModel//扩展MonsterModel，添加Chao值属性，不过还没怎么研究，后面还会继续加
{
    public abstract int MaxInitialChao { get; }
    public abstract decimal[] DefaultChaoResistance { get; }
    public abstract decimal[] DefaultDamageResistance { get; }
}

