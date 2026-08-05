using LibraryLib.Models;
using LibraryLib.Powers.LibraryPowerMode;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace LibraryLib.Utils;
public static class LibraryHoverTipFactory
{
	/// <summary>
	///     用于创建特殊模式的powertip
	/// </summary>
    public static IHoverTip FromPower<T,U>(int? amount = null) 
    where T : LibraryMultipleModePowerModel
    where U : LibraryPowerMode,new()
    {
        U mode = new();
        return FromPower<T>(mode,amount);
    }
	/// <summary>
	///     用于创建特殊模式的powertip，一般调用另一个重载。
	/// </summary>
    public static IHoverTip FromPower<T>(LibraryPowerMode mode, int? amount = null) 
    where T : LibraryMultipleModePowerModel
    {
        T model = ModelDb.Power<T>();
        model.Mode = mode;
        return HoverTipFactory.FromPower(model,amount);
    }
}