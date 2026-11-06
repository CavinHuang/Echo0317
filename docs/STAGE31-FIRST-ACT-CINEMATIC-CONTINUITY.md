# Stage 31｜第一幕原生音效 + 缺片演出可靠性 + 字幕同步

更新：2026-11-06。项目为 Unity 6.3 LTS Windows 原生互动悬疑影游《03:17 · 回声》。

## 本阶段解决的真实玩家问题

Stage 30 已经能在第一幕调查猫眼、照片、门禁口令及旧录音，并在后续剧情得到文字反馈。但正式镜头、配音仍未制作完毕。如果播放器遇到缺少 MP4，旧实现会直接调用 `Halt()` 丢失镜头 ID；当下场景图不存在时，`SetSceneArt` 会继续显示上一场景的图（错误的角色/地点）；与此同时字幕计算会在影片准备首帧或暂停时退回故事最后一行，出现下一句剧透或镜头错配。

## 已实际写入 Unity 的实现

### 1. 第一幕 10 个场景的原创 Foley 音效

`Assets/Scripts/Core/EchoFirstActFoleyPlan.cs` 为以下真实节点设置了不会重复的时间线：

| 节点 | 与叙事绑定的原创临时声音 |
| --- | --- |
| `intro` | 手机震动、远处金属响动 |
| `warning` | 第二次手机震动 |
| `c1_peephole` | 走廊金属轻响 |
| `c1_photo` | 翻动相纸 |
| `c1_receipt` | 查看回执时的短信提示 |
| `c1_knock` | **三次**时间间隔不同的门板敲击 |
| `c1_password` | 门禁终端双音短鸣 |
| `c1_playvoice` | 录音机按键与细微磁带底噪 |
| `c1_trydoor` | 短暂锁舌/电子门禁警告 |
| `c1_hook` | 暗藏的金属轻响 |

`Assets/Scripts/Runtime/EchoFirstActFoley.cs` 使用 **Unity AudioClip.Create** 和数学波形程序化生成以上小音效，完全本地、无网络、无外部商业音频采样和许可证依赖。声音只用于**预演与缺少正式影片**的场景；已经存在合法 `approved_final` 影片文件时会完全跳过，以避免叠加导演正式混音。

它与当前游戏播放器一样跟随静音设置；游戏暂停、读档/案卷弹窗及进入热点调查时停止内部音效进度，恢复后接着播放**尚未发生的**敲击；新游戏、回主菜单时停止所有音效；从证据推理界面重新渲染**同一剧情步**不会重启三次敲门。此阶段保留游戏内原有交互 UI 短音。

### 2. 失去 MP4 以后也有正确的故事演出

`EchoVideoDirector`：
- 场景图每次切换**先清空旧画面**。当请求的 `Art/Sxx.png` 文件缺失或损坏时，隐藏 RawImage 以露出 Unity 画面本身的暗色背景，避免上一场景错误画面和白色全屏。
- 没有 MP4、无法解码或首帧超时：真正取消过期解码请求、隐藏双播放器，**保留当前镜头编号**，标记 `IsFallback`，显示静态场景与真实剧情；不会制造一段不存在的视频、不会修改游戏节点。
- 手动 **R 重播**可以重新发起该镜头请求；**K 跳过画面**仍不跳剧情，返回菜单时 `Halt()` 真正清除旧片 ID。
- `HasApprovedFinal` 根据导演标记和**正式文件实际存在**两项条件判定，不能把预演视频、被删除的正式片误判为已批准演出。

`EchoGame` 镜头状态现在清楚区分「已批准正式镜头」「预演影片 · 非正式素材」「静态场景 · 镜头未制作」，避免玩家以为预演已经是最终正式版本。解码失败还能在状态栏提示本次失败原因和 R 重试操作。

### 3. 字幕与真实视频状态同步

`Assets/Scripts/Core/EchoSubtitlePresentation.cs` 统一字幕策略：
- 正在 `Prepare()`、等待首帧：清空字幕，不展示上一个镜头文本，也不抢先播最后一句。
- 真正持有活动影片（**包括暂停期间**）：仅显示该片 `captions.json` 里按当前播放器时间匹配的字幕；镜头没有当前 cue 就留空，不用剧情最后一句填充导致剧透。
- 玩家主动跳过画面、片播完、影片缺失或解码失败：显示**当前剧情节点**已验证的最后一行，保留阅读无视频场景的可访问性。
- 关闭字幕设置时始终为空。

## 本期交付代码清单

| 路径 | 真实作用 |
| --- | --- |
| `Assets/Scripts/Core/EchoFirstActFoleyPlan.cs` | 第一幕原生音效的不可变、可重复验证时间表 |
| `Assets/Scripts/Runtime/EchoFirstActFoley.cs` | 本地合成波形、播放、暂停/继续/静音/离场 |
| `Assets/Scripts/Core/EchoSubtitlePresentation.cs` | 不泄漏上一个镜头的字幕状态机 |
| `Assets/Scripts/Runtime/EchoVideoDirector.cs` | 有条件的正式片判断、安全静态替代、失败保留 ID |
| `Assets/Scripts/Runtime/EchoGame.cs` | 影片、字幕、原声音效统一接入游戏操作、设置和选择 |
| `Tools/CoreHost/Program.cs` | 在 .NET 8 实际编译共用音效时间表与字幕策略并跑功能测试 |
| `Assets/Tests/EditMode/EchoStage31CinematicPolicyTests.cs` | Unity Editor 音效和字幕测试源码 |
| `Assets/Tests/PlayMode/EchoStage31CinematicFallbackTests.cs` | Unity 原生播放器与音效暂停/重播测试源码 |
| `Tests/test_stage31_first_act_cinematic.py` | 本机无 Unity 的核心/运行时集成契约校验 |

## 严格测试边界与发行缺口

真正自动执行：GitHub Actions 编译和运行游戏共用 C# 叙事核心，核对 10 节点音效、三次敲门顺序及字幕加载/缺片/暂停策略，以及已有四结局、存档、调查与所有 Python 回归。

**尚未执行**：Unity Editor 音频设备 PlayMode、真实 MP4 与原声音效混音、不同声卡、Windows EXE 和全屏/21:9 实机验收。音效是游戏自身生成的**预演音效**，不是演员配音和完成母带。**正式必须制作的 27 个影像镜头仍未交付**，这一阶段没有冒充已替代。

## 进一步要做

真正 Unity 导入与 Windows Alpha 打包后，第一幕按实际视频帧和导演节奏校准这些音效的 `AtSeconds`，在实际 PC 声卡和音箱上做响度/音频重复检查，然后开始制作第一幕正式视频及配音。
