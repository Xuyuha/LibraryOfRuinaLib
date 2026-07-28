# 速度骰子、情感与 Light 接入指南

适用版本：

- LibraryOfRuinaLib `1.1.0`
- Slay the Spire 2 Steam Beta `0.109.0`

本文说明下游 Mod 如何为自己的角色注册一套速度骰子、情感等级和独立
Light（光芒）系统。RolandMod 使用的也是同一入口。

## 1. 依赖边界

依赖方向必须是：

```text
你的角色 Mod -> LibraryOfRuinaLib -> STS2 / Godot / Harmony
```

LibraryOfRuinaLib 不依赖 RitsuLib 或其他基础库。下游 Mod 可以自行使用
RitsuLib，并通过 `ILibraryLightStore` 将 Light 接到自己的资源系统。

在下游 manifest 中声明：

```json
{
  "dependencies": [
    {
      "id": "LibraryOfRuinaLib",
      "min_version": "1.1.0"
    }
  ]
}
```

在下游项目中引用 `LibraryOfRuinaLib.dll`。运行时由 Mod 依赖加载，不要把
基础库 DLL 再打包进下游 Mod：

```xml
<Reference Include="LibraryOfRuinaLib"
           HintPath="$(LibraryOfRuinaLibDir)\LibraryOfRuinaLib.dll"
           Private="False" />
```

接入代码需要：

```csharp
using Library.Light;
using Library.SpeedDice;
```

### 1.1 Roland 的依赖写法

RolandMod 的 manifest 同时依赖 RitsuLib、LibraryOfRuinaLib 和内容库。
其中只有 `LibraryOfRuinaLib` 是本指南这套机制的必需依赖：

```jsonc
{
  "id": "RolandMod",
  "min_game_version": "0.109.0",
  "dependencies": [
    {
      // Roland 用 RitsuLib 显示和保存 Light。
      // 你的 Mod 如果只用默认内存 Light，可以不依赖它。
      "id": "STS2-RitsuLib",
      "version": "0.4.58"
    },
    {
      // 速度骰子、情感和独立 Light 的公共 API。
      "id": "LibraryOfRuinaLib",
      "min_version": "1.1.0"
    }
  ]
}
```

RolandMod 的工程引用：

```xml
<!--
  Private=False 很重要：
  编译时引用基础库，但不把 LibraryOfRuinaLib.dll 复制进 RolandMod 包。
  游戏运行时按 manifest 依赖加载唯一的一份基础库。
-->
<Reference Include="LibraryOfRuinaLib"
           HintPath="$(LibraryOfRuinaLibDir)\LibraryOfRuinaLib.dll"
           Private="False" />
```

Roland 的初始化顺序也体现了依赖边界：

```csharp
public static void Initialize()
{
    Assembly assembly = Assembly.GetExecutingAssembly();

    // 下游自己的内容注册、网络、资源 UI 先初始化。
    RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
    RolandSpeedDiceNetwork.Initialize();
    RolandLightService.Initialize();
    ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

    // 最后把已经准备好的角色类型、Store 和模块交给基础库。
    RegisterRolandLibrarySystems();
}
```

这里的 `RolandLightService`、Ritsu 注册和 UI 都属于下游。基础库只接收
`RolandRitsuLightStore.Factory`，并不知道 RitsuLib 的存在。

## 2. 最小角色注册

在 Mod 初始化期间、进入战斗前注册一次。`TCharacter` 必须是自己角色的
`CharacterModel` 类型：

```csharp
private const string ModId = "MyCharacterMod";

public static void RegisterLibrarySystems()
{
    LibrarySpeedDice
        .ForCharacter<MyCharacter>(
            ModId,
            new LibrarySpeedDiceOptions(
                BaseCount: 1,
                MinRoll: 2,
                MaxRoll: 7))
        .WithEmotion(CreateEmotionConfig())
        .WithLight(
            new LibraryLightOptions(
                starting: 4,
                baseMaximum: 4,
                maximumPerEmotionLevel: 1,
                recoveryPerTurn: 1,
                refillOnLevelIncrease: true))
        .UseModule(MySpeedDiceModule.Instance)
        .Register();
}

private static LibraryEmotionConfig CreateEmotionConfig() =>
    new()
    {
        UnitThresholds = [3, 3, 5, 7, 9],
        GainEmotionFromDamage = true,
        DamageUnitFractionOfMaxHp = 0.10m,
        ExtremeRollEmotionUnits = 1,
        KillEmotionUnits = 3,
        AllyDeathEmotionUnits = 0,
        MaxEnergyPerLevel = 1,
        ExtraSpeedDieLevel = 4,
        ExtraSpeedDice = 1,
        BonusDrawLevel = 5,
        BonusDrawRequiredTriggeredCards = 2,
        BonusDrawAmount = 2,
    };
```

如果暂时不需要角色专属逻辑，可以不调用 `.UseModule(...)`。如果不需要
Light，可以不调用 `.WithLight(...)`。情感配置未显式提供时使用默认值。

注册规则：

- 注册 ID 必须非空，并在全局唯一；推荐直接使用 Mod ID。
- 同一个 Builder 只能 `.Register()` 一次。
- Builder 不允许重复注册相同 ID，会直接抛出异常。
- `BaseCount` 不能小于 0，`MinRoll` 至少为 1，
  `MaxRoll` 不能小于 `MinRoll`。
- 不要同时用 Builder 和旧 `RegisterParticipant` 注册同一个 ID。

RolandMod 的实际入口位于 `RolandModCode/Entry.cs`。

### 2.1 Roland 的完整 Builder 注册

下面是 RolandMod 当前入口的等价代码，注释解释每个参数：

```csharp
LibrarySpeedDice
    .ForCharacter<RolandModCharacter>(
        // Registration.Id：全局唯一、跨客户端一致。
        // Roland 直接复用 manifest 的 ModId。
        ModId,

        new LibrarySpeedDiceOptions(
            // 情感等级等规则加入额外骰子前，角色拥有的基础槽位数。
            BaseCount: 1,

            // 自动投掷的基础最小值和最大值。
            MinRoll: 2,
            MaxRoll: 7))

    // 情感和 Light 是两个独立配置。
    .WithEmotion(CreateRolandEmotionConfig())

    .WithLight(
        new LibraryLightOptions(
            // Roland 每场战斗从 4 Light 开始。
            starting: RolandLightState.StartingLight,

            // 0 级情感时的基础上限。
            baseMaximum: RolandLightState.StartingLight,

            // 每升 1 级情感，Light 上限增加 1。
            maximumPerEmotionLevel: 1,

            // 没有升级补满时，每回合回复 1。
            recoveryPerTurn: 1,

            // 本回合情感升级时直接补满，而不是只回复 1。
            refillOnLevelIncrease: true),

        // 把基础库 Light 当前值接到 Roland 的 Ritsu 次级资源。
        // 省略此参数时会改用基础库的战斗期内存 Store。
        RolandRitsuLightStore.Factory)

    // 这不是基础库提供的现成实例。
    // RolandSpeedDiceModule 是 RolandMod 自己实现的下游适配模块；
    // Instance 只是 Roland 为这个无状态模块提供的单例对象。
    // 基础库会读取它实现的接口并在对应时机回调。
    .UseModule(RolandSpeedDiceModule.Instance)

    // 初始化期间只调用一次。
    .Register();
```

`ForCharacter<RolandModCharacter>` 最终生成的启用条件就是：

```csharp
player => player.Character is RolandModCharacter
```

因此不要在同一个具体角色类上注册两套不同 ID，除非你明确希望两套系统
同时匹配。正常角色只保留一个主注册。

### 2.2 `RolandSpeedDiceModule.Instance` 到底是什么

它由两部分组成：

```csharp
// 类型：RolandMod 自己实现，基础库中不存在这个类。
internal sealed class RolandSpeedDiceModule : ILibrarySpeedDiceModule
{
}

// 实例：RolandMod 自己创建，传给 Builder。
public static RolandSpeedDiceModule Instance { get; } = new();
```

`.UseModule(...)` 做的事情只是把这个对象交给本次 Registration：

```csharp
.UseModule(RolandSpeedDiceModule.Instance)
```

注册完成后，基础库会检查该对象还实现了哪些职责接口。例如对象实现
`ILibrarySpeedDicePolicy`，基础库就在装备、卸下、目标检查和槽位数量计算
时调用它；实现 `ILibraryLightPolicy`，基础库就在计算 Light 时调用它。

它不是：

- 速度骰子的战斗状态；
- Light 当前值的存储对象；
- Godot 节点；
- 必须继承的基础类；
- LibraryOfRuinaLib 自动创建的 Roland 专属对象。

### 2.3 Roland 为什么必须提供自己的模块

Builder 只能知道通用配置：

```text
角色类型
基础骰子数量
投掷范围
情感配置
Light 配置与 Store
```

但基础库不应该依赖 RolandMod，因此它不可能知道下面这些 Roland 规则：

| Roland 需求 | 基础库为什么不能写死 | Roland 实现的接口 |
| --- | --- | --- |
| 只允许 RolandPageCard 装备 | 基础库不知道 RolandPageCard 类型 | `ILibrarySpeedDicePolicy` |
| 使用时书页锁定、触发专属效果 | 基础库不知道 Roland timing metadata | `ILibrarySpeedDiceLifecycle` |
| 群攻目标特殊校验 | 基础库不知道 Roland 群攻关键词 | `ILibrarySpeedDicePolicy` |
| 装备、卸下、换目标走 Ritsu 联网 action | 基础库不能依赖 RitsuLib | `ILibrarySpeedDiceInputRouter` |
| 使用 Ritsu 的同步 RNG | 基础库不能调用下游 RNG 框架 | `ILibrarySpeedDiceDeterminism` |
| Roland 槽位颜色、字体与附加 UI | 角色表现必须由下游负责 | `ILibrarySpeedDicePresentation` |
| Stiletto 增加 Light 回复 | 基础库不知道 Roland Power | `ILibraryLightPolicy` |
| TheUdjat 允许 Light 溢出 | 基础库不知道 Roland Power | `ILibraryLightPolicy` |
| 交锋开始、真卦、结算后选择 | 都是 Roland 角色机制 | `ILibrarySpeedDiceLifecycle` |

所以 Roland 需要：

```csharp
.UseModule(RolandSpeedDiceModule.Instance)
```

