# 验证记录 — 2026-11-04

## 已执行

- `node Stage8/tests/story.test.cjs` — PASS，55 场景、69,497 唯一状态、75,976 合法转移、4 结局、5 章审核。
- `node Stage8/tests/stage6-deductions.test.cjs` — PASS，5 道推理、正确/错误推断、反伪造与回溯。
- `node Stage8/tests/stage5-save.test.cjs` — PASS，确定性 JSON 保存/导入、版本保护。
- `node Stage8/tests/stage8-paths.cjs` — PASS，四结局最短路线分别 31、31、31、34 步。
- `node Stage8/tests/stage8-distribution.test.cjs` — PASS，旧桌面发行工程基线（**不证明 Unity 构建**）。
- `python Tests/test_export.py` — PASS，迁移数据独立图遍历 55 节点 / 69,497 状态 / 四结局；8 MP4 存在。
- `python Tests/test_csharp_structure.py` — PASS，8 个 C# 文件的括号/注释语法表面检查。

## 不能宣称通过的项目

Unity 编译器、EditMode/PlayMode 测试、Unity 视频解码、Windows EXE 构建及上架审核 **尚未执行**。没有 Unity Editor 的环境不能以 Python/Node 静态分析替代。

## 原始测试路径

`/mnt/data/0317-echo-stage8-release/tests/`（原始 Stage8 工程存于本次会话；迁移包内只保留必要的原始 JS 数据和导出脚本）。
