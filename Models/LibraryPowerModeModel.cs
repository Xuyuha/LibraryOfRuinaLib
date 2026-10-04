
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
    public abstract string Name { get; }
    public override bool ShouldReceiveCombatHooks => true;
}
