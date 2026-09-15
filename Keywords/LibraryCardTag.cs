using MegaCrit.Sts2.Core.Entities.Cards;

namespace LibraryLib.Keywords;
/// <summary>
///     只有可施加对应power的卡牌需要添加对应标签
/// </summary>
public class LibraryCardTag
{
    public static readonly CardTag Bleed = (CardTag)379;
    public static readonly CardTag Burn = (CardTag)380;
    public static readonly CardTag Termor = (CardTag)381;
    public static readonly CardTag Charge = (CardTag)382;
    public static readonly CardTag Strong = (CardTag)383;
    public static readonly CardTag Weak = (CardTag)384;
    public static readonly CardTag Quickness = (CardTag)385;
    public static readonly CardTag Binding = (CardTag)386;
    public static readonly CardTag Endurce = (CardTag)387;
    public static readonly CardTag Disarm = (CardTag)388;
    public static readonly CardTag Protection = (CardTag)389;
    public static readonly CardTag Vulnerable = (CardTag)390;
    public static readonly CardTag Smoke = (CardTag)391;
}
