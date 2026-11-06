# Stage 30｜第一幕：镜头调查、线索交叉核验与真实对话反应

日期：2026-11-06。目标：不是增加外围页面，而是让《03:17 · 回声》第一幕的玩家**看见、检查、比较、记住**，并让操作在后续剧情中产生可以感知的差异。

## 实际可玩的操作链（Unity 原生）

原生影片和热点交互仍使用 Stage24–26 的暂停电影画面，按 H 进入，鼠标点击或方向键/手柄选择，Esc/B 返回。不需要 Electron 或网页。

**可复现路线 A：猫眼与口令的矛盾**

1. 在序章 `intro` 检查短信屏幕（`intro_phone`），看到本地时间与网络投递的区分。如果同时调查合照（`intro_photo`），两项观察将出现首个交叉核验：“不能让两种记录互相担保”。
2. 在 `warning` 选择“查看猫眼”，到 `c1_peephole` 检查反常的影子（`peephole_shadow`）。会出现现场反应，调查记录存入手记。
3. 到 `c1_knock` 选择让她补完暗号，进入 `c1_password`；检查门禁终端（`door_password`）。**只有玩家亲手查看过猫眼中的影子**，右侧调查结果卡才会出现跨场景“线索交叉核验”：口令为真与影子反常仍不足以证明门外人的身份。
4. 到 `c1_hook`，剧情正文额外回应刚刚真正检查过的影子与口令；到第二幕开头 `c2_call`，继续出现基于实际调查的身份辨识反馈。通过查看本轮的对白回顾，也能看到当时留下的反应。

**可复现路线 B：照片或短信与录音**

- 在 `intro` 先检查合照或短信，之后在 `c1_photo` / `c1_receipt` 检查照片背面和本地队列，能与序章已检查的同类物件形成跨场景核对。
- 选择播放旧录音后，在 `c1_playvoice` 检查录音机；只有先查看过短信屏幕，录音剪辑与本地短信队列的跨介质对照才会出现。后续剧情将回应“是否真正标记了音频剪辑处”。

## 本次交付的数据与规则

- 在原有 **23 个**热点上补充 **7 对交叉核验关联**，不造新镜头，也不破坏已有归一化画面坐标。
- 在 **8 个**真实剧情节点上追加 **15 条条件对话反应**：`intro`、`c1_peephole`、`c1_photo`、`c1_receipt`、`c1_password`、`c1_playvoice`、`c1_hook`、`c2_call`。
- `StoryReaction.ifInspected / unlessInspected` 增量字段用于控制剧情显示；旧 `if / unless` 证据条件兼容，**不会把没调查过的内容冒充为已经检查过**。
- `HotspotDefinition.crossCheckId / crossCheckText` 用于跨目标、跨时间点的核对，所有关联都必须存在于实际热点目录，并禁止目标被设为自身、跨到未来章节。未知关联会阻止游戏正常导入，避免 silent failures。
- 核验结果来自 `GameProgress.inspections` 实际记录，**不创建 flags、不创建推理 attempts**。自动存档、手动槽及回溯仍由相同 canonical 时间线驱动。
- `EchoScenePresentation.Lines(..., observations)` 被 **`EchoGame.Render`、当前无影片时的字幕回退、`InspectSceneHotspot` 现场刷新**和 `EchoDialogueHistory.Read` 统一调用。
- 历史回顾仅使用 `v.at <= i` 的调查记录；玩家后查到的物件**不能穿越时间反过来改变旧场景**。
- 检查物件之后只更新剧情正文，**不会重启或卸载当前暂停的 VideoPlayer**。右侧结果卡突出显示「线索交叉核验」，有具体矛盾而非笼统“新证据已记录”。

## 自动化测试

- `Tools/CoreHost/Program.cs`：在实际游戏 C# 叙事实现中模拟完整序章 → 猫眼 → 敲门 → 口令 → 第一幕收束，执行调查、交叉核验、保存/读回、错误证据防护、对白回顾、回溯后的线索撤销。仍执行原有四结局和五推理完整测试。
- `Tests/test_stage30_first_act_investigation.py`：验证所有热点关联 ID、数据反应数量与归属、Unity UI 接线、历史筛选、无伪造 flags、Editor/PlayMode 测试源码存在。
- `Assets/Tests/EditMode/EchoStage30FirstActInvestigationTests.cs`：Unity 编辑器原生 C# 测试：同一场景双物件、跨场景影子/口令、剧情响应、回溯、非法引用拒绝。
- `Assets/Tests/PlayMode/EchoStage30InvestigationFeedbackUiTests.cs`：Unity PlayMode 对真实本地 UI 的显示测试，确保只查看短信时绝不会假显示核验、查看双方物件后才显示完整结果。

### 保留既有游戏契约

- 55 剧情节点、4 结局、5 案件、23 热点、69,497 个可达故事状态不变，独立审计仍需完成 Stage29 的 `independent_will` 证明。
- 旧的 `format 5/6/7` 存档按 Stage29 已有规则加载；本次只增加可由现有访问记录动态重建的反馈字段，不升级格式、不影响旧存档。
- 未新增必须的 27 个正式电影镜头；镜头生产与审批、角色配音/音画统一、真实 Unity Editor 及 Windows EXE 构建仍有缺口。
- GitHub CI 的 C# CoreHost 和 Python 回归确实执行，但**不能宣称 Unity PlayMode 或 Windows Player 实机已经通过**。

## 下一阶段建议

在 Unity Editor 正式可运行的情况下优先实机打磨第一幕：镜头热点坐标与真实演员构图校准、合适的调查提示动效、音量/字幕时序、手柄回退和全屏/21:9 适配。同时制作第一幕正式镜头及声音素材，把“可验证的数据驱动互动”变成最终可发行的影视演出。
