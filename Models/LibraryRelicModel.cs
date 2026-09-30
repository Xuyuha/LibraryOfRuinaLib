using LibraryLib.Utils.RelicRightClick;
using MegaCrit.Sts2.Core.Models;

namespace LibraryLib.Models;

/// <summary>
///     支持右键触发的遗物基类。库钩子的默认实现见 LibraryModelHookDefaults.cs。
/// </summary>
public abstract partial class LibraryRelicModel : RelicModel, ILibraryAbstractModel
{
    public virtual bool HasRightClick => false;

    public virtual bool CanHandleRightClickLocal(LibraryRightClickContext context) => HasRightClick;

    public virtual bool CanExecuteRightClick(LibraryRightClickExecutionContext context) => HasRightClick;

    public virtual Task OnRightClick(LibraryRightClickExecutionContext context) => Task.CompletedTask;
}
