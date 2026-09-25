# AGENTS.md — AI 编程助手指南

> 本文件为本项目 AI 编程规范的**权威源**。详细规则见 `docs/B/B-01-开发规范.md`；原版 API 证据见 `docs/C/C-05-原版API简述.md`；长期事实与约定见 `docs/C/C-03-项目长期记忆.md`。

***

## 一、项目身份

**Clap Your Hands** — RimWorld 1.6 社交互动扩展模组。为殖民者加入「击掌」互动：四档结果（糟糕/普通/不错/完美）带来不同情绪与好感度变化，完美击掌额外给予 12 小时随机增益。

| 项    | 值                                                                          |
| ---- | -------------------------------------------------------------------------- |
| 模组名  | Clap Your Hands                                                            |
| 包 ID | `yanyu.clapyourhands`                                                      |
| 语言   | C# 10 / .NET Framework 4.7.2（`net472`，`LangVersion 10.0`）                   |
| 游戏   | RimWorld 1.6（`Assembly-CSharp`）+ Unity（`UnityEngine.CoreModule`）            |
| 依赖   | **零第三方依赖** — 不使用 Harmony、不使用 HugsLib（见「四、架构核心」）                             |
| 模组主类 | `ClapYourHands.ClapYourHandsMod`（原版 `Verse.Mod`），在 `About.xml` 的 `modClass` 中声明 |
| 输出   | SDK 风格 csproj，`OutputPath` 直出 `Clap_Your_Hands/Assemblies/Clap_Your_Hands.dll` |

### 设计文档（只读参考，改动需先对齐）

- `plans/击掌功能开发计划.md` — 功能设计、数值表、概率模型、实现顺序（**阶段性思路，非事实源**）
- 玩家可见文本一律进 `Languages/`，中英逐键对称

***

## 二、常用命令

| 操作     | 命令                                                                    |
| ------ | --------------------------------------------------------------------- |
| 编译     | `dotnet build Clap_Your_Hands/Clap_Your_Hands.csproj`（输出直写 `Assemblies/`） |
| 清理构建产物 | 运行 `11.bat`                                                           |
| 覆盖游戏路径 | `dotnet build ... -p:RimWorldManagedDir="<Managed 目录>"`                 |
| 反编译原版  | `ilspycmd` 输出到 `decompiledFiles/`（布局规范见「十、文档维护」）                       |
| 检查 XML | PowerShell `[xml](Get-Content <file>)` 解析验证                           |
| 游戏日志   | 游戏目录 `Player.log`（检索前缀 `[ClapYourHands]`）                             |

> 游戏程序集引用路径为 Steam 本地目录（见 csproj 属性 `RimWorldManagedDir`），CI 上不可编译；构建与测试均在本机完成。

***

## 三、Boundaries

**Allowed**：`Clap_Your_Hands/Source/`、`Defs/`、`Patches/`、`Languages/`、`Textures/`、`docs/`、XML 文件。
**Ask First**：csproj 程序集引用变更、`About.xml` 依赖/版本变更、破坏性重构、新增第三方依赖。
**Never Touch**：`decompiledFiles/` 中内容（只读参考）、游戏 `Data/` 原版 XML、第三方 DLL、`Player.log`（仅读取分析）。

***

## 四、架构核心：零补丁原则（本项目最重要）

本模组**刻意不使用任何 Harmony 补丁**。原版 1.6 已提供完整扩展点，全部功能应在官方虚方法/Def 内实现：

```
✅ 新增 InteractionDef + 自定义 InteractionWorker → 自动进入原版随机社交池
   （Pawn_InteractionsTracker.TryInteractRandomly 遍历 DefDatabase<InteractionDef>.AllDefsListForReading，
     按 Worker.RandomSelectionWeight(initiator, recipient) 加权抽取）
✅ 结算逻辑写 InteractionWorker.Interacted(initiator, recipient, ...) 虚方法
✅ 加情绪 / 加好感 → Pawn_InteractionsTracker.AddInteractionThought(pawn, other, thoughtDef)（public static）
✅ 长时增益 → HediffDef + stages[].statOffsets / statFactors（自动 Scribe、自动过期）
❌ 为「能少写几行」引入 Harmony 补丁——补丁是本项目最后手段，引入前必须先问
```

