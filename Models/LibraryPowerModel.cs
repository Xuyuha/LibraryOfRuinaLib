using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Combat;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Library.Utils;
using Library.Hooks;
using MegaCrit.Sts2.Core.ValueProps;
using Library.Entities.Creatures;
using Library.Resistance;
using Library.Powers.Mode;

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
public abstract class LibraryPowerModel : PowerModel,ILibraryAbstractModel
{
    private NPower? _boundNPower;
    private LibraryPowerMode? _mode;
    protected virtual LibraryPowerMode? DefaultMode => null;
    protected virtual string Name => Mode.Name;
    private string UpName => Name.ToUpperInvariant();
    private string LowName => Name.ToLowerInvariant();
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
        IsDynamic ? new LocString("powers", $"{base.Id.Entry}_{UpName}.title") :
        LegacyPowerId != null ? new LocString("powers", $"{LegacyPowerId}.title") :
        base.Title;

    /// <inheritdoc />
    public override LocString Description =>
        IsDynamic ? new LocString("powers", $"{base.Id.Entry}_{UpName}.description") :
        LegacyPowerId != null ? new LocString("powers", $"{LegacyPowerId}.description") :
        base.Description;

    /// <inheritdoc />
    protected override string SmartDescriptionLocKey =>
        IsDynamic ? $"{base.Id.Entry}_{UpName}.smartDescription" :
        LegacyPowerId != null ? $"{LegacyPowerId}.smartDescription" :
        $"{base.Id.Entry}.smartDescription";

    /// <summary>
    ///     远程（多人游戏）描述的本地化键。
    /// </summary>
    protected override string RemoteDescriptionLocKey =>
        IsDynamic ? $"{base.Id.Entry}_{UpName}.remoteDescription" :
        LegacyPowerId != null ? $"{LegacyPowerId}.remoteDescription" :
        $"{base.Id.Entry}.remoteDescription";

    /// <inheritdoc cref="PowerModel.PackedIconPath" />
    public new virtual string PackedIconPath
    {
        get
        {
            if (IsDynamic)
                return ImageHelper.GetImagePath($"atlases/power_atlas.sprites/{base.Id.Entry.ToLowerInvariant()}_{LowName}.tres");
            return base.PackedIconPath;
        }
    }

    /// <inheritdoc cref="PowerModel.ResolvedBigIconPath" />
    public new virtual string ResolvedBigIconPath
    {
        get
        {
            if (IsDynamic)
                return ImageHelper.GetImagePath($"powers/{base.Id.Entry.ToLowerInvariant()}_{LowName}.png");
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
    public LibraryPowerMode Mode
    {
        get{
            return _mode ?? DefaultMode;
        }
        private set
        {
            if(value == null)return;
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
    ///     设置动态模式，并向所有 <see cref="ILibraryAbstractModel"/> 监听器派发 before/after 钩子。
    /// </summary>
    public async Task SetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerMode mode, Creature? dealer, CardModel? cardSource)
    {
        ICombatState? combatState = Owner?.CombatState ;
        if(combatState == null)
            return;
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
    public virtual Task BeforeDiceEffect(PlayerChoiceContext choiceContext, Creature? target, CardModel cardSource, LibraryDice dice)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterDiceEffect(PlayerChoiceContext choiceContext, Creature? target, CardModel cardSource, LibraryDice dice)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforeSetChaoResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer, LibraryDamageType type,LibraryResistanceLevel resistanceValue)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterSetChaoResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual bool TrySetChaoResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer, LibraryDamageType type,LibraryResistanceLevel resistanceValue)
    {
        return true;
    }
    public virtual bool TrySetPhysicalResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer,LibraryDamageType type,LibraryResistanceLevel resistanceValue)
    {
        return true;
    }
    public virtual Task BeforeSetPhysicalResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer,LibraryDamageType type,LibraryResistanceLevel resistanceValue)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterSetPhysicalResistance(PlayerChoiceContext choiceContext,LibraryCreature target,Creature? dealer,LibraryDamageType type)
    {
        return Task.CompletedTask;
    }

    public virtual bool TryDiceEffect(PlayerChoiceContext choiceContext,Creature? target, CardModel cardSource, LibraryDice dice)
    {
        return true;
    }
    public virtual Task AfterAttack(PlayerChoiceContext choiceContext, LibraryAttackCommand command)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterBlockBroken(Creature target, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterChaoDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, LibraryChaoResult results, ValueProp props, Creature target, CardModel cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterChaoDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, LibraryChaoResult result, ValueProp props, Creature dealer, CardModel cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterCurrentChaoValueChanged(Creature target, decimal amount, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterModifyingHpLostAfterOsty(LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterModifyingHpLostBeforeOsty(LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode)
    {
        return Task.CompletedTask;
    }
    public virtual Task AfterStun(Creature creature)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforeAttack(LibraryAttackCommand command)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforeChaoDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforePowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforePowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforeSetPowerMode(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource, LibraryPowerMode mode)
    {
        return Task.CompletedTask;
    }
    public virtual Task BeforeStun(Creature creature)
    {
        return Task.CompletedTask;
    }
    public virtual int ModifyAttackHitCount(LibraryAttackCommand attackCommand, int num)
    {
        return num;
    }
    public virtual decimal ModifyChaoDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return 0m;
    }
    public virtual decimal ModifyChaoDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return decimal.MaxValue;
    }
    public virtual decimal ModifyChaoDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return 1m;
    }
    public virtual decimal ModifyDamageAdditive(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return 0m;
    }
    public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return decimal.MaxValue;
    }
    public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return 1m;
    }
    public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return num;
    }
    public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return num;
    }
    public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return num;
    }
    public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal num, ValueProp props, Creature? dealer, CardModel? cardSource, LibraryDamageType type)
    {
        return num;
    }
    public virtual Creature ModifyUnblockedDamageTarget(Creature creature, decimal amount, ValueProp props, Creature? dealer, LibraryDamageType type)
    {
        return creature;
    }
    public virtual bool TryPowerEffect(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        return true;
    }
    public virtual bool TryPowerReduce(PlayerChoiceContext choiceContext, LibraryPowerModel power, Creature? dealer, CardModel? cardSource)
    {
        return true;
    }
}