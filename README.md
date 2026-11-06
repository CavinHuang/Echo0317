# 《03:17 · 回声》Stage 27｜Unity Windows Alpha 合法授权与构建通路

**当前首要目标仍是真正可运行的 Windows EXE。** 已核验 GitHub 托管 Unity CI 的实际授权缺口：原生 C# CoreHost 通过，但 Unity 测试因缺少授权配置没有运行。**不要从互联网“捞”他人的 Unity License/账号/密钥。** Unity 官方 6.3 文档要求 Unity Personal 使用 Unity Hub 登录激活；本轮实现官方支持的**免费 Personal + 本机 Windows 构建 / 同用户自托管 GitHub Actions**路线，保留已有合法付费授权的 GitHub Hosted GameCI 路线。

在已安装 Unity Hub 的 Windows 11 中，通过自己的 Unity ID 激活 Personal 并装好 **6000.3.2f1 + Windows Build Support**，之后可执行：

```powershell
.\Tools\prepare-unity-windows.ps1 -OpenHub
.\Tools\prepare-unity-windows.ps1 -VerifyEditor
.\Tools\prepare-unity-windows.ps1 -BuildAlpha
```

脚本会真正运行 Unity Editor 导入/测试/Windows Player 构建与本机四结局 QA；**仅脚本存在和 CoreHost CI 通过不代表已生成 EXE**。此时若授权不可用将明确失败，并写入无敏感信息的 `Builds/unity-local-activation-preflight.json`。GitHub 自托管 `windows-unity-native.yml` 现在也必须通过真实 Editor 授权预检才会尝试编译。

- [Stage27 合法授权方式、CI 与操作说明](docs/STAGE27-LEGAL-UNITY-ACTIVATION.md)
- [本机一键构建与授权预检](Tools/prepare-unity-windows.ps1)
- [个人设备上的 GitHub 原生 Unity 构建工作流](.github/workflows/windows-unity-native.yml)

**当前构建边界：** GitHub Hosted 前次运行并未提供合法 Unity Secrets；没有实际 Unity Editor/Windows Player 结果，不声称正式 Alpha 构建成功。

---

# 《03:17 · 回声》Stage 26｜影片与热点真正对齐的 Unity 调查界面

本阶段修复了 Stage 24/25 镜头调查的核心画面错位问题：现在将**真实的、暂停中的 `VideoLayer`** 临时移入调查视窗，使用 Unity 16:9 `AspectRatioFitter` 保留影片比例；热点直接成为影片层的子元素。退出会恢复原层级和原焦点，不重启解码器。手柄/键盘方向选择使用可在 .NET 8 真实编译测试的空间最近邻算法，替代不稳定的自动焦点；ESC/B 仍防同帧重复返回。

- [Stage 26 实现、测试与实机边界](docs/STAGE26-FILM-HOTSPOT-VIEWPORT.md)
- [Unity 原生电影/热点挂载逻辑](Assets/Scripts/UI/EchoInvestigationOverlay.cs)
- [手柄方向焦点算法](Assets/Scripts/Core/EchoHotspotNavigation.cs)

现有调查 PNG 素材已经在 `Assets/StreamingAssets/Investigation/`，不需再次安装。**注意：** .NET / Python CI 不能代替 Unity 真实 PlayMode、窗口分辨率及 Windows EXE 验收。

---

# 《03:17 · 回声》Stage 25｜调查美术已入库 + 可回看的现场手记

**可以直接拉取源码使用调查素材，无须下载额外依赖**：18 张原创透明 PNG 已优化后直接提交到 `Assets/StreamingAssets/Investigation/`，包括热点三态、调查卡、物件图标和反馈通知。镜头调查新增「现场调查手记」：从暂停菜单或线索档案查看本轮真实调查过的物件，支持分页、场景出处、已核实证据关联和 Esc/手柄分层返回。手记只读，不会绕过原来的剧情证据校验。