**判定顺序**：任何需求先问「原版有没有扩展点/Def 字段能表达」，再考虑代码，最后才考虑补丁。

### 关键机制（实现前务必读 `docs/C/C-05-原版API简述.md`）

| 需求 | 原版机制 |
|---|---|
| 互动被小人自动触发 | `InteractionWorker.RandomSelectionWeight` 返回权重（0 = 不抽取） |
| 敌对排除 | 原版 `TryInteractRandomly` 已挡 `pawn.HostileTo(p)`，**无需自己写** |
| 仇视排除 | 在 `RandomSelectionWeight` 里返回 0 |
| 好感度 | opinion 是**派生值**，没有「直接 +N」的 API；必须用 Social 型 `ThoughtDef`（`opinionOffset`） |
| 互动冷却 | 原版 `lastInteractionTime` 是 private（仅 120 tick 硬下限）；24h 冷却用**隐藏 Hediff** 承载 |
| 灵感 | `pawn.mindState.inspirationHandler.GetRandomAvailableInspirationDef()` + `TryStartInspiration()` |
| 工作速度 / 移速 | `StatDefOf.WorkSpeedGlobal`（factor）、`StatDefOf.MoveSpeed`（offset） |

***

## 五、强制开发规范

### 核心开发原则

```
✅ 数值系数抽为 private const 或 Def 字段（顶部集中，便于调参）
✅ 随机抽取必须有「可抽项为空即返回」的保护，禁止可能死循环的权重重试
✅ 需要跨存读档保持的运行时状态，随所属对象 ExposeData 持久化（Hediff / GameComponent），禁止裸 static
✅ 对外入口（互动结算 / Debug 指令）先校验关键依赖（null → Log.Error + return），校验排在**任何副作用之前**
✅ 动手前先反编译确认原版 API 签名/字段（不改则问，不凭旧记忆猜）
✅ 玩家可见文本一律走本地化四条路径，不在逻辑代码里硬编码
❌ 凭旧记忆写 RimWorld API（版本差异大，易踩坑）
❌ 引入可由现有依赖覆盖的新依赖；❌ 无必要地引入 Harmony/HugsLib
❌ 只走 happy path、吞掉异常、硬编码不再问
❌ 把需要存读档的状态放进不参与 Scribe 序列化的字段
```

### 代码规范（C# 核心规则）

```
✅ 类/接口/方法/属性/常量/枚举 → PascalCase；接口前缀 I；私有字段 → _camelCase
✅ 常量数字集中为 const；字符串用 $"" 插值；空值判断用 is null / is not null
✅ 过滤/转换/聚合用 LINQ；❌ foreach 中增删集合（用 ToList 快照或反向遍历）
✅ 只在系统边界（玩家输入/原版回调）校验，内部代码信任框架保证
✅ 避免过度注释 — 仅在逻辑不自明处写注释
```

### 注释规范

```
✅ 注释只描述**当前状态** —— 这个类型 / 成员现在是什么、做什么
✅ 力求简短，一两句话概括即可
✅ 仅在逻辑不自明处、或需说明接口契约与约束时才写注释
✅ 实现发生变化时同步更新注释 —— 与实现不符的注释比冗长的注释更糟
❌ 不写历史状态（"此前…"、"原本…"、"已由…改为…"）
❌ 不写设计理由与推导（"为了…"、"因此…"、"之所以…"、"为什么不直接用…"）
❌ 不写方案对比与前后变化（"而不再需要…"、"比…更…"）
❌ 不写改动过程与决策记录 —— 改动缘由属于 `docs/`、`plans/` 与提交信息
```