如果你的角色没有任何自定义规则，只使用 Builder 默认行为，则可以完全
省略 `.UseModule(...)`。

### 2.4 Roland 为什么使用 `Instance` 单例

RolandSpeedDiceModule 本身没有可变字段：

```csharp
internal sealed class RolandSpeedDiceModule :
    ILibrarySpeedDicePolicy,
    ILibrarySpeedDiceLifecycle,
    ILibrarySpeedDiceInputRouter,
    ILibrarySpeedDiceDeterminism,
    ILibrarySpeedDicePresentation,
    ILibraryLightPolicy
{
    // 全局只有一个适配对象，避免初始化时反复 new。
    public static RolandSpeedDiceModule Instance { get; } = new();

    // 禁止外部创建第二个相同模块。
    private RolandSpeedDiceModule()
    {
    }

    // 稳定身份，用于排序和重复 ID 检测。
    public string Id => "roland.speed-dice";

    public int Order => 0;
}
```

所有方法需要的战斗数据都由参数传入：

```csharp
LibrarySpeedDiceCombatState state
LibrarySpeedDiceSlot slot
LibrarySpeedDiceCardLease lease
LibraryLightState lightState
```

所以该模块适合无状态单例。每场战斗的数据必须放在
`LibrarySpeedDiceCombatState`、`LibraryLightState`，或你自己按
`Player` / `CombatState` 绑定的状态对象中。

错误写法：

```csharp
internal sealed class MyModule : ILibrarySpeedDiceLifecycle
{
    public static MyModule Instance { get; } = new();

    // 错误：双人局、重连和下一场战斗会共享这个字段。
    private LibrarySpeedDiceSlot? _lastUsedSlot;
}
```

正确写法：

```csharp
public Task OnUseAsync(
    PlayerChoiceContext choiceContext,
    LibrarySpeedDiceCombatState state,
    LibrarySpeedDiceSlot slot,
    LibrarySpeedDiceCardLease lease)
{
    // 当前调用需要的数据全部来自参数或 state。
    // 如果还需保存角色状态，应按 state.Player / CombatState 建立存储。
    return Task.CompletedTask;
}
```

### 2.5 Roland 模块是如何实现的

第一步：声明实际需要的职责。一个类可以一次实现多个接口：

```csharp
internal sealed class RolandSpeedDiceModule :
    ILibrarySpeedDicePolicy,
    ILibrarySpeedDiceLifecycle,
    ILibrarySpeedDiceInputRouter,
    ILibrarySpeedDiceDeterminism,
    ILibrarySpeedDicePresentation,
    ILibraryLightPolicy
{
    public static RolandSpeedDiceModule Instance { get; } = new();

    private RolandSpeedDiceModule()
    {
    }

    public string Id => "roland.speed-dice";
    public int Order => 0;
}
```

第二步：把 Roland 的装备规则接到 Policy：

```csharp
public bool CanEquipCard(
    LibrarySpeedDiceCombatState state,
    CardModel card) =>
    // 真正规则留在 Roland 自己的 service 中，
    // 模块只是把基础库调用适配过去。
    RolandSpeedDiceCardRules.CanEquip(card);

public bool CanUnequipCard(
    LibrarySpeedDiceCombatState state,
    CardModel card)
{
    LibrarySpeedDiceSlot? slot = state.Slots.FirstOrDefault(
        candidate => ReferenceEquals(candidate.Card, card));

    // OnUseAsync 中 LockUnequip 后，这里拒绝卸下。
    return slot?.Lease?.PreventUnequip != true;
}

public bool CanTargetCard(
    LibrarySpeedDiceCombatState state,
    CardModel card,
    Creature? target) =>
    !RolandGroupAttackKeyword.IsPresentOn(card)
    || RolandGroupAttackTargeting.IsValidPrimaryTarget(card, target);

public int ModifySpeedDiceCount(
    LibrarySpeedDiceCombatState state,
    int currentCount) =>
    RolandSpeedDiceService.ModifyDiceCount(state, currentCount);
```

第三步：把战斗生命周期接到 Roland 的 service：

```csharp
public void OnStateCreated(LibrarySpeedDiceCombatState state) =>
    // 新状态创建后立刻纳入 Roland 多人快照跟踪。
    RolandMultiplayerPersistence.TrackState(state);

public void BeforePlayerTurn(LibrarySpeedDiceCombatState state) =>
    // 准备 Roland 本回合专属计数和效果。
    RolandSpeedDiceService.PreparePlayerTurn(state.Player);

public Task AfterRollAsync(
    PlayerChoiceContext choiceContext,
    LibrarySpeedDiceCombatState state) =>
    // 应用迅捷、额外抽牌和同步控制器。
    RolandSpeedDiceLifecycle.AfterRollAsync(
        choiceContext,
        state);
```

第四步：实现 Use、TargetedUse 和 lease 锁定：

```csharp
public async Task OnUseAsync(
    PlayerChoiceContext choiceContext,
    LibrarySpeedDiceCombatState state,
    LibrarySpeedDiceSlot slot,
    LibrarySpeedDiceCardLease lease)
{
    if (lease.Card is not RolandPageCard card)
        return;

    // 只有明确带“使用时”timing 的书页才锁定。
    if (card.HasTiming(RolandCardTiming.Use))
        lease.LockUnequip();

    // 基础库保证当前 lease 的 Use 只触发一次。
    await card.InvokeUseAsync(choiceContext);
}

public Task OnTargetedUseAsync(
    PlayerChoiceContext choiceContext,
    LibrarySpeedDiceCombatState state,
    LibrarySpeedDiceSlot slot,
    LibrarySpeedDiceCardLease lease,
    Creature target) =>
    // 该回调只会在基础库确认目标有效后触发。
    lease.Card is RolandPageCard card
        ? card.InvokeTargetedUseAsync(choiceContext, target)
        : Task.CompletedTask;
```

第五步：实现类型化批次事件和释放清理：

```csharp
public async Task BeforeResolutionBatchAsync(
    LibrarySpeedDiceResolutionBatchContext context)
{
    foreach (LibrarySpeedDiceSlot slot in context.Slots)
    {
        if (slot.Card is not RolandPageCard card)
            continue;

        // Roland 的“交锋开始”在这里执行，不再依赖字符串事件总线。
        await TrueTrigramClashService.BeforeClashAsync(
            context.ChoiceContext,
            context.State.Player);
        await card.InvokeClashStartAsync(
            context.ChoiceContext,
            slot.Target);
    }
}

public Task AfterResolutionBatchAsync(
    LibrarySpeedDiceResolutionBatchContext context) =>
    RolandSpeedDiceLifecycle.AfterResolutionAsync(
        context.ChoiceContext,
        context.State);

public void OnCardReleased(
    LibrarySpeedDiceCombatState state,
    LibrarySpeedDiceSlot slot,
    CardModel card,
    LibrarySpeedDiceCardLease? lease) =>
    // 基础库清 lease/预留；Roland 只清自己的 timing facade。
    RolandCardTimingService.Clear(card);
```

第六步：把输入送入 Roland 自己的网络层：

```csharp
public async Task<bool> RouteAsync(
    LibrarySpeedDiceInputRequest request)
{
    // request 已包含 Kind、Player、SlotIndex、
    // TurnNumber、Revision、Card 和 Target。
    await RolandSpeedDiceNetwork.RequestAsync(request);

    // true 表示该请求已处理，不再交给后续 InputRouter。
    return true;
}
```

这也是为什么 Roland 必须在 Builder 注册前初始化：

```csharp
RolandSpeedDiceNetwork.Initialize();
RolandMultiplayerPersistence.Initialize();
RolandLightService.Initialize();

LibrarySpeedDice
    .ForCharacter<RolandModCharacter>(ModId, options)
    .UseModule(RolandSpeedDiceModule.Instance)
    .Register();
```

第七步：提供可重放 RNG 和稳定目标键：

```csharp
public Rng? CreateGameplayRng(Player player) =>
    RitsuLibFramework.GetModPlayerRng(
        player,
        Entry.ModId,
        "speed-roll");

public Rng? CreateTargetRepairRng(Player player) =>
    RitsuLibFramework.GetModPlayerRng(
        player,
        Entry.ModId,
        "speed-target-repair");

public string? GetStableTargetKey(Creature target) =>
    RolandSpeedDiceNetwork.GetStableTargetSortKey(target);
```

第八步：把 UI 和 Light 角色规则留在下游：

```csharp
public void ConfigureSlotUi(
    Control control,
    LibrarySpeedDiceCombatState state,
    LibrarySpeedDiceSlot slot) =>
    RolandSpeedDiceUi.ConfigureSlot(control, state, slot);

public int ModifyTurnRecovery(
    LibraryLightState state,
    int currentRecovery) =>
    checked(
        currentRecovery
        + state.Player.Creature.GetPowerAmount<StilettoPower>());

public bool ShouldRecoverForTurn(LibraryLightState state) =>
    state.Player.Creature
        .GetPower<RolandNoLightRecoveryNextTurnPower>() == null;

public bool AllowOverflow(LibraryLightState state) =>
    state.Player.Creature.GetPower<TheUdjatPower>() != null;
```

这八组方法共同构成 `RolandSpeedDiceModule.Instance`。基础库只定义接口、
稳定派发顺序和调用时机；所有 Roland 类型、Power、网络与 UI 都留在
RolandMod。

### 2.6 你的角色最少需要实现多少

只想限制卡牌类型时，最小模块只有一个 Policy：

```csharp
internal sealed class MySpeedDiceModule : ILibrarySpeedDicePolicy
{
    public static MySpeedDiceModule Instance { get; } = new();

    private MySpeedDiceModule()
    {
    }

    public string Id => "my-mod.speed-dice";

    public bool CanEquipCard(
        LibrarySpeedDiceCombatState state,
        CardModel card) =>
        card is MyPageCard;
}
```

注册：

```csharp
.UseModule(MySpeedDiceModule.Instance)
```

没有任何自定义规则时：

```csharp
LibrarySpeedDice
    .ForCharacter<MyCharacter>(ModId, options)
    .WithEmotion(emotion)
    .WithLight(light)
    // 不调用 UseModule，全部使用基础库默认规则。
    .Register();
```

如果拆成多个模块：

