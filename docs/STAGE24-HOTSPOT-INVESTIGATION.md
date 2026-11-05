# Stage 24｜基于生成素材的镜头内调查交互

日期：2026-11-05。目标平台是 Unity 6.3 LTS / Windows；交付的是项目实际可编译的原生 C# 游戏交互，不是 Electron 或 HTML 原型。

## 实际玩家交互

- 新增 **23 个可调查热点**，覆盖序章、第一幕、第二幕与第三幕的部分关键节点；一处场景可以含多个物件。坐标以当前画面左侧的 normalized film-area 定义，左下为 (0,0)，宽高均为 0–1。
- 在可调查的场景中，剧情选项末尾出现「现场调查 H · 已检查 X/N」。在正常剧情界面按 **H** 也可进入。进入时暂停影片，禁用背后的剧情/菜单按钮。
- 调查层真实叠加在 Unity Canvas 上。左侧热点可鼠标点击、方向键/手柄选中+确认；已调查变为完成状态，聚焦时换成金色焦点边框。右侧展示动态调查文字和证据状态。
- 重复调查不会重复记录；按 Esc、手柄 Cancel 或「返回剧情」恢复原来的影片和主游戏输入。
- **存档联动不是假的**：每次检查记录 `{ id, node, at }`，保存在 GameProgress.inspections。反复点击幂等，读档保留，同节点/同路径核对，回溯删除废弃时间线的记录。原有格式 6 存档可不做手工迁移。
- **剧情安全**：热点不会直接添加 `flags`，更不能跨场景解锁谜题；真实证据仍由 `EchoNarrative.Advance` 合法选择产生。已经拿到某证据时，画面调查会显示证据已核实，否则仅记录现场观察。这防止 UI 快捷键突破原有 55 节点、69,497 状态、五道谜题与四结局的合法路径。
- **本期热点为可选调查**，不强制玩家完成才能继续剧情，以保证原来的四条完整通关路径和旧存档兼容。强制调查门禁应在对应视频与剧情节点审定后再引入，不能虚称已经实现。

## 使用生成的图像资产

素材是本次对话为《03:17 · 回声》专门生成的原创图像，共 18 张透明 PNG：

- `hotspot-idle.png`、`hotspot-focus.png`、`hotspot-complete.png`：默认、选中、已调查三态边框。
- `inspect-result.png`：调查结果面板。
- `notice-new.png`、`notice-repeat.png`、`notice-updated.png`：三种调查提示。
- `icon-door.png` 等 11 种线索对象图标：门、影子、终端、麦克风、录音机、波形、收据、短信、通报、证件、监控。

ChatGPT 交付 `Echo0317-Stage24-InvestigationAssets.zip`，保留原图分辨率、透明度并在 `Assets/StreamingAssets/Investigation/` 下分门别类。**由于当前 GitHub 连接器仅支持文本文件写入，原始 PNG 文件未直接通过 PR 上传到 GitHub**。代码可以原生识别此目录；如果素材包尚未安装，热点边框/结果卡有 C# 绘制的降级方案，不会阻止游戏开始。图标的正式版本依赖安装素材包。

安装命令（从项目根目录）：

```bash
python Tools/install-stage24-assets.py --archive /path/to/Echo0317-Stage24-InvestigationAssets.zip --root .
```

安装脚本校验 ZIP 资源清单和每个 PNG 的 SHA-256，避免解压任意来源文件覆盖工程。安装后 Unity 会直接通过 `StreamingAssets` 读取真实图像（无需额外 Unity Package）。

## 修改路径

| 文件 | 内容 |
| --- | --- |
| `Assets/StreamingAssets/Data/hotspots.json` | 23 个具体热点、文本、证据关联与画面坐标 |
| `Assets/Scripts/Core/EchoHotspotInvestigation.cs` | 时间线校验、条件显示、幂等调查记录与已核验证据 |
| `Assets/Scripts/Core/EchoData.cs` | 可选读取热点 JSON，验证节点 ID、证据 ID、坐标与重复 ID |
| `Assets/Scripts/Core/EchoNarrative.cs` | GameProgress 存储/回溯/防伪检查新字段 |
| `Assets/Scripts/UI/EchoInvestigationArt.cs` | 读取 PNG、缓存 Sprite、无素材时回退绘制 |
| `Assets/Scripts/UI/EchoInvestigationOverlay.cs` | 原生热点按钮、选中高亮、结果面板、继续操作 |
| `Assets/Scripts/UI/EchoGameUI.cs` | 调查模式隔离背景按钮和焦点 |
| `Assets/Scripts/Runtime/EchoGame.cs` | H/ESC、视频暂停恢复、点击调查、存档、剧情入口 |
| `Tools/install-stage24-assets.py` | 安装高分辨率资源包并 SHA-256 验证 |
| `Tools/CoreHost/Program.cs` | 编译并运行真实 C# 调查链与存档核心 |
| `Assets/Tests/EditMode/EchoStage24HotspotTests.cs` | Unity Editor 编辑模式测试源码 |
| `Assets/Tests/PlayMode/EchoStage24HotspotUiTests.cs` | Unity 交互/焦点/图像加载的测试源码 |
| `Tests/test_stage24_hotspots.py` | JSON 完整性、关键安全逻辑、素材加载契约 |

## 已验证 vs 仍需真实 Unity

GitHub CI 运行 `.NET 8 CoreHost` 和全套 Python 测试，验证实际 C# 代码的 55 节点四结局、存档与场景调查逻辑。**本次没有真实调用 Unity 视频 Player、PlayMode、实体手柄或 Windows EXE**；后续需配置有效 Unity 激活并运行 .github/workflows/hosted-unity-windows-alpha.yml。

高分辨率图像目前以单独安装包交付；正式 Windows Alpha 制作前，必须将生成图片纳入 Unity 构建输入，并确认在 Windows 实机上无缺失的纹理/字幕/提示音。原有 27 个正式电影镜头仍是发行素材缺口。
