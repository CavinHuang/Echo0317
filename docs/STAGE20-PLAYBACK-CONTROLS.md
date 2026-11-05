# 《03:17 · 回声》Stage 20｜Unity 视频播放状态与玩家控制

日期：2026-11-05。此阶段保持 Unity 6.3 LTS + C# 桌面影游路线，**不等待 Unity 许可证才继续游戏功能开发**。

## 定位并修复的问题

1. **准备中跳过影片无效**：原 `Skip()` 只检查 `active >= 0`，玩家在新镜头 `Prepare()` / 等首帧时点击跳过可能跳过旧画面，随后新镜头仍开始播放。
2. **旧影片的迟到回调干扰新镜头**：旧 VideoPlayer 在准备另一个镜头时仍可能触发完成/报错，污染当前镜头状态甚至停止新影片。
3. **暂停菜单中的重播有短暂恢复旧影片的风险**：先关闭菜单再重播会短暂调用 `Resume()`。
4. **玩家缺少快捷操作**：原重播和跳过只藏在设置菜单内。

## 玩家功能

- 观看剧情时按 **R** 强制重播当前镜头；按 **K** 仅跳过**当前影片画面**，不推进剧情、不改动已收集证据和存档。
- 原生播放 HUD 上提示 `R 重播 / K 跳过画面`，设置面板中同样提供两个操作。
- 影片准备中也可跳过：取消待解码的协程/VideoPlayer；上一段被暂停的画面同步隐藏，露出场景参考背景图。
- 同一段已跳过影片，不会被重复跳过或在关闭暂停窗口后自动重播；显式 **R** 仍可重播。
- 切换镜头期间保持暂停状态；关闭暂停面板后，只会恢复已经准备完成、仍属于当前有效请求的影片。
- 播放请求使用 token（代次）过滤取消/过期的 Prepare、首帧和完成事件；旧影片的报错不再中断新影片准备。

## 修改的真实工程代码

| 路径 | 责任 |
| --- | --- |
| `Assets/Scripts/Core/EchoPlaybackState.cs` | Unity 无关的 C# 播放状态：Request / CanPresent / Complete / Skip / Pause / Resume / Halt |
| `Assets/Scripts/Runtime/EchoVideoDirector.cs` | 把原生双 VideoPlayer 与代次状态接线，清理取消的解码器、防重复完成及旧回调 |
| `Assets/Scripts/Runtime/EchoGame.cs` | 游戏 R/K 快捷键，显示操作提示，设置中重播时先提交新请求再恢复窗口 |
| `Tools/CoreHost/Echo0317.CoreHost.csproj` / `Program.cs` | 编译**同一份** C# 播放状态源码，在无许可证 .NET 8 环境执行竞态/跳过/重播/暂停检查 |
| `Assets/Tests/EditMode/EchoPlaybackStateTests.cs` | Unity Editor 内的生命周期测试（待 Unity 环境验证） |
| `Assets/Tests/PlayMode/EchoStage20PlaybackTests.cs` | Unity VideoPlayer 快速切换、跳过、重播、菜单退出测试（待 Unity 环境验证） |
| `Tests/test_stage20_playback.py` | 无 Unity 环境的代码接线检查 |

## 真实验收状态与边界

GitHub Actions 的 `Compile and execute real C# narrative core (without Unity)` 会真正编译并执行 `EchoPlaybackState`（和现有剧情、存档、四结局 CoreHost），`Unity migration data parity` 继续验证全套 Python 回归。这些通过只代表**可运行的核心状态与源码契约**。

**不得将其写成已经运行 Unity 视频。** 真正的 VideoPlayer、UI、实际音画、Unity EditMode/PlayMode、Windows x64 EXE 仍必须等 Unity 许可证/Editor 可以使用后执行。此阶段尚不意味着已交付可启动的 Windows Alpha。

正式 Release 仍受 27 个必需、未审批镜头的资源门禁限制；当前 8 条预演视频仅供内部 Alpha。

## 后续真机重点

在 Windows 11 的 Unity 6.3 LTS PlayMode 或编译 Player 中验证：连续切换视频（V01 → V02 → V04）、准备中立即按 K、跳过后按 R、设置菜单暂停/恢复、视频缺失背景回退、长时间播放是否存在异步回调交错和音频泄露。需要保留真实运行日志，而不是凭脚本存在声称已通过。
