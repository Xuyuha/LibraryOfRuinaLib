using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using Library.Resistance;

namespace Library.Models;
public abstract class LibraryMonsterModel : MonsterModel,LibraryAbstractModel//扩展MonsterModel，添加Chao值属性，不过还没怎么研究，后面还会继续加
{
    public virtual int MaxInitialChao => 0;
    public virtual decimal[] DefaultChaoResistance => [1m, 1m, 1m, 1m];
    public virtual decimal[] DefaultDamageResistance => [1m, 1m, 1m, 1m];

    /// <summary>混乱抗性值。null = 无混乱抗性条。</summary>
    public virtual int? DefaultStaggerResistance => null;

    /// <summary>混乱抗性等级数据（斩/刺/打）。null = 全部 Normal。</summary>
    public virtual LibraryCreatureResistanceData? DefaultStaggerResistanceData => null;
}

