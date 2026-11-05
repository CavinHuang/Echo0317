# 《03:17 · 回声》Stage 11 — 原生运行安全加固与发行门槛审计

**审计日期：2026-11-05**  
**项目目标：Unity 6.3 LTS / Windows x64 / Steam**  
**结论：Stage 11 的代码修复及独立预检已完成，但没有 Unity Editor，因此 Unity 编译、EditMode/PlayMode 执行和 Windows EXE 构建均仍未验证。不能称为可上架版本。**

## 1. 对 Stage 10 的实际修改

| 编号 | 发现的风险 | 实施修复 | 验证状态 |
|---|---|---|---|
| S11-01 | 读取历史存档时，如果选项文字被篡改，原逻辑会按目标节点回退匹配，降低历史路径校验严格性 | `EchoNarrative.Trace` 要求存在的文字必须精确匹配；仅在旧存档未提供文字且目标唯一时兼容 | 新增原生 EditMode 用例；尚待 Unity 执行 |
| S11-02 | 保存时只替换主文件，没有上一版可恢复文件 | `EchoSaves` 使用同盘替换同时写 `.bak`，自动存档及 3 个手动槽支持从备份恢复；删除槽位会同时删除备份 | 新增原生 EditMode 用例；尚待 Unity 执行 |
| S11-03 | 双播放器反复复用同一 RenderTexture，可能短暂暴露上一条视频的旧帧；切换交接期间可能有音频重叠 | 切镜前清理后台 RenderTexture；新播放器播放期间先静音，到交接时暂停旧播放器再恢复音频 | 已修 C# 逻辑；Unity 原生视觉/声音验证待执行 |
| S11-04 | 生成 Windows 构建包时可能漏检 `UnityPlayer.dll` 或关键 `StreamingAssets` | 强化 PowerShell 构建后文件校验 | 脚本静态审查完成；Windows 构建待执行 |
| S11-05 | 以 Alpha 的占位动态分镜误执行“正式发行”构建 | 发行镜头清单与双构建配置：默认 `internal`；`-Release` 需要 27 个剧情必需镜头全部获批并存在对应媒体文件 | Python 发行门禁与 3 个专项测试通过；Unity Editor 发行门禁待执行 |
| S11-06 | C# 原生验收与纯数据检查被混为一谈 | 新增离线 `Tools/doctor.py` 产出 JSON，明确显示“没有 Unity / 原生测试未运行” | 本地已实际运行 |

## 2. 实际执行结果

| 检查项 | 结果 | 证据 |
|---|---|---|
| 剧情图导出检查 | **PASS** | `python Tests/test_export.py`；55 节点、69,497 可达状态、4 结局、5 证据推理 |
| 镜头与资源检查 | **PASS** | `python Tests/test_release_assets.py`；40 份提示词、43 镜头 ID（含 3 个不生成视频的父 ID） |
| 预演视频格式 | **PASS** | `ffprobe`；8 条 H.264 / AAC 视频，注意不是正式影片 |
| C# 文本结构检查 | **PASS（静态表面）** | `Tests/test_csharp_structure.py`；不能代替 C# 编译器或 Unity |
| 内部 Alpha 发布门禁 | **PASS** | 预演视频齐全；可继续编译内部版本 |
| 正式 Release 发布门禁 | **BLOCKED（符合预期）** | 27/27 必需镜头尚无获批正式视频；`python Tests/test_release_gate.py --profile release` 返回失败 |
| 发行门禁单元测试 | **3/3 PASS** | `python -m unittest discover -s Tests -p 'test_stage11_*.py' -v` |
| Unity EditMode / PlayMode | **NOT RUN** | 当前机器无 Unity Editor；新增测试仅为源码 |
| C# 真正编译 | **NOT RUN** | 当前机器无 Unity Editor 或 C# 编译器 |
| Windows x64 EXE | **NOT BUILT** | 当前不是带 Unity Editor 的 Windows 构建环境 |
| Windows 11 实机完整通关 | **NOT RUN** | 目前没有经确认的 Windows EXE |

机器可读取的原始预检记录在 `Builds/stage11-preflight.json`。其状态不应被解释为“Unity 通过”。

## 3. 为什么 40 个任务只要求 27 条发行必需视频？

全项目有 **40 个需制作的镜头任务**和 **43 个镜头登记 ID**（其中 `V07/V13/V17` 是组合父节点，不生成单条影片）。正式发行门槛专门挑出 **27 个在剧情中实际会播放的必需镜头及其分支变体**；`X01–X12` 和 `V11` 属于可选补充内容，不应阻止最小完整作品发行。正式镜头清单在 `Assets/StreamingAssets/Data/release-required-shots.json`。

**注意：** 上述门槛只验证视频存在及审核元数据，不替代人工审看、字幕配音、权利证明、完整章节验收和 Steam 平台审核。

## 4. 运行 Windows 原生验证

前置：Windows 11、Unity Hub、Unity `6000.3.2f1`（或经验证的兼容 6.3 LTS 补丁版本）、Windows Build Support、Unity 许可证、Python 3。

```powershell
$env:UNITY_EDITOR_PATH = 'C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe'
python Tools/doctor.py
.\Tools\test-unity.ps1
.\Tools\build-windows.ps1
```

**内部 Alpha 构建**默认允许预演视频，用于 Unity 真正运行和修复编译问题。`build-windows.ps1` 正常结束必须包含：

- `Builds/Windows/Echo0317.exe`
- `Builds/Windows/UnityPlayer.dll`
- `Builds/Windows/Echo0317_Data/StreamingAssets/Data/{story,cases,shots,release-required-shots}.json`
- `Builds/EditMode-results.xml`、`Builds/PlayMode-results.xml`
- `Builds/Echo0317-Windows-x64-internal.zip`、`Builds/windows-build-sha256.txt`、`Builds/Windows/ECHO_BUILD_PROFILE.txt`

正式内容全部制作并审核后，才应执行：

```powershell
.\Tools\build-windows.ps1 -Release
```

该命令目前必定被 27 条未完成镜头拦截，这是有意的保护。

## 5. 距离“可上架游戏”的真实缺口

**P0 技术验收：** 首次 Unity Editor 导入编译；跑 EditMode/PlayMode 测试；生成 Windows x64 EXE 并在 Windows 11 实测启动、菜单、真实视频解码、暂停、存档与四结局。

**P0 正式内容：** 27 条剧情必需正式视频，至少需要逐镜头一致性审查；配音、音效、字幕、音画同步和完整电影节奏审查。

**P1 品质和发行：** 原生 UI 视觉和手柄适配、不同分辨率和显卡测试、弱磁盘/缺媒体容错、长时游玩测试、玩家盲测、Steamworks 集成、商店素材、授权与 AI 内容披露。

**P2 后置功能：** 额外 13 个可选镜头、云存档、完整成就体系、多语言发行和内容生产后台。它们不是打通首个可发行影游的必要条件。

## 6. 本轮总结

已完成的是可检查的 Unity 原生源码加固、发行门槛、防损坏存档与离线预检。**没有伪称 Unity 已成功编译，也没有生成虚构的 EXE**。下一个真正的“迁移完成”证据，应是 Unity 的编译与测试日志、Windows ZIP、干净环境启动和四结局完整路径截图或录屏。
