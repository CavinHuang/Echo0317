# 《03:17 · 回声》Stage 25｜调查美术入库 + 现场调查手记

日期：2026-11-05。目标是消除 Stage 24 的人工素材安装步骤，并让镜头调查与档案之间形成真实、可回溯的玩家闭环。

## 本轮已交付

### 1. 正式随 GitHub 源码分发的原始生成素材

- 将上期生成的 **18 张透明 PNG** 进行尺寸和颜色优化后，直接以**二进制 Git Blob** 提交到 `Assets/StreamingAssets/Investigation/`，由 Unity 在运行时按文件名加载，**不再要求用户手工下载 ZIP 或执行安装脚本**。
- 包含热点三态、调查结果面板、三种通知提示、11 类物件图标；沿用 Stage24 的 `EchoInvestigationArt` 加载器，不需要引入新的 Unity 插件。
- 图片优化后合计约 290 KB；保留原始透明区域，所有资源直接跟踪在远程 Git 树中。
- `Tests/test_stage25_bundled_art_journal.py` 自动核对文件名、PNG 头、尺寸、色彩类型、透明索引及总体大小，避免 CI 误把缺失的美术资产当成可发布状态。
- Stage24 交付的独立高清 ZIP 仍可用作以后替换素材的可选升级，但不是运行游戏的前提。

### 2. 可玩的「现场调查手记」

- 在**暂停菜单**和**线索档案**新增「现场调查手记」入口；打开后自动暂停影片。
- 按本轮有效时间线倒序列出玩家实际调查过的物件，包含所在章节、场景、影片时刻、具体观察描述；8 条/页，可浏览新旧记录。
- 选中记录可以看完整现场笔记；若其关联证据已由**真实剧情**获取，则显示正式证据名称，并提供跳转到线索档案的入口；否则明确标注「观察已记录，尚未核实」。
- 用户的观察**不能通过查看手记变成证据**，推理仍由 Stage22/23 的 C# 证据组合门禁决定。
- Esc/手柄 Cancel 从记录详情返回手记列表，再返回原先的线索档案或暂停菜单；不存在遗失的跨页或隐式回溯行为。
- 新游戏/读档/回溯后重新从已校验 `GameProgress.inspections` 构建；废弃时间线内容自动消失。
- `EchoInvestigationJournal.cs` 是**Unity 游戏与 .NET8 CoreHost 编译同一份 C# 源码**。

## 代码变更

| 目录/文件 | 说明 |
| --- | --- |
| `Assets/StreamingAssets/Investigation/*.png` | 18 张真实透明、优化后的原创调查图形资源 |
| `Assets/Scripts/Core/EchoInvestigationJournal.cs` | 独立、可分页、校验存档后生成的调查手记 |
| `Assets/Scripts/Runtime/EchoGame.cs` | 暂停菜单/线索档案入口、手记详情、翻页、手柄返回与证据跳转 |
| `Tools/CoreHost/Echo0317.CoreHost.csproj` | 编译实际新增的 C# 手记源文件 |
| `Tools/CoreHost/Program.cs` | 执行观察、核实、存档与回溯下的手记规则 |
| `Tests/test_stage25_bundled_art_journal.py` | 实际图片存在性和透明度测试、UI 接线测试 |
| `Assets/Tests/EditMode/EchoStage25InvestigationJournalTests.cs` | 原生 Unity 编辑器测试源码，待 Unity 可用时执行 |

## 真实验收范围

- GitHub Actions 的 .NET8 CoreHost 会**真实编译执行**新增 C# 核心，与原剧情/四结局/五道推理保持兼容。
- Python 进行数据、图片实际字节以及 UI 源码接线回归。
- **不声称**在 Unity Editor 中看到完整游戏画面；真实 EditMode/PlayMode、实体手柄、影片音画、Windows EXE 尚未执行/生成。仍需仓库的 Unity 合法激活。
- 当前 27 个正式电影镜头资源仍是单独的发行缺口；本次消除的是 Stage24 *交互美术* 的部署缺口。

## 建议的本机检查

拉取最新 `main`，确认 `Assets/StreamingAssets/Investigation/` 包含 18 个 PNG。用 Unity 6.3 LTS 加载项目，开始新游戏，按 H 进入现场调查，点击手机/照片热点，退出后打开「暂停 → 现场调查手记」，核对观察记录与存档；回溯后再次检查记录是否撤销。