### XML 定义

```
✅ 引用类名带命名空间前缀（如 <hediffClass>ClapYourHands.Hediff_ClapBuff</hediffClass>）
   —— 例外：InteractionDef/ThoughtDef 的 workerClass/thoughtClass 原版用简名，按其惯例
✅ 新 Def 的 defName 全局唯一，统一前缀 Clap / ClapYourHands
✅ 逻辑优先做进 Defs（Def 字段能表达的不写代码），减少代码分支
```

### 本地化（四条文本路径）

```
✅ Def 字段文本 → DefInjected（文件在 Languages/<语言>/DefInjected/<对应 Def 类型目录>/，键为 <DefName>.<字段>）
✅ rulesStrings 整表替换 → DefInjected
✅ 代码内文本 → Keyed + "Key".Translate()（键名 ClapYourHands.<域>.<语义>）
✅ Def 内文本一律保留作默认/兜底，由语言文件覆盖（原版模式 <label TKey="Xxx">Default</label>）
❌ 把 Keyed 键填进 Def 字段——Def 字段不会自动 Translate，只会把键原样显示
❌ 清空 Def 内文本（语言键缺失时会退化成 defName）
```

***

## 六、Git 规范

- 分支：`main` 唯一常驻，正式版本用 tag 发布（版本号见 `About.xml`）
- 提交：中文描述，一句话说清变更（如 `添加了击掌互动` / `修复了X` / `重构了X`）
- ❌ `git push --force` / `git reset --hard`
- ❌ 提交 `Assemblies/` 第三方 DLL、`obj/`、`decompiledFiles/`、`Player.log`、临时文件（`.gitignore` 已覆盖）

***

## 七、模块速查表

| 文件 | 职责 |
| --- | --- |
| `ClapYourHandsMod.cs` | 模组主类（原版 `Verse.Mod`）；日志前缀 `LogPrefix` |
| `ClapDefOf.cs` | `[DefOf]` Def 引用缓存（字段名必须与 `defName` 完全一致） |
| `ClapOutcome.cs` | 击掌结果档位枚举（枚举值同时是权重数组下标） |
| `ClapUtility.cs` | **核心逻辑**：抽取权重、好感度曲线（`SimpleCurve`）、结果抽取、结算应用、可用手判定（`TryGetHand`）、增益与冷却写入。**所有可调数值集中在本文件顶部** |
| `InteractionWorker_Clap.cs` | 原版扩展点：`RandomSelectionWeight`（触发权重与排除）+ `Interacted`（结算入口） |
| `ClapEffects.cs` | 完美击掌的 Fleck 表现：双方中点处的闪电纹样（核心 + 外发光两层）与溅落火花；音效播放与试听 |
| `ClapSettings.cs` | 模组设置页（`ModSettings`）：音效音量缩放滑块（0%~100%）+ 试听按钮；`ClapYourHandsMod.Settings` 静态可读 |
| `Hediff_ClapBuff.cs` | 12h 增益载体：等级合并（取最高／同级覆盖时长）；时长用原版 `HediffComp_Disappears.SetDuration` |
| `Hediff_ClapCooldown.cs` | 24h 冷却载体：`Visible => false` 隐藏；时长同样用 `HediffComp_Disappears` |
| `ClapDebug.cs` | 开发期调试开关（静态 bool，**不参与存档**）；`WeightMultiplier` 常量 |
| `DebugTabMenu_ClapSettings.cs` | 把 `ClapDebug` 的静态开关**并入原版 settings 分页**（复用共享的 `absRoot`，归类到 `Clap` 分类；复选框由 `DebugActionNode.settingsField` 渲染） |
| `Defs/DebugTabMenuDefs/` | `DebugTabMenuDef`（注册入口；零补丁限制：分页栏会随之多一个 tab，其内容复用原版 Settings 节点） |
| `Defs/InteractionDefs/` | 互动定义（`Clap`） |
| `Defs/ThoughtDefs/` | 四档结果想法（Social；`baseMoodEffect` + `baseOpinionOffset`） |
| `Defs/HediffDefs/` | `Clap_Buff`（3 级增益）、`Clap_Cooldown`（隐藏冷却） |
| `Defs/FleckDefs/` | 黑闪闪电纹样（`Clap_BlackFlashBoltCore` / `Clap_BlackFlashBoltGlow`，各含 4 张随机贴图变体）与溅落火花 `Clap_HandSpark`（`FleckSystemThrown`，抛物线拱 + 空气阻力） |
| `Defs/SoundDefs/` | `Clap_HandClap`：完美击掌音效，`AudioGrain_Folder` 从 `Sounds/Clap_Your_Hands/` 随机取变体 |
| `Textures/ClapYourHands/` | 闪电纹样 4 变体（程序化生成，脚本 `tools/gen_clap_fleck.py`）与火花 `ChargeSpark`（均为白色 + 透明底，靠 `color` 染色） |
| `Sounds/Clap_Your_Hands/` | 完美击掌音效素材（单声道 44.1 kHz 16-bit WAV；**同目录只放同一效果的变体**，`AudioGrain_Folder` 会全量随机） |
| `Languages/` | 中英逐键对称的本地化文本 |