```csharp
.UseModule(MyGameplayModule.Instance) // Order = 0
.UseModule(MyNetworkModule.Instance)  // Order = 100
.UseModule(MyUiModule.Instance)       // Order = 200
```

这种拆分适合大型角色。小型角色可以像 Roland 一样用一个模块实现多个
接口，再把具体业务继续分派到 `Service` / `Lifecycle` / `Network` 类。

## 3. 让卡牌进入速度骰子

普通卡牌不必实现额外接口。此时速度骰子使用卡牌原版 Energy/Stars 费用和
原版 `TargetType`。

如果需要为速度骰子单独定义费用或目标，实现
`ILibrarySpeedDiceCard`：

```csharp
using Library.Light;
using Library.SpeedDice;
using MegaCrit.Sts2.Core.Entities.Cards;

public sealed class MyPageCard :
    MyCardBase,
    ILibrarySpeedDiceCard,
    ILibraryLightCard
{
    // 速度骰子内不消耗原版 Energy/Stars。
    public LibrarySpeedDiceResourceCost SpeedDiceResourceCost =>
        new(Energy: 0, Stars: 0);

    public TargetType SpeedDiceTargetType => TargetType.AnyEnemy;

    // 独立 Light 费用。
    public int BaseLightCost => 2;

    public bool HasLightCostX => false;
}
```

常见组合：

| 卡牌接口 | 装备到速度骰子时的费用 |
| --- | --- |
| 都不实现 | 原版 Energy/Stars |
| 仅 `ILibrarySpeedDiceCard` | 接口指定的 Energy/Stars |
| 仅 `ILibraryLightCard` | 原版 Energy/Stars，再加 Light |
| 两者都实现 | 接口指定的 Energy/Stars，再加 Light |

因此，类似罗兰的“只消耗 Light 的书页”应同时实现两个接口，并把
`SpeedDiceResourceCost` 设为 `(0, 0)`。

原版 Energy-X 和 Star-X 卡不能装备到速度骰子。Light-X 受支持：

```csharp
public int BaseLightCost => 0;
public bool HasLightCostX => true;
```

Light-X 会在装备时按当时可用 Light 冻结 X 值，结算时可通过
`LibraryLight.GetCost(card).GetResolved()` 读取本次实际 X。

注意：`ILibraryLightCard` 只声明独立 Light 费用。基础库会在速度骰子装备
与结算中预留、扣除 Light，但不会把普通手牌打出的原版 Energy 付费流程
自动替换为 Light。若同一张牌还允许从手牌正常打出，下游需要自行接入
普通出牌的 Light UI、可用性检查与扣费。

### 3.1 Roland 的书页基类

Roland 用一个统一书页基类实现两种接口：

```csharp
public abstract class RolandPageCard : ModCardTemplate,
    ILibrarySpeedDiceCard,
    IRolandLightCard // IRolandLightCard 继承 ILibraryLightCard
{
    // Roland 的速度骰子书页不消耗原版 Energy。
    // Roland 不使用 Stars；默认结构体中的 Stars 始终为 0。
    // 普通手牌出牌仍使用 ModCardTemplate 的 baseCost。
    public LibrarySpeedDiceResourceCost SpeedDiceResourceCost { get; }

    // 速度骰子可以使用与普通出牌不同的目标类型。
    // MookWorkshop 普通出牌是 Self，装备后则改为 AnyEnemy。
    public TargetType SpeedDiceTargetType { get; }

    // 新 Light API 从卡牌实例取得独立费用。
    int ILibraryLightCard.BaseLightCost =>
        _canonicalSpeedDiceLightCost;

    bool ILibraryLightCard.HasLightCostX =>
        HasSpeedDiceLightCostX;
}
```

MookWorkshop 展示了“普通目标”和“速度骰子目标”分离：

```csharp
public sealed class MookWorkshop() : RolandPageCard(
    baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Basic,

    // 从手牌正常打出时不选敌人。
    target: TargetType.Self,

    // 装备到速度骰子后必须选择敌人。
    speedDiceLightCost: 2,
    speedDiceTarget: TargetType.AnyEnemy)
{
}
```

### 3.2 Roland 明确区分 Energy、Light 与 Stars

`normalEnergyCost` 只表示普通手牌出牌的原版 Energy 费用；
`speedDiceLightCost` 只表示装备到速度骰子后的独立 Light 费用。Roland
没有 Stars 费用，也不会把 Light 存进 `SpeedDiceResourceCost.Stars`：

```csharp
private readonly int _canonicalSpeedDiceLightCost =
    Math.Max(0, speedDiceLightCost);

public LibrarySpeedDiceResourceCost SpeedDiceResourceCost { get; }
```

新角色也应明确写成：

```csharp
public abstract class MyPageCard(
    int normalEnergyCost,
    int baseLightCost,
    TargetType normalTarget,
    TargetType? speedDiceTarget = null)
    : MyCardTemplate(normalEnergyCost, normalTarget),
      ILibrarySpeedDiceCard,
      ILibraryLightCard
{
    // 清晰地区分：普通 Energy 与独立 Light。
    // 本角色不使用 Stars。
    public LibrarySpeedDiceResourceCost SpeedDiceResourceCost { get; }

    public TargetType SpeedDiceTargetType =>
        speedDiceTarget ?? normalTarget;

    public int BaseLightCost { get; } = Math.Max(0, baseLightCost);

    public virtual bool HasLightCostX => false;
}
```

Roland 的 KeepItFresh 是固定 1 Light 书页：

```csharp
public sealed class KeepItFresh() : RolandPageCard(
    0,
    CardType.Attack,
    CardRarity.Common,
    TargetType.AnyEnemy,
    speedDiceLightCost: 1)
{
    public override int GetSpeedDiceLightCost() => 1;
}
```

新角色不需要再创建 `GetSpeedDiceLightCost()` 兼容方法，直接使用：

```csharp
int preview = LibraryLight.GetCost(card)
    .GetWithModifiers(LibraryLightCostModifiers.All);
```

## 4. 角色模块

一个模块可以实现一个或多个职责接口：

- `ILibrarySpeedDicePolicy`：装备、卸下、目标与骰子数量规则。
- `ILibrarySpeedDiceLifecycle`：回合、投掷、Use、结算批次等时机。
- `ILibrarySpeedDiceInputRouter`：将装备、卸下和换目标请求送入下游网络层。
- `ILibrarySpeedDiceDeterminism`：提供 gameplay RNG、目标修复 RNG 和稳定目标键。
- `ILibrarySpeedDicePresentation`：配置角色专属槽位表现。
- `ILibraryLightPolicy`：修改 Light 费用、上限、回复和溢出规则。

最小模块示例：

```csharp
using Library.Light;
using Library.SpeedDice;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

public interface IMySpeedDiceUseCard
{
    bool LockAfterUse { get; }

    Task OnSpeedDiceUseAsync(PlayerChoiceContext choiceContext);

    Task OnSpeedDiceTargetedUseAsync(
        PlayerChoiceContext choiceContext,
        Creature target);
}

internal sealed class MySpeedDiceModule :
    ILibrarySpeedDiceLifecycle,
    ILibraryLightPolicy
{
    public static MySpeedDiceModule Instance { get; } = new();

    private MySpeedDiceModule()
    {
    }

    public string Id => "my-character.speed-dice";

    public int Order => 0;

    public async Task OnUseAsync(
        PlayerChoiceContext choiceContext,
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot,
        LibrarySpeedDiceCardLease lease)
    {
        if (lease.Card is not IMySpeedDiceUseCard card)
            return;

        if (card.LockAfterUse)
            lease.LockUnequip();

        await card.OnSpeedDiceUseAsync(choiceContext);
    }

    public Task OnTargetedUseAsync(
        PlayerChoiceContext choiceContext,
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot,
        LibrarySpeedDiceCardLease lease,
        Creature target) =>
        lease.Card is IMySpeedDiceUseCard card
            ? card.OnSpeedDiceTargetedUseAsync(
                choiceContext,
                target)
            : Task.CompletedTask;

    public Task BeforeResolutionBatchAsync(
        LibrarySpeedDiceResolutionBatchContext context)
    {
        // 对该角色的本次结算批次只调用一次。
        return Task.CompletedTask;
    }

    public int ModifyTurnRecovery(
        LibraryLightState state,
        int currentRecovery) =>
        currentRecovery;
}
```

模块先按 `Order` 升序，再按 `Id` 的 ordinal 顺序稳定执行。相同注册中出现
重复模块 ID 会直接拒绝。多个 Policy 的数值修改会按此顺序串联；装备、
卸下和目标检查必须全部返回 `true`。Input Router 中第一个返回 `true` 的
模块会终止后续路由。

多人游戏下不要用 `System.Random`、本地对象哈希或枚举顺序决定玩法结果。
应通过 `ILibrarySpeedDiceDeterminism` 返回同步 RNG，并为目标返回跨客户端
一致的稳定键。网络请求应携带 `TurnNumber` 和 `Revision`，由权威端调用
公开的 `ExecuteEquipAsync`、`ExecuteUnequipAsync` 或
`ExecuteRetargetAsync`。

若需要把多个玩家一起结算，调用：

```csharp
await LibrarySpeedDice.ResolveBatchAsync(choiceContext, states);
```

基础库会按最终骰值降序、玩家 NetId、槽位索引稳定排序。不要反射私有
resolver。

### 4.1 Roland 如何把六种职责放进一个模块

Roland 的模块声明：

```csharp
internal sealed class RolandSpeedDiceModule :
    ILibrarySpeedDicePolicy,       // 能否装备、卸下、选目标、骰子数量
    ILibrarySpeedDiceLifecycle,    // 回合、Use、批次结算、释放
    ILibrarySpeedDiceInputRouter,  // 把本地点击转成联网 action
    ILibrarySpeedDiceDeterminism,  // 同步 RNG 和稳定目标键
    ILibrarySpeedDicePresentation, // 槽位 UI
    ILibraryLightPolicy            // Light 上限、回复、溢出
{
}
```

不要求每个角色都实现全部接口。拆成多个模块也可以，例如：

```csharp
.UseModule(MyGameplayModule.Instance) // Order = 0
.UseModule(MyNetworkModule.Instance)  // Order = 100
.UseModule(MyUiModule.Instance)       // Order = 200
```

但各模块 ID 必须唯一，并且模块不能依赖“注册顺序刚好等于调用顺序”；真实
顺序始终是 `Order`，然后是 ordinal `Id`。

