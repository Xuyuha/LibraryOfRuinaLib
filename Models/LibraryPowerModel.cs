using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Combat;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Library.Utils;

namespace Library.Models;
public abstract class LibraryPowerModel : PowerModel,LibraryAbstractModel//实现了动态的power展示
{
    private NPower? _boundNPower;
    private int _mode = 0;
    private static FieldInfo? _iconField;
    private static FieldInfo? _powerFlashField;
    public override LocString Title => IsDynamic ? new LocString("powers", base.Id.Entry + "_" + Mode.ToString() + ".title") : base.Title;
    public override LocString Description => IsDynamic ? new LocString("powers", base.Id.Entry + "_" + Mode.ToString() + ".description") : base.Description;
    protected override string SmartDescriptionLocKey => IsDynamic ? base.Id.Entry + "_" + Mode.ToString() + ".smartDescription" : base.Id.Entry + ".smartDescription";
    public virtual bool IsDynamic => false;
    public new virtual string PackedIconPath
    {
        get
        {
            if (IsDynamic)
                return ImageHelper.GetImagePath("atlases/power_atlas.sprites/" + base.Id.Entry.ToLowerInvariant() + "_" + Mode.ToString() + ".tres");
            return base.PackedIconPath;                                                   
        }
    }
    public new virtual string ResolvedBigIconPath
    {
        get
        {
            if (IsDynamic)
                return ImageHelper.GetImagePath("powers/" + base.Id.Entry.ToLowerInvariant() + "_" + Mode.ToString() + ".png");
            return base.PackedIconPath;
        }
    }

    public new virtual Texture2D BigIcon
    {
        get
        {
            return PreloadManager.Cache.GetTexture2D(ResolvedBigIconPath);
        }
    }

    public new virtual Texture2D Icon
    {
        get
        {
            return ResourceLoader.Load<Texture2D>(PackedIconPath, null, ResourceLoader.CacheMode.Reuse);
        }
    }
    public int Mode
    {
        get
        {
            return _mode;
        }
        set
        {
            _mode = value;
            RefreshIcon();
        }
    }
    public NPower BoundNPower
    {
        set
        {
            _boundNPower = value;
            Log.Info("Bound");
            if (_iconField == null)
            {
                _iconField = typeof(NPower).GetField("_icon", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new InvalidOperationException("无法找到NPower的_icon字段");
                _powerFlashField = typeof(NPower).GetField("_powerFlash", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new InvalidOperationException("无法找到NPower的_powerFlash字段");
            }
        }
    }
    public async Task SetMode(PlayerChoiceContext choiceContext, int mode, Creature? dealer, CardModel? cardSource)
    {
        ICombatState combatState = Owner?.CombatState ?? throw new InvalidOperationException("Owner cannot be null");
        await LibraryHooks.BeforeSetMode(combatState, choiceContext, this, dealer, cardSource, mode);
        Mode = mode;
        await LibraryHooks.AfterSetMode(combatState, choiceContext, this, dealer, cardSource, mode);
    }
    private void RefreshIcon()
    {
        if (_boundNPower == null) return;
        if (Icon != null)
        {
            if (_boundNPower == null || !GodotObject.IsInstanceValid(_boundNPower))
                return;
            if (_iconField?.GetValue(_boundNPower) is TextureRect iconTextureRect)
            {
                iconTextureRect.Texture = Icon;
            }
            if (_powerFlashField?.GetValue(_boundNPower) is CpuParticles2D powerFlash)
            {
                powerFlash.Texture = BigIcon;
            }
        }
    }

}