# Build 摘要自动化指南

## 目标

这份文档说明如何把固件构建结果变成 Codex 可直接分析的摘要，而不是每次都人工翻长日志。

当前目标包括：

- 自动执行 `configure + build`
- 自动保存完整日志
- 自动识别 warning/error
- 自动归类已知噪声和真实风险
- 自动关联当前 `map` 摘要

## 当前入口

```bash
task build-summary
```

默认产物：

- [build-summary.log](/Users/cs/Downloads/work/todo/030-309-template/artifacts/build-summary.log)
- [build-summary.json](/Users/cs/Downloads/work/todo/030-309-template/artifacts/build-summary.json)
- [build-summary.md](/Users/cs/Downloads/work/todo/030-309-template/artifacts/build-summary.md)

## 当前能识别的分类

### warning

- `newlib_nosys_stub`
- `linker_rwx_segment`
- `unused_variable`
- `implicit_declaration`
- `generic_warning`

### error

- `compiler_or_cmake_error`
- `build_failed`

## 当前推荐工作流

对于固件相关改动，建议顺序：

1. `task build-summary`
2. `task sim-protection-suite-summary`
3. 必要时再看单个原始日志和 `jsonl`

如果改动涉及内存：

1. `task build-summary APP_HEAP_SIZE=... APP_STACK_SIZE=...`
2. 重点看 `build-summary.json` 中的 `map_summary`
3. 再决定是否继续板级验证

## 为什么这条链路适合 Codex 接管

因为它把原本分散的三类输入收敛到一起了：

- 构建命令
- warning/error 分类
- map 关联摘要

这样 Codex 不需要每次先读完整构建日志再推断上下文，而是可以直接看：

- 这次构建有没有失败
- warning 是预期噪声还是新风险
- RAM/FLASH 是否已经进入危险区
- 后续更该看 `map`、源码，还是回到第一条报错

## 当前边界

这条链路现在还是第一版，仍有边界：

- 还没有做多次构建趋势对比
- 还没有对每类 warning 建立项目级白名单
- 还没有自动关联 PR 和 commit 维度的变化量

所以当前更适合作为：

- 本地开发第一层诊断
- Codex 审查前置输入
- PR 自动审查的后续增强基础