### 4.2 Roland 的装备与卸下 Policy

```csharp
public bool CanEquipCard(
    LibrarySpeedDiceCombatState state,
    CardModel card) =>
    // 角色自己的规则集中在独立 service，模块只负责适配。
    RolandSpeedDiceCardRules.CanEquip(card);

public bool CanUnequipCard(
    LibrarySpeedDiceCombatState state,
    CardModel card)
{
    // 先从当前状态找到真正持有该卡的槽位。
    LibrarySpeedDiceSlot? slot = state.Slots.FirstOrDefault(
        candidate => ReferenceEquals(candidate.Card, card));

    // Use 时调用 lease.LockUnequip() 后，这里返回 false。
    return slot?.Lease?.PreventUnequip != true;
}

public bool CanTargetCard(
    LibrarySpeedDiceCombatState state,
    CardModel card,
    Creature? target) =>
    // 普通书页由基础库目标规则处理；
    // 群攻书页再叠加 Roland 的主目标合法性。
    !RolandGroupAttackKeyword.IsPresentOn(card)
    || RolandGroupAttackTargeting.IsValidPrimaryTarget(card, target);
```

Policy 只判断，不应该直接移动卡牌、扣资源或修改目标。真正写状态由基础库
的 Execute API 完成，否则多人重放时会出现两次写入。

### 4.3 Roland 的骰子数量与投掷后修正

```csharp
public int ModifySpeedDiceCount(
    LibrarySpeedDiceCombatState state,
    int currentCount) =>
    RolandSpeedDiceService.ModifyDiceCount(state, currentCount);

public Task AfterRollAsync(
    PlayerChoiceContext choiceContext,
    LibrarySpeedDiceCombatState state) =>
    RolandSpeedDiceLifecycle.AfterRollAsync(choiceContext, state);
```

Roland 的迅捷在 `AfterRollAsync` 中使用公开 API 修正槽位值：

```csharp
int quickness = state.Player.Creature
    .GetPowerAmount<RolandQuicknessPower>();

foreach (LibrarySpeedDiceSlot slot in state.Slots)
{
    // 同时更新 FinalValue / DisplayValue，并触发 gameplay revision。
    state.SetSlotRollValue(
        slot.Index,
        checked(slot.FinalValue + quickness));
}
```

不要从下游反射修改 `FinalValue` 或 `DisplayValue`。现行公共入口是
`state.SetSlotRollValue(...)`。

### 4.4 Roland 的 Light Policy

```csharp
public int ModifyTurnRecovery(
    LibraryLightState state,
    int currentRecovery) =>
    checked(
        currentRecovery
        // StilettoPower 每层再增加 1 点回合回复。
        + state.Player.Creature.GetPowerAmount<StilettoPower>());

public bool ShouldRecoverForTurn(LibraryLightState state) =>
    // 该 Power 存在时整次回合回复被禁止。
    state.Player.Creature
        .GetPower<RolandNoLightRecoveryNextTurnPower>() == null;

public bool AllowOverflow(LibraryLightState state) =>
    // TheUdjatPower 允许 Current 暂时超过 Maximum。
    state.Player.Creature.GetPower<TheUdjatPower>() != null;
```

这里修改的是 Light Policy。名字里即使出现 Energy，也不会自动修改原版
Energy；Roland 的效果如果需要同时影响两者，必须在效果代码中分别调用。

## 5. 生命周期与时机

类型化生命周期顺序如下：

| 顺序 | 时机 |
| --- | --- |
| 1 | 创建战斗状态，调用 `OnStateCreated` |
| 2 | 回合准备，调用 `BeforePlayerTurn` |
| 3 | 尝试情感升级并准备槽位 |
| 4 | 自动投掷速度骰子 |
| 5 | 情感升级时调用 `OnEmotionLevelChanged` |
| 6 | Light 执行本回合回复或升级补满 |
| 7 | 调用 `AfterRollAsync` |
| 8 | 装备成功，创建并冻结本次卡牌 lease 与预留计划 |
| 9 | 调用一次 `OnUseAsync` |
| 10 | 目标有效时调用一次 `OnTargetedUseAsync` |
| 11 | 调用 `AfterCardEquippedAsync` |
| 12 | 每个角色状态调用一次 `BeforeResolutionBatchAsync` |
| 13 | 每张牌依次调用 `BeforeCardResolutionAsync`、提交预留、出牌、`AfterCardResolutionAsync` |
| 14 | 调用 `AfterResolutionBatchAsync` |
| 15 | 卡牌离开槽位时调用 `OnCardReleased` 并释放 lease |

Use 状态绑定本次装备 lease，不绑定卡牌类型或全局状态：

- 装备成功后立即触发 Use。
- 同一 lease 的 Use 和 TargetedUse 都只触发一次。
- TargetedUse 只有在目标有效时才触发。
- 需要“Use 后不能卸下”时，在 `OnUseAsync` 中调用
  `lease.LockUnequip()`。
- 卡牌卸下、结算完毕或离开速度骰子后，lease 状态自动清理。

新代码应使用 `BeforeResolutionBatchAsync` 表示“批次结算开始”。不要创建
字符串式全局 timing 总线，也不要依赖兼容用的
`LibraryClashResolver.Current`。

### 5.1 Roland 的 Use 与 TargetedUse

Roland 把书页的虚方法映射到 lease 生命周期：

```csharp
public async Task OnUseAsync(
    PlayerChoiceContext choiceContext,
    LibrarySpeedDiceCombatState state,
    LibrarySpeedDiceSlot slot,
    LibrarySpeedDiceCardLease lease)
{
    if (lease.Card is not RolandPageCard card)
        return;

    // 只有明确声明“使用时”的牌才会锁定。
    // 不能只凭 IsUseTriggered 判断卡牌是否应该禁止卸下。
    if (card.HasTiming(RolandCardTiming.Use))
        lease.LockUnequip();

    // 基础库保证同一个 lease 最多调用一次。
    await card.InvokeUseAsync(choiceContext);
}

public Task OnTargetedUseAsync(
    PlayerChoiceContext choiceContext,
    LibrarySpeedDiceCombatState state,
    LibrarySpeedDiceSlot slot,
    LibrarySpeedDiceCardLease lease,
    Creature target) =>
    // 只有基础库确认 target 有效后才进入这里。
    lease.Card is RolandPageCard card
        ? card.InvokeTargetedUseAsync(choiceContext, target)
        : Task.CompletedTask;
```

RolandPageCard 只暴露受保护的角色逻辑：

```csharp
protected virtual Task OnUse(PlayerChoiceContext choiceContext) =>
    Task.CompletedTask;

protected virtual Task OnTargetedUse(
    PlayerChoiceContext choiceContext,
    Creature target) =>
    Task.CompletedTask;
```

因此具体书页不需要知道 slot、lease、Revision 或联网实现。

### 5.2 Roland 的批次开始

Roland 过去用全局 ClashResolver 模拟“交锋开始”。现在改为类型化批次事件：

```csharp
public async Task BeforeResolutionBatchAsync(
    LibrarySpeedDiceResolutionBatchContext context)
{
    foreach (LibrarySpeedDiceSlot slot in context.Slots)
    {
        if (slot.Card is not RolandPageCard card)
            continue;

        try
        {
            // 角色级的批次前效果。
            await TrueTrigramClashService.BeforeClashAsync(
                context.ChoiceContext,
                context.State.Player);

            // 每张已装备书页自己的“交锋开始”效果。
            await card.InvokeClashStartAsync(
                context.ChoiceContext,
                slot.Target);
        }
        catch (Exception exception)
        {
            // 单张书页失败要保留完整异常和卡牌 ID。
            Entry.Logger.Error(
                $"Roland clash-start effect failed for "
                + $"{card.Id.Entry}: {exception}");
        }
    }
}
```

`BeforeResolutionBatchAsync` 对每个注册状态每批调用一次，不是每张牌一次。
需要逐卡前后逻辑时，改用 `BeforeCardResolutionAsync` 和
`AfterCardResolutionAsync`。

### 5.3 Roland 的批次结束与释放

```csharp
public Task AfterResolutionBatchAsync(
    LibrarySpeedDiceResolutionBatchContext context) =>
    RolandSpeedDiceLifecycle.AfterResolutionAsync(
        context.ChoiceContext,
        context.State);

public void OnCardReleased(
    LibrarySpeedDiceCombatState state,
    LibrarySpeedDiceSlot slot,
    CardModel card,
    LibrarySpeedDiceCardLease? lease) =>
    // 清除的是本次牌的下游 timing 状态；
    // 基础库已负责释放资源预留和 lease。
    RolandCardTimingService.Clear(card);
```

Roland 的批次结束逻辑会标记槽位、检查胜利条件，再准备下回合角色状态：

```csharp
state.MarkAllSlotsSpent();
await CombatManager.Instance.CheckWinCondition();

if (!CombatManager.Instance.IsOverOrEnding)
{
    await RolandMultiplayerPersistence.EnsureControllerAsync(
        choiceContext,
        state);
}
```

## 6. 情感系统

情感等级范围为 0 到 5。`UnitThresholds` 必须正好包含 5 个正整数，分别
表示升到下一级需要的单位数。单位达到当前阈值后，在下一次玩家回合准备
阶段升级。

配置字段：

| 字段 | 含义 |
| --- | --- |
| `UnitThresholds` | 五级升级阈值 |
| `GainEmotionFromDamage` | 是否从造成/承受伤害获得单位 |
| `DamageUnitFractionOfMaxHp` | 每 1 单位需要的最大生命比例，余数会累积 |
| `ExtremeRollEmotionUnits` | 投出最小值或最大值时获得的单位 |
| `KillEmotionUnits` | 击杀敌人获得的单位 |
| `AllyDeathEmotionUnits` | 友方死亡获得的单位 |
| `MaxEnergyPerLevel` | 每级增加的原版最大 Energy |
| `ExtraSpeedDieLevel` / `ExtraSpeedDice` | 达到等级后增加的速度骰子 |
| `BonusDrawLevel` | 启用额外抽牌的等级 |
| `BonusDrawRequiredTriggeredCards` | 上回合需由速度骰子触发的牌数 |
| `BonusDrawAmount` | 达标后的额外抽牌数 |

