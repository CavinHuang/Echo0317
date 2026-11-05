# Stage 15｜《03:17 · 回声》玩家推进连续性与原生客户端安全修复报告

日期：2026-11-05 ｜ 基线：Stage 14 Unity 6.3 LTS 工程

## 结论

本轮不是添加新的策划或静态验收文件，而是修改了 Unity C# **游戏交互代码**，针对连点、剧情切换、回溯、存档覆盖、推理反馈及影片音频串线做修复。**不能认定已经成功在 Unity 内编译或 Windows 真机运行**：当前执行环境仍没有 Unity Editor / dotnet / Mono / C# 编译器，也无法联网下载安装。交付物为完整源代码工程和可在有 Unity 的环境运行的原生测试。

## 已实现的改动

| 位置 | 实际修改 | 用户获益 |
| --- | --- | --- |
| `Assets/Scripts/UI/EchoGameUI.cs` | 旧剧情按钮/弹窗按钮 `SetActive(false)` 后再 `Destroy` | 避免 Unity 延迟销毁期的旧按钮短暂可点击 |
| `Assets/Scripts/Runtime/EchoGame.cs` | 选项 callback 记录渲染时节点 ID，`Choose` 进行节点核验 | 旧事件不能跳过当前剧情节点 |
| UI + Runtime | 新剧情刷新滚动位置、设置键盘/手柄焦点滚动跟随 | 长剧情与多个选项的可见性更可靠 |
| UI + Runtime | 新线索收录通知，3.5 秒自动消退；推进异常可见提示 | 不再只靠日志判断是否取得线索、推进失败 |
| UI + Runtime | 自动存档失败后游戏界面显示告警、主菜单显示状态 | 避免玩家误以为进度已经可靠保存 |
| Runtime | 读取手动槽位、旧 JSON 导入、时间线回溯的二次确认 | 防止误操作破坏当前轮回 |
| Runtime | 替换状态前 `VideoDirector.Halt()` | 避免读档/回溯后旧剧情的影片/对白继续播放 |
| Runtime | 推理正确/错误结果置于卷宗顶部 | 重要反馈不再隐藏在长列表末尾 |
| `EchoNativeQa.cs` | 编译后原生 QA 新增全状态图遍历与软锁检测 | 除四结局重放外，还检查所有可达状态是否卡死 |
| `Tools/check-native-qa.py` | Native QA 报告 schema v2：要求 69,497 个状态、55 个可达节点、0 个非结局死路 | 旧报告/不完整原生自检不能被误判通过 |
| `Tools/build-windows.ps1` | 构建前运行 Stage 15 离线安全检查 | 新安全检查成为 Windows 构建必备门禁 |

## 已执行的实际检查及证据

- `python -m unittest discover -s Tests -p 'test_*.py' -q`：**30 项 Python 离线测试通过**。证据：`Builds/stage15-offline-unit.log`。
- `python Tools/doctor.py`：9 组离线检查通过，明确报告 Unity 未安装。证据：`Builds/stage15-preflight.json`。
- 剧情 JSON 全状态 BFS：**69,497 个可达状态、55 个可达节点、4 个结局、0 个非结局死路**。这个检查覆盖“玩家是否可能因选择进入无有效后续选项的状态”，但没有验证真实 UI、输入响应和影片解码。
- 8 条预演 MP4：文件存在并通过 `ffprobe` H.264/AAC 检查；正式 AI 视频仍未制作。
- 视频制作表：40 个任务与提示词保留，43 个镜头 ID 中另有 3 个分组父镜头。
- 内部 Alpha 内容门禁通过；**Release 门禁按预期拦截 27/27 个尚未获批的必需镜头**。
- `Assets/Tests/PlayMode/EchoStage15InteractionTests.cs`：新增 3 个 Unity PlayMode 用例源码，覆盖旧按钮立即退役、滚动重置、线索与存档失败提示；**尚未运行**。
- `EchoNativeQa` 新增原生全状态审计，必须在真实 Unity 编译并构建 Windows Player 之后才能验证结果，当前没有实际 Player QA JSON。

## 剩余阻塞（按优先级）

**P0：原生可执行性**
1. Unity 6.3 LTS Editor 首次打开是否成功编译、入口场景是否创建和启动；当前**未运行**。
2. Unity EditMode/PlayMode 全测试、Windows x64 构建、EXE 自检，产出可复现的编辑器日志、测试 XML、原生 QA v2 JSON；当前**未运行**。
3. Win11 实机四结局路径点击、手柄导航、覆盖/回溯取消、存档失败提示、视频切镜首帧和音频交接；当前**未验收**。

**P0：正式内容缺失**
- 27 个剧情必需镜头全部没有获批终片；8 条是程序化预演，并非影视成片。配音、配乐、音频混合与字幕校对未完成。正式发行不应允许带占位素材的构建。

**P1：商业化体验**
- Unity 原生 UI 尚未经过真实构建后的视觉审核、无障碍/分辨率验证、转场/动画节奏调试；现有 UI 只能认定为原生初版。
- 真实视频的首帧延迟、内存/显存压力、跨机型解码稳定性未测试。

**P2：发行与运营**
- Steamworks 平台接入、商店图文与审核材料、成就/云存档、崩溃报告、签名与升级测试。**不影响内部 Alpha 运行，但正式上架前必须评估和完成所需项**。

## Windows 原生验收入口

在 Windows 11 安装 Unity 6.3 LTS + Windows Build Support、有效许可证和 Python 后：

```powershell
$env:UNITY_EDITOR_PATH = 'C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe'
python Tools/doctor.py
.\Tools\verify-unity-import.ps1
.\Tools\test-unity.ps1
.\Tools\build-windows.ps1
.\Tools\smoke-windows.ps1
```

`build-windows.ps1` 当前只允许生成经原生自检的内部 Alpha 分发包；Release 会先因缺失正式素材而失败。不要将源码 ZIP、测试模拟数据或离线检查视为 Windows 游戏安装包。
