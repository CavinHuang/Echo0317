# Stage 28｜不提供本地 Unity License 的官方云端 Windows Alpha 构建

日期：2026-11-06。目标：用 Unity 自己提供的 **Build Automation (Unity DevOps)** 云构建服务生成 Windows x64 Alpha，避开之前 game-ci 在 GitHub 托管虚拟机缺少 `UNITY_LICENSE` 时的激活阻塞。

> **真正的授权边界**：官方云构建不需要向 GitHub Actions 放入 `UNITY_LICENSE` 或 `UNITY_SERIAL`，但必须先**使用有权访问该 Unity Cloud 项目的 Unity ID 登录，并授权它读取源码仓库**。没有 Unity ID、Unity 项目权限或仓库授权时，不能自行制造可用的凭证，也不能假称构建完成。

## 采用官方云构建的依据

- [Unity Build Automation](https://docs.unity.com/en-us/build-automation)：官方持续集成服务，可以从 GitHub 构建 Windows Desktop 等平台。
- [Unity DevOps 2026 免费额度公告](https://discussions.unity.com/t/unity-devops-updates-in-2026/1711090)：2026 年 3 月方案描述了每月 200 Windows Standard Compute 分钟的免费额度；实际可用额度和费用仍以账号 Dashboard 为准。
- [Build Automation 配置](https://docs.unity.com/en-us/build-automation/basic-build-configuration/overview)：允许选择 Git branch、自动读取 `ProjectSettings/ProjectVersion.txt` 的 Unity 版本、选择 Windows Builder。
- [自定义 pre-export 方法](https://docs.unity.com/en-us/build-automation/advanced-build-configuration/run-custom-scripts-during-the-build-process)：可在 Editor 编译完、Player 导出前调用仓库内公开的静态 C# 方法，直接执行资源和场景的 fail-closed 检查。
- [连接 GitHub](https://docs.unity.com/en-us/build-automation/get-started-with-build-automation/connect-your-version-control-system)：官方支持 GitHub PAT 或 SSH Key 进行源代码读取。请只使用自己授权、权限最小化的凭证，勿把 Token 粘贴到聊天或提交进代码。

## 源码已经做到的部分

新增真正的 Unity Editor Pre-export 钩子 **`Echo0317.Editor.EchoCloudBuild.PreExport`**，放在 `Assets/Editor/EchoCloudBuild.cs`。

它在 Unity 云构建环境中执行并要求：

1. Unity Editor 为 `6000.3.x`，活动构建平台是 Windows 64-bit。
2. 调用现有 `EchoBuild.EnsureScene()` 创建且注册实际 `EchoEntry.unity` 场景。Git 中原本没有这个由 Editor 初始化产生的场景，因此云构建不得直接使用空 Scenes In Build。
3. 调用现有 `EchoBuild.VerifyImport()`，真正检查 Unity Editor 导入、Legacy Input、五幕剧情与内部 8 个预演视频的可用性。
4. 加载实际 `GameCatalog` 并验证 55 节点、5 道推理、23 个调查热点。
5. 检查已经随 GitHub main 交付的全部 18 张 PNG。
6. 检查最终只有 `Assets/Scenes/EchoEntry.unity` 是有效 Player 入口。任何一步失败则抛出异常阻止云构建，而不是产生看似成功的坏包。

这个方法只负责 Unity Editor 内的真实预检；**云构建服务本身负责随后编译和打包 Windows EXE**。普通 C# .NET CoreHost 或 Python 测试无法代替它。

## Unity Dashboard 首次配置（一次登录授权）

1. 打开 [Unity Dashboard](https://cloud.unity.com/)，以**自己的 Unity ID** 登录。若尚未使用 DevOps，按官方界面开启免费 DevOps（不要擅自开通按量收费）。
2. 打开或创建一个 Cloud Project，例如 `Echo0317`；进入 **DevOps → Build Automation → Settings → Source Control**，选择 GitHub，授权读取 `CavinHuang/Echo0317`。仓库所有者可使用权限最小化的细粒度 Token：Repository Contents = Read，若开启自动触发再增加必要 Webhooks 权限。
3. 到 **DevOps → Build Automation → Configurations** 新建 Windows Desktop 64-bit，名称建议 `Echo0317-Windows-Internal-Alpha`。
4. 选择 Branch `main`，Project subfolder 留空（Unity 工程在 GitHub 根目录），Unity Version 选择 **6000.3.2f1** 或启用 Auto detect（仓库的 `ProjectSettings/ProjectVersion.txt` 明确锁定此版本）。
5. 进入 Advanced settings，找到 **Pre-Export Method**，填入精确方法名：

   ```text
   Echo0317.Editor.EchoCloudBuild.PreExport
   ```

6. 若有 **Scenes** override，留空以便使用脚本自动注册的唯一场景（不要填一个不存在的 Unity 场景）。勾选或配置 EditMode/PlayMode 测试（根据当前账号方案和平台支持情况）。
7. 初次先关闭 Auto-build，手动 Run Build，对照日志确认 `[Echo0317][Unity Build Automation] PRE-EXPORT VERIFIED`，再下载它**确实生成的 Windows EXE 及 Data 目录**。
8. 真正生成的云构建包还需要在 Windows 上运行项目的 `Tools/qa-native-windows.ps1` 四结局自检和交互式界面冒烟测试；云构建本身通过不等于 Player 完整验收通过。

## 结果状态

截至创建此文档，已准备好 Cloud Build 原生预检源码并将其推送到 GitHub，但 **未通过 Unity Dashboard 创建 Cloud Project、授权 GitHub 仓库或执行任何云构建**。因为当前连接只提供 GitHub 仓库写入权限，没有 Unity ID 登录会话或 Unity Cloud 项目写入连接。可用的个人 Unity Cloud 免费额度也不能由不持有账户的人私自领取。

不存在公开共享、可安全挪用的 Unity Personal 构建凭证；也不应使用任何绕过 Unity 官方许可流程的旧版网页手动激活技巧。
