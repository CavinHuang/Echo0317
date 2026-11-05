# 《03:17 · 回声》Stage 13：Unity 原生 Player 自检与发行构建验证报告

日期：2026-11-05  
基线：Stage 12 Unity 6.3 LTS 工程  
阶段目标：**让 Windows 原生程序在完成编译后自行验证四结局、存档与数据读取；并让分发打包只在真实 Player 自检通过后执行。**

## 1. 本阶段已经写入的实现

| 组件 | 变更 | 核验状态 |
|---|---|---|
| `Assets/Scripts/Runtime/EchoNativeQa.cs` | 新增 `-echoQa` 纯原生 Player 诊断模式；在编译后的客户端里读取 StreamingAssets、用同一 C# 状态机重走四条结局、在每条路线中做存档读写、拒绝伪造线索、输出 JSON 与退出码 | **源码已完成；Player 内未执行** |
| `EchoGame.cs` | 在自动创建 uGUI / VideoPlayer 前切换到诊断入口；命令行 `-runTests` 的 Editor 测试期间避免自动启动第二个游戏；正常启动输出 `ECHO_GAME_READY` | 静态验证通过；Unity PlayMode 待执行 |
| `EchoJourneyTests.cs` | 新增 Editor 侧直接调用同一份 Native QA 核心逻辑的测试，断言 4 条结局、123 次选择、存档与非法线索处理 | 测试源码完成；Unity EditMode 待执行 |
| `Tools/qa-native-windows.ps1` | 运行已编译 EXE 的 `-batchmode -nographics -echoQa`，要求进程成功退出、诊断日志和 JSON 存在且一致 | 审查流程完成；需 Windows EXE 才可执行 |
| `Tools/check-native-qa.py` | 校验原生报告结构、Unity 版本、四结局、123 步以上、存档和防伪造状态 | 本地单元测试通过；尚无真实报告可验证 |
| `Tools/build-windows.ps1` | 先完成 Editor 测试、构建 EXE，再运行 EXE 的原生自检；未通过不能打包 ZIP | 脚本链路静态验证通过 |
| `Tools/smoke-windows.ps1` | 交互式启动检查必须看到真实客户端写出的 `ECHO_GAME_READY`，不再只检查进程存活 | 脚本链路静态验证通过 |
| `.github/workflows/windows-unity-native.yml` | 收集原生 QA 日志与 JSON，以便诊断构建失败 | 配置已更新，未在 Runner 执行 |
| `Tests/test_stage13_*.py` | 8 项新增离线合同测试：拒绝损坏报告、缺失报告、错误版本和视频分支映射异常 | **8/8 通过** |

## 2. 实际已执行的验证与边界

- **离线 Python 单元测试：17/17 通过**（含本阶段新增 8 项）。这验证测试门禁的输入与代码结构，不代表 Unity C# 编译成功。
- **剧情 JSON 遍历：55 个节点、69,497 种可达状态、四结局、五道推理，验证通过**。数据和上阶段一致。
- **制作素材：43 个登记镜头 ID = 40 项生成任务 + 3 个父镜头；40 份提示词均在。**
- **现有视频：8 条动态分镜 MP4 的媒体格式与时长检查通过。**
- **Internal 资源预检通过**：允许 Alpha 使用动态分镜。
- **Release 资源门禁按预期拒绝**：27/27 个剧情必需镜头未获正式视频审批。这个拒绝不应当算测试失败。
- **Unity Editor、C# 编译器不可用**：本机未执行 Unity 导入编译、EditMode、PlayMode、Windows Player 自检和 Windows EXE 构建。项目中不会附带伪造的原生 QA PASS 报告。

日志：`Builds/stage13-offline-tests.log`。本次环境与门禁诊断：`Builds/stage13-preflight.json`。

## 3. 在 Windows 上实际完成原生验收

前置：Unity 6.3 LTS Editor（项目锁定 6000.3.2f1）、Windows Build Support、有效许可、Python 3、Windows 11 或适配的 Windows Runner。

```powershell
$env:UNITY_EDITOR_PATH = 'C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe'
python Tools/doctor.py
.\Tools\verify-unity-import.ps1
.\Tools\test-unity.ps1
.\Tools\build-windows.ps1
.\Tools\smoke-windows.ps1
```

`build-windows.ps1` 现在自动调用 `qa-native-windows.ps1`，成功后才会打包。关键的**真实产物**是：

- `Builds/Windows/Echo0317.exe` 和配套 `_Data` 目录
- `Builds/EditMode-results.xml`、`Builds/PlayMode-results.xml`
- `Builds/native-game-qa.json` **（必须是 EXE 亲自生成）**
- `Builds/native-game-qa.log` 含 `ECHO_NATIVE_QA_PASS`
- `Builds/Echo0317-Windows-x64-internal.zip` 和 SHA-256 清单
- `Builds/player-smoke.log` 含 `ECHO_GAME_READY`

对失败情况的要求：任何步骤返回非零、文件缺失、报告字段不符、没有原生日志 PASS 或超过超时时限，均应中止交付。

## 4. 发行缺口与当前定位

**五幕剧情的“数据结构完整”与“Unity 真正可玩”是两层独立的证明。** Stage 13 加入了后者所需的机器验收链路，但没有替代真实的 Unity 编译与真机播放。

| 关口 | 状态 | 阻塞/后续动作 |
|---|---|---|
| Unity 6.3 导入、C# 编译 | 未执行 | 在 Windows Unity Editor 运行并按编译报错修复 |
| Unity EditMode、PlayMode | 未执行 | 执行真实 NUnit 测试，修复原生回归失败 |
| Windows EXE、内建 QA | 未执行 | 执行构建、123 次剧情选择与存档自检，取得证据 |
| 主菜单 / 点击 / 视频 / 音频真实设备检查 | 未执行 | Windows 11 手动/自动交互路径测试，测量切镜黑屏和性能 |
| 正式 AI 视频、配音、混音 | 未完成 | 27 个剧情必需成片镜头；全部 40 项任务按制作计划处理 |
| 商业化发行 | 未准备就绪 | 商店素材、内容权利、Steamworks 平台验收 |

**项目当前仍是 Unity Alpha 源码，不能声称是已验证的 Windows/Steam 上架版本。**
