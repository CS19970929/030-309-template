# Codex 接管总览指南

## 目标

这份文档说明如何把当前仓库已经存在的 5 条自动分析主线，汇总成一份统一总览：

- `build-summary`
- `map`
- `protection-suite-summary`
- `soc-suite-summary`
- `low-power-suite-summary`

目标是让 Codex 在接管问题分析时，先看一份高密度摘要，再决定应该深入哪条链路。

## 当前入口

### 只聚合现有产物

```bash
task codex-overview
```

适合：

- 你刚刚已经分别跑过各条摘要
- 想快速看一眼当前全局状态

### 先刷新再聚合

```bash
task codex-overview-refresh
```

适合：

- 刚改完核心逻辑
- 想让 Codex 重新拿一套完整、最新的输入做分析

## 当前产物

- [codex-overview.json](/Users/cs/Downloads/work/todo/030-309-template/artifacts/codex-overview.json)
- [codex-overview.md](/Users/cs/Downloads/work/todo/030-309-template/artifacts/codex-overview.md)

## 当前输出内容

总览会输出：

- 总状态
- 构建状态
- Map 状态
- 保护回归状态
- `SOC` 回归状态
- 低功耗回归状态
- 关键计数
- 推荐动作
- 各原始摘要入口路径

其中当前关键指标包括：

- build warning/error 数
- RAM/FLASH 占用
- 保护三级故障步数
- `SOC` 最大绝对误差
- `SOC` 循环计数步数
- 低功耗进入休眠步数
- 低功耗深度休眠步数

## 推荐使用方式

你平时最实用的顺序是：

1. 改代码
2. 跑：

```bash
task codex-overview-refresh
```

3. 先看：

- [codex-overview.md](/Users/cs/Downloads/work/todo/030-309-template/artifacts/codex-overview.md)

4. 如果总览提示问题，再让 Codex 深入对应专项摘要

## 当前边界

这份总览是“聚合器”，不是新的分析模型。

它当前不会：

- 替代各专项摘要
- 自动给出源码级修复
- 自动理解真板噪声和硬件失效

它当前负责的是：

- 先做全局分诊
- 把注意力引到最可能有问题的链路
- 降低 Codex 每次接管时的上下文成本
