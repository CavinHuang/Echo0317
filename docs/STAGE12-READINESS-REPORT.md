# 《03:17 · 回声》Stage 12 — Unity 原生导入准备 / 视频解码竞争修复

**日期**：2026-11-05  
**发行技术**：Unity 6.3 LTS / C# / Windows x64，**不使用 Electron**  
**验证性质**：本机已实际执行 Python、JSON 和 FFprobe 检查；本机没有 Unity Editor / .NET/C# 编译工具，也没有 Windows x64 原生构建能力。以下不把静态检查写成 Unity 编译通过。

## 本轮实际修改

| 工作 | 修改的源文件 | 风险与验证 |
|---|---|---|
| 修复 Unity 原生 UI 程序集引用 | `Assets/Scripts/Echo0317.Runtime.asmdef`、`Assets/Tests/PlayMode/Echo0317.PlayModeTests.asmdef` | 显式引用 `UnityEngine.UI`；结构校验已通过，需在 Editor 验证解析 |
| 取消过时的视频预备任务 | `Assets/Scripts/Runtime/EchoVideoDirector.cs` | 请求切换时停止旧待解码 slot，防止快速连续选择复用同一个 decoder |
| 首帧确认后再显示画面 | 同上 | `VideoPlayer.frameReady` 确认首帧，失败时回退背景图，避免露出旧视频帧或黑帧；实机时延待检 |
| 解码失败快速回退 | 同上 | 异步准备/首帧阶段侦测 `errorReceived`，不必一直等完整超时 |
| 防止镜头音频串音 | 同上 | 开始切换时暂停旧镜头；待解码镜头保持静音，前景切换后再解除静音 |
| 启动素材缺失有错误界面 | `EchoGame.cs`、`EchoGameUI.cs` | 先创建 UI，再读取 JSON；加载失败显示错误并禁用无法使用的按钮 |
| Unity Editor 真正导入报告 | `Assets/Editor/EchoBuild.cs`、`Tools/verify-unity-import.ps1` | `-executeMethod` 只能在 Editor 成功编译后运行；写入版本、程序集与数据统计 JSON，构建脚本必须取得报告 |
| 避免旧包污染新构建 | `Tools/build-windows.ps1` | 构建前清理之前的 Windows 输出目录，正式 Release 禁止 `-SkipTests` |
| 更严格的测试运行门槛 | `Tools/test-unity.ps1` | XML 结果要求 EditMode 至少 5 条、PlayMode 至少 2 条真正成功，不能只看 Editor 退出码 |
| 原生 UI / 快速切镜测试 | `Assets/Tests/PlayMode/EchoPlaybackSmokeTests.cs` | 覆盖快速 V01→V02→V04、停止后重播、缺资源错误界面。**测试已编写，待 Unity 执行** |
| 独立静态守约测试 | `Tests/test_stage12_native_readiness.py` | 6 项已执行通过，**不属于 C# 编译或真实 PlayMode** |
| Windows 进程启动冒烟工具 | `Tools/smoke-windows.ps1` | 只有 Windows 构建真实完成后，才可在交互式桌面执行；非完整游戏验收 |

## 本机执行过的检查

| 运行项目 | 实际结果 |
|---|---|
| `python Tests/test_export.py` | PASS：55 节点、69,497 可达状态、4 结局、5 推理 |
| `python Tests/test_release_assets.py` | PASS：8 份预演 H.264/AAC 视频（ffprobe），40 份镜头提示词 |
| `python Tests/test_csharp_structure.py` | PASS：10 个 C# 源文件结构平衡；**不等于编译** |
| `python -m unittest discover -s Tests -p 'test_stage11_*.py' -v` | PASS：3/3 |
| `python -m unittest discover -s Tests -p 'test_stage12_*.py' -v` | PASS：6/6 |
| `python Tests/test_release_gate.py --profile internal` | PASS：允许预演占位镜头的内部测试 |
| `python Tests/test_release_gate.py --profile release` | BLOCKED（预期）：27/27 必需镜头尚未获批 |
| `python Tools/doctor.py` | PASS：所有可执行的离线检查；明示 Editor 不可用 |
| Unity Editor 首次导入、C# 编译 | **NOT RUN**：没有 Unity Editor |
| Unity EditMode/PlayMode | **NOT RUN**：没有 Unity Editor |
| Windows 原生 EXE、启动/通关 | **NOT BUILT / NOT RUN** |

## 真正完成 Unity 迁移的可操作验收

在 Windows 11 交互桌面或已配置的自托管 Windows runner 上，安装 Unity Hub、Unity `6000.3.2f1`（Unity 6.3 LTS）、Windows Build Support、有效 Unity 许可、Python 3（建议安装 FFmpeg `ffprobe` 用于媒体编码检查）。

在本项目根目录打开 PowerShell：

```powershell
$env:UNITY_EDITOR_PATH='C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe'
python Tools/doctor.py
.\Tools\verify-unity-import.ps1
.\Tools\test-unity.ps1
.\Tools\build-windows.ps1
.\Tools\smoke-windows.ps1
```

其中 `verify-unity-import.ps1` 必须输出 `Builds/unity-import-verification.json`、`Builds/unity-import.log`；`test-unity.ps1` 必须输出两个非空的测试 XML；`build-windows.ps1` 必须输出 `Echo0317.exe`、`UnityPlayer.dll`、`Echo0317_Data`、SHA-256 清单及可分发 ZIP。`smoke-windows.ps1` 只能说明进程在指定时间内未明显崩溃，**仍须人工/自动 UI 验证四结局和真实视频播放**。

正式发行素材不足时，`build-windows.ps1 -Release` 应继续拒绝构建，不能关掉门禁来冒充发行版本。

## 当前缺口按优先级

**P0 原生验收**：Editor 实际编译、PlayMode 结果、Windows x64 二进制、Windows 11 启动和五幕通关。没有这些证据不应称迁移完成。

**P0 内容**：目前有 40 份制作任务提示词，但 **0 条正式 AI 获批视频**；正式发行必需 27 条视频，后续还要配音、音效、字幕、授权和完整导演试映。

**P1 商业游戏质量**：Unity UI 视觉动效与手柄支持、多分辨率、真实切镜帧率与播放延迟、真实玩家盲测、Steamworks 集成和平台合规。

**P2 后置**：12 个补镜、额外玩家成就、云存档、多语种。

**当前结论**：Stage 12 的编译准备、视频异步切换安全和可复现构建验收代码已交付；**项目仍是 Unity 未通过实际编译的开发源代码，不是已经可上架的 Windows 游戏。**
