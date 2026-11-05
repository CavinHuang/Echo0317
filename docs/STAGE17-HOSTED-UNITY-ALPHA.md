# 《03:17 · 回声》Stage 17：托管 Unity Windows Alpha 构建

日期：2026-11-05。主要目标：不再强制要求用户自行配置 Windows 自托管 Runner；使用 GitHub 托管机器、经过授权的 Unity Editor 生成 Windows x64 Player，然后实际执行生成的 EXE 验证四结局。

## 本轮实际完成

- 增加 `.github/workflows/hosted-unity-windows-alpha.yml`，四级依赖：Windows .NET 8（真实 C# 游戏 Core）→ Unity 6.3 LTS EditMode/PlayMode → Unity Windows x64 cross-build → GitHub Windows Runner 运行 EXE headless QA / 校验 / 打包。
- 增加 `EchoBuild.BuildWindowsHosted()`，在 GameCI 的 Unity Linux 容器中使用 Unity BuildPipeline 生成 `build/StandaloneWindows64/Echo0317.exe`；复用现有 Release/Alpha 内容预检、入口场景与构建逻辑，不切回 HTML/Electron。
- 增加 `Tools/pack-hosted-alpha.py`：强制校验实际 EXE 存在且具备 Windows PE AMD64 头、`UnityPlayer.dll`、Unity Data 与 StreamingAssets、项目数据字节哈希一致、八条预演视频存在、编译后的 Player 生成真实 QA 日志及报告，之后才打包 ZIP 和 SHA256 清单。
- 发布命名明确为 `echo0317-Windows-Alpha-verified-headless`，不标记为正式 Release，也不把 headless 自检算作实际 GUI/视频/音效真机验收。
- 新增 6 项专项 CI 与伪造输入失败测试，现有剧情、存档、推理与资源检查保持通过。

## 真实执行结果与限制

| 测试或产物 | 已执行结果 | 说明 |
|---|---|---|
| Python / 数据 / 资源离线测试 | **42/42 通过** | 本机 `python -m unittest discover -s Tests -p 'test_*.py' -q` |
| 55 个剧情节点、69,497 个去重状态、四结局 | 通过（离线） | 非 Unity Player 内部运行 |
| 伪造/缺失 Windows EXE 的打包阻断 | 通过 | 没有 PE / DLL / 真实自检不得打包 |
| GitHub Actions YAML 及工作依赖测试 | 通过（本机解析） | 没有实际触发 Workflow |
| .NET 8 CoreHost 编译/运行 | **未执行** | 本机无 dotnet；已包含托管 Windows job |
| Unity Editor / EditMode / PlayMode | **未执行** | 本机没有 Unity Editor 和许可证 |
| Windows x64 原生 Unity EXE | **未生成** | 本轮无法触发实际仓库运行 |
| 原生 Windows Player headless 四结局 QA | **未执行** | 尚无编译得到的 EXE |
| 真机主菜单、视频、声音 | **未执行** | 仍需要 Windows 11 交互会话 |
| 27 个正式影片素材及导演审批 | **0/27** | 仅允许内部 Alpha |

`Builds/hosted-alpha-evidence.json` 当前测试 **失败/阻塞** 是预期行为，不能解释为“已编译”。

## 从零到真实 Windows Alpha 的最短路线

### 方案 A：GitHub 托管 Runner（本轮新增）

1. 创建**私有** GitHub 仓库，将本包中的 Unity 项目根目录内容作为仓库根目录提交（包括隐藏文件夹 `.github`；不要提交任何口令或许可证文件）。本轮 GitHub 连接里未发现能确认属于本项目的现成仓库，因此本次没有执行推送或运行工作流。
2. 在仓库 Settings → Secrets and variables → Actions 设置 Unity 的授权变量。Unity Personal 使用 `UNITY_LICENSE`、`UNITY_EMAIL`、`UNITY_PASSWORD`；Unity Pro 可使用 `UNITY_SERIAL`、`UNITY_EMAIL`、`UNITY_PASSWORD`。请按 Unity 与 GameCI 当期的激活规则自行配置；**不要在聊天里发送密码或许可证**。
3. 在 Actions 中选择 **Hosted Unity Windows Alpha - real EXE and native QA**，点击 Run workflow。这是一次真实构建，可能消耗 Actions 计费分钟和存储。Unity 版本从 `ProjectSettings/ProjectVersion.txt` 锁定为 6000.3.2f1。
4. 工作流全部成功后下载 `echo0317-Windows-Alpha-verified-headless`，其中应有 `Echo0317-Windows-x64-internal-hosted.zip` 和 `hosted-alpha-sha256.txt`；另下载真实 Unity 测试结果和 `echo0317-native-player-qa`，核查 `native-game-qa.json` 和 `native-game-qa.log`。
5. 解压后在实际 Windows 11 桌面启动 `Echo0317.exe`，完成主菜单、影像/音频与存档真机验收。**只有这一步真正通过，才能称“可启动 Windows Alpha”。**

GameCI 官网参考：<https://game.ci/docs/github/getting-started/>、<https://game.ci/docs/github/test-runner/>、<https://game.ci/docs/github/builder/>。

### 方案 B：本机 Unity 6.3 LTS

在 Windows 11 安装 Unity Editor + Windows Build Support + 激活许可证；将 `UNITY_EDITOR_PATH` 指向 `Unity.exe`，再运行 `Tools/build-alpha-windows.ps1`。该流程同时运行桌面主菜单冒烟检查，故需登录桌面会话，不能仅使用无 GUI 的服务进程。

## 当前真实阻塞

- 这里是 Linux 容器，无 Unity Editor、Windows 或 .NET 8 SDK，且 DNS 无法访问 Unity 下载站点。
- 未找到可明确对应本游戏的已连接 GitHub 仓库；无法在用户未指定其他仓库的前提下把游戏代码放入无关仓库。
- Unity 授权是用户/组织控制的外部凭据，无法从聊天创建、读取或代填。

**未满足时不应重复报告“Windows EXE 已完成”。** 下次一旦获得包含项目的仓库及 CI 授权，可直接以失败的第一条真正 Unity 编译日志为起点修复代码，并复跑直至通过。
