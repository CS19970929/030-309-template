# AFE 通信异常复盘与预防规则

## 现象

同一份项目代码出现了下面的差异：

- 使用 `Keil` 编译并下载后，`AFE` 通信正常
- 使用早期的 `task build` 生成 `GCC` 固件并下载后，`AFE` 通信异常

这类问题最容易误判成：

- AFE 驱动代码被改坏了
- 板子硬件异常
- 调试时注释的那段初始化逻辑影响了 AFE

但这次的真实问题不在单一业务代码，而在 **构建链行为不一致**。

## 根因

### 1. GCC 默认构建和 Keil 并不等价

早期的 `task build` 默认使用的是一条更激进的 `GCC/CMake` 构建链，和 `Keil` 的板级行为并不一致，主要差异有：

- 启用了 `LTO`
- 默认编入 `CMake` 独有的运行时监控模块
- 使用更偏尺寸优化的编译方式

这些差异会放大如下风险：

- 软件 `I2C` 时序偏移
- 初始化顺序边界变化
- 额外模块引入的时序/资源扰动
- 编译器优化导致的行为差异

### 2. AFE 通信链路本身对时序敏感

本项目的 `AFE` 通信依赖软件 `I2C`，关键路径在：

- [I2C_AFE1.c](E:/TODO/030%20+%20309/Code/Source/I2C_AFE1.c)
- [System_Init.c](E:/TODO/030%20+%20309/Code/Source/System_Init.c)
- [bsp_timer.c](E:/TODO/030%20+%20309/Code/Source/BSP/bsp_timer.c)

其中 `Delay4us()`、`__delay_us()`、`bsp_DelayUS()` 都会直接影响 GPIO 拉高拉低的节拍。

这意味着：

- `Keil` 正常，不代表 `GCC -Os/-flto` 一定正常
- `GCC` 编译通过，不代表板级行为就已经等价

### 3. CMake 独有模块不应该默认混入板级验证固件

运行时监控模块本身是为了日志、快照、长期监控准备的：

- [app_runtime_monitor.c](E:/TODO/030%20+%20309/Code/Source/BSP/app_runtime_monitor.c)
- [app_state_snapshot.c](E:/TODO/030%20+%20309/Code/Source/BSP/app_state_snapshot.c)

它适合：

- PC/MCU 联合调试
- 快照记录
- 运行时分析

但不适合在“还没确认 GCC 与 Keil 板级一致性之前”，默认作为上板固件的一部分。

## 修复动作

### 1. 重新定义 `task build`

现在的 `task build` 已经改成 **板级安全构建**，目标是尽量贴近 `Keil`：

- 关闭 `LTO`
- 默认不编入运行时监控模块
- 优化级别降为 `-O1`
- 增加 `-fno-strict-aliasing`

对应文件：

- [Taskfile.yml](E:/TODO/030%20+%20309/Taskfile.yml)
- [CMakePresets.json](E:/TODO/030%20+%20309/CMakePresets.json)
- [firmware/CMakeLists.txt](E:/TODO/030%20+%20309/firmware/CMakeLists.txt)

### 2. 保留独立的优化构建

现在另外保留：

```powershell
task build-optimized
```

它用于：

- 运行时监控
- 快照采集
- 日志/分析实验

但它 **不是默认板级验证固件**。

## 现在的构建规则

### 场景 1：验证板子功能、AFE、休眠、MOS、保护链

优先使用：

```powershell
task build
task flash-stlink
```

如果要和原始工具链直接对照，用：

```powershell
C:\Keil_v5\UV4\UV4.exe -b CommomSH367309_16series_030C8T6_C.uvprojx -j0
```

### 场景 2：做日志、快照、运行时监控实验

使用：

```powershell
task build-optimized
task flash-stlink
```

### 场景 3：做 PC 长期仿真

使用：

```powershell
task sim-replay
task sim-replay-long
```

## 以后禁止再犯的规则

### 规则 1

**板级验证固件** 和 **监控/实验固件** 必须分开，不能共用一个默认入口。

### 规则 2

任何会改变时序、链接裁剪、初始化路径的能力，默认都不能直接并入 `task build`。

包括但不限于：

- `LTO`
- 新增运行时监控模块
- 新增大块日志逻辑
- 更激进的优化等级

### 规则 3

以后只要出现“Keil 正常，GCC 不正常”，优先先查：

1. 两边是否真的编了同样的源文件
2. 两边宏定义是否一致
3. GCC 是否启用了 `LTO`
4. GCC 优化级别是否过高
5. 是否引入了 `Keil` 没有的初始化/监控模块

而不是先怀疑硬件坏了。

### 规则 4

任何新的 `CMake` 增强功能，如果要默认上板，必须先经过这两个验证：

1. `Keil` 构建正常
2. `task build` 上板后，`AFE`/休眠/通讯主链全部正常

只有两个都过，才能把这项增强保留在默认板级构建里。

## 当前结论

这次问题已经收束为：

- 不是单纯 AFE 代码坏了
- 不是单纯硬件坏了
- 主要是早期 `GCC` 默认构建和 `Keil` 不等价，导致板级行为偏离

修复后，默认 `task build` 已恢复为更接近 `Keil` 的安全模式。

后续如果要继续追更深层的 GCC/Keil 行为差异，下一优先级是：

1. 软件 `I2C` 时序
2. `SH367309` bitfield/union 在不同编译器下的布局差异

但这两项属于“进一步消除潜在风险”，不再是这次主问题的第一根因。
