# 《03:17 · 回声》Stage 14 原生游戏开发进度与缺口报告

## 结论

本轮解决了 Unity 原生客户端的 **误覆盖存档、设置不持久、菜单导航焦点、输入配置风险**。这是游戏代码的实质修改，不是新一轮纯文档/验收脚本建设。全剧剧情和玩法逻辑没有重写；当前仍属于 **Unity 内部 Alpha 源码工程**，不能上架。

**严禁将“离线检查通过”理解为“Unity 编译、Win11 实际操作或 EXE 构建成功”。** 本运行环境没有 Unity Editor、C# 编译器和 Windows 构建工具，本轮没有产出 EXE，也没有运行 EditMode/PlayMode。

## 本轮具体改动

1. **新游戏二次确认**。已经有游戏进度时，“新的轮回”打开警告界面；取消保留当前进度，确认才重置自动存档，已解锁结局及手动槽位保留。
2. **手动槽位数据安全**。覆盖和删除已使用槽位前必须确认，取消不会写磁盘。操作失败显示错误提示。
3. **独立玩家设置**。`Assets/Scripts/Core/EchoPreferences.cs` 实现字幕、静音、0.75～1.5 倍速及窗口/无边框全屏的持久保存；从主菜单直接进入设置。设置与剧情档案独立，带 `.bak` 文件容错。
4. **菜单焦点与导航**。打开主菜单、弹窗或回到剧情时重设可用按钮焦点，并清理已销毁元素的选中状态，为键盘/手柄 UI 导航建立可靠起点。
5. **Unity 6 输入模式预检查**。当工程只启用新版 Input System 时，导入验证和 Windows 构建会在启动阶段提示设置 `Active Input Handling = Both` 或 `Input Manager (Old)` 并重启 Unity。旧版 `Input.GetKeyDown` 和 `StandaloneInputModule` 不能假定在新模板中默认可用。
6. **原生测试源码**。增加偏好设置轮换/备份恢复/播放速度边界的 EditMode NUnit，菜单和确认弹窗焦点的 PlayMode 用例。测试待 Unity Editor 执行。

## 离线证据

- `Builds/stage14-preflight.json`：机器可读的测试结果；每项注明来源类型。
- `Builds/stage14-actual-tests.log`：离线检查输出。
- 55 节点、4 结局、5 推理题、69,497 可达状态的结构验证通过。
- 8 个预演 MP4 媒体格式校验通过。
- 新增 6 项 Stage 14 源码契约检查通过。
- 正式 Release 门禁按预期拒绝构建：27 个剧情必需视频尚无正式批准版本。

## 当前状态矩阵

| 领域 | 当前状态 | 主要缺口 |
|---|---|---|
| 五幕剧情结构 | 完整，离线可达 | 真实玩家盲测、导演节奏仍未完成 |
| C# 状态机 / 五道推理 / 四结局 | 源码存在，离线剧情合同验证通过 | Unity 原生编译及 Player 全路线验收 |
| 原生主菜单 / 剧情界面 | C# 初版及 Stage 14 交互改动 | 真机验证及商业级视觉动画 |
| 存档与偏好 | 已实现独立设置、三槽存档和确认流程 | Unity Editor 测试、Windows 文件权限与升级验证 |
| 视频播放 | 双 VideoPlayer、预演回退 | 硬件解码、首帧、切镜延迟、弱设备回归 |
| AI 视频资产 | 40 项任务提示词；8 条预演 | 27 条剧情必需正式镜头均未批准；配音/音乐/混音 |
| Windows 构建 / Steam | 脚本和 Release 门禁存在 | 缺 Unity Editor、Windows 原生实测、Steam 商店与平台审核 |

## 下一道不可绕过的硬关口

在装有 **Unity 6.3 LTS + Windows Build Support** 的 Win11 机器：

1. Unity Hub 打开本工程，设置 Player → Other Settings → `Active Input Handling` 为 `Both`（或 `Input Manager (Old)`），重启编辑器。
2. 项目根目录设置 `UNITY_EDITOR_PATH`，依次运行 `Tools/verify-unity-import.ps1`、`Tools/test-unity.ps1`、`Tools/build-windows.ps1`。Release 配置会因为缺正式镜头而拒绝，先以内部 Alpha 构建。
3. 编译成功后运行 `Tools/smoke-windows.ps1`，实际手动确认新游戏取消、覆盖/删除存档取消、选项持久保存、电影与剧情同步、四结局。
4. 将 Editor.log、EditMode/PlayMode XML、原生 QA JSON 和 EXE 产物一起归档后，才可宣告原生迁移验收通过。

当前版本仍不应上传 Steam 商店作为销售中的成品。