> 新增模块/文件时同步更新本表。

***

## 八、AI 高频错误防犯清单（本技术栈）

| #  | 易错点 | 正确做法 |
| -- | --- | --- |
| 1  | 凭记忆写 RimWorld API，方法/字段不存在 | 先 `ilspycmd` 反编译核对 |
| 2  | 复用原版的「自动挑选 / 自动填充」逻辑（如 `HediffDef.defaultInstallPart`、原版工厂方法）时，未核对其**筛选条件** | 先反编译确认它怎么筛：原版常只按 Def 匹配、**不校验可用性**（如 `body.AllParts` 含缺失部位却不过滤），缺数据会在更晚的结算期才炸（见 `B-04` BUG-001） |

***

## 九、文档维护

- **维护范围**：`docs/` 目录下的文件与根目录 `AGENTS.md`。
- **不加时间戳**：维护文档时不在文件头部添加或更新「更新: 日期」之类的时间戳。
- **计划文件统一存放 `plans/`**：开发计划一律放 `plans/`，不散落在 `docs/` 或工具目录。**计划仅为阶段性思路参考，其中的代码/数值/接口可能与最终实现存在差异，落地一律以 `Source/` `Defs/` 实际代码与用户确认为准。**
- **反编译文件管理**：`ilspycmd` 输出统一到 `decompiledFiles/`，按「命名空间目录 + 类型名文件」存放；该目录不提交 git、不参与构建、不得当作作品代码改动。
- **中文思考链**：始终使用中文输出思考链，便于与用户协作审阅。
- **疑问即问**：对用户的要求或说明有疑惑时直接提问确认；不猜测、不否定。
- **结论需证据**：给出任何结论前先查证（反编译优先）；无证据时明确标注「无证据/未经验证」。

### 何时更新哪些文档

| 操作 | 更新内容 |
| --- | --- |
| 新增模块/文件 | 更新「七、模块速查表」 |
| 新增/迁移玩家可见文本 | 按「四条文本路径」处理 + 更新 `docs/C/C-06-本地化术语表.md` |
| 发现新的原版 API 事实 | 追加到 `docs/C/C-05-原版API简述.md`（附反编译来源） |
| 修复典型 Bug | 写入 `docs/B/B-04-BUG知识库.md`；根因是规范缺失则反哺 B-01 与本文件 |
| 新增依赖/工具 | 更新「项目身份」技术栈行 |
| 做出架构决策 | 追加到 `docs/C/C-02-架构详解.md` |
