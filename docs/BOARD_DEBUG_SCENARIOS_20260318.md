# 板级单步调试场景与方法

## 本次已验证的链路

当前 `ST-Link + OpenOCD + arm-none-eabi-gdb + Cortex-Debug` 的底层链路已经验证到以下程度：

- `task build` 可生成带符号的 GCC `release` 固件
- `task flash-stlink` 可成功烧录并校验
- `OpenOCD` 可识别 `STLINK V2J36S7`
- 目标芯片可识别为 `STM32F0 / Cortex-M0`
- 可在 `main` 处下硬件断点
- 可执行 `continue`
- 可执行 `next`
- 可执行 `step`

当前用于调试的固件：

- [artifacts/cmake/firmware-release/firmware/CommomSH367309_16series_030C8T6_C.elf](E:/TODO/030%20+%20309/artifacts/cmake/firmware-release/firmware/CommomSH367309_16series_030C8T6_C.elf)

## 为什么不是 Debug 构建

当前 `firmware-debug` 会超出应用区 Flash 大小，实测溢出约 `24168 bytes`，因此暂时不能作为板级调试入口。

所以当前板级调试使用的是：

- `Release + 调试符号`

这意味着：

- 可以下断点、单步、查看源码位置
- 但有些局部变量、调用栈和步进行为会受优化影响

## 常用调试动作

### 1. 启动后停在 main

适用场景：

- 上电流程是否正常
- 初始化前后寄存器/状态是否合理

做法：

1. `Run and Debug`
2. 选择 `Cortex-Debug: STM32F030 (ST-Link/OpenOCD)`
3. 调试器会尝试停在 `main`

### 2. 单步跳过当前语句

使用：

- `Step Over`

适用场景：

- 你确认当前函数基本可信
- 只想观察这一行执行后的结果

例如在 `main` 里跳过：

- `SystemInit();`

### 3. 进入函数内部

使用：

- `Step Into`

适用场景：

- 想进入 `InitDevice()`、`SystemInit()` 等函数内部看具体执行路径

### 4. 从当前子函数返回上层

使用：

- `Step Out`

适用场景：

- 你已经在某个被调函数内部
- 不想一行行走完，直接回到调用者

## 你遇到的报错是什么意思

报错原文：

```text
Could not step out: "finish" not meaningful in the outermost frame.
```

这不是调试器坏了，而是你当前就在最外层栈帧。

典型场景：

- 当前停在 `main`
- 或当前停在启动入口最外层

这时没有“更上一层调用者”可返回，所以 `Step Out` 没有意义。

### 正确做法

如果当前停在 `main`：

- 想执行下一行，用 `Step Over`
- 想进入被调函数，用 `Step Into`
- 想直接跑到下一个断点，用 `Continue`

只有当你已经进入 `InitDevice()`、`SystemInit()`、`Fault_Deal()` 之类的函数内部时，`Step Out` 才有意义。

## 推荐的调试场景

### 场景 1：验证启动流程

入口：

- `main`

操作：

1. 在 `main` 断住
2. `Step Over` 看 `SystemInit()`
3. `Step Into` 进入 `InitDevice()`

### 场景 2：验证某个业务函数是否被调用

做法：

1. 在目标函数首行下断点
2. `Continue`
3. 观察是否命中

适合：

- 通讯处理
- 故障处理
- 温控流程
- 休眠唤醒流程

### 场景 3：怀疑某个函数内部异常

做法：

1. `Step Into` 进入目标函数
2. 在函数内部使用 `Step Over`
3. 需要回到上层时再用 `Step Out`

### 场景 4：只想快速验证“程序有没有活着”

做法：

1. 在 `main` 或主循环附近下断点
2. `Continue`
3. 看能否稳定命中断点

## 当前已知限制

### 1. `Step Out` 在最外层会报错

这是正常行为，不是故障。

### 2. 当前不是纯 Debug 构建

由于 Flash 空间不足，当前是 `Release + 符号`。

所以可能出现：

- 单步位置略有跳动
- 某些变量被优化
- 某些小函数不容易完整按源码一行行走

### 3. 调试体验优先级

当前最适合：

- 断点验证流程
- 主路径单步
- 关键函数进入/返回

不适合期待：

- 完全未优化 Debug 体验

## 推荐日常用法

1. 先编译：

```powershell
bbuild
```

2. 再烧录：

```powershell
bflash
```

3. 然后在 VS Code 中启动：

- `Run and Debug -> Cortex-Debug: STM32F030 (ST-Link/OpenOCD)`

4. 停在 `main` 后：

- 用 `Step Over` 和 `Step Into`
- 不要在 `main` 直接点 `Step Out`
