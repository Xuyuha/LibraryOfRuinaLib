
using LibraryLib.Models;
using MegaCrit.Sts2.Core.Models;

namespace LibraryLib.Models;
public abstract partial class LibraryPowerModeModel : AbstractModel
{
	public LibraryMultipleModePowerModel? SourcePower;
	public LibraryPowerModeModel(LibraryMultipleModePowerModel sourcePower)
	{
		SourcePower = sourcePower;
	}
	public LibraryPowerModeModel()
	{
	
	}
    /// <summary>
    ///     模式已由 ModelDb 注册，初始化后不能再直接构造；取规范实例，由能力绑定时克隆。
    /// </summary>
    public static T Canonical<T>() where T : LibraryPowerModeModel
    {
        return ModelDb.GetById<T>(ModelDb.GetId<T>());
    }

    public abstract string Name { get; }
    public override bool ShouldReceiveCombatHooks => true;
}
