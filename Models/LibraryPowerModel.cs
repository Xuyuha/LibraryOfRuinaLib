using Godot;
using HarmonyLib;
using LibraryLib.Utils;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace LibraryLib.Models;

/// <summary>
///     废墟图书馆基础库模组的基类 PowerModel。支持三种本地化策略：
///     <list type="number">
///         <item><see cref="IsDynamic"/> — 动态模式切换，使用 "{Id}_{Mode}.key" 模式</item>
///         <item><see cref="LegacyPowerId"/> — 自定义本地化键前缀（"{LegacyPowerId}.key"）</item>
///         <item>默认 — 标准 PowerModel 基于 Id 的本地化</item>
///     </list>
///     同时通过 Harmony 绑定的 NPower 引用提供动态图标刷新功能。
/// </summary>
public abstract partial class LibraryPowerModel : PowerModel, ILibraryAbstractModel
{
    public virtual void AddVariablesToDescription(LocString description, int? amountOverride = null){}
    private NPower? _boundNPower;
    protected string _suffix = "";
    protected virtual string DefaultSuffix => "";
    protected string UpSuffix => Suffix.ToUpperInvariant();
    protected string LowSuffix => Suffix.ToLowerInvariant();
    private static AccessTools.FieldRef<NPower, TextureRect>? _iconAccessor;
    private static AccessTools.FieldRef<NPower, CpuParticles2D>? _powerFlashAccessor;

    // Dynamic subclasses need their current suffix-specific icon. Static icon
    // overrides are limited to models whose assets are owned by this library.
    internal bool ShouldOverrideBaseIcon =>
        IsDynamic || GetType().Assembly == typeof(LibraryPowerModel).Assembly;

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
    public virtual bool NeedNpower => IsDynamic;

    /// <inheritdoc />
    public override LocString Title =>
        IsDynamic ? new LocString("powers", $"{base.Id.Entry}_{UpSuffix}.title") :
        LegacyPowerId != null ? new LocString("powers", $"{LegacyPowerId}.title") :
        base.Title;

    /// <inheritdoc />
    public override LocString Description =>
        IsDynamic ? new LocString("powers", $"{base.Id.Entry}_{UpSuffix}.description") :
        LegacyPowerId != null ? new LocString("powers", $"{LegacyPowerId}.description") :
        base.Description;

    /// <inheritdoc />
    protected override string SmartDescriptionLocKey =>
        IsDynamic ? $"{base.Id.Entry}_{UpSuffix}.smartDescription" :
        LegacyPowerId != null ? $"{LegacyPowerId}.smartDescription" :
        $"{base.Id.Entry}.smartDescription";

    /// <summary>
    ///     远程（多人游戏）描述的本地化键。
    /// </summary>
    protected override string RemoteDescriptionLocKey =>
        IsDynamic ? $"{base.Id.Entry}_{UpSuffix}.remoteDescription" :
        LegacyPowerId != null ? $"{LegacyPowerId}.remoteDescription" :
        $"{base.Id.Entry}.remoteDescription";

    /// <inheritdoc cref="PowerModel.PackedIconPath" />
    public new virtual string PackedIconPath
    {
        get
        {
            string fileName = base.Id.Entry.ToLowerInvariant()
                + (IsDynamic ? $"_{LowSuffix}" : string.Empty)
                + ".png";

            if (GetType().Assembly != typeof(LibraryPowerModel).Assembly)
            {
                return ImageHelper.GetImagePath($"powers/{fileName}");
            }

            return $"res://LibraryOfRuinaLib/images/powers/{fileName}";
        }
    }

    /// <inheritdoc cref="PowerModel.ResolvedBigIconPath" />
    public new virtual string ResolvedBigIconPath
    {
        get => PackedIconPath;
    }

    /// <inheritdoc cref="PowerModel.BigIcon" />
    public new virtual Texture2D? BigIcon => LoadTexture(ResolvedBigIconPath);

    /// <inheritdoc cref="PowerModel.Icon" />
    public new virtual Texture2D? Icon => LoadTexture(PackedIconPath);

    private static Texture2D? LoadTexture(string path)
    {
        try
        {
            Texture2D? texture = ResourceLoader.Load<Texture2D>(
                path,
                null,
                ResourceLoader.CacheMode.Reuse);
            return LibraryTextureSafety.IsValid(texture) ? texture : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    ///     当前动态模式索引。设置时会触发绑定 NPower 节点的图标刷新。
    /// </summary>
    public virtual string Suffix
    {
        get => _suffix == "" ? DefaultSuffix : _suffix;
        set
        {
            if (value == null) return;
            _suffix = value;
            RefreshIcon();
        }
    }

    /// <summary>
    ///     将此 power model 绑定到 NPower UI 节点以支持动态图标刷新。
    ///     由 <see cref="Library.Patches.ModelSetterPatch"/> 自动设置。
    /// </summary>
    public NPower? BoundNPower
    {
        get => _boundNPower;
        set
        {
            _boundNPower = value;
            _iconAccessor ??= AccessTools.FieldRefAccess<NPower, TextureRect>("_icon");
            _powerFlashAccessor ??= AccessTools.FieldRefAccess<NPower, CpuParticles2D>("_powerFlash");
        }
    }

    protected void RefreshIcon()
    {
        if (_boundNPower == null || !GodotObject.IsInstanceValid(_boundNPower))
            return;

        Texture2D? icon = Icon;
        if (!LibraryTextureSafety.IsValid(icon))
            return;

        if (_iconAccessor != null)
            _iconAccessor(_boundNPower).Texture = icon;

        Texture2D? bigIcon = BigIcon;
        if (_powerFlashAccessor != null && LibraryTextureSafety.IsValid(bigIcon))
            _powerFlashAccessor(_boundNPower).Texture = bigIcon;
    }
}
