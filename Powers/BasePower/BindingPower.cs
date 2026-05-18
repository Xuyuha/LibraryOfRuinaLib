using Library.Models;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Library.Powers;
public sealed class LibraryBindingPower : LibraryPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    //效果待讨论
}
