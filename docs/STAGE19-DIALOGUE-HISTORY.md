# 《03:17 · 回声》Stage 19｜原生剧情对白回顾与选择日志

日期：2026-11-05。此阶段在**无需安装/激活 Unity 的条件下继续真实游戏功能开发**，不把旧的 HTML 原型作为发售版本。

## 玩家可见的功能

- 剧情 HUD 增加 **「回顾」** 按钮；游戏中按 **L** 或从暂停菜单也能打开。
- 回顾当前**这一轮**已经看过的场景对白、说话人、时刻、所属幕次及实际选择；不显示未探索节点、锁定选项或未获得的结局。
- 每页最多显示 12 条记录；「更早的对白」和「更新的对白」可以翻页，不创建额外存档、网络服务或后台记录。
- 打开回顾时沿用证据档案的暂停逻辑，影片不会在阅读旧台词时继续播放；关闭面板后恢复原有播放状态。
- 回溯、读取手动存档、导入旧存档、开始新轮回之后，回顾内容自动从**当前存档路径**重建，避免显示被舍弃分支内容。
- 对旧版存档中缺失选项文字的路径，使用真实 C# 剧情规则进行规范化；伪造选项文字、断裂路径不进入回顾。

## 具体代码

| 文件 | 责任 |
| --- | --- |
| `Assets/Scripts/Core/EchoDialogueHistory.cs` | 纯 C#、存档派生的只读分页剧情记录，先进行 Normalize 验证 |
| `Assets/Scripts/Runtime/EchoGame.cs` | 新增 `history` 模态页面、菜单/热键/暂停导航、分页和暂停状态 |
| `Assets/Scripts/UI/EchoGameUI.cs` | 原生 uGUI 顶栏「回顾」入口与窄屏布局调节 |
| `Tools/CoreHost/{Echo0317.CoreHost.csproj,Program.cs}` | .NET 8 真实编译和执行这一新增 C# 类：四结局路径、回溯与分页 |
| `Assets/Tests/EditMode/EchoDialogueHistoryTests.cs` | Unity 原生 EditMode：新游戏、真实选择、分页、回溯和伪造存档拒绝 |
| `Assets/Tests/PlayMode/EchoStage19HistoryUiTests.cs` | Unity 原生 PlayMode：可点击的 HUD 入口与模态打开/关闭 |
| `Tests/test_stage19_history.py` | 无 Unity 的架构接线、CoreHost 编译入口回归检查 |

## 验收边界

1. **可立刻验证**：GitHub 托管 `.NET 8` CoreHost 真实编译/运行同一份 `Assets/Scripts/Core`，Python 数据/结构及全部现有剧情可达状态、四结局、存档检查。
2. **需 Unity 授权才能验证**：EditMode/PlayMode 真正执行、新按钮焦点导航、可视化文本滚动、Windows EXE 实际运行/影片音画；源码中已纳入测试，但不报告为通过。
3. **不可混淆**：Stage 19 是功能交付，不意味着已生成可发行的 Windows 安装包。正式影片仍缺少 27 个必需镜头，内测版可使用 8 段预演片。

## 操作方法

在 Unity Windows Player 主剧情界面点击 **回顾** 或按 **L**；也可以打开暂停菜单再选择 **剧情回顾**。选择更早/更新切换历史页，点击关闭回到原位置。没有读过的剧情不会出现。首次新游戏仅显示 intro；通关后显示本轮结局场景和到达它的实际选择。

GitHub CI 真正的执行日志以当前分支、PR 和合并的 `main` GitHub Actions 为准。Unity 许可证尚未配置时无 Windows 原生二进制。