伤害统计使用未格挡且不超过剩余生命的实际伤害。造成伤害与承受伤害分别
累积。

`MaxEnergyPerLevel` 只控制原版 Energy，绝不控制 Light。Light 的等级上限
增长由 `LibraryLightOptions.MaximumPerEmotionLevel` 单独配置。需要同时
修改两种资源的效果，必须显式调用两套 API。

主动增加情感：

```csharp
LibrarySpeedDice.AddEmotionUnits(player, 2);
```

强制升级：

```csharp
if (LibrarySpeedDice.TryForceEmotionLevelUp(
        player,
        out int previousLevel,
        out int currentLevel))
{
    // 已完成一次升级。
}
```

### 6.1 Roland 的情感配置

Roland 当前不从普通伤害和速度骰极值获取情感，而是主要通过书页骰子、
击杀和友方死亡获取：

```csharp
private const bool GainEmotionFromDamage = false;
private const int SpeedDiceExtremeRollEmotionUnits = 0;
private const int KillEmotionUnits = 2;
private const int AllyDeathEmotionUnits = 2;
private const int MaxEnergyPerEmotionLevel = 0;

private static LibraryEmotionConfig CreateRolandEmotionConfig() =>
    new()
    {
        // Roland 五次升级分别需要 6 / 9 / 12 / 15 / 20 单位。
        UnitThresholds = [6, 9, 12, 15, 20],

        GainEmotionFromDamage = false,
        ExtremeRollEmotionUnits = 0,
        KillEmotionUnits = 2,
        AllyDeathEmotionUnits = 2,

        // Roland 的情感等级不增加原版 Energy 上限。
        MaxEnergyPerLevel = 0,

        // 4 级情感开始获得第 2 个速度骰子。
        ExtraSpeedDieLevel = 4,
        ExtraSpeedDice = 1,

        // 5 级时，上回合至少触发 3 张速度骰子书页则多抽 1。
        BonusDrawLevel = 5,
        BonusDrawRequiredTriggeredCards = 3,
        BonusDrawAmount = 1,
    };
```

Roland 入口目前用反射设置三个后加字段，是为了兼容曾经的基础库 DLL：

```csharp
typeof(LibraryEmotionConfig)
    .GetProperty("GainEmotionFromDamage")
    ?.SetValue(config, false);
```

新下游以 `1.1.0` 为最低版本时不需要这样写，直接使用对象初始化器即可。
反射会丢失编译期检查，不应作为新接入的默认写法。

### 6.2 Roland 从书页骰子获取情感

Roland 在 `LibraryHooks.AfterDiceRoll` 后检查是否掷出书页骰子的最大值：

```csharp
public static void Prefix(LibraryDice dice)
{
    decimal maximum = dice.BaseValue + dice.FloatValue;
    if (dice.SourceCard.Owner.Character is not RolandModCharacter
        || dice.CurrentBaseValue != maximum
        || !LibrarySpeedDice.TryGetState(
            dice.SourceCard.Owner,
            out LibrarySpeedDiceCombatState? state)
        || state == null)
    {
        return;
    }

    // 真正写情感状态仍走基础库公开入口。
    LibrarySpeedDice.AddEmotionUnits(
        dice.SourceCard.Owner,
        units: 1);
}
```

这里是 Roland 特有的“书页骰子最大值”规则，不等于
`ExtremeRollEmotionUnits`；后者针对速度骰子的投掷。

### 6.3 Roland 的主动升级效果

```csharp
if (LibrarySpeedDice.TryForceEmotionLevelUp(
        owner,
        out int previousLevel,
        out int currentLevel))
{
    // WithLight 已配置 refillOnLevelIncrease=true。
    // 正常回合升级会由基础库统一恢复；
    // Roland 的主动强制升级兼容层显式刷新自己的 facade。
    if (RolandLightService.TryGetState(
            owner,
            out RolandLightState? light)
        && light != null)
    {
        await light.RefillAfterEmotionLevelIncreasedAsync(currentLevel);
    }

    RolandSpeedDiceService.ApplyForcedEmotionLevelEffects(state);
}
```

一般下游如果没有额外兼容 facade，只需调用
`TryForceEmotionLevelUp(...)`，不要再手动重复派发
`OnEmotionLevelChanged`。

获取状态：

```csharp
if (LibrarySpeedDice.TryGetState(player, out var state)
    && state is not null)
{
    int level = state.Emotion.Level;
    int units = state.Emotion.Units;
}
```

## 7. Light 状态

注册 `.WithLight(...)` 后，每个匹配玩家会获得一个 sealed
`LibraryLightState`。不传 Store Factory 时使用基础库的战斗期内存 Store。

```csharp
if (LibraryLight.TryGetState(player, out LibraryLightState? light)
    && light is not null)
{
    int current = light.Current;
    int maximum = light.Maximum;
    int reserved = light.Reserved;
    int spendable = light.Available;

    await light.Gain(1, source);
    await light.Lose(1, source);
    await light.Set(3, source);
    await light.Reset(source);

    await light.ModifyMaximum(
        amount: 1,
        temporary: false,
        gainCurrent: true,
        source: source);

    await light.ClearTemporaryMaximum(source);
}
```

所有改变 Store 的方法都是异步的，必须 `await`。

数值含义：

- `Current`：当前总量。
- `Maximum`：基础上限、情感等级、永久/临时修正和 Light Policy 的结果。
- `Reserved`：已被装备卡牌占用、尚未提交的总量。
- `Available = Current - Reserved`：外部效果真正可消费的数量。

外部消费必须检查 `Available` 或调用 `HasEnoughAvailable`，不能只检查
`Current`。`Set` 和 `Lose` 不会把 Current 降到已预留量以下。

可订阅事件：

- `Changed`
- `CurrentChanged(oldValue, newValue)`
- `MaximumChanged(oldValue, newValue)`
- `ReservationChanged(oldValue, newValue)`

`LibraryLightOptions` 的行为：

- 初始值为 `starting`。
- 基础上限为 `baseMaximum`。
- 当前基础上限 =
  `baseMaximum + emotionLevel * maximumPerEmotionLevel`。
- 每回合回复 `recoveryPerTurn`，每回合最多执行一次。
- 情感升级且 `refillOnLevelIncrease` 为 `true` 时，本回合改为补满。
- `ILibraryLightPolicy` 可以继续修改上限、回复、是否回复和是否允许溢出。

基础库不负责角色专属 Light 图标、颜色、HUD 或 Ritsu 次级资源注册，这些
仍由下游 Mod 实现。

### 7.1 Roland 的 Light facade

Roland 保留原来的 `RolandLightState` 签名，但内部只转发到公共
`LibraryLightState`：

```csharp
internal sealed class RolandLightState
{
    public const int StartingLight = 4;

    private readonly LibraryLightState _inner;

    internal RolandLightState(LibraryLightState inner)
    {
        _inner = inner;
    }

    // 旧 Roland 名称 -> 新公共状态。
    public int MaxLight => _inner.Maximum;
    public int Light => _inner.Current;
    public int ReservedLight => _inner.Reserved;
    public int AvailableLight => _inner.Available;

    public bool HasEnoughAvailableLight(int amount) =>
        _inner.HasEnoughAvailable(amount);

    public Task GainLightAsync(
        int amount,
        AbstractModel? source = null) =>
        _inner.Gain(amount, source);

    public Task LoseLightAsync(
        int amount,
        AbstractModel? source = null) =>
        _inner.Lose(amount, source);

    public Task ResetLightAsync(AbstractModel? source = null) =>
        _inner.Reset(source);
}
```

新角色没有旧 API 兼容负担时，直接使用 `LibraryLightState`，不需要再包一层。

### 7.2 Roland 的最大值修改

```csharp
public Task GainMaxLightAsync(
    int amount,
    bool gainCurrent = false,
    AbstractModel? source = null) =>
    _inner.ModifyMaximum(
        Math.Max(0, amount),
        temporary: false,
        gainCurrent: gainCurrent,
        source: source);

public Task GainTemporaryMaxLightAsync(
    int amount,
    bool gainCurrent = false,
    AbstractModel? source = null) =>
    _inner.ModifyMaximum(
        Math.Max(0, amount),
        temporary: true,
        gainCurrent: gainCurrent,
        source: source);

public Task ClearTemporaryMaxLightBonusAsync(
    AbstractModel? source = null) =>
    _inner.ClearTemporaryMaximum(source);
```

`gainCurrent` 只决定“上限增加时是否同时增加 Current”。它不会改变情感升级
时的补满策略。

### 7.3 Roland 如何保护已预留 Light

Roland 的 Ritsu 次级资源 Hook 在外部消费前检查公共状态的 `Available`：

```csharp
public bool ShouldSpendSecondaryResource(
    SecondaryResourceSpendContext context)
{
    return !IsLight(context.Definition)
        || !LibraryLight.TryGetState(
            context.Player,
            out LibraryLightState? state)
        || state == null

        // 关键：只允许消费 Current - Reserved。
        || context.Amount <= state.Available;
}
```

如果写成 `context.Amount <= state.Current`，玩家可以在装备多张书页后从其他
效果花掉已被 lease 占用的 Light，最终导致结算失败。

### 7.4 Roland 的 UI 刷新

```csharp
state = States.GetValue(player, owner =>
{
    var facade = new RolandLightState(inner);

    // Current、Maximum 或 Reservation 改变后刷新手牌费用。
    facade.Changed += () => QueueHandCardRefresh(owner);
    return facade;
});
```

基础库负责发状态事件，不负责 Roland 的卡框颜色、图标和 HUD。下游 UI
只订阅状态，不应重新维护第二份 Light 数值。

## 8. Light 费用

通过卡牌实例获取独立费用对象：

```csharp
LibraryLightCost cost = LibraryLight.GetCost(card);

int preview = cost.GetWithModifiers(
    LibraryLightCostModifiers.All);
int amountToReserve = cost.GetAmountToSpend();
int resolvedAmount = cost.GetResolved();
```

`GetAmountToSpend()` 用于当前装备预检；`GetResolved()` 对 Light-X 返回
已经捕获的 X 值。非 X 卡两者都应用本地与全局 Light modifier。

费用修改 API：

