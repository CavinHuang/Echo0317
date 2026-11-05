# 《03:17 · 回声》Stage 10 — Unity 原生工程加固与构建验收

日期：2026-11-04

## 实际结果与边界

此轮基于 Stage 9 **原生 Unity 工程**修复了运行时状态、菜单和镜头恢复、输入条件与发布验证流程；增加了 Unity EditMode/PlayMode 测试，以及 Windows x64 全链路构建、日志、哈希和分发包生成工具。**本会话环境没有 Unity Editor、Unity License 或 C# 编译器，所以未产出 EXE，也没有运行这些原生测试。**

### 修复问题

| ID | 影响 | 修复 | 验证方式 |
| --- | --- | --- | --- |
| R10-01 | 返回主菜单、再进入相同镜头时，`selectedClip` 未清空导致直接跳过播放 | `Halt` 清空镜头 ID、取消旧生成请求，显式 `Replay()` | 新增 Unity PlayMode 测试（待 Unity 执行），静态检查通过 |
| R10-02 | 数字键可对“条件未满足”的选项调用提交（虽然底层阻止，但会报错） | 键盘与按钮统一走 `EchoNarrative.Allowed` | 代码复审，Unity UI 真机待执行 |
| R10-03 | 打开证据档案/存档时视频仍在背后播放 | 打开模态页面自动暂停，关闭后恢复 | 新增用户暂停状态，Unity PlayMode 待运行 |
| R10-04 | 视频还在 `Prepare()` 时暂停，准备结束后可能自动播放 | `userPaused` 在预加载完成后继续生效 | 新增 PlayMode 断言，真机待执行 |
| R10-05 | 本地自动存档损坏造成启动失败 | 保留损坏文件、警告并新游戏回退 | C# 处理已实现，Unity 文件测试待执行 |
| R10-06 | 镜头无法从设置中重播 | 增加“重播当前影片镜头” | UI 代码已加入，原生点击待执行 |
| B10-01 | 编辑器有场景但 Build Settings 为空，无法正常构建 | 首次打开检查场景及启用状态并补齐 | Editor 脚本逻辑已修，待 Editor 运行 |
| B10-02 | 只验证 EXE 存在，漏掉 `_Data` 或依赖文件 | Windows 脚本检查完整构建目录、生成分发 ZIP 和 SHA256 清单 | PowerShell 脚本已准备，待 Windows 跑通 |
| B10-03 | 未验证镜头批准记录是否实际对应文件 | 构建前读取 Unity 数据并校验批准资源存在 | Python 预检通过；Unity Editor 内预检待执行 |

## 本次实跑的检查

- `python Tests/test_export.py`：**PASS** — 55 节点、69,497 个去重可达状态、4 结局、5 个推理规则、43 镜头 ID（含 3 父镜头）。
- `python Tests/test_release_assets.py`：**PASS** — 4 条此前验证的结局路径结构有效、40 提示词与 40 项任务映射一致、8 个预演视频存在。
- `ffprobe`：**PASS** — 8 个程序化分镜 MP4 可解析为 H.264/AAC，有正时长（不等于 Unity 内解码成功）。
- `python Tests/test_csharp_structure.py`：**PASS** — 10 个 C# 文件语法表面上的括号匹配（**不是 Roslyn 或 Unity 编译**）。
- Unity EditMode、Unity PlayMode、Windows x64 编译与 EXE 运行：**NOT RUN**，因为当前执行环境不存在 Unity Editor。

## 可执行的 Windows 原生验收顺序

Windows 11 机器需要 Unity Hub、Unity `6000.3.2f1` 或兼容的 Unity 6.3 LTS 补丁版本、Windows Build Support、有效的 Unity 许可证和 Python 3。

```powershell
$env:UNITY_EDITOR_PATH = 'C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe'
python Tests/test_export.py
python Tests/test_release_assets.py
.\Tools\test-unity.ps1      # EditMode + PlayMode：均要求生成 XML 且有通过用例
.\Tools\build-windows.ps1   # 默认先测再构建，实际输出 EXE + _Data + ZIP + SHA256
```

构建输出目标：`Builds/Windows/Echo0317.exe`、`Builds/Windows/Echo0317_Data/`、`Builds/Echo0317-Windows-x64.zip`、`Builds/windows-build-sha256.txt`。**以上是目标输出；当前交付的 ZIP 内没有伪造的 EXE。**

若连接 GitHub 自托管 Windows Runner，可手动触发 `.github/workflows/windows-unity-native.yml`。Runner 需 `self-hosted,Windows,X64,unity-6-3` 标签、预装 Unity 与许可证，并在仓库变量中设置 `UNITY_EDITOR_PATH`。这不是已启用/运行的远程流水线。

## 必须在 Unity Editor 内完成的实际验证

1. **编译**：首次打开无编译错误，能初始化 `Assets/Scenes/EchoEntry.unity`，项目中无缺失组件或无效资源引用。
2. **UI**：运行原生菜单，鼠标可开始游戏、暂停、读取档案/存档、恢复；全屏与 16:9、16:10、1080p、1440p 显示不遮挡。
3. **剧情**：四种结局实际游玩、隐藏结局条件锁定，回溯后证据与推理撤销，存档继续。
4. **视频**：首次 V01 播放，重进菜单再进入 V01、重播同镜头、暂停预加载和连续镜头复用；禁用或缺失素材后保持剧情可通关。
5. **Windows 构建**：完整 ZIP 解压后在 Windows 11 干净账户运行，验证读写权限、音视频解码、退出重启与存档恢复；至少一次 1～5 幕完整通关。

## 发行相关仍然阻塞

- Unity Editor 实际编译 / PlayMode / Windows EXE 的第一轮真实验收（当前 **P0 阻塞**）。
- 正式 AI 视频、角色配音、音效、字幕审核与导演成片审核（40 项任务未生成正式成片）。
- 正式用户体验测试、设备矩阵、完整 Steam 商店材料、授权证明、AI 使用披露、Steamworks 功能与平台审核。

## 下一阶段入口条件

收到 Unity Build 的 **成功构建日志、测试 XML 和完整 Windows 压缩包**后，才可以宣称“原生迁移技术验收通过”。在此之前，本项目应标识为 **Unity 原生工程迁移 Beta 源码 / Native build unverified**，而不是可上架候选安装包。
