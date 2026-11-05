# 《03:17 · 回声》Stage 21｜键盘/手柄操作、模态返回和可读字幕

日期：2026-11-05。真实项目仍为 Unity 6.3 LTS / C# / Windows，尚无可启动 EXE。

## 本阶段修复的玩家交互

1. **防止一次确认推进两段剧情**：之前 `EchoGame.Update()` 直接对 Space / Return 调用 `Choose()`，同时 Unity `StandaloneInputModule` 的 Submit 也可能触发已选中的按钮。现在由 **uGUI Button 唯一处理 Enter / Space / 手柄确认**；数字键 1–9 仍直达对应合法分支。
2. **Esc / 手柄取消统一回退**：对模态按钮添加 `ICancelHandler`；Esc 与 Unity Cancel 事件通过同一 `HandleBack` 路径，使用帧级防重。暂停菜单→档案/回顾/设置→取消先返回暂停菜单；从直接打开的档案→取消返回剧情；读取/覆盖/删除存档和时间线回溯的确认框→取消退回原页面，不触发危险动作。
3. **焦点不能透过弹窗**：显示模态窗时，主菜单与剧情的 `CanvasGroup` 同时禁止交互及鼠标射线；关闭后恢复。进入弹窗前保留的有效按钮焦点可恢复，已删除或已禁用按钮则安全回退到其他可用选项。
4. **焦点可见、长表可操作**：按钮新增金色轮廓焦点状态，仍由原 `EchoScrollSelection` 确保当前选项可见；重复调整设置后尽量留在同一设置项，不再每次跳到第一项。
5. **字幕可读性与偏好**：新增衬底（不拦截点击）和 100% / 125% / 150% 字号设置，随字幕开关显示/隐藏；字号写入 `player-options.json`，与自动/手动剧情存档隔离；旧版本未存字号时默认 100%，非法数字与极端值被归一化。

## 实际代码改动

- `Assets/Scripts/Core/EchoModalFlow.cs` — Unity 无关、可执行测试的模态页/返回逻辑。
- `Assets/Scripts/Runtime/EchoGame.cs` — 输入去重、统一退出、确认返回、字幕设置接线。
- `Assets/Scripts/UI/EchoGameUI.cs` — 焦点和模态输入隔离、控制器 Cancel、字幕背景及字号布局。
- `Assets/Scripts/Core/EchoPreferences.cs` — 兼容旧数据的持久化字幕字号。
- `Tools/CoreHost/Program.cs` — .NET 8 真实编译/运行模态返回链、设置迁移、非法字号钳制。
- `Assets/Tests/EditMode/EchoModalFlowTests.cs` / `EchoPreferencesTests.cs` — 编辑器逻辑测试源码。
- `Assets/Tests/PlayMode/EchoStage21InteractionTests.cs` — Unity 真实焦点、控制器 Cancel、字幕控制交互测试源码。
- `Tests/test_stage21_interaction.py` — 无 Unity 源码接线检查。

## 玩家操作

| 场景 | 键盘 | 手柄（依赖 Unity Legacy Input 的 Submit / Cancel 映射） |
| --- | --- | --- |
| 选择剧情 | ↑/↓ 选择，Enter/Space 确认；1–9 直选合法剧情选项 | 方向键/摇杆导航，确认键 |
| 打开/返回菜单 | Esc 暂停 / 返回上一层 | 已聚焦弹窗按钮时，取消键返回 |
| 回顾 | L | 暂停菜单→剧情回顾 |
| 影片 | R 重播、K 跳过画面（不跳过剧情） | 设置→重播/跳过 |
| 调整字幕 | 设置→字号，三档 | 暂停→设置→字号，保持选中 |

## 验证边界

- **不需要 Unity 许可证**：GitHub `Compile and execute real C# narrative core (without Unity)` 真正编译 `EchoModalFlow` 与 `EchoPreferences` 等实际 C# 类并执行断言；`Unity migration data parity` 运行 Python 回归。
- **当前不能声称通过**：Unity Editor/PlayMode、控制器实体设备的轴/按钮映射、字体中日韩字形覆盖、实际画面布局、影片音画同步、Windows x64 EXE 的运行。
- **发行仍受限**：27 个必需正式镜头未审批，当前 8 个预演影片只适用于内部开发/Alpha。

## 下一次原生验收重点

在 Unity 6.3 LTS 打开项目后，执行真实 EditMode/PlayMode，尤其验证：快速连续 Submit 不会跨两个节点、长列表滚动和焦点可见性、Esc+Cancel 同一帧只退一层、焦点不会移动到弹窗后、中文字体及三档字幕是否覆盖影片和选项区域，再生成 Windows Player 并进行完整通关。
