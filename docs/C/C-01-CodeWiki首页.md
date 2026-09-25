# C-01 CodeWiki 首页

> 代码 Wiki 入口。维护核心类索引与阅读路线，帮助新 Agent/开发者快速定位。

## 阅读路线

| 想了解 | 入口 |
| --- | --- |
| 模组入口与初始化 | `ClapYourHandsMod.cs`（原版 `Verse.Mod`；`About.xml` 的 `modClass` 声明；日志前缀 `LogPrefix`；设置入口） |
| **零补丁架构与扩展点** | `AGENTS.md`「四、架构核心」+ `docs/C/C-02-架构详解.md` |
| 互动为何被触发 / 频率 / 排除项 | `InteractionWorker_Clap.RandomSelectionWeight` + `C-02` 四 |
| 结果抽取与结算 | `InteractionWorker_Clap.Interacted` + `ClapUtility.cs` |
| 概率模型 | `ClapUtility` 顶部常量与曲线（`BaseSelectionWeight`、`BaseOutcomeWeights`、`PerfectRewardWeights`、`PositiveWeightFactor` / `NegativeWeightFactor`） |
| 情绪与好感度 | `Defs/ThoughtDefs/Thoughts_Clap.xml` + 原版 `Pawn_InteractionsTracker.AddInteractionThought` |
| 12h 增益与叠加规则 | `Hediff_ClapBuff.cs` + `Defs/HediffDefs/Hediffs_Clap.xml` |
| 24h 冷却 | `Hediff_ClapCooldown.cs` + `Clap_Cooldown` Def |
| 完美击掌的特效与音效 | `ClapEffects.cs` + `Defs/FleckDefs/` + `Defs/SoundDefs/`（设计见 `C-02` 六） |
| 模组设置页（音量滑块 / 试听） | `ClapSettings.cs` + `ClapYourHandsMod.cs`（设计见 `C-02` 七） |
| 贴图与音频资源 | `Textures/ClapYourHands/`、`Sounds/Clap_Your_Hands/`；闪电贴图生成脚本 `tools/gen_clap_fleck.py` |
| 原版 API 事实（反编译证据） | `docs/C/C-05-原版API简述.md` |
| 本地化 | `Languages/`（四条文本路径见 `AGENTS.md`「五、强制开发规范」） |

## 内容索引

- `C-02-架构详解.md` — 系统设计与生命周期、状态载体、判定顺序、表现层与设置页
- `C-03-项目长期记忆.md` — 技术基线、架构决策（ADR）、已核实原版行为
- `C-04-已实现内容清单.md` — 功能进度
- `C-05-原版API简述.md` — 反编译证据 API 速查
- `C-06-本地化术语表.md` — 中英译名基准
- 设计与数值：`plans/击掌功能开发计划.md` + `docs/A/A-01-PRD.md`
