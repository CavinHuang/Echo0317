# 《03:17 · 回声》Stage 16 — Windows Alpha 原生构建冲刺与证据报告

日期：2026-11-05（UTC+8）

## 核心验收结论

**Windows Unity Alpha 尚未实际构建成功。不得标记 EXE 可运行或已通过 Unity PlayMode。**

这轮在当前容器中检查到：
- Unity Editor/Unity Hub：**未安装**。
- C# 编译器 `dotnet`/`mono`/`csc`：**均不可用**。
- 当前系统：Linux，无法作为真实的 Windows 11 Unity Player 交互验收环境。
- Unity 官网、GitHub、NuGet 等域名的 DNS：当前独立执行环境无法解析，无法在线取得或安装缺少的工具链。
- 尚无该游戏可调用的已激活 Unity 的 Windows 自托管 Runner，也无本轮触发的远程 Unity 原生构建日志。

**没有创建任何伪装为 Unity Windows 原生应用的 EXE。**

## 本轮实际新增

1. **单入口构建：** `Tools/build-alpha-windows.ps1` 自动查找 Unity 6.3 LTS、检查 Windows Build Support，运行离线预检、可选 .NET 核心编译、Unity EditMode/PlayMode、Windows 编译、编译后 Player QA 和交互式启动冒烟。
2. **独立 C# 验证：** `Tools/CoreHost` 的 `.NET 8` 项目直接编译和运行 Unity 现有 `EchoData.cs`、`EchoNarrative.cs`、`EchoSaves.cs`、`EchoPreferences.cs`。使用有限的 UnityEngine API 适配层，但**没有复制或重写剧情逻辑**，执行五幕 69,497 状态遍历、四结局路线、存档、反伪造和设置持久化测试。这一代码在本地没有 `dotnet`，**尚未真正执行**。
3. **无需 Unity 许可证的托管 CI 准备：** `.github/workflows/core-csharp-verify.yml`，使用 GitHub-hosted `windows-2022` 编译 C# 核心并运行测试。**文件已创建但未推送到游戏仓库，也未触发 GitHub Actions。**
4. **Windows Runner 原生流水线更新：** 内部 Alpha 在 `.github/workflows/windows-unity-native.yml` 中使用新版单入口（运行在已经激活 Unity 的自托管 Windows Runner）。Runner **尚未配置**，未执行。
5. **发行证据的防误报验证：** `Tools/alpha-evidence.py` 必须实际读到 Unity Editor 导入报告、EditMode/PlayMode XML、EXE、UnityPlayer.dll、`StreamingAssets`、编译后运行的原生四结局诊断、分发 ZIP 与全部 SHA256；可选要求**真机主菜单 GUI 启动证明**。缺失任何关键步骤就写失败结果，严禁用历史报告冒充当前构建。
6. **游戏代码修复：** `EchoVideoDirector` 修复「影片到达结尾或跳过后，关闭档案导致视频自动重播」；`EchoGame.ReturnToMenu` 改为先关界面再停片，避免退出游戏时瞬间恢复旧声音。
7. **Git 源码一致性：** `.gitignore` 专门豁免 CoreHost 的 `.csproj`，避免代码推送后 hosted CI 找不到工程文件。

## 已执行测试（本环境）

运行命令：

```bash
python -m unittest discover -s Tests -p 'test_*.py' -q
```

结果：**36/36 个 Python 离线单测通过**，包含 Stage 16 新增的 6 个测试。此前剧情结构检查仍为 55 节点、四结局、69,497 可达状态、零非结局死路；8 条预演影片媒体校验通过。

特别测试：只提供伪造的通过 JSON 而缺少真实 Editor/Player 文件，Stage 16 验收工具按预期失败。

**未执行：** .NET 核心工程实际编译、Unity C# 项目实际编译、Unity EditMode/PlayMode、编译后的 Windows QA、EXE 图形启动。

## 真正取得 Windows Alpha 的下一步（无需继续开发新的验收脚本）

在 Windows 11 用户桌面环境：

1. 安装并激活 **Unity 6.3 LTS**（项目锁定 `6000.3.2f1`）和 **Windows Build Support**；确保能用 Unity Hub 打开本项目。Unity 首次导入需要联网下载 Package Manager 依赖。安装 Python 3.11+（推荐另装 .NET 8 SDK，以便运行独立 C# 核心 QA）。
2. 保留全项目文件夹。若 Unity 提示旧输入系统不可用，在 Player Settings → Other Settings → Active Input Handling 选 **Both** 或 **Input Manager (Old)**，并按提示重启 Unity 后再构建。项目原生 UI 当前使用 `UnityEngine.Input` 与 `StandaloneInputModule`。
3. 在工程根目录的 PowerShell 中执行：

```powershell
$env:UNITY_EDITOR_PATH = 'C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe'
.\Tools\build-alpha-windows.ps1
```

脚本会完成所有阶段；若某一阶段失败，将提供 `Builds/stage16-alpha-evidence.json` 中的失败阶段，不会假装成功。

4. 仅当该脚本全部通过后，才能认定生成了本机真实 Windows Unity Alpha。关键产物：
   - `Builds/Windows/Echo0317.exe` 及同级的 `UnityPlayer.dll`、`Echo0317_Data`；
   - `Builds/Echo0317-Windows-x64-internal.zip`；
   - `Builds/native-game-qa.json`（实际编译的 Player 生成）；
   - `Builds/player-smoke-success.json`（真实交互桌面主菜单冒烟）；
   - `Builds/stage16-alpha-evidence.json`（所有阶段成功且哈希通过）。

如使用 GitHub 自托管 Windows Runner，仓库中的 `windows-unity-native.yml` 可以在**跳过交互式 GUI 冒烟**的条件下自动产出构建和运行时数据；GUI 冒烟仍需在真正登录的 Windows 桌面会话中执行。托管 Windows C# 核心验证独立于 Unity License，只能提前验证核心逻辑，不能代替 Unity EXE。

## 风险与交付边界

| 维度 | 状态 |
|---|---|
| 剧情导出、五幕和四结局逻辑结构 | 离线检查通过 |
| 玩家体验层源码修复 | 已写入，尚未经过原生实际运行验证 |
| C# 核心真实编译验证 | 可执行 CI 已准备，**未运行** |
| Unity Editor、PlayMode、Windows 构建、桌面启动 | **未执行，环境硬阻塞** |
| 影片正式成片 | 27 个必需镜头 0 个获批；只能内部 Alpha |
| Steam 上架 | 不具备条件 |

本次交付重点不是增加新的故事功能，而是将**成功/失败判据收敛到实际编译与启动的证据**。下一项交付目标只能是来自真实 Unity 环境的 Windows EXE、编译日志及玩家端主菜单启动证据。
