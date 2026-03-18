# 构建模式与防呆规则

## 默认原则

以后仓库里只保留两个明确的 GCC 固件用途：

### 板级安全构建

用途：
- 上板验证
- AFE 通信
- 休眠/唤醒
- MOS/保护链路
- 和 `Keil` 对照

命令：

```powershell
task build
task flash-stlink
```

VS Code：
- `mcu: build gcc release`
- `Cortex-Debug: STM32F030 (ST-Link/OpenOCD)`

### 优化/监控构建

用途：
- 运行时快照
- 日志增强
- 长期监控实验
- 体积/优化验证

命令：

```powershell
task build-optimized
task flash-stlink
```

VS Code：
- `mcu: build gcc optimized`
- `Cortex-Debug: STM32F030 (ST-Link/OpenOCD, Optimized)`

## 防呆规则

### 规则 1

默认上板永远先用 `task build`，不要先用 `task build-optimized`。

### 规则 2

只要改了 `CMake` 源文件列表、宏定义或优化级别，先运行：

```powershell
task build-check
```

如需看优化构建差异，再运行：

```powershell
task build-check-optimized
```

### 规则 3

如果出现“Keil 正常、GCC 异常”，先打开下面两个 JSON：

- [build-alignment-board-safe.json](E:/TODO/030%20+%20309/artifacts/build-alignment-board-safe.json)
- [build-alignment-optimized.json](E:/TODO/030%20+%20309/artifacts/build-alignment-optimized.json)

优先看：
- 是否多编了源文件
- 是否多开了宏

### 规则 4

新增运行时监控、日志、快照、LTO 之类能力时，不允许直接塞进默认板级构建。

必须先走：

1. `task build-optimized`
2. 板上验证稳定
3. 再决定是否进入默认板级构建

## Codex 接管建议

以后让 Codex 接手时，优先按下面命令序列：

```powershell
task build-check
task build
task flash-stlink
```

只有当任务目标明确是“监控增强/快照分析/尺寸优化”时，才切到：

```powershell
task build-check-optimized
task build-optimized
```
