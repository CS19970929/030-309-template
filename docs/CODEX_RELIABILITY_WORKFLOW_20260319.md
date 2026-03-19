# Codex 接管可靠性工作流

## 目标

这份文档面向“让 Codex 稳定接管当前项目”的实际工作流，而不是泛泛讨论 AI。

目标是让 Codex 具备以下能力：

- 调整构建参数
- 运行标准回归
- 读取结构化摘要
- 在异常时快速定位问题范围

## 当前关键入口

### 构建参数

当前固件构建已经支持直接传入：

- `APP_HEAP_SIZE`
- `APP_STACK_SIZE`

例如：

```bash
task build APP_HEAP_SIZE=0x400 APP_STACK_SIZE=0xC00
```

这比手工进 `Keil` 面板修改更适合：

- Win/Mac 统一
- 命令可复现
- Codex 可直接执行
- 参数变更可进提交记录

### 构建摘要

当前推荐入口：

```bash
task build-summary
```

该命令会生成：

- [build-summary.log](/Users/cs/Downloads/work/todo/030-309-template/artifacts/build-summary.log)
- [build-summary.json](/Users/cs/Downloads/work/todo/030-309-template/artifacts/build-summary.json)
- [build-summary.md](/Users/cs/Downloads/work/todo/030-309-template/artifacts/build-summary.md)

其中会包含：

- configure/build 返回码
- warning/error 数量
- warning/error 分类
- 推荐动作
- 关联的 `map` 摘要

### 保护回归

当前推荐入口：

```bash
task sim-protection-suite-summary
```

该命令会顺序执行：

- `sim-protection-ovp-uvp`
- `sim-protection-ocp`
- `sim-protection-otp-utp`
- `sim-protection-pack`

然后生成：

- [protection-suite-summary.json](/Users/cs/Downloads/work/todo/030-309-template/artifacts/host-sim/protection-suite-summary.json)
- [protection-suite-summary.md](/Users/cs/Downloads/work/todo/030-309-template/artifacts/host-sim/protection-suite-summary.md)

### SOC 回归

当前推荐入口：

```bash
task sim-soc-suite-summary
```

该命令会生成：

- [soc-suite-summary.json](/Users/cs/Downloads/work/todo/030-309-template/artifacts/host-sim/soc-suite-summary.json)
- [soc-suite-summary.md](/Users/cs/Downloads/work/todo/030-309-template/artifacts/host-sim/soc-suite-summary.md)

其中会包含：

- 各场景的 `SOC` 趋势范围
- `OCV` 修正步数
- 最大绝对误差
- 边界钳位次数

### 低功耗回归

当前推荐入口：

```bash
task sim-low-power-suite-summary
```

该命令会生成：

- [low-power-suite-summary.json](/Users/cs/Downloads/work/todo/030-309-template/artifacts/host-sim/low-power-suite-summary.json)
- [low-power-suite-summary.md](/Users/cs/Downloads/work/todo/030-309-template/artifacts/host-sim/low-power-suite-summary.md)

### 接管总览

当前推荐入口：

```bash
task codex-overview
```

该命令会生成：

- [codex-overview.json](/Users/cs/Downloads/work/todo/030-309-template/artifacts/codex-overview.json)
- [codex-overview.md](/Users/cs/Downloads/work/todo/030-309-template/artifacts/codex-overview.md)

### 内存与链接摘要

当前推荐入口：

```bash
task map
```

该命令会生成：

- [map-summary.json](/Users/cs/Downloads/work/todo/030-309-template/artifacts/map-summary.json)
- [map-summary.md](/Users/cs/Downloads/work/todo/030-309-template/artifacts/map-summary.md)

其中会包含：

- RAM 占用
- FLASH 占用
- `heap/stack` 预留
- Top RAM 对象
- Top FLASH 对象
- 自动风险分级

## 为什么这套工作流适合 Codex

核心原因不是“用了 CMake”，而是满足了接管的 4 个条件：

1. 参数是文本化的  
   `heap/stack` 不再藏在 IDE 面板里。

2. 动作是命令化的  
   Codex 可以稳定调用 `task`，不依赖人工点击。

3. 结果是结构化的  
   `jsonl + json + markdown` 让 Codex 能先看摘要再深入。

4. 回归是分层的  
   出问题时可以先判断是 `OVP/UVP`、`OCP`、`OTP/UTP` 还是 `pack/soc/vdelta`。

## 当前建议使用顺序

日常修改后，优先这样执行：

1. `task build-summary`
2. `task sim-protection-suite-summary`
3. `task sim-soc-suite-summary`
4. `task sim-low-power-suite-summary`
5. `task codex-overview`
4. 必要时再看单个 `artifacts/host-sim/*.jsonl`

如果是内存相关调整：

1. `task build-summary APP_HEAP_SIZE=... APP_STACK_SIZE=...`
2. 必要时再单独运行 `task map`
3. 再决定是否进入板级验证

## 下一步建议

要继续提升 Codex 接管能力，优先做这两件事：

1. 为 `SOC` 增加同样的 `suite + summary` 入口
2. 为 `SOC` 增加长时间漂移与断电恢复场景
3. 为 `map/RAM/Flash` 增加多版本趋势对比
