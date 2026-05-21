using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Combat;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Library.Utils;
using Library.Hooks;

namespace Library.Models;

/// <summary>
///     废墟图书馆基础库模组的基类 PowerModel。支持三种本地化策略：
///     <list type="number">
///         <item><see cref="IsDynamic"/> — 动态模式切换，使用 "{Id}_{Mode}.key" 模式</item>
///         <item><see cref="LegacyPowerId"/> — 自定义本地化键前缀（"{LegacyPowerId}.key"）</item>
///         <item>默认 — 标准 PowerModel 基于 Id 的本地化</item>
///     </list>
///     同时通过 Harmony 绑定的 NPower 引用提供动态图标刷新功能。
/// </summary>
public abstract class LibraryPowerModel : PowerModel, LibraryAbstractModel
{
    private NPower? _boundNPower;
    private int _mode;
    private static AccessTools.FieldRef<NPower, TextureRect>? _iconAccessor;
    private static AccessTools.FieldRef<NPower, CpuParticles2D>? _powerFlashAccessor;

    /// <summary>
    ///     重写此属性可提供自定义本地化键前缀，替代模型的 Id.Entry。
    ///     当非空时，Title/Description/SmartDescription/RemoteDescription 使用
    ///     "{LegacyPowerId}.title" 等模式。仅在 <see cref="IsDynamic"/> 为 false 时生效。
    /// </summary>
    protected virtual string? LegacyPowerId => null;

    /// <summary>
    ///     为 true 时启用动态模式切换，本地化键变为 "{Id.Entry}_{Mode}.key"。
    /// </summary>
    public virtual bool IsDynamic => false;

    /// <inheritdoc />
    public override LocString Title =>
        IsDynamic ? new LocString("powers", $"{base.Id.Entry}_{Mode}.title") :
        LegacyPowerId != null ? new LocString("powers", $"{LegacyPowerId}.title") :
        base.Title;

    /// <inheritdoc />
    public override LocString Description =>
        IsDynamic ? new LocString("powers", $"{base.Id.Entry}_{Mode}.description") :
        LegacyPowerId != null ? new LocString("powers", $"{LegacyPowerId}.description") :
        base.Description;

    /// <inheritdoc />
    protected override string SmartDescriptionLocKey =>
        IsDynamic ? $"{base.Id.Entry}_{Mode}.smartDescription" :
        LegacyPowerId != null ? $"{LegacyPowerId}.smartDescription" :
        $"{base.Id.Entry}.smartDescription";

    /// <summary>
    ///     远程（多人游戏）描述的本地化键。
    /// </summary>
    protected override string RemoteDescriptionLocKey =>
        IsDynamic ? $"{base.Id.Entry}_{Mode}.remoteDescription" :
        LegacyPowerId != null ? $"{LegacyPowerId}.remoteDescription" :
        $"{base.Id.Entry}.remoteDescription";

    /// <inheritdoc cref="PowerModel.PackedIconPath" />
    public new virtual string PackedIconPath
    {
        get
        {
            if (IsDynamic)
                return ImageHelper.GetImagePath($"atlases/power_atlas.sprites/{base.Id.Entry.ToLowerInvariant()}_{Mode}.tres");
            return base.PackedIconPath;
        }
    }

    /// <inheritdoc cref="PowerModel.ResolvedBigIconPath" />
    public new virtual string ResolvedBigIconPath
    {
        get
        {
            if (IsDynamic)
                return ImageHelper.GetImagePath($"powers/{base.Id.Entry.ToLowerInvariant()}_{Mode}.png");
            return base.PackedIconPath;
        }
    }

    /// <inheritdoc cref="PowerModel.BigIcon" />
    public new virtual Texture2D BigIcon => PreloadManager.Cache.GetTexture2D(ResolvedBigIconPath);

    /// <inheritdoc cref="PowerModel.Icon" />
    public new virtual Texture2D Icon => ResourceLoader.Load<Texture2D>(PackedIconPath, null, ResourceLoader.CacheMode.Reuse);

    /// <summary>
    ///     当前动态模式索引。设置时会触发绑定 NPower 节点的图标刷新。
    /// </summary>
    public int Mode
    {
        get => _mode;
        set
        {
            _mode = value;
            RefreshIcon();
        }
    }

    /// <summary>
    ///     将此 power model 绑定到 NPower UI 节点以支持动态图标刷新。
    ///     由 <see cref="Library.Patches.ModelSetterPatch"/> 自动设置。
    /// </summary>
    public NPower BoundNPower
    {
        set
        {
            _boundNPower = value;
            _iconAccessor ??= AccessTools.FieldRefAccess<NPower, TextureRect>("_icon");
            _powerFlashAccessor ??= AccessTools.FieldRefAccess<NPower, CpuParticles2D>("_powerFlash");
        }
    }

    /// <summary>
    ///     设置动态模式，并向所有 <see cref="LibraryAbstractModel"/> 监听器派发 before/after 钩子。
    /// </summary>
    public async Task SetPowerMode(PlayerChoiceContext choiceContext, int mode, Creature? dealer, CardModel? cardSource)
    {
        ICombatState combatState = Owner?.CombatState ?? throw new InvalidOperationException("Owner cannot be null");
        await LibraryHooks.BeforeSetPowerMode(combatState, choiceContext, this, dealer, cardSource, mode);
        Mode = mode;
        await LibraryHooks.AfterSetPowerMode(combatState, choiceContext, this, dealer, cardSource, mode);
    }

    private void RefreshIcon()
    {
        if (_boundNPower == null || !GodotObject.IsInstanceValid(_boundNPower))
            return;
        if (Icon == null)
            return;
        if (_iconAccessor != null)
            _iconAccessor(_boundNPower).Texture = Icon;
        if (_powerFlashAccessor != null)
            _powerFlashAccessor(_boundNPower).Texture = BigIcon;
    }
}