| API | 到期时机 |
| --- | --- |
| `SetUntilPlayed` / `AddUntilPlayed` | 打出后 |
| `SetThisTurnOrUntilPlayed` / `AddThisTurnOrUntilPlayed` | 回合结束或打出后，先到者 |
| `SetThisTurn` / `AddThisTurn` | 回合结束 |
| `SetThisCombat` / `AddThisCombat` | 战斗结束 |

`Set...` 设置绝对费用，`Add...` 添加相对值。`reduceOnly: true` 保证该
modifier 不会把进入它时的费用提高。

升级示例：

```csharp
LibraryLight.GetCost(this).UpgradeBy(-1);
```

其他维护 API：

- `FinalizeUpgrade()`
- `ResetForDowngrade()`
- `SetCustomBaseCost(int)`
- `CapturedXValue`

基础库补丁会随卡牌深拷贝复制 Light 费用和 modifier；克隆卡不会共享可变
modifier。回合结束、打出后、升级完成和降级时会跟随卡牌费用生命周期做
清理。卡牌自己的升级代码仍需调用 `UpgradeBy(...)` 声明 Light 费用变化。

基础库层的 Light modifier 只影响 Light。是否让某个角色的 Energy 改费
同步影响 Light，由该角色自己的 `ILibraryLightPolicy` 明确决定。

### 8.1 Roland 的费用 facade

Roland 保持两个资源状态独立，但为了让原版/第三方“改费”效果同时作用于
书页，会在 Light Policy 中按同一时间线镜像未标记的 Energy modifier：

```csharp
internal static class RolandLightCost
{
    public static int ApplyMirroredEnergyModifiersToLight(
        CardModel card,
        int currentLightCost)
    {
        foreach (LocalCostModifier modifier in GetLocalModifiers(card))
        {
            if (IsEnergyOnly(card, modifier))
                continue;
            currentLightCost = IsLightOnly(card, modifier)
                ? GetLightModifier(card, modifier).Modify(currentLightCost)
                : modifier.Modify(currentLightCost);
        }
        return ApplyGlobalEnergyCostHooks(card, currentLightCost);
    }
}
```

### 8.2 Roland 显式区分 Energy 与 Light

Roland 的标记只用于决定改费作用域，不会把余额或付费混为同一资源：

```csharp
public static void SetEnergyAndLightToFreeThisTurn(CardModel card)
{
    // 一个未标记 modifier 同时投影到 Energy 与 Light，不能再追加第二次
    // Light modifier，否则会重复应用。
    card.SetToFreeThisTurn();
}

public static void AddEnergyOnlyThisCombat(
    CardModel card,
    int amount)
{
    LocalCostModifier modifier =
        AppendEnergyModifier(card, amount, EndOfCombat);
    TagAsEnergyOnly(card, modifier);
}

public static void AddLightOnlyThisCombat(
    CardModel card,
    int amount)
{
    LocalCostModifier marker =
        AppendNeutralEnergyMarker(card, EndOfCombat);
    TagAsLightOnly(card, marker, amount);
}
```

未标记表示“同时改 Energy 与 Light”；`EnergyOnly` 与 `LightOnly` 明确
表示单资源效果。标记会通过 Ritsu `ModelCloneRegistry` 随卡牌克隆复制，
避免克隆后作用域丢失。余额、上限、预留和实际扣费仍完全独立。

### 8.3 Roland 的 Light 费用升级

RolandPageCard 的兼容帮助方法：

```csharp
protected void UpgradeSpeedDiceLightCostBy(int addend)
{
    // X 费用没有固定 canonical 值，不做这种升级。
    if (HasSpeedDiceLightCostX || addend == 0)
        return;

    LibraryLight.GetCost(this).UpgradeBy(addend);

    // 复用现有卡牌费用视觉刷新入口。
    InvokeEnergyCostChanged();
}

protected override void AfterDowngraded()
{
    base.AfterDowngraded();
    LibraryLight.GetCost(this).ResetForDowngrade();
    InvokeEnergyCostChanged();
}
```

具体 Roland 书页在升级回调中降 1 Light：

```csharp
protected override void OnUpgrade()
{
    // -1 表示费用从例如 4 降到 3。
    UpgradeSpeedDiceLightCostBy(-1);
}
```

新下游也可以直接写：

```csharp
protected override void OnUpgrade()
{
    LibraryLight.GetCost(this).UpgradeBy(-1);
}
```

### 8.4 Roland 的 Light-X 与推荐新写法

Roland 的 SwordOfVolition 声明：

```csharp
public override bool HasSpeedDiceLightCostX => true;
```

Roland 的 Ritsu Hook 从当前 lease 读取已经冻结的数值：

```csharp
LibrarySpeedDiceSlot? slot = null;
if (!LibrarySpeedDice.TryGetEquippedSlot(context.Card, out slot))
{
    LibrarySpeedDice.TryGetResolvingSlot(context.Card, out slot);
}

return slot?.Lease?.ReservationPlan.GetAmount(ResourceId)
    ?? value;
```

新角色在卡牌效果中不必再维护 `_capturedSpeedX`，可以直接读取：

```csharp
int resolvedX = LibraryLight.GetCost(this).GetResolved();
```

注意调用时机：只有装备预留提交后，X 卡的 `CapturedXValue` 才代表本次
结算值。预览阶段使用 `GetAmountToSpend()`。

## 9. 自定义 Light Store

默认内存 Store 足以支持纯战斗内 Light。需要接到自己的持久化或次级资源
框架时，实现：

```csharp
using Library.Light;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

internal sealed class MyLightStore :
    ILibraryLightStore,
    ILibraryLightStoreIdentity
{
    private int _current;

    public MyLightStore(int starting)
    {
        _current = Math.Max(0, starting);
    }

    // 跨客户端、存档和版本保持稳定，并避免与其他资源重名。
    public string ResourceId => "my-character.light";

    public event Action? Changed;

    public bool TryRead(out LibraryLightStoreSnapshot snapshot)
    {
        snapshot = new LibraryLightStoreSnapshot(_current);
        return true;
    }

    public Task WriteAsync(
        LibraryLightStoreSnapshot snapshot,
        AbstractModel? source = null)
    {
        int next = Math.Max(0, snapshot.Current);
        if (next != _current)
        {
            _current = next;
            Changed?.Invoke();
        }

        return Task.CompletedTask;
    }

    public void Restore(LibraryLightStoreSnapshot snapshot)
    {
        _current = Math.Max(0, snapshot.Current);
        Changed?.Invoke();
    }

    public static ILibraryLightStore Factory(
        Player player,
        LibraryLightOptions options) =>
        new MyLightStore(options.Starting);
}
```

注册时传 Factory：

```csharp
.WithLight(options, MyLightStore.Factory)
```

Store 只负责当前值的读取、异步写入、恢复和外部变更通知。最大值、预留、
情感联动和费用计算仍由 LibraryOfRuinaLib 管理。外部系统改变当前值后，
Store 必须触发 `Changed`。实现 `ILibraryLightStoreIdentity` 虽非强制，但
持久化或多人资源应提供稳定、唯一的 `ResourceId`。

Roland 的 RitsuLib 适配器可参考
`RolandModCode/Light/RolandLightState.cs` 中的
`RolandRitsuLightStore`。该适配器属于 RolandMod，不属于基础库。

### 9.1 RolandRitsuLightStore 的完整职责

Roland 用 `ConditionalWeakTable<Player, ...>` 保证同一个 Player 获得同一个
Store，同时不阻止 Player 被回收：

```csharp
internal sealed class RolandRitsuLightStore :
    ILibraryLightStore,
    ILibraryLightStoreIdentity
{
    private static readonly ConditionalWeakTable<
        Player,
        RolandRitsuLightStore> Stores = new();

    private readonly Player _player;
    private int _fallbackCurrent;

    private RolandRitsuLightStore(
        Player player,
        LibraryLightOptions options)
    {
        _player = player;
        _fallbackCurrent = options.Starting;
    }

    // 必须是稳定、唯一的资源 ID。
    public string ResourceId => RolandLightService.ResourceId;

    // 外部资源系统改变余额后通知基础库重新读取。
    public event Action? Changed;

    public static ILibraryLightStore Factory(
        Player player,
        LibraryLightOptions options)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(options);

        return Stores.GetValue(
            player,
            owner => new RolandRitsuLightStore(owner, options));
    }
}
```

读取与写入：

```csharp
public bool TryRead(out LibraryLightStoreSnapshot snapshot)
{
    int current = _player.Creature.CombatState == null
        // 战斗状态尚未创建时使用 Builder 的 starting 值。
        ? _fallbackCurrent

        // 战斗中由 Ritsu 次级资源持有真实余额。
        : SecondaryResourceCmd.Get(
            _player,
            RolandLightService.ResourceId);

    _fallbackCurrent = Math.Max(0, current);
    snapshot = new LibraryLightStoreSnapshot(_fallbackCurrent);
    return true;
}

public async Task<LibraryLightStoreMutationResult> MutateAsync(
    LibraryLightStoreMutation mutation,
    AbstractModel? source = null)
{
    int amount = Math.Max(0, mutation.Amount);
    if (mutation.Kind == LibraryLightStoreMutationKind.Spend)
    {
        bool spent = await SecondaryResourceCmd.Spend(
            _player,
            ResourceId,
            amount,
            mutation.Card,
            source);
        return new LibraryLightStoreMutationResult(
            spent,
            new LibraryLightStoreSnapshot(
                SecondaryResourceCmd.Get(_player, ResourceId)));
    }

    int current = mutation.Kind switch
    {
        LibraryLightStoreMutationKind.Set =>
            await SecondaryResourceCmd.Set(
                _player, ResourceId, amount, source),
        LibraryLightStoreMutationKind.Gain =>
            await SecondaryResourceCmd.Gain(
                _player, ResourceId, amount, source),
        LibraryLightStoreMutationKind.Lose =>
            await SecondaryResourceCmd.Lose(
                _player, ResourceId, amount, source),
        LibraryLightStoreMutationKind.ResetToMaximum =>
            await SecondaryResourceCmd.Reset(
                _player, ResourceId, toMax: true, source),
        _ => throw new ArgumentOutOfRangeException(),
    };
    return new LibraryLightStoreMutationResult(
        true,
        new LibraryLightStoreSnapshot(current));
}
```

命令式 Store 让 Gain/Spend Hook、原因、历史、钳制和联网仍由 Ritsu
权威处理；返回的实际快照而不是请求值会写回 `LibraryLightState`。外部
变更通知：

