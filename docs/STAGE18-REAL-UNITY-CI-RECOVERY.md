# 《03:17 · 回声》Stage 18｜真实 Unity Windows Alpha CI 故障修复

更新：2026-11-05。目标保持 **Unity 6.3 LTS 原生 Windows x64 EXE**，而不是以 HTML、Electron、.NET CoreHost 或 Python 代替 Unity Player。

## 已取得的实际证据

仓库：[CavinHuang/Echo0317](https://github.com/CavinHuang/Echo0317)。

- `main` 提交 `624437dbc80d147b49e2fa18b6dd602d852baf27` 的 [独立 C# CoreHost](https://github.com/CavinHuang/Echo0317/actions/runs/41966612295) 与 [迁移数据一致性检查](https://github.com/CavinHuang/Echo0317/actions/runs/41966612380) 均 **成功**。这证明已运行实际 C# Core，但**不证明** Unity Player 能启动。
- [托管 Unity 实测 #41966421571](https://github.com/CavinHuang/Echo0317/actions/runs/41966421571) 的 `core-csharp` 通过；`unity-tests` 停在激活预检：`UNITY_LICENSE`、`UNITY_SERIAL`、`UNITY_EMAIL` 和 `UNITY_PASSWORD` 均未配置；因此 Unity Editor 没有真正启动，`unity-build` 和 `native-player-qa` 均为 skipped。
- 此失败还暴露第二个独立缺陷：Test Runner 未提供结果路径时，`upload-artifact@v4` 报 `Input required and not supplied: path`，干扰故障定位。

## Stage 18 代码改动

1. Unity Test Runner 与 Windows cross-build 的 Runner 固定为 `ubuntu-24.04`，不受 GitHub `ubuntu-latest` 正向迁移到 Ubuntu 26.04 的影响。
2. 明确向两个 GameCI v4 action 传递 `unityVersion: 6000.3.2f1`，与 `ProjectSettings/ProjectVersion.txt` 一致，不依赖自动推断。
3. Test Runner 显式设置 `artifactsPath: unity-test-artifacts`；日志工件上传固定使用同一路径，并且仅当 Test Runner 真正尝试执行时上传。即使测试本身失败，也保留可能产生的工件；若没有文件，仅发 warning，不让无关异常掩盖原始失败。
4. 新增离线契约测试，校验固定 Runner/Unity 版本和工件路径，防止再次出现同类 CI 配置回退。

## 真正解除阻塞所需的一次性外部操作

**当前仓库是 public**；不应把 Unity 许可证、账户密码或授权令牌提交到文件或传入聊天。托管 GameCI 的构建仍依赖 Unity 的合法许可证激活。按 [GameCI 当前授权指引](https://game.ci/docs/github/activation/)：

- **Unity Personal**：在自己的电脑使用 **Unity Hub → Preferences → Licenses → Add → Get a free personal license** 正常激活，取得 `.ulf` 文件，打开 [仓库 Actions Secrets 页面](https://github.com/CavinHuang/Echo0317/settings/secrets/actions) 设置 `UNITY_LICENSE`（文件完整内容）、`UNITY_EMAIL`、`UNITY_PASSWORD`。
- **Unity Pro**：在相同 Secrets 页面设置 `UNITY_SERIAL`、`UNITY_EMAIL`、`UNITY_PASSWORD`。
- 当前 Unity 官方文档说明 **Unity Personal 不支持旧式离线/手动 ALF 激活**；不要依赖旧版 GameCI 文档的 `.alf` 手动激活步骤。
- 然后在 [Hosted Unity Windows Alpha](https://github.com/CavinHuang/Echo0317/actions/workflows/hosted-unity-windows-alpha.yml) 中使用 **Run workflow**（选择合并后的 main 或待验证的分支）。

**注意**：GitHub App 连接不能在用户账号下代登录 Unity 或自行创建账户许可证；Action 必须从 Secrets 中读取用户合法授权，项目代码无法绕开这一要求。

## 必须依次满足的通过条件

| 阶段 | 通过依据 | 当前证据 |
| --- | --- | --- |
| .NET 8 / Python 离线门禁 | GitHub Actions 核心编译、剧情/存档/资源测试全部成功 | 已成功 |
| Unity Editor | Unity 6.3 真实 EditMode 与 PlayMode 运行并成功、留存测试报告 | 阻塞：缺少许可证 |
| Unity Windows x64 | 编译产出真实 `Echo0317.exe`、`UnityPlayer.dll`、`Echo0317_Data` | 尚未生成 |
| Windows Player headless QA | GitHub Windows Runner **运行** EXE，自身产出四结局、69,497 状态和存档验证日志 | 尚未执行 |
| 玩家桌面 Alpha 验证 | Windows 11 真正显示菜单并可操作；影片、声音、存档和完整游戏路线实测 | 尚未执行 |
| Release 素材门禁 | 完成并通过全部必需正式镜头审核 | 仍缺 27 个，不属于内部 Alpha |

不要把前两行的成功写成“完整 Unity EXE 已交付”；只有工件 `echo0317-Windows-Alpha-verified-headless` 确实产出，才可报告 headless Alpha。GUI/音视频仍必须另行实际运行验证。

## 继续排障顺序

当 Secrets 配好后，以**第一次真正 Unity 6.3 Editor 的编译/导入日志**为准修 C# / .asmdef / ProjectSettings 等报错，复跑 EditMode/PlayMode → Windows Player → EXE QA，直到获得真实分发 ZIP 与 SHA-256 报告。未实际出现的 Unity 错误不能预先宣称已修复。
