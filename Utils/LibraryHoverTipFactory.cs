using Library.Models;
using Library.Powers.Mode;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Ruina.Utils;
public static class LibraryHoverTipFactory
{
    public static IHoverTip FromPower<T,U>(int? amount = null) 
    where T : LibraryMultipleModePowerModel
    where U : LibraryPowerMode,new()
    {
        U mode = new();
        return FromPower<T>(mode,amount);
    }
    public static IHoverTip FromPower<T>(LibraryPowerMode mode, int? amount = null) 
    where T : LibraryMultipleModePowerModel
    {
        T model = ModelDb.Power<T>();
        model.Mode = mode;
        return HoverTipFactory.FromPower(model,amount);
    }
}