```csharp
internal static void NotifyResourceChanged(
    Player player,
    int newAmount)
{
    if (!Stores.TryGetValue(
            player,
            out RolandRitsuLightStore? store))
    {
        return;
    }

    store._fallbackCurrent = Math.Max(0, newAmount);
    store.Changed?.Invoke();
}
```

Roland 在 `AfterSecondaryResourceChanged` 中调用 `NotifyResourceChanged`，
从而让 LibraryLightState 与 Ritsu UI 保持同一余额。Library 发起的命令
用异步上下文标记，回调不会在同一非重入锁内再次写 Light；外部命令超过
角色动态上限时才执行一次独立钳制。

### 9.2 Roland 的下游资源注册

```csharp
ModSecondaryResourceRegistry registry =
    ModSecondaryResourceRegistry.For(Entry.ModId);

_definition = registry.Register(
    "light",
    new SecondaryResourceDefinition(
        defaultAmount: RolandLightState.StartingLight,
        baseMaxAmount: RolandLightState.StartingLight,
        minAmount: 0,
        hardMaxAmount: 999,

        // 回复由 LibraryLightState 处理，Ritsu 不再自动回合回复。
        turnStartPolicy: SecondaryResourceTurnStartPolicy.None,

        persistencePolicy: SecondaryResourcePersistencePolicy.Combat,
        locTable: "gameplay_ui",
        titleKey: "ROLAND_MOD_LIGHT_HOVER.title",
        descriptionKey: "ROLAND_MOD_LIGHT_HOVER.description",
        smallIconPath:
            "res://RolandMod/images/cards/frames/light_cost.png",
        largeIconPath:
            "res://RolandMod/images/cards/frames/light_cost.png"));

// UI 属于 Roland/Ritsu，下游自行决定如何展示。
registry.AlwaysShowInCombatUiForCharacter<RolandModCharacter>("light");
```

关键是 `turnStartPolicy: None`：如果下游资源框架和 LibraryLightState 都
自动回复，就会一回合回复两次。

## 10. 预留与确定性

装备成功时，基础库创建不可变 lease 和预留计划：

- 固定费用或当时的 Light-X 值立即冻结。
- 装备后再改变费用、情感等级或资源上限，不会重算本次消费。
- 多个槽位共享资源时，会考虑已有预留，不能超额装备。
- 结算先预检全部资源，再按稳定资源 ID 顺序提交。
- 任一预检失败时不会扣除任何资源。
- 提交失败会回滚已提交资源，不留下部分扣费。
- 卸下、目标失败、结算失败和卡牌释放都会成对释放预留。

`ReservedEnergy`、`ReservedStars` 和
`ReservedSecondaryResources` 只是兼容投影。新代码应读取
`slot.Lease.ReservationPlan`，不要自行写入这些投影。

情感、Light、装备/目标/时机、预留和上限变化都会增加 gameplay
`Revision`；纯表现变化不会。多人请求应使用当前回合号和 Revision，过期
请求必须拒绝后重新同步。

可用快照 API：

```csharp
LibrarySpeedDiceStateSnapshot snapshot =
    LibrarySpeedDice.CreateSnapshot(state);

bool restored =
    LibrarySpeedDice.TryRestoreSnapshot(player, snapshot);
```

旧快照缺少扩展字段时使用兼容默认值。联机恢复后应比较双方快照和 gameplay
hash，而不是只比较界面。

### 10.1 Roland 如何把本地输入变成确定性请求

模块只负责把基础库输入交给网络层：

```csharp
public async Task<bool> RouteAsync(
    LibrarySpeedDiceInputRequest request)
{
    await RolandSpeedDiceNetwork.RequestAsync(request);

    // true 表示输入已由本模块消费，后续 InputRouter 不再执行。
    return true;
}
```

请求必须保留基础库给出的回合号和 Revision：

```csharp
new EquipMessage(
    request.SlotIndex,
    request.TurnNumber, // 防止上一回合的延迟消息污染当前回合
    request.Revision,   // 防止基于旧槽位/资源状态执行
    cardIdentity,
    cardModelIdHash,
    cardOrdinal,
    target);
```

权威 action 收到消息后，不直接改字段，而是调用公共 Execute API：

```csharp
bool applied = await LibrarySpeedDice.ExecuteEquipAsync(
    context.PlayerChoiceContext,
    context.Player,
    card,
    context.Message.SlotIndex,
    target,
    context.Message.TurnNumber,
    context.Message.Revision);

if (!applied)
{
    // false 是确定性校验 no-op：状态已经变化或请求非法。
    Entry.Logger.Warn(
        $"speed/equip validation no-op; "
        + $"turn={context.Message.TurnNumber} "
        + $"revision={context.Message.Revision}");
}
```

卸下和换目标同样调用：

```csharp
await LibrarySpeedDice.ExecuteUnequipAsync(
    player,
    slotIndex,
    turnNumber,
    revision);

await LibrarySpeedDice.ExecuteRetargetAsync(
    player,
    slotIndex,
    target,
    turnNumber,
    revision);
```

### 10.2 Roland 的同步 RNG

```csharp
public Rng? CreateGameplayRng(Player player) =>
    RitsuLibFramework.GetModPlayerRng(
        player,
        Entry.ModId,
        "speed-roll");

public Rng? CreateTargetRepairRng(Player player) =>
    RitsuLibFramework.GetModPlayerRng(
        player,
        Entry.ModId,
        "speed-target-repair");
```

两个流使用不同稳定 key，避免“修复一次目标”改变之后所有骰子结果。新角色
即使不用 RitsuLib，也必须从自己的同步 RNG 系统提供两个可重放流，不能用
`new Random()`。

### 10.3 Roland 的稳定目标键

```csharp
public static string GetStableTargetSortKey(Creature target)
{
    if (target.Player != null)
    {
        // 玩家使用跨客户端一致的 NetId。
        return $"player:{target.Player.NetId:D20}";
    }

    if (target.Monster != null
        && TryGetMonsterOrdinal(target, out int ordinal))
    {
        // 同型号多个怪物用稳定出现序号区分。
        return $"model:{target.Monster.Id}:{ordinal:D10}";
    }

    // 最后回退也不使用对象哈希。
    return $"fallback:{target.Monster?.Id}:{target.SlotName}";
}
```

`GetHashCode()`、Godot instance ID 和本地集合地址都不能作为稳定键。

### 10.4 Roland 的共享批次结算

```csharp
private static async Task ResolveStatesAsync(
    PlayerChoiceContext choiceContext,
    IReadOnlyList<LibrarySpeedDiceCombatState> candidateStates)
{
    if (candidateStates.Count == 0)
        return;

    // 使用基础库公开批次 API。
    // 不再通过反射调用私有 ResolveBatchCoreAsync。
    await LibrarySpeedDice.ResolveBatchAsync(
        choiceContext,
        candidateStates);
}
```

基础库随后按：

```text
FinalValue descending -> Player.NetId -> Slot.Index
```

稳定结算所有候选状态。

### 10.5 Roland 的快照扩展

创建：

```csharp
LibrarySpeedDiceStateSnapshot speed =
    LibrarySpeedDice.CreateSnapshot(state);

foreach (LibrarySpeedDiceSlotSnapshot slot in speed.Slots)
{
    // Roland 额外保存跨客户端可解析的 card/target identity。
    // 基础库快照仍保留槽位、lease、预留和 Light 扩展。
}
```

恢复：

```csharp
bool restored = LibrarySpeedDice.TryRestoreSnapshot(
    state.Player,
    new LibrarySpeedDiceStateSnapshot(
        saved.TurnNumber,
        saved.Revision,
        saved.HasRolled,
        saved.IsLocked,
        saved.CurrentTurnTriggeredCards,
        saved.PreviousTurnTriggeredCards,
        saved.BonusDrawPending,
        saved.DamageGivenAccumulator,
        saved.DamageReceivedAccumulator,
        saved.EmotionLevel,
        saved.EmotionUnits,
        restoredSlots)
    {
        // 新字段走附加扩展，不改变旧 positional 构造器。
        Extension = restoredExtension,
    });
```

下游快照必须先把 card/target 恢复成当前客户端真实模型，再交给基础库。不要
把另一客户端的对象引用、instance ID 或指针直接序列化。

## 11. 旧 API 兼容

以下旧入口仍保留：

- `LibrarySpeedDiceParticipant`
- 所有 participant delegate
- `LibrarySpeedDice.RegisterParticipant(...)`
- 旧快照 positional 构造器
- `LibraryClashResolver.Current`

旧 participant 会通过兼容适配器进入同一内核，原有 delegate 的签名与
调用时点不变。新角色应使用 `ForCharacter<T>()` Builder 和类型化模块；
不要在新代码中继续扩展全局 resolver。

### 11.1 Roland 的薄 facade 示例

RolandLightState 保留旧方法名，但不再拥有第二份 Light 状态：

```csharp
public int Light => _inner.Current;
public int MaxLight => _inner.Maximum;

public Task GainLightAsync(
    int amount,
    AbstractModel? source = null) =>
    _inner.Gain(amount, source);
```

RolandLightCost 用带作用域标记的中性 Energy modifier 保留改费顺序：

```csharp
public static void SetLightToFreeThisTurn(CardModel card)
{
    AddLightOnlyMarker(
        card,
        new LocalCostModifier(
            0,
            LocalCostType.Absolute,
            EndOfTurn | WhenPlayed,
            reduceOnly: false));
}
```

这个 marker 对 Energy 是 0 变化，对 Light 是绝对 0；因此资源命名和余额
完全分离，同时仍按原版 modifier 顺序清理、复制和计算。

### 11.2 Roland 仍存在的旧 participant 投影

Roland 的共享 resolver 仍会读取兼容 callback：

```csharp
var callback = state.Participant.AfterSpeedResolutionAsync;
if (callback != null)
{
    await callback(choiceContext, state);
}
```

这是兼容阶段代码。新批次逻辑应放进：

```csharp
ILibrarySpeedDiceLifecycle.BeforeResolutionBatchAsync
ILibrarySpeedDiceLifecycle.AfterResolutionBatchAsync
```

不要同时在旧 callback 和新 lifecycle 中注册同一个效果，否则会重复执行。

### 11.3 哪些 Roland 代码不应成为新范例

