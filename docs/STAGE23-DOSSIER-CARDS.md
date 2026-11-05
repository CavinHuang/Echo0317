# 《03:17 · 回声》Stage 23｜原生证据卡片、双槽推理板与交互反馈

日期：2026-11-05。目标是将 Stage 22 已实现的真实证据/推理规则，转化为更接近成品的 **Unity 原生可操作界面**，而非 HTML 原型。

## 玩家可见的交互

- **档案卡**：证据不再用统一的文本按钮表示。每张卡片展示归属类别、名称、取得场景和时刻；选中卡片有金色焦点描边，可鼠标点击、键盘/手柄提交，也可按取消键返回。
- **程序绘制的原创美术**：`EchoDossierArtwork.cs` 在游戏内绘制、缓存档案纸纹、细网格、边缘金色标记与认证印章；空证据槽是虚线框样式。无需网上下载贴图、安装 SVG 插件或手工补资源，也不用处理第三方美术授权。
- **证据槽 01 / 02**：同一屏有两个真实可点击槽位，选中档案卡后固定证据；点击已固定的槽位或卡片可取消。固定项有高亮状态和短入场动画。槽位满时点击第三张卡片不会丢失已有证据，会提示先移除。
- **判断卡**：选对两条证据后才出现三种独立判断卡，继续调用原游戏的 `EchoNarrative.RecordDeduction` 完成存档/分支状态变更。错误的两条组合不会扣推理次数。
- **反馈动效**：已固定卡片和推理结果卡使用 `Time.unscaledDeltaTime` 播放约 0.24 秒的淡入与轻微缩放，剧情影片暂停时也能正常显示；用户无需另外生成 GIF。
- **原创操作音效**：`EchoDossierSound.cs` 使用 `AudioClip.Create` 在内存中合成短提示音，分别用于固定、错误组合和推理成功；没有第三方音乐或音效依赖，并遵循当前游戏的静音设置。
- **手柄连续操作**：刷新证据板会保留证据卡 ID 对应的选中焦点及此前滚动位置，避免每次固定证据就返回第一页。失效按钮仍会先禁用后等待 Unity 的延迟销毁，不接受旧回调。

## 实际修改目录

| 文件 | 作用 |
| --- | --- |
| `Assets/Scripts/UI/EchoDossierArtwork.cs` | 原创程序绘制、缓存两种 Sprite 材质，正式随源码构建 |
| `Assets/Scripts/UI/EchoDossierBehaviours.cs` | 卡片语义 ID、证据结果的轻量淡入动效 |
| `Assets/Scripts/UI/EchoDossierSound.cs` | 三种运行时合成提示音，遵守静音 |
| `Assets/Scripts/UI/EchoGameUI.cs` | 可点击档案卡、双证据槽、独立判断卡、反馈面板、焦点恢复 |
| `Assets/Scripts/Runtime/EchoGame.cs` | 档案、案件、证据固定、判断结果接入新界面及提示音 |
| `Assets/Tests/PlayMode/EchoStage23DossierUiTests.cs` | Unity 原生素材、卡片、槽位、焦点和静音行为测试源码 |
| `Tests/test_stage23_dossier.py` | Unity 无关的静态接线与资源代码契约检查 |
| `Tests/test_stage15_progression_safety.py` / `Tests/test_stage22_investigation.py` | 更新旧文字 UI 测试，仍验证反馈优先、真实证据配对和核心存档 |

## 验证与不能夸大的部分

- **可立即运行**：.NET 8 CoreHost 继续编译并执行同一份 C# 剧情/调查核心，验证 55 节点、69,497 个状态、四结局、五个可解推理、线索真实性；GitHub Python 契约测试增加素材绘制/原生 UI 接线检查。
- **尚无 Unity 实机结果**：原生 uGUI 纹理实际呈现、手柄/键鼠焦点和滚动、音频设备播放、动画视觉效果、Unity EditMode/PlayMode 及 Windows EXE 仍未在真实 Unity Editor 里运行。PlayMode 测试源码已写好，但 CI 缺 Unity 许可证并不会自动执行它们。
- **目标**：仍为 Windows / Steam 上架的 Unity 原生游戏，不是 Electron、网页应用或只有代码检查的替代品。
- **正式素材缺口**：27 个必需正式影片镜头仍未通过制作/审批；现有 8 段预演 MP4 仅用于内部 Alpha。以上 UI 素材不依赖这 27 个影片，因此可以继续独立开发。

## 可复核步骤

在 Unity 6.3 LTS 中打开项目，进入有已获得证据的章节：从「档案」查看卡片来源，进入「证据推理板」，选择案件、固定两条卡片、点击槽位移除、换成错误配对、重新选对正确配对，最后选择一种判断；确认字幕与视频保持暂停。分别以鼠标、Enter/Space、手柄 Submit/Cancel 操作，测试静音打开和关闭两种情况，验证反馈不引起剧情重复推进。

后续应使用真实 Unity Editor/Windows Player 日志复核视觉与音画，并迭代更丰富的插画资产；不能用静态代码检查代替正式上架验收。
