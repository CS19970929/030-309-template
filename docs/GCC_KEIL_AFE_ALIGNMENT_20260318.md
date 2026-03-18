# GCC 与 Keil 板级行为对齐说明

## 结论

当前仓库里，`Keil` 与 `GCC/CMake` 不是天然等价的两条构建链。

在本项目里，`AFE` 通信是否稳定，优先受下面三项影响：

1. `GCC` 是否启用了 `LTO`
2. `GCC` 是否额外编入 `CMake` 独有的运行时监控模块
3. `GCC` 优化级别是否过高，导致软件 `I2C` 时序与 `Keil` 偏离

因此，默认 `task build` 已调整为“板级安全构建”：

- 关闭 `LTO`
- 默认不编入 `app_runtime_monitor.c` 与 `app_state_snapshot.c`
- 默认优化级别降为 `-O1`
- 增加 `-fno-strict-aliasing`

这样做的目标不是追求最小体积，而是先尽量贴近 `Keil` 的板级行为，优先保证上板一致性。

## 现有构建模式

### 1. Keil 构建

用途：
- 现阶段最接近你原始量产/调试行为
- 适合直接验证 `AFE`、休眠、保护链路

命令：

```powershell
C:\Keil_v5\UV4\UV4.exe -b CommomSH367309_16series_030C8T6_C.uvprojx -j0
```

### 2. GCC 板级安全构建

用途：
- 作为 `task build` 默认入口
- 优先对齐 `Keil`
- 适合上板验证 `AFE` 与基础运行

命令：

```powershell
task build
```

当前配置：
- `APP_RUNTIME_MONITOR=OFF`
- `APP_ENABLE_LTO=OFF`
- `APP_OPT_LEVEL=1`

### 3. GCC 优化构建

用途：
- 用于运行时监控、快照、尺寸优化
- 适合 PC/MCU 监控联动验证
- 不建议在未确认板级一致性前，作为默认上板固件

命令：

```powershell
task build-optimized
```

当前配置：
- `APP_RUNTIME_MONITOR=ON`
- `APP_ENABLE_LTO=ON`
- `APP_OPT_LEVEL=s`

## 为什么这次先改构建链，而不是先大改 AFE 代码

当前最重要的是先确认问题边界：

- 如果 `Keil` 正常、`GCC 板级安全构建` 也恢复正常，那么根因更偏向 `LTO/优化/CMake 独有模块`
- 如果 `Keil` 正常、`GCC 板级安全构建` 仍异常，那么下一步就要重点检查：
  - 软件 `I2C` 延时
  - `SH367309` 相关 bitfield/union 在 `GCC` 下的布局差异

这能避免过早重构 AFE 驱动。

## 后续定位顺序

如果 `task build` 上板后仍然 `AFE` 不通，按下面顺序继续：

1. 在板上断点 `InitAFE1`、`AFE_IsReady`、`MTPRead`
2. 检查软件 `I2C` 的 `Delay4us()` 实际效果
3. 去掉 `MTP_CONF/BSTATUS` 的 bitfield 读写，改成显式掩码
4. 再比较 `Keil` 与 `GCC` 的寄存器写入值

## 日常建议

你现在可以这样使用：

```powershell
task build
task flash-stlink
```

如果只是做日志/监控或实验性验证：

```powershell
task build-optimized
task flash-stlink
```

如果要和原始行为直接对照：

```powershell
C:\Keil_v5\UV4\UV4.exe -b CommomSH367309_16series_030C8T6_C.uvprojx -j0
```
