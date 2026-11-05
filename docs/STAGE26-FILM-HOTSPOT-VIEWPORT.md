# Stage 26｜镜头与调查热点同坐标系、键盘/手柄导航修复

日期：2026-11-05。平台仍是 Unity 6.3 LTS / Windows x64。

## 本阶段解决的真实问题

原有 Stage 24 的 `EchoInvestigationOverlay` 把热区放在屏幕左侧固定尺寸的 UI 容器，但实际 `EchoVideoDirector` 的视频和静态场景图渲染在全屏 `VideoLayer`。因两层没有共享坐标，16:9 之外的分辨率和窗口模式下，热点很可能不在画面物体的对应位置。

这轮不再用假的影片缩略图替代实际视频：**进入调查时将正在播放的同一个 VideoLayer 实例临时挂到调查左侧的电影视窗里**，热点容器再挂到这个 VideoLayer 上。画面和热点成为父子关系，位置和缩放一致。影片不需要重新初始化、重新解码或重播。

## 具体玩家体验

- 暂停影片进入「现场调查 H」后：电影画面等比嵌入左侧视窗，比例固定为当前 1280×720 渲染纹理的 **16:9**；非 16:9 窗口自动留黑边而不是拉伸演员和物品。
- 每个热点沿用作者在 `hotspots.json` 中填写的影片规范化坐标（原点左下角），直接绑定在**等比后的影片 RectTransform**，而不是视窗外层或全屏根层。原有 23 个热点不变。
- UI 的右半部分仍显示原始已入仓库的 `inspect-result.png`、提示条及线索图标，电影部分不会被信息卡覆盖。
- 鼠标悬停切换聚焦美术；键盘方向键、手柄方向键/摇杆使用 `EchoHotspotNavigation` 根据位置寻求最近邻，并写入 Unity `Selectable.Navigation.Explicit`。同方向多个目标时有稳定 ID 排序，不会因重绘出现随机焦点跳跃。
- ESC/B 退出调查时，热区容器先移出电影层，**原始 VideoLayer** 恢复到原来的 UI 父节点、索引、锚点、偏移量及纵横比设置；停止调查后剧情继续从暂停时刻播放，绝不多次实例化视频解码器。
- 退出按钮也有 Unity Cancel 处理；和 Stage 24 的帧内防双退逻辑共同工作。

## 代码变化

| 路径 | 作用 |
| --- | --- |
| `Assets/Scripts/UI/EchoInvestigationOverlay.cs` | `MountFilm` / `UnmountFilm`，16:9 `AspectRatioFitter`，共享 RectTransform，显式方向焦点 |
| `Assets/Scripts/Core/EchoHotspotNavigation.cs` | 无 Unity 依赖的规范化位置方向选择算法 |
| `Assets/Scripts/Runtime/EchoGame.cs` | 在影片暂停后把当前 `ui.VideoLayer` 挂入调查界面，退出恢复 |
| `Tools/CoreHost/Echo0317.CoreHost.csproj` 和 `Program.cs` | 真实编译运行热点方向判定、异常输入、平局规则 |
| `Assets/Tests/EditMode/EchoStage26HotspotNavigationTests.cs` | Unity Editor 下的方向逻辑测试源码 |
| `Assets/Tests/PlayMode/EchoStage26FilmViewportTests.cs` | UI 真实挂载/卸载、纵横比、手柄导航和 Cancel 测试源码 |
| `Tests/test_stage26_film_hotspot_viewport.py` | 无 Unity 的源码接线与回归检查 |

## 保留不变

- GitHub main 已有 18 张真实透明 PNG，加载目录 `Assets/StreamingAssets/Investigation/`；**无需重新下载美术**。
- 55 个节点、69,497 个可达剧情状态、四结局、五推理、Stage25 调查手记与格式 6 存档结构不变。
- 调查行为不会伪造新证据，不会绕过合法剧情分支，也不会强制阻断以前的可达结局。
- 没有将 Unity 运行时交互降格成 HTML 或 Electron。

## 必须实测的部分

1. 用 Unity 6.3 LTS Editor 分别在 16:9、21:9、窗口化画面里，检查手办/手机/终端热点和实际影片的位置以及小窗口右侧的说明文字不遮挡；如果导演调整分镜构图，应重新校准 `hotspots.json` 的规范化热区坐标。
2. 通过实体手柄测试十字键和摇杆 Up/Down/Left/Right、确认与 B 返回；确认电影在进入前、调查中、退出后无重播、重复音频和首帧闪烁。
3. 使用现有 `.github/workflows/hosted-unity-windows-alpha.yml`，在配置有效 Unity 账号与许可证后运行真实 EditMode/PlayMode 和 Windows EXE 打包；当前 GitHub .NET CoreHost / Python 检查**不是 Unity Editor/Player 执行结果**。

**剩余发行阻塞**：Unity Editor/Windows Player 本机验收，以及 27 个必需正式电影镜头的制作与审批。
