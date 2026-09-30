# LibraryOfRuinaLib 架构与维护说明

本库是《废墟图书馆》系列模组共用的战斗基础库：伤害类型与抗性、混乱（Chao / stagger）、
自定义钩子、骰子、速度骰子与情感、Light 资源，以及多人同步用的托管网络协议。
下游（LibraryOfRuina、RolandMod 等）按**已发布的 DLL 二进制**绑定本库，见文末「兼容性规则」。

## 目录速览

| 目录 | 内容 |
| --- | --- |
| `Hooks/` | `LibraryHooks`：向所有战斗监听器派发库钩子。`LibraryHooks.cs` 是事件类钩子，`LibraryHooks.Modifiers.cs` 是数值修改类钩子。 |
| `Models/` | `ILibraryAbstractModel`（钩子接口）、各 `Library*Model` 基类、能力（Power）基类。 |
| `Powers/` | 库自带的能力；`LibraryPowerMode/` 是可切换模式的能力（烧伤、流血、充能）。 |
| `Commands/` | `LibraryCreatureCmd`（伤害/混乱伤害/格挡/混乱）、`LibraryAttackCommand`、`LibraryPowerCmd`。 |
| `Entities/` | `LibraryCreature`：图书馆怪物的战斗实体（混乱值、抗性、混乱状态）。 |
| `Combat/` | 对外扩展点：伤害类型改判、攻击目标过滤、数值解析策略、受击拦截、血条预告。 |
| `Patches/` | Harmony 补丁：把库接入原版伤害管线、界面与网络。 |
| `SpeedDice/` | 速度骰子、情感与 Light 的运行时；`LibrarySpeedDiceService.*.cs` 按职责拆分。 |
| `Light/` | Light 资源（独立于能量的第二种费用）。 |
| `Multiplayer/` | 托管网络协议（消息/动作的稳定类型键与编解码）。 |
| `localization/` | 动态变量（骰子、伤害变量）与抗性预览、本地化表合并。 |

## 伤害管线

1. 原版 `CreatureCmd.Damage` 被 `DamageTargetPatch`（改目标）与 `LibraryAttackChaoDamagePatch`（Priority.Last）拦截。
   对玩家或怪物发出的、带 Move 且非 Unpowered 的非 Library 卡牌伤害：目标里有图书馆怪物，
   或存在 `ILibraryIncomingDamageInterceptor` 且有非玩家目标时，改走 `LibraryCreatureCmd.Damage`。
2. `LibraryCreatureCmd.Damage` 与原版同构（便于对照游戏更新）：修改伤害 → 受击前钩子 → 拦截 → 格挡 →
   物理抗性（`LibraryDamageCalculate`）→ HP 损失钩子（Osty 前后）→ 结算与表现 → 事后钩子。玩家目标直接交回原版。
3. 攻击随后按「伤害 − 攻击前格挡」追加混乱伤害（`LibraryCreatureCmd.ChaoDamage`），混乱值归零即进入混乱。
4. 伤害类型：Library 卡牌自带类型；原版卡/攻击由 `LibraryDamageTypes` 推断，再交给已注册的 `ILibraryDamageTypeModifier`。
5. 数值策略：实现 `ILibraryCombatValueResolutionPolicy` 的监听器可让预览与实战只保留基础值（`LibraryCombatValueResolver`）。

## 钩子系统

- `ILibraryAbstractModel` 定义库钩子；`LibraryHooks` 遍历 `IterateHookListeners()`，
  配对钩子先调原版 `AbstractModel` 钩子、再调库重载，每个监听器结束后 `InvokeExecutionFinished`。
- 数值钩子统一走四种 pass：加法、乘法、上限（取最小）、变换（整数部分变化才算修改者）。
- `LibraryMultipleModePowerModel` 把每个钩子先交给当前 `Mode`，再交给子类的 `object? _ = null` 重载；
  原签名已 sealed，子类只重写带 `_` 的重载。`LibraryTurnsPowerModel` 的回合钩子同理。

### 新增一个库钩子

1. 在 `ILibraryAbstractModel` 对应分组里声明。
2. 在 `Models/LibraryModelHookDefaults.cs` 的**每个**基类块里加同一行默认实现（漏掉会报 CS0535）。
3. 在 `LibraryHooks` 里用 `ForEachLibraryListener` / `ForEachListener` / 数值 pass 写派发。
4. 若是能力会用到的钩子：在 `LibraryPowerMode` 加默认实现，在 `LibraryMultipleModePowerModel` 加
   sealed 转发和 `object? _` 重载（按文件内说明的组合规则）。

## 兼容性规则

- **不要删除或改签名任何 public/protected 成员**（含基类里的空默认实现）：下游按二进制绑定，
  删掉会在运行时出现 MissingMethodException 或静默丢失重写。可以新增成员、把非 virtual 改为 virtual。
- 多人同步：新代码只读同步状态、结果与遍历顺序确定；不要改动网络动作/消息的序列化布局。
- 版本号与下游依赖约束只在明确要求时修改。

## 验证方法

- 构建：`dotnet build .\LibraryLib.csproj /p:Sts2Flavor=Beta`（`LibraryLib.csproj` 未纳入 Git，新 worktree 需从主检出复制）。
- 本地契约测试（`tests/`，未纳入 Git，用 `dotnet run --project tests/<名称>` 运行）：
  - `HookDispatchContract`：钩子派发顺序、修改者统计、模型作用域与短路规则。
  - `PatchBindingContract`：在游戏外对全部补丁执行 PatchAll，列出绑定并检查反射查找。
  - `CombatValueResolutionContract`、`ManagedNetCodecContract`：数值策略与网络编解码。
- 公共 API 变更前后，用反编译或 API 导出对比公共成员；有 API 变化时重建下游
  `D:\sls2-resource\RolandMod` 与 `D:\sls2-resource\sls2-better-extension-mod`。
