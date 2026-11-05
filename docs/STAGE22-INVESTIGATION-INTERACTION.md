# 《03:17 · 回声》Stage 22｜可交互的证据调查与双线索推理板

日期：2026-11-05。项目技术路线继续采用 Unity 6.3 LTS + C# Windows 影游。本阶段交付的是实际游戏交互源码，不是 HTML 原型。

## 为什么改动

旧版「线索档案」只是打印所有 flags；「证据推理」一次性列出五道谜题所需的准确证据及全部选项。玩家只需阅读清单、点击答案，无须自己寻找证据间的关联，还可能提前看见之后章节的谜题标题。

## 本阶段玩家可操作的玩法

1. **调查档案**：每条已经取得的记录都是可点击项目。点击可看到**首次获得它的章节、场景标题、时刻与剧情步骤**，这些出处来自当前存档 `EchoNarrative.Trace`，不是 AI 虚构的线索文案。
2. **案件目录**：只展示本轮实际到达过的章节的案件；随新剧情解锁，不提前披露之后章节的案件名。案件状态区分「待调查」「线索已齐」「已解开」。
3. **双线索推理板**：玩家进入单个案件后，亲自从已收集的记录中选**两条**放入两个槽位；可以单独取消一条、替换或者全部清空。
4. **判断门禁**：只有组合了本案件正确且已获得的两条证据，才会出现三种可选推断；错误配对提示重试，**不扣推理次数，不自动给出正确组合**。即便组合正确，玩家仍须自己做判断，正确/错误答案继续交给原有 `EchoNarrative.RecordDeduction` 存档和校验。
5. **回溯安全**：新游戏、读档、导入旧档、时间线回溯均清除 UI 中暂存的证据固定状态；档案和案件即时从当前合法历史重建。手动改动存档 flags 无法凭空解锁推理。已解过的案件不可重复刷答案。
6. **键鼠/手柄可操作**：继续使用 Stage 21 的 uGUI 按钮焦点与手柄 Submit/Cancel；在案件详细页按 Esc/取消先回案件目录，线索详细页则先回档案列表，再沿既有暂停菜单返回。

## 五道谜题保留不变

| 章节 | 案件 ID | 玩家需要关联的两条线索 |
| --- | --- | --- |
| 1 | `door_identity` | `shadow`, `tested_password` |
| 2 | `voice_identity` | `used_recording`, `voice_match` |
| 3 | `message_origin` | `receipt`, `cache46` |
| 3 | `incident_record` | `public_summary`, `incident_proof` |
| 4 | `independent_will` | `protocol_read`, `lin_willing` |

以上为开发验收资料：**实际游戏 UI 不会提前展示这份答案表**。原 `cases.json` 未修改，剧情图、四结局和存档格式未修改。

## 修改的代码

- `Assets/Scripts/Core/EchoInvestigation.cs`：纯 C# 线索出处、案件可见范围及两证据校验；对存档先执行 `EchoNarrative.Normalize`。
- `Assets/Scripts/Runtime/EchoGame.cs`：把旧列表接成可点击线索档案、单案件推理板、证据槽、反馈提示、层级返回与进度重置。
- `Tools/CoreHost/Echo0317.CoreHost.csproj`、`Program.cs`：同一份 C# 核心在无 Unity 环境的 .NET 8 CI 真正编译、重放四条结局路线，核对线索出处、可见案件、正确/错误配对、解谜成功与回溯。
- `Assets/Tests/EditMode/EchoInvestigationTests.cs`：Unity Editor 原生逻辑测试，等待实际 Unity 执行。
- `Assets/Tests/PlayMode/EchoStage22InvestigationUiTests.cs`：UI 可交互证据按钮、锁定按钮、异步销毁后避免旧回调的 Unity 测试源码。
- `Tests/test_stage22_investigation.py`：无 Unity 的功能接线与数据不变回归测试。
- `Tests/test_stage15_progression_safety.py`：老测试迁移到单案件推理板，仍要求答题反馈先于其他可操作线索/推论可见。

## 真实验证边界

**GitHub Actions 可验证**：.NET 8 编译并运行现有 Unity C# 核心、四结局路线、69,497 剧情状态、存档及本阶段证据规则；Python 数据与代码回归检查。

**尚未实测**：Unity Editor EditMode/PlayMode、真机鼠标和手柄输入、长页面焦点/滚动体验、窗口分辨率、影片音画、Windows x64 EXE。这需要 Unity Editor 的实际运行环境；代码合并本身不等于真实 Player QA 通过。

**Release**：仍有 27 个必需镜头未完成正式素材审批；现有 8 段预演视频只是内部 Alpha 占位。此阶段没有伪称制作了正式影片或可上架二进制。

## 后续重点

Unity 可用后应优先跑 UI 的真实焦点与长内容验收，再迭代为更视觉化的双列「证据卡 + 案件槽」界面；当前版本已经实现可玩的选择、固定、核验和判定闭环。