- [Stage25 完整开发与验收报告](docs/STAGE25-BUNDLED-ART-AND-JOURNAL.md)
- [直接随源码交付的完整 18 张 PNG](Assets/StreamingAssets/Investigation/)
- [新的 C# 现场调查手记规则](Assets/Scripts/Core/EchoInvestigationJournal.cs)
- [原生 Unity 调查 UI](Assets/Scripts/UI/EchoInvestigationOverlay.cs)

**验证边界：** C# CoreHost + Python 可在 GitHub Actions 真正运行，Unity Editor/PlayMode 与 Windows EXE 仍需合法激活后实际执行，不能仅凭脚本检查称为已完成原生玩家验收。

---

# 《03:17 · 回声》Stage 24｜镜头内可点击调查与生成素材接入

**实际 Unity 交互新增**：23 个跨序章/第一至第三幕的画面热点；在有调查对象的场景按 **H** 或点击「现场调查」暂停影片，使用鼠标、键盘或手柄检查物体。热区显示默认/选中/已调查三态和对应图标，右侧显示调查结果；Esc/B 可安全返回剧情。调查记录存入当前时间线，重复点击不会刷记录，回溯时自动撤销废弃路径，且不会伪造剧情证据。

**素材已制作**：三种热点边框、调查面板、三种通知条、11 种图标，另提供原始高分辨率透明 PNG 安装包。Unity 代码直接加载 `Assets/StreamingAssets/Investigation/`，缺失图片时提供绘制回退。安装图像使用：

```bash
python Tools/install-stage24-assets.py --archive /path/to/Echo0317-Stage24-InvestigationAssets.zip --root .
```

- [Stage24 功能、素材清单及真实测试边界](docs/STAGE24-HOTSPOT-INVESTIGATION.md)
- [场景交互数据](Assets/StreamingAssets/Data/hotspots.json)
- [Unity 调查视觉层](Assets/Scripts/UI/EchoInvestigationOverlay.cs)
- [防伪时间线调查逻辑](Assets/Scripts/Core/EchoHotspotInvestigation.cs)

**Stage25 更新：** 调查 UI 的 18 张优化透明 PNG 已直接提交到 GitHub，以上 ZIP 安装是可选的高清替换方案。C# / Python CI 可验证源码和资源字节；Unity Editor/PlayMode/Windows EXE 仍未执行。

---

# 《03:17 · 回声》Stage 23｜图形化档案卡、双槽推理板与原生操作反馈

**已纳入 Unity 原生 C# 源码：** 证据列表/案件列表使用程序绘制的原创档案卡和安全标记；双线索有可点击、可撤销的槽位；判断使用独立交互卡；推理结果与固定证据有淡入反馈，并提供遵循静音的原创合成提示音。手柄/键盘在固定线索后保持选中卡片和滚动位置。无需额外下载视觉资源，也没有将 HTML 原型当作游戏。

- [Stage 23 完整功能与真实验收边界](docs/STAGE23-DOSSIER-CARDS.md)
- [Unity 原生自绘卡片纹理](Assets/Scripts/UI/EchoDossierArtwork.cs)
- [真正的 UI 交互入口](Assets/Scripts/UI/EchoGameUI.cs)
- [原生播放反馈音效合成](Assets/Scripts/UI/EchoDossierSound.cs)

**仍未构建 Windows EXE，也未运行 Unity Editor/PlayMode；** GitHub 现有无 Unity 许可证的 C# CoreHost 与 Python 回归检查只能验证共用游戏核心和接口约束，不能代替真实画面或音频验收。

---

# 《03:17 · 回声》Stage 22｜调查档案与双线索推理板

**游戏交互功能**：线索档案支持点击记录查看实际剧情来源；五个谜题按已到达章节解锁，玩家须从真正获得的线索中亲手选两条、固定到证据槽，正确关联后才能选择三种推断；错误配对不消耗推理次数。存档读取、回溯、新游戏会重建案卷并清理临时固定，不透露未探索剧情。

- [Stage22 实际玩法、实现清单和验证边界](docs/STAGE22-INVESTIGATION-INTERACTION.md)
- [C# 证据来源与组合校验](Assets/Scripts/Core/EchoInvestigation.cs)
- [Unity 原生调查与推理界面接线](Assets/Scripts/Runtime/EchoGame.cs)

**验收边界：** 可在 .NET 8 GitHub CI 实际编译运行共用 C# 调查核心与四结局测试；尚未运行 Unity Editor / Windows EXE，也未声称完成音画或可发行 Alpha 验收。

---

# 《03:17 · 回声》Stage 21｜交互、手柄焦点与字幕可读性

**新增真实 Unity uGUI 游戏交互源码**：防止 Enter/Space 与 Unity Submit 重复推进剧情；Esc / 控制器 Cancel 统一退回正确的暂停或确认页面；模态打开后锁定背景按钮、恢复合法焦点并显示选中金色轮廓；设置菜单反复调整时保留当前选项。字幕新增衬底和独立持久化的 **100% / 125% / 150%** 字号，兼容旧用户设置。

- [Stage 21 交互功能、测试与验收边界](docs/STAGE21-INTERACTION-UX.md)
- [原生 UI 与控制器输入](Assets/Scripts/UI/EchoGameUI.cs)
- [可执行的模态导航 C# 规则](Assets/Scripts/Core/EchoModalFlow.cs)

**实际验证区分**：.NET 8 CoreHost / Python CI 能验证核心规则与测试接线；真正 Unity 编辑器、实体手柄、视频及 Windows EXE **仍未执行或构建**，不可将源码检查当作游戏实机通过。

---

# 《03:17 · 回声》Stage 20｜视频跳过竞态修复与播放快捷键

游戏中按 **R 重播当前镜头**、**K 跳过当前影片画面（不跳过剧情）**。影片准备中跳过现在会取消待解码请求，不会被迟到首帧重新启动；镜头切换与设置菜单保留暂停状态，旧影片回调不再干扰新镜头。与 Stage 19 的剧情回顾 **L** 快捷键兼容。

- [Stage 20 实现、测试与真实验收范围](docs/STAGE20-PLAYBACK-CONTROLS.md)
- [独立 C# 播放生命周期源码](Assets/Scripts/Core/EchoPlaybackState.cs)
- [Unity 原生双解码器修复](Assets/Scripts/Runtime/EchoVideoDirector.cs)

**构建边界：** GitHub .NET 8 CoreHost 可真实编译/执行播放状态与剧情代码；Unity Editor / PlayMode 和 Windows EXE 仍需要 Unity 合法激活，尚不能宣称通过原生影片与音效实测。

---

# 《03:17 · 回声》Stage 19｜原生剧情回顾与选择日志

**新增可玩的 Unity 原生交互源码**：游戏 HUD「回顾」按钮、快捷键 **L**、暂停菜单入口、当前轮回的对白与真实选择记录、12 条/页浏览。所有记录从已验证的存档路径实时重建；回溯/读档不会泄露废弃的剧情分支。已增加 .NET 8 CoreHost 与 Unity EditMode / PlayMode 对应测试。

- [Stage 19 功能实现与验收边界](docs/STAGE19-DIALOGUE-HISTORY.md)
- [核心实现](Assets/Scripts/Core/EchoDialogueHistory.cs)
- [实际 Unity UI 接入](Assets/Scripts/Runtime/EchoGame.cs)

**仍未生成 Windows EXE。** 缺少 Unity 许可证时继续做 C# 游戏功能及真实 C# CoreHost 验证；Unity Editor 测试/Windows Player 编译仍需合法 Unity 激活。

---

# 《03:17 · 回声》Stage 18｜Unity CI 失败回收与原生 Alpha 构建准备

**GitHub 已验证：.NET 8 真正的 C# 游戏核心与剧情迁移检查成功；Unity 原生 Editor 测试因缺少 Unity 授权 Secrets 而未启动，Windows x64 EXE 仍未生成。** Stage 18 修复了测试产物上传缺少路径的二次异常、锁定 Ubuntu 24.04 和 Unity 6000.3.2f1，并添加 CI 回归测试。不把离线核心检查当作玩家实机通过。

- [Stage 18 最新证据、授权配置与下一步](docs/STAGE18-REAL-UNITY-CI-RECOVERY.md)
- [已确认的 Unity CI 失败日志](https://github.com/CavinHuang/Echo0317/actions/runs/41966421571)
- [托管 Unity Windows Alpha 手动运行入口](https://github.com/CavinHuang/Echo0317/actions/workflows/hosted-unity-windows-alpha.yml)

---

# 《03:17 · 回声》Stage 17｜GitHub 托管 Unity Windows Alpha 构建通路

**Windows EXE 尚未生成。** 本轮加入了无需用户自备 Windows Runner 的 GitHub Hosted Unity CI：真实 .NET 8 C# 编译 → Unity EditMode/PlayMode → GameCI Windows x64 Unity 编译 → Windows Runner 启动生成的 EXE 并进行原生四结局自检 → 校验、打包。

**当前 42 项离线测试通过；真实 Unity 编译和 Windows EXE 未执行。** 此路线需要 Unity 授权 Secrets；仓库可为 public，但不得提交凭据。请勿在对话中发送任何许可证、Unity 密码或账号令牌。

- [Stage 17 完整说明、测试与阻塞](docs/STAGE17-HOSTED-UNITY-ALPHA.md)
- [GitHub Actions 真正的托管 Unity 构建工作流](.github/workflows/hosted-unity-windows-alpha.yml)
- [生成的 EXE 原生 QA / 二进制验证与打包器](Tools/pack-hosted-alpha.py)
- [原有 Windows 自托管/本地 Unity 构建入口](Tools/build-alpha-windows.ps1)

---

# 《03:17 · 回声》Stage 16｜Windows Alpha 构建验收工程

**当前状态：尚未生成 Unity Windows EXE。** Stage 16 已编写 Windows 一键原生构建/验收、独立 C# 核心编译与四结局 QA，并修复视频重复播放与返回菜单的音频问题。**本机仅运行离线测试，未装 Unity Editor / .NET SDK，不能宣称原生构建通过。**

## Windows 11 一次性验收入口

安装并激活 Unity **6.3 LTS**、Windows Build Support、Python 3.11+，然后在工程根目录运行：

```powershell
$env:UNITY_EDITOR_PATH = 'C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe'
.\Tools\build-alpha-windows.ps1
```

流程会自动执行 Unity 导入 → EditMode/PlayMode → Windows EXE → EXE 四结局运行时 QA → GUI 主菜单启动 → 打包、SHA-256 和证据报告。如果失败，`Builds/stage16-alpha-evidence.json` 记录失败阶段。`-SkipInteractiveSmoke` 仅供没有 GUI 桌面的 CI 使用，不能视为通过真实桌面启动。

独立核心 C# 编译：`dotnet run --project Tools/CoreHost/Echo0317.CoreHost.csproj --configuration Release -- .`；不依赖 Unity License，但不能代替 Unity Editor 和 Player 构建。

详细的执行环境、代码变更、测试和阻塞证据：[`docs/STAGE16-WINDOWS-ALPHA-STATUS.md`](docs/STAGE16-WINDOWS-ALPHA-STATUS.md)。

---

# 《03:17 · 回声》 Stage 15｜Unity 原生玩家推进安全与连续性

**正式开发路线：Unity 6.3 LTS + C#，Windows / Steam。** 该包是 Unity 源码工程，**不是 Windows EXE**；当前执行环境没有 Unity Editor 或 C# 编译器，因此没有验证实际 Player 运行。

### 本阶段已写入的游戏代码

- 切换剧情时立即停用旧选项，并在 C# 层验证选项所属节点，阻止旧点击回调导致意外跳剧情。
- 剧情、卷宗滚动复位；键盘/手柄选中的长列表按钮尝试滚动到可见区域。
- 线索收录反馈、自动保存失败的游戏内告警；推理结果在卷宗顶部展示。
- 手动读取存档、导入旧 JSON 与回溯增加确认；切换进度前停止旧影片解码/声音。
- Windows 编译后自检扩大为 **69,497 全状态可达审计 + 0 个非结局死路**，并保留四结局、存档往返和防伪验证。

离线验证：**30 项 Python 单测通过**、55 个可达剧情节点、四结局、五推理题、8 条预演 MP4。Unity EditMode / PlayMode 和 Windows EXE **尚未运行或构建**。Release 仍被 27 个未获批正式影片镜头阻止。

- **完整进度与改动审计**：[Stage 15 报告](docs/STAGE15-PLAYER-CONTINUITY.md)
- **真实离线验证证据**：`Builds/stage15-preflight.json`、`Builds/stage15-offline-unit.log`
- **构建与原生验收**：下方保留完整 Unity 导入和 Windows 构建流程。

---

# 《03:17 · 回声》Stage 14 — Unity 游戏玩家体验与数据安全

> 本项目是 Unity 6.3 LTS 原生源码工程。**尚未在 Unity Editor 编译或生成已验证 Windows EXE**。当前正式 AI 镜头缺失，不能按正式 Release 发行。

本轮改动：
- 新游戏、覆盖和删除存档前二次确认。
- 独立持久的字幕、静音、倍速、全屏/窗口模式设置；从主菜单直接进入。
- 菜单、剧情、弹窗的键盘/手柄可访问焦点。
- Unity 6 `ENABLE_LEGACY_INPUT_MANAGER` 导入和构建预检查。
- EditMode/PlayMode 新用例（仅新增源码，等待在 Unity 执行）。

详细清单：[Stage 14 进度与缺口报告](docs/STAGE14-PLAYER-UX-REPORT.md)。

---

# 《03:17 · 回声》Unity 6.3 LTS / Stage 13 原生 Player 自检工程

本仓库是独立的 **Unity + C#** 影游源代码项目，正式发行目标为 Windows/Steam；**不需要 Electron，也不在 Unity 内运行 HTML/WebView**。Stage 10 着重修复视频状态、输入与菜单暂停、补 Unity 原生测试与 Windows 发行构建管线。

> 当前环境没有 Unity Editor，因此**尚未通过 Unity 编译、PlayMode 和 Windows x64 EXE 实机测试**。下载的是项目源码，不是已完成上架的游戏。

## 已迁移并保留

- 五幕 55 节点、38 个分支入口、四种结局，69,497 个可达剧情状态的独立数据验证。
- 五道证据推理、自动存档 + 三手动槽位、章节回溯、图鉴与复盘。
- Unity uGUI 主菜单、剧情界面、档案、推理与设置；双 VideoPlayer 与 RenderTexture 影片播放器。
- 8 条 H.264/AAC 动态分镜、五张场景参考图；40 份待生成的 AI 视频提示词。视频未制作完成仍可走剧情。
- 43 个镜头登记 ID 中 40 个为生成任务、3 个 V07/V13/V17 为分组父镜头。

## 打开与启动

1. Windows 上安装 Unity Hub 与 Unity **6.3 LTS**（本项目首次锁定 `6000.3.2f1`）；如需 Windows 构建，还要 Windows Build Support。
2. Unity Hub > Add project from disk，选解压后的**项目根目录**，而不是 `Assets` 文件夹。
3. 首次导入将创建 `Assets/Scenes/EchoEntry.unity` 并填入 Build Settings；如未创建，在 Unity 顶部执行 **03:17 / 初始化入口场景**。
4. 按 Play，主菜单应出现。视频资源缺失时可继续剧情，不影响证据与四结局逻辑。

## 本机已执行的非 Unity 检查

```bash
python Tests/test_export.py
python Tests/test_release_assets.py
python Tests/test_csharp_structure.py
```

## 真正的原生验收（必须在安装 Unity 的 Windows 机器运行）

```powershell
$env:UNITY_EDITOR_PATH = 'C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe'
.\Tools\test-unity.ps1
.\Tools\build-windows.ps1
```

`test-unity.ps1` 分别运行 EditMode 和 PlayMode 测试，并验证 XML 测试输出；`build-windows.ps1` 默认先运行这些测试，再构建 Windows x64，校验 `.exe` 与 Unity `_Data` 目录并生成分发 ZIP 和 SHA-256 文件清单。

GitHub 自托管 Windows Runner 的手动工作流位于 `.github/workflows/windows-unity-native.yml`；它不会自动生成已验证的安装包，必须先配置 Runner、Unity 和许可。

## 工程目录

| 路径 | 内容 |
| --- | --- |
| `Assets/Scripts/Core` | C# 剧情、推理、JSON 存档和校验 |
| `Assets/Scripts/Runtime` | 游戏控制器与原生双 VideoPlayer |
| `Assets/Scripts/UI` | Unity 原生 uGUI 主菜单/对话/推理/存档 |
| `Assets/StreamingAssets/Data` | 剧情、镜头、字幕、五个推理和四结局回归路线 |
| `Assets/StreamingAssets/Media/Previs` | 八条内部预演视频 |
| `Assets/StreamingAssets/Art` | 五张场景参考 |
| `Assets/Tests/EditMode` | 图验证、四结局 C# 重放、存档与防伪测试 |
| `Assets/Tests/PlayMode` | Unity UI、视频重播/暂停的基础运行测试 |
| `Production/Stage4/prompts` | 40 个 AI 正式视频生成任务提示词 |
| `Tools` | PowerShell Unity 原生测试及 Windows 构建/打包脚本 |
| `docs/STAGE10-NATIVE-VERIFICATION.md` | 逐项修复及发行剩余阻塞的真实审计报告 |

**下一步重点：在有 Unity 许可证和 Windows Build Support 的实际环境首次导入、跑测试、修编译问题，再拿到真正的 Windows 运行日志与全幕游戏验证。**

## Stage 11 增量（2026-11-05）

- 修复历史存档选项文字校验的宽松匹配；仅允许唯一目标的无文字旧存档兼容。
- 自动存档及 3 个手动存档槽使用 `.bak` 版本回退；修复损坏恢复及槽位删除。
- 修复视频复用旧 RenderTexture 帧及播放器交接时音频可能重叠。
- `python Tools/doctor.py`：统一验证剧情、视频、提示词与 C# 静态结构，并明确报告 Unity 编辑器是否存在。
- `python Tests/test_release_gate.py --profile internal`：允许原 8 条动态分镜的内部 Alpha。
- `python Tests/test_release_gate.py --profile release`：目前按预期阻止发行；缺少 27 条剧情必需的获批正式影片。
- `Tools/build-windows.ps1 -Release`：新增正式发行构建开关；默认无参数仍为内部 Alpha。
- 完整结果参见 `docs/STAGE11-NATIVE-VERIFICATION.md`。**当前没有生成 Windows EXE，也没有在 Unity 中运行测试。**

## Stage 12 增量：原生编译准备与首帧切换保护

- **新增程序集引用**：运行时与 PlayMode 测试显式引用 `UnityEngine.UI`，避免 Unity 6.3 由于 asmdef 引用不完整而导入失败。
- **切镜安全**：取消过期的 `VideoPlayer.Prepare`，在新镜头首帧就绪后切换，防止快速连续操作时暴露旧画面或产生音频重叠。播放是否平滑仍需 Unity 实机测量。
- **启动失败反馈**：缺失 `StreamingAssets/Data` 时显示可见的错误提示，不留下空白游戏窗口。
- **可验证的导入证据**：`Tools/verify-unity-import.ps1` 运行原生 Unity Editor 导入与编译检查，写 `Builds/unity-import-verification.json`；这一过程必须在实际 Unity 环境执行。
- **构建保护**：Release 不能跳过测试、清理旧构建输出避免旧文件误通过；追加 6 项离线工程约束测试和原生 PlayMode 测试用例。

Windows 验收流程：

```powershell
$env:UNITY_EDITOR_PATH = 'C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe'
python Tools/doctor.py
.\Tools\verify-unity-import.ps1
.\Tools\test-unity.ps1
.\Tools\build-windows.ps1
.\Tools\smoke-windows.ps1
```

详细审计与剩余缺口：`docs/STAGE12-READINESS-REPORT.md`。**当前无 Unity Editor，未实际生成或运行 EXE。**


## Stage 13 增量：让编译后的游戏亲自证明能通关

- 新增 `-echoQa` 模式，真正编译出的 Unity Windows Player 可以在无图形模式下使用**同一份 C# 剧情/存档代码**重走四结局、123 次推进，验证槽位读写和伪造证据阻断。
- `Tools/build-windows.ps1` 将原生 Player QA 设为 Windows 分发包的**必过关卡**：不再满足于“Editor 测试通过、EXE 文件存在”。
- `Tools/qa-native-windows.ps1` 负责运行 EXE、检查退出码和实际日志，再调用 `Tools/check-native-qa.py` 验证由 EXE 写出的 JSON。
- `Tools/smoke-windows.ps1` 新增 `ECHO_GAME_READY` 主菜单初始化标记检查。
- 原生 PlayMode 自动化期间不再自动注入第二个 EchoGame 对象，降低重复 UI 与 VideoPlayer 干扰。
- 新增 8 项 Stage 13 独立离线契约测试；本环境 **17/17 Python 单元测试通过**，但不代表真实 Unity 编辑器编译/EXE 测试通过。

详细进度：`docs/STAGE13-NATIVE-PLAYER-QA.md`。真实环境检测：`Builds/stage13-preflight.json`。

Windows 构建和原生运行验收：

```powershell
$env:UNITY_EDITOR_PATH = 'C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe'
.\Tools\verify-unity-import.ps1
.\Tools\test-unity.ps1
.\Tools\build-windows.ps1
.\Tools\smoke-windows.ps1
```

**重要：** 本阶段尚未产生或验证 Windows EXE、Unity NUnit XML 或 `native-game-qa.json`。以上运行产物必须由实际 Unity 工具链生成。
