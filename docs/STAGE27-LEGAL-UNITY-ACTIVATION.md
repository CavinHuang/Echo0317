# Stage 27｜合法 Unity 激活与真实 Windows Alpha 构建闭环

日期：2026-11-06。Unity 版本仍锁定为 **6000.3.2f1 / 6.3 LTS**。本阶段不把 .NET CoreHost 代替 Unity Editor，不伪造 Windows EXE、测试报告或许可证。

## 已确认的真正阻塞

此前的 GitHub 托管 Unity 构建运行：[`41966421571`](https://github.com/CavinHuang/Echo0317/actions/runs/41966421571)。它的 `core-csharp` 通过，`unity-tests` 的授权前检查失败，日志提示 **`UNITY_EMAIL / UNITY_PASSWORD` 没有配置**，导致后续原生 Windows 构建与 Player QA 未开始。这表明**不能声称仓库已有可用的 Unity 授权**。此处无需尝试提取或复用其他人的 license、token 或账号。

Unity 官方 6000.3 文档说明：**Unity Personal 的激活应通过 Unity Hub 登录完成**；手动激活流程不支持 Personal。GameCI 文档仍描述将现有 Personal `.ulf` 作为 GitHub Secret 的方法，但与新版 Unity 官方支持范围有差异，不能保证能在临时 GitHub Runner 上正常激活。因此优先推荐**已经通过 Unity Hub 激活的个人 Windows 11 设备**，而不是尝试私下取得网络许可证。官方资料：
- [Unity 6000.3 License activation methods](https://docs.unity.com/en-us/engine/6000.3/manual/get-started/install-and-upgrade/licenses-and-activation/license-activation-methods)
- [GameCI Activation](https://game.ci/docs/github/activation/)

## 路线 A（推荐）：免费 Unity Personal + 本地一键构建

使用你有权限的 Windows 10/11 机器，不需要在 GitHub Secrets 保存 Unity 密码。

1. 安装 [Unity Hub 官方版](https://unity.com/download)，并从 Hub 安装 **Unity 6.3 `6000.3.2f1`** 和 `Windows Build Support` 模块。
2. 在 Unity Hub 中使用**自己的** Unity ID 登录：`Settings / Preferences → Licenses → Add → Get a free Personal license`。需符合 Unity Personal 使用资格。
3. 打开 PowerShell，进入项目根目录（此项目在 [GitHub main](https://github.com/CavinHuang/Echo0317)）：

   ```powershell
   .\Tools\prepare-unity-windows.ps1 -OpenHub
   .\Tools\prepare-unity-windows.ps1 -VerifyEditor
   .\Tools\prepare-unity-windows.ps1 -BuildAlpha
   ```

   - `-OpenHub`：只打开 Hub/官方下载页，不使用 CLI 绕过账号登录。
   - `-VerifyEditor`：真正运行 Unity Editor 的项目导入及编译验证，报告将写入 `Builds/unity-import-verification.json`。没有合法激活会失败。
   - `-BuildAlpha`：调用项目现有的 Unity EditMode/PlayMode 测试、Windows x64 EXE 编译、Player 四结局自检、交互式启动冒烟以及内部 Alpha 打包。**只有执行成功才能宣称生成了真实 EXE**。
   - 如果只在没有图形桌面的自托管 Runner 中执行，可额外设置 `-SkipInteractiveSmoke`；这表示未通过窗口启动验收，不等于正式可发布。

仅运行 `prepare-unity-windows.ps1` 无参数只检查前置条件，不意味着授权已成功。本地会生成 `Builds/unity-local-activation-preflight.json`，其中只有安装情况、编辑器验证状态与非敏感错误信息。

## 路线 B：免费 Personal + 自托管 GitHub Windows CI

想让 GitHub Actions 自动上传真实 EXE，可以把上述**已通过 Hub 激活的同一 Windows 账户**配置成自托管 Runner。

1. 在仓库 `Settings → Actions → Runners → New self-hosted runner` 按 GitHub 官方命令注册 Windows x64 Runner。
2. 注册时使用标签 `unity-6-3`（工作流要求 `[self-hosted, Windows, X64, unity-6-3]`），应让 Runner 在拥有 Unity 激活会话的**同一个 Windows 用户**中运行。使用 Windows 服务时特别注意服务账户不同可能无法访问个人命名用户许可；首次建议交互式运行 Runner。
3. 如 Editor 不在默认路径，在 GitHub 仓库 `Settings → Secrets and variables → Actions → Variables` 设置 **非敏感**变量 `UNITY_EDITOR_PATH` 为完整 Unity.exe 路径。
4. GitHub Actions 手动运行 [`Unity native Windows verification`](https://github.com/CavinHuang/Echo0317/actions/workflows/windows-unity-native.yml)，选择 `internal`。工作流先运行 `Tools/prepare-unity-windows.ps1 -VerifyEditor`，之后执行原生编译及 Player QA；执行结果在 Actions 中可下载。

**本路线不需要提交或提取 Unity 密码，也不将 Unity 授权文件复制到仓库。** 自托管 Runner 仅执行可信分支，禁止让外部来源的 PR 在持有本机登录/许可的 Runner 上执行任意代码。

## 路线 C：已有合法付费授权 + GitHub Hosted Unity

如果用户持有可用于 CI 的 Unity Pro/Enterprise 授权，可在仓库 GitHub Actions Secrets 配置官方支持的合法 `UNITY_SERIAL`（若对应授权采用序列号），以及必要的 `UNITY_EMAIL` / `UNITY_PASSWORD`。其他许可证类型需要依照 Unity 授权政策与组织的 License Server/Seat 配置，不可假设所有 Pro 账号都有 Serial。

现有 [`Hosted Unity Windows Alpha`](https://github.com/CavinHuang/Echo0317/actions/workflows/hosted-unity-windows-alpha.yml) 已包含 `game-ci/unity-test-runner@v4`、`game-ci/unity-builder@v4`、Windows Player QA，以及 EXE ZIP 校验。GitHub Secrets 由仓库所有者在设置界面输入，**不要发送到聊天，也不要写进代码**。Personal 的 GitHub ULF 方式受 Unity 当前官方支持范围约束，不能作为必然成功的方案。

## 本阶段代码交付

- 新增 `Tools/prepare-unity-windows.ps1`：统一检测 Hub、精确 Unity 版本、Windows Build Support 与 Python；可打开官方 Hub；可真实运行 Editor 验证和一键 Windows Alpha 构建。报告不读取许可证或密码，不把缺少授权伪装成成功。
- 更新 `.github/workflows/windows-unity-native.yml`：在自托管 Windows 构建前执行真实 Unity 导入/授权验证，上传非敏感预检查报告，且保持原 Player QA 与发行阻断。
- 相关离线测试：`Tests/test_stage27_legitimate_unity_activation.py`。

## 尚不能完成的环节

目前此执行环境没有 Unity Editor 和已授权的 Unity Hub 会话；仓库历史 GitHub Hosted 运行日志也明确显示没有配置授权 Secrets。因此**本阶段无法代表用户登录 Unity Hub、领取账户许可证、构建真实 Windows EXE 或声称原生 PlayMode 通过**。

拿到合法授权之后，以上脚本和工作流会输出可核查的真实结果；不要把 CoreHost 测试或文件存在性当作 Unity 二进制成功。
