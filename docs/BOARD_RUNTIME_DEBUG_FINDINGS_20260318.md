# 板级运行定位记录（2026-03-18）

## 现象

- 用户反馈当前板子 `AFE` 通信不上，并且“程序像是在一直重启”。
- 已知正常版本是提交 `d4f60c332f3f5c2c9d67ba4b397c32342927b62a`。
- 用户补充说明：正常版本在**未接开关**时，本身就会进入休眠，因此“看起来重启”不能直接等价为异常复位。

## 本轮确认到的事实

1. 当前仓库里，`AFE` 初始化参数下发流程必须保留。
   关键代码在 [`EEPROM.c`](E:/TODO/030%20+%20309/Code/Source/EEPROM.c) `InitData_E2prom()`：
   - `initAFE1_IIC();`
   - `AFE_IsReady();`
   - `SH367309_UpdataAfeConfig();`
   - `DataLoad_CurrentCali_startup();`

2. 当前调试现场不适合让板子继续自动休眠。
   原因：
   - 当前没有接开关。
   - 原始代码会在启动恢复和定时任务中进入睡眠/唤醒链路。
   - 这会干扰联机调试、单步和运行状态判断。

3. `OpenOCD` 在目标状态异常时不一定总能重新接回。
   本轮使用 [`STM32_Programmer_CLI.exe`](C:/Program%20Files/STMicroelectronics/STM32Cube/STM32CubeProgrammer/bin/STM32_Programmer_CLI.exe) 的 `Under Reset` 模式重新烧录，可以稳定抢回目标连接。

## 本轮做的代码调整

### 1. 调试态关闭自动休眠

在 [`conf.h`](E:/TODO/030%20+%20309/Code/Source/conf/conf.h) 新增：

```c
#define APP_DEBUG_DISABLE_AUTO_SLEEP
```

用途：
- 板上联机调试时，避免无开关场景反复进入休眠/唤醒流程。

### 2. 启动时跳过睡眠恢复链

在 [`SleepDeal.c`](E:/TODO/030%20+%20309/Code/Source/SleepDeal.c) 的 `IsSleepStartUp()` 中增加调试旁路：

- 调试态下先 `BootFlag_Clear();`
- 然后直接 `return;`

用途：
- 避免上次残留的休眠标志把本次调试会话重新带回 `Sys_StopMode()` / `MCU_RESET()` 链路。

### 3. 调试态屏蔽 Flash 更新后的自动复位

在 [`Flash.c`](E:/TODO/030%20+%20309/Code/Source/Flash.c) 的 `App_FlashUpdateDet()` 中增加调试旁路：

- 调试态下检测到 `u8FlashUpdateFlag` 时，只清标志并返回；
- 不再立刻 `MCU_RESET()`。

用途：
- 防止调试现场因为升级握手/误触发标志导致板子自动复位，打断排查。

### 4. 运行时监控改为低频调度

在 [`main.c`](E:/TODO/030%20+%20309/Code/Source/main.c) 中：

- 不再在前台轮询里每圈执行监控逻辑；
- 改为通过 `AppRuntimeMonitor_RunTask()` 每 1 秒调度一次；
- 调试态下不注册 `App_SleepDeal` 定时任务。

用途：
- 降低主循环抖动；
- 避免监控代码本身干扰运行时序判断。

### 5. `Keil` 与 `CMake` 分开处理运行时监控

- `CMake/GCC` 通过 `APP_RUNTIME_MONITOR_ENABLE` 编译完整运行时监控实现；
- `Keil` 工程不再编译 `app_runtime_monitor.c` / `app_state_snapshot.c`；
- `Keil` 下由 [`app_runtime_monitor.h`](E:/TODO/030%20+%20309/Code/Source/BSP/app_runtime_monitor.h) 提供空实现。

用途：
- 避免 `Keil` 因链接区 / RAM 紧张报 `L6406E No space in execution regions`；
- 保留 `CMake` 侧的长期监控与主机联调能力；
- 让你还能继续优先用 `Keil` 看板上问题。

## 本轮验证结果

### 编译/下载

- `task build` 通过
- `C:\Keil_v5\UV4\UV4.exe -b CommomSH367309_16series_030C8T6_C.uvprojx -j0` 通过
- `STM32_Programmer_CLI.exe -c port=SWD mode=UR -hardRst ...` 下载通过
- `OpenOCD` 在目标恢复后可重新连接

### 运行采样

使用 `OpenOCD` 做了“复位后运行 5 秒再强停”的采样：

- 停止时 `PC = 0x08003918`
- 不在复位入口地址

这说明经过本轮调试旁路后，板子**至少不再处于立刻反复重启的状态**。

## 当前结论

1. 之前“像在一直重启”的判断里，混入了正常版本无开关时的休眠行为，不能直接当崩溃证据。
2. 当前调试分支已经通过“禁自动休眠 + 禁启动睡眠恢复 + 禁调试态自动复位”把板子运行状态稳定下来，便于继续查 `AFE` 通信。
3. 后续若需要恢复正式出货行为，删除或注释掉：

```c
#define APP_DEBUG_DISABLE_AUTO_SLEEP
```

## 建议的下一步

1. 先在当前版本上重新观察 `AFE` 通信是否恢复。
2. 如果仍不上，继续沿 `I2C_AFE1.c` / `SH367309_Func.c` / `SH367309_DataDeal.c` 做联机断点。
3. 调试稳定后，再把 `APP_DEBUG_DISABLE_AUTO_SLEEP` 退回为发布前可选开关，而不是默认开启。