- `speedDiceStarCost`：已移除；不得再把 Light 命名或存储为 Stars。
- `GetSpeedDiceLightCost()`：旧 Roland facade；新代码使用
  `LibraryLight.GetCost(card)`。
- 空的 `SetReservation` / `ClearReservation` facade：预留已由基础库 lease
  管理，新代码不要手动维护第二份字典。
- `LibraryClashResolver.Current`：只保留兼容，不作为批次开始事件。
- 反射设置 `LibraryEmotionConfig`：仅用于兼容旧 DLL，新最低版本为 1.1.0
  时直接写属性。

## 12. 接入检查表

- [ ] manifest 依赖 LibraryOfRuinaLib `1.1.0`。
- [ ] DLL 引用不复制进下游发布包。
- [ ] 初始化时只注册一次，ID 全局唯一。
- [ ] `UnitThresholds` 正好五个正数。
- [ ] Roland Light-only 书页的速度骰子 Energy 为 0，且不使用 Stars。
- [ ] 普通手牌出牌若也消耗 Light，已由下游单独接入。
- [ ] 所有外部 Light 消费检查 `Available`。
- [ ] Use 后锁定通过当前 lease 的 `LockUnequip()` 完成。
- [ ] 批次开始逻辑使用 `BeforeResolutionBatchAsync`。
- [ ] 多人输入携带回合号与 Revision，RNG 和目标键可重放。
- [ ] 验证装备、卸下、换目标、失败释放、改费后冻结费用和多槽位防超额。
- [ ] 验证 Light 每回合回复、情感升级补满、费用升级/降级与 Light-X。
- [ ] 验证双人重连后的快照与 gameplay hash 一致。

Roland 的完整高级示例：

- 注册：`RolandModCode/Entry.cs`
- 组件：`RolandModCode/RolandSpeedDiceModule.cs`
- Light/Ritsu Store：`RolandModCode/Light/RolandLightState.cs`
- 同时实现两种卡牌接口：`RolandModCode/Cards/RolandPageCard.cs`

### 12.1 按 Roland 对照验收

注册检查：

```csharp
// Roland：角色类型、ID、速度范围、情感、Light、Store、模块齐全。
LibrarySpeedDice
    .ForCharacter<RolandModCharacter>(ModId, options)
    .WithEmotion(emotion)
    .WithLight(light, RolandRitsuLightStore.Factory)
    .UseModule(RolandSpeedDiceModule.Instance)
    .Register();
```

卡牌检查：

```csharp
// RolandPageCard：
// 1. 速度骰子 Energy 为 0，Roland 不使用 Stars；
// 2. Light 独立；
// 3. 速度骰子目标可与普通目标不同。
ILibrarySpeedDiceCard speedCard = page;
ILibraryLightCard lightCard = page;
```

资源检查：

```csharp
// Roland 的所有外部扣费必须以 Available 为上限。
bool canSpend = amount <= libraryLightState.Available;
```

时机检查：

```csharp
// Use 锁定当前 lease，不锁同型号的其他卡牌。
if (card.HasTiming(RolandCardTiming.Use))
    lease.LockUnequip();

// 批次逻辑使用类型化 context。
await module.BeforeResolutionBatchAsync(context);
```

多人检查：

```csharp
// 所有写操作带 TurnNumber + Revision。
await LibrarySpeedDice.ExecuteEquipAsync(
    context,
    player,
    card,
    slotIndex,
    target,
    turnNumber,
    revision);

// 多玩家统一批次。
await LibrarySpeedDice.ResolveBatchAsync(context, states);
```

## 13. 可复制的“类 Roland”完整骨架

下面只包含 LibraryOfRuinaLib 必需部分。卡牌注册、图鉴、资源图标和 UI
继续使用你自己的框架。

### 13.1 入口

```csharp
using Library.Light;
using Library.SpeedDice;

internal static class MyLibraryRegistration
{
    public const string ModId = "MyCharacterMod";

    public static void Register()
    {
        LibrarySpeedDice
            .ForCharacter<MyCharacter>(
                ModId,
                new LibrarySpeedDiceOptions(
                    BaseCount: 1,
                    MinRoll: 2,
                    MaxRoll: 7))
            .WithEmotion(
                new LibraryEmotionConfig
                {
                    UnitThresholds = [3, 3, 5, 7, 9],
                    GainEmotionFromDamage = true,
                    DamageUnitFractionOfMaxHp = 0.10m,
                    ExtremeRollEmotionUnits = 1,
                    KillEmotionUnits = 3,
                    AllyDeathEmotionUnits = 0,

                    // 原版 Energy，与 Light 无关。
                    MaxEnergyPerLevel = 0,

                    ExtraSpeedDieLevel = 4,
                    ExtraSpeedDice = 1,
                    BonusDrawLevel = 5,
                    BonusDrawRequiredTriggeredCards = 2,
                    BonusDrawAmount = 1,
                })
            .WithLight(
                new LibraryLightOptions(
                    starting: 4,
                    baseMaximum: 4,
                    maximumPerEmotionLevel: 1,
                    recoveryPerTurn: 1,
                    refillOnLevelIncrease: true))
            .UseModule(MyCharacterModule.Instance)
            .Register();
    }
}
```

与 Roland 的差异只有：

- `RolandModCharacter` 换成 `MyCharacter`。
- `RolandRitsuLightStore.Factory` 被省略，因此使用默认内存 Store。
- 数值和模块换成自己的实现。

### 13.2 书页基类

```csharp
using Library.Light;
using Library.SpeedDice;
using MegaCrit.Sts2.Core.Entities.Cards;

public abstract class MyPageCard(
    int baseLightCost,
    TargetType speedDiceTarget)
    : MyCardBase,
      ILibrarySpeedDiceCard,
      ILibraryLightCard
{
    // 类 Roland：装备时不使用原版 Energy，本角色也不使用 Stars。
    public LibrarySpeedDiceResourceCost SpeedDiceResourceCost { get; }

    public TargetType SpeedDiceTargetType { get; } =
        speedDiceTarget;

    public int BaseLightCost { get; } =
        Math.Max(0, baseLightCost);

    public virtual bool HasLightCostX => false;

    protected void UpgradeLightBy(int amount)
    {
        LibraryLight.GetCost(this).UpgradeBy(amount);
    }
}
```

如果你的 `MyCardBase` 需要构造参数，把普通 Energy 和普通 Target 单独传给
它；不要复用 `baseLightCost`。

### 13.3 角色模块

```csharp
using Library.Light;
using Library.SpeedDice;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

internal sealed class MyCharacterModule :
    ILibrarySpeedDicePolicy,
    ILibrarySpeedDiceLifecycle,
    ILibraryLightPolicy
{
    public static MyCharacterModule Instance { get; } = new();

    private MyCharacterModule()
    {
    }

    public string Id => "my-character.speed-dice";

    public int Order => 0;

    public bool CanEquipCard(
        LibrarySpeedDiceCombatState state,
        CardModel card) =>
        // 类 Roland：只允许自己的书页。
        card is MyPageCard;

    public async Task OnUseAsync(
        PlayerChoiceContext choiceContext,
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot,
        LibrarySpeedDiceCardLease lease)
    {
        if (lease.Card is not MyPageCard card)
            return;

        // 只有自己的 metadata 要求时才锁定。
        if (card.ShouldLockAfterUse)
            lease.LockUnequip();

        await card.InvokeSpeedDiceUseAsync(choiceContext);
    }

    public Task OnTargetedUseAsync(
        PlayerChoiceContext choiceContext,
        LibrarySpeedDiceCombatState state,
        LibrarySpeedDiceSlot slot,
        LibrarySpeedDiceCardLease lease,
        Creature target) =>
        lease.Card is MyPageCard card
            ? card.InvokeSpeedDiceTargetedUseAsync(
                choiceContext,
                target)
            : Task.CompletedTask;

    public Task BeforeResolutionBatchAsync(
        LibrarySpeedDiceResolutionBatchContext context)
    {
        // 在这里实现类 Roland 的“交锋开始”。
        return Task.CompletedTask;
    }

    public int ModifyTurnRecovery(
        LibraryLightState state,
        int currentRecovery) =>
        currentRecovery;
}
```

示例中的 `ShouldLockAfterUse`、`InvokeSpeedDiceUseAsync` 和
`InvokeSpeedDiceTargetedUseAsync` 是你需要在 `MyPageCard` 上定义的下游
成员，不属于基础库 API。

### 13.4 效果代码

```csharp
using Library.Light;
using Library.SpeedDice;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

public static async Task GainLightAndEmotionAsync(
    Player owner,
    AbstractModel source)
{
    if (LibraryLight.TryGetState(
            owner,
            out LibraryLightState? light)
        && light != null)
    {
        // 所有状态修改都 await。
        await light.Gain(1, source);
    }

    // 情感是同步 gameplay 状态。
    LibrarySpeedDice.AddEmotionUnits(owner, 1);
}

public static async Task SpendExternalLightAsync(
    Player owner,
    int amount,
    AbstractModel source)
{
    if (!LibraryLight.TryGetState(
            owner,
            out LibraryLightState? light)
        || light == null
        || !light.HasEnoughAvailable(amount))
    {
        return;
    }

    // HasEnoughAvailable 使用 Current - Reserved，
    // 不会偷走已经分配给速度骰子书页的 Light。
    await light.Lose(amount, source);
}
```

### 13.5 最终目录映射

```text
MyCharacterMod/
├─ Entry.cs                         # 调用 MyLibraryRegistration.Register()
├─ MyLibraryRegistration.cs         # Builder 配置
├─ MyCharacterModule.cs             # Policy / Lifecycle / Light Policy
├─ Cards/
│  ├─ MyPageCard.cs                 # 两个卡牌接口
│  └─ ...
├─ Light/
│  └─ MyLightStore.cs               # 可选；不用外部资源框架时不需要
├─ Networking/
│  └─ MySpeedDiceNetwork.cs         # 多人时需要
└─ MyCharacterMod.json              # LibraryOfRuinaLib 依赖
```

对应 Roland：

```text
RolandModCode/Entry.cs
RolandModCode/RolandSpeedDiceModule.cs
RolandModCode/Cards/RolandPageCard.cs
RolandModCode/Light/RolandLightState.cs
RolandModCode/Networking/RolandSpeedDiceNetwork.cs
```
