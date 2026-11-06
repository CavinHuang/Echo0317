# Stage 29｜剧情选择后果可见 + 独立审计推理门槛

更新：2026-11-06。项目：Unity 6.3 LTS Windows 影游《03:17 · 回声》。

## 修复的真实游戏问题

以往 `story.json` 在九个场景里包含了依据玩家线索变化的 `reactions` 文本，但 `EchoGame.Render()` 只渲染 `n.lines`：玩家前面调查过门锁、照片或消息回执，后续台词没有反映这些选择。

另外，玩家可以不解开五道推理中的任何一道，单凭线索旗标走到最复杂的「独立审计」结局。推理板形同一个可跳过的附加菜单。

### 本轮真正进入可玩流程的修复

1. **真实剧情反馈**：`EchoScenePresentation.Lines()` 依据已经取得的 canonical flags 选出每个剧情节点的 `if` / `unless` 反应，并拼到当前主剧情上。第一幕 `c1_hook` 会根据玩家检查的猫眼/照片/密码/回执/录音展示不同后果，第四幕会呼应玩家是否曾越权/误认。
2. **历史回顾同源**：`EchoDialogueHistory` 只根据当时历史检查点的 `Flags` 和在该时间点之前的推理记录生成对白。之后取得的线索或答案不能修改过去的台词，读档和回溯也不会泄露废弃路径的内容。
3. **审计结局真实推理门槛**：在已有剧情合法拿到双方独立签署 `joint_signed` 后，最终 `c5_gate → ending_audit` 还要求在原来的双证据推理板完成 `independent_will` 案件。必须用「协议内容」(`protocol_read`) 与「林岚独立意愿」(`lin_willing`) 两张真正取得的证据正确推断 `independent`。
4. **可读的锁定提示**：未完成推理时，审计选项会提示玩家去哪里解锁；原来的合并、循环、终止结局不要求这道推理，四结局继续可达。完成推理后自动刷新实际剧情选项，玩家无需离开游戏或重进节点。
5. **不只锁按钮**：`EchoNarrative.Advance` 真正检查已解开案例，`Normalize` 逐步验证一条历史时间线在抵达审计结局的**当时**是否已经完成推理。后补答题、跳过、伪造历史都不会被接受。重播所有四结局的原生 C# 核心与 Player 自检现明确模拟玩家完成这道案子的操作。
6. **旧存档有限迁移**：存档 `format=7`。仅在真正已到达审计结局的旧格式 5/6 存档中，依据历史合法已有的两条证据补入单次推理核实，以免先前完成的结局突然无法加载。旧的普通中途存档不虚构推理；新的 format-7 存档绝不享受该迁移通道。

### 实现与测试

| 源码/测试 | 作用 |
|---|---|
| `Assets/Scripts/Core/EchoScenePresentation.cs` | 依据历史证据与已解案件组合显示对白 |
| `Assets/Scripts/Core/EchoNarrative.cs` | `AllowedWithDeductions`、历史时间线案件校验、存档 v7 |
| `Assets/Scripts/Core/EchoData.cs` | 分支 `requiresSolved` 配置 schema 和有效 ID 检查 |
| `Assets/StreamingAssets/Data/story.json` | `c5_gate` 的独立意愿门槛 |
| `Assets/Scripts/Runtime/EchoGame.cs` | 实际 UI 锁定状态、正确对白、字幕和推理成功解锁后的选项重绘 |
| `Assets/Scripts/Core/EchoDialogueHistory.cs` | 只使用历史真实拥有的证据与解题结果 |
| `Assets/Scripts/Core/EchoSaves.cs` | v5/v6 完成审计结局的安全迁移、v7 强制规则 |
| `Tools/CoreHost/Program.cs` | 真实 .NET 8 编译执行四结局、伪造存档拒绝、复盘、旧档迁移 |
| `Assets/Scripts/Runtime/EchoNativeQa.cs` | 真正编译后的 Windows Player 需核验这道推理才可走审计结局 |
| `Assets/Tests/EditMode/EchoStage29NarrativeConsequencesTests.cs` | Unity 原生编辑模式的剧情/推理/迁移单测（尚未实测） |
| `Assets/Tests/EditMode/EchoStage29ProjectBootstrapTests.cs` | Unity Editor 首次导入后验证真实 `.unity` 入口场景存在、AssetDatabase 已导入、Build Settings 与 Player 信息一致（尚未执行） |
| `Tests/test_stage29_narrative_consequences.py` | 数据/核心/UI 接线回归 |

## 当前明确未完成的部分

- **只对审计结局设置了 1 道强制推理**，其余 4 道仍是可自愿完成的调查案件。避免瞬间把四结局破坏成死路，本轮不冒充整个调查系统已做到 100% 完整剧情联动。
- `required` 场景热点仍未作为硬性剧情拦截。它们的点击访问会被存档并进入调查手记；未虚构「点一下就获取证据」功能。
- Unity Editor/PlayMode、影像画面、输入和 Windows EXE 仍未获得真实可运行验证，不能把非 Unity CI 等同于已发行的 Alpha。仍缺 27 个正式获批镜头和配音/演出。

## 下一阶段

优先进行真正 Unity 首次导入、构建和操作验收，并继续迭代第一幕的真实电影镜头/音频与脚本；完成第一幕视觉闭环后再复制到其余四幕。
