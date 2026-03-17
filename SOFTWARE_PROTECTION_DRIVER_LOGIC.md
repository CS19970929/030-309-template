# 软件保护驱动逻辑梳理

## 1. 总体结论

当前软件保护到驱动执行的主链路是清楚的，分为四层：

1. `Fault.c`
   保护阈值判定、滤波计时、二级/三级故障位生成
2. `IO_Control.c`
   将保护结果汇总到 `Driver_Element`，并把功能关闭结果同步到系统功能位
3. `IODrivers.c`
   按具体硬件拓扑做驱动决策，生成最终 `MOS/Relay` 开关状态
4. `IO_Control.c`
   将最终状态下发到 AFE / MOS / Relay

所以当前架构不是“保护和驱动完全揉在一起”，而是：

`保护判定` -> `故障位` -> `驱动输入` -> `驱动状态机` -> `硬件执行`

## 2. 调度入口

任务注册在 [main.c](/E:/TODO/030%20+%20309/Code/Source/main.c)：

- `App_AFEGet`
- `App_WarnCtrl`
- `App_AnlogCal`
- `App_SOC`
- `App_LogRecord`
- `App_SleepDeal`
- `App_Heat_Cool_Ctrl`
- `App_ChargerLoad_Det`

其中和保护驱动主链路最直接相关的是：

- `App_WarnCtrl`
- `App_AFEGet`
- `App_MOS_Relay_Ctrl`

`App_MOS_Relay_Ctrl()` 在 [DataDeal.c](/E:/TODO/030%20+%20309/Code/Source/DataDeal.c) 内被调用。

## 3. 保护判定层

位置：[Fault.c](/E:/TODO/030%20+%20309/Code/Source/Fault.c)

### 3.1 判定模式

大多数保护函数都采用相同模式：

- 构造本地 `SPUBOPUPCHK t_sPubOPUPChk`
- 填入：
  - 当前检测值 `u16ChkVal`
  - 动作阈值 `u16OPValB`
  - 恢复阈值 `u16OPValS`
  - 计时器地址 `i16ChkCnt`
  - 动作/恢复时间 `u16TimeCntB/u16TimeCntS`
  - 正负逻辑 `u8FlagLogic`
  - 当前旧状态 `u8FlagBit`
- 调用 [PubFunc.c](/E:/TODO/030%20+%20309/Code/Source/PubFunc.c) 的 `App_PubOPUPChk()`
- 将返回结果写回：
  - `g_stCellInfoReport.unMdlFault_Second`
  - 或 `g_stCellInfoReport.unMdlFault_Third`
- 再同步 `Fault_Flag_Second/Third`
- 最后做故障记录 `FaultWarnRecord/FaultWarnRecord2`

### 3.2 当前覆盖的保护类型

主要包括：

- 单体过压/欠压
- 总压过压/欠压
- 充电过流
- 放电过流
- 充电高温/低温
- 放电高温/低温
- MOS 高温
- SOC 低
- 单体压差过大

### 3.3 二级与三级保护的作用

当前软件结构中：

- `unMdlFault_Second`
  更像“告警/较轻动作/较快上报”
- `unMdlFault_Third`
  更像“驱动实际使用的保护结果”

原因是 [IO_Control.c](/E:/TODO/030%20+%20309/Code/Source/IO_Control.c) 的 `RefreshData_Drivers()` 里默认直接把：

- `Driver_Element.Fault_Flag.all = g_stCellInfoReport.unMdlFault_Third.all`

只有在定义 `_SECOND_CURR_PROTECT_FUNC_` 时，才额外把二级充放过流 OR 进去。

## 4. 驱动桥接层

位置：[IO_Control.c](/E:/TODO/030%20+%20309/Code/Source/IO_Control.c)

### 4.1 `RefreshData_Drivers()`

这是保护层到驱动层之间最关键的桥接函数。

它做了三件事：

1. 保护输入同步
   - `Driver_Element.Fault_Flag.all = unMdlFault_Third.all`
   - 可选叠加二级充放过流
2. 运行态数据同步
   - `u16_CurChg`
   - `u16_CurDsg`
3. 系统级强制关闭条件同步
   - `AFE` 通讯错误
   - `EEPROM` 错误
   - `CBC` 错误
   - 温度断线
   - 其他 `SystemStatus` 关闭条件

### 4.2 `u8_FuncOFF_Flag` 的作用

`IODrivers.c` 一旦判定某类严重故障达到“锁死功能”的条件，会设置：

- `Driver_Element.u8_FuncOFF_Flag = 1`

`RefreshData_Drivers()` 内部状态机会把它翻译成：

- `System_OnOFF_Func.bits.b1OnOFF_MOS_Relay = 0`

并把具体原因写到：

- `Driver_Element.MosRelay_Status.bits.b1_FuncOFF_*`
- `ChargerLoad_Func.bits.b1OFFDriver_*`

所以它不仅是驱动关断标志，还是“系统功能级关闭”的传播桥。

## 5. 驱动决策层

位置：[IODrivers.c](/E:/TODO/030%20+%20309/Code/Source/IODrivers.c)

### 5.1 驱动输入结构

定义见 [IODrivers.h](/E:/TODO/030%20+%20309/Code/Source/IODrivers.h)：

- `Driver_Element.Fault_Flag`
- `Driver_Element.u16_CurChg`
- `Driver_Element.u16_CurDsg`
- `Driver_Element.DriverForceExt`
- `Driver_Element.MosRelay_Status`
- `Driver_Element.u8_FuncOFF_Flag`

这说明 `IODrivers.c` 不是直接读取全局保护位到处判断，而是依赖 `Driver_Element` 作为统一输入。

### 5.2 决策模式

以 `RelayCtrl_SameDoor_NoPreChg()` 为例，决策逻辑是：

1. 为每类故障生成一个局部子状态
   - `s_Main_Status_Normal`
   - `s_Main_Status_Vdelta`
   - `s_Main_Status_ChgOcp`
   - `s_Main_Status_DsgOcp`
   - `s_Main_Status_VolOvp`
   - `s_Main_Status_VolUvp`
2. 每类故障各自维护自己的恢复计数、复归次数、2 分钟清零计数
3. 最后把所有子状态按位与汇总
4. 再叠加外部强制开/关/保持
5. 得到最终的 `b1Status_Relay_MAIN` / `b1Status_MOS_*`

这实际上已经是“子模块独立判断，最后集合裁决”的结构。

### 5.3 当前驱动层处理的行为

除了简单开关外，还包含：

- OVP / UVP 的强制等待恢复
- OCP 的 3 次锁死
- 2 分钟正常计数后清恢复次数
- 压差异常直接功能锁死
- 外部强制开关
- 不同硬件拓扑的独立状态机

所以 `IODrivers.c` 体积大的根因不是单纯代码写得啰嗦，而是业务策略本身确实复杂。

## 6. 驱动执行层

位置：[IO_Control.c](/E:/TODO/030%20+%20309/Code/Source/IO_Control.c)

入口函数：

- `App_MOS_Relay_Ctrl()`

调用顺序：

1. `App_DI1_Switch()`
2. `RefreshData_Drivers()`
3. `GetData_Drivers()`
4. `Drivers_Ctrl(...)`
5. `Drivers_External_Ctrl()`

其中：

- `Drivers_Ctrl(...)`
  决定走哪一种驱动拓扑状态机
- `Drivers_External_Ctrl()`
  将 `Driver_Element.MosRelay_Status` 与 `SystemStatus.bits.b1Status_MOS_*` 比较
  只在状态变化时调用 `SH367309_DriverMos_Ctrl()`

这一步相当于把“软件决策状态”真正下发到硬件。

## 7. 当前逻辑的优点

- 保护判定和驱动决策已经分层
- 驱动层已经具备“独立子状态 -> 汇总裁决”的正确思路
- 系统级强制关闭条件被统一收口到 `RefreshData_Drivers()`
- AFE / MOS 实际下发有“仅变化时写入”的节制逻辑

## 8. 当前逻辑的主要问题

### 8.1 `Fault.c` 重复极高

这是最明显的问题。

大量 `App_*Check()` 函数只是以下参数不同：

- 输入量
- 大小阈值
- 滤波时间
- 正负逻辑
- 对应故障位
- 对应记录位

函数骨架几乎一致，导致：

- 代码体积大
- 新增/修改保护容易漏改
- 一处修复要复制多次

### 8.2 `IODrivers.c` 局部静态变量过多

以 `RelayCtrl_SameDoor_NoPreChg()` 为例：

- 每类故障有一组静态变量
- 同一类 OVP/UVP/OCP 逻辑结构高度相似
- 但都写成单独展开

结果是：

- 代码体积大
- 阅读成本高
- 很难验证“两个分支是否真的保持一致”

### 8.3 `IO_Control.c` 职责偏杂

这里同时做了：

- 保护结果桥接
- 系统功能关闭传播
- 外部 DI 控制
- 硬件最终下发

逻辑上还能读，但职责已经开始堆叠。

## 9. 是否能优化

能，而且可以在“不改变功能”的前提下继续瘦身和提高清晰度。

## 10. 优化优先级建议

### 第一优先级：`Fault.c`

建议目标：

- 抽出通用保护判定模板
- 把“阈值/标志位/记录位”参数化

收益：

- 代码体积下降最明显
- 逻辑一致性最好提升
- 风险可控，因为行为模板已经很统一

### 第二优先级：`IODrivers.c`

建议目标：

- 抽出 OVP/UVP/OCP 的共用恢复状态机 helper
- 将“单故障子状态”的更新逻辑参数化

收益：

- 直接命中 `map` 大头
- 可维护性提升明显

风险：

- 比 `Fault.c` 高，因为这里有更多“动作时序”与“驱动恢复策略”

### 第三优先级：`IO_Control.c`

建议目标：

- 将 `u8_FuncOFF_Flag` 对应的原因处理整理成统一映射
- 拆分桥接、关断传播、硬件下发三种职责

收益：

- 可读性提升
- 后续更容易继续瘦身

### 暂不建议优先动

- 保护策略本身阈值和动作规则
- 具体硬件拓扑分支的业务规则

原因：

- 这些一旦动到，就不再是“瘦身/整理”，而是行为级改动

## 11. 推荐重构顺序

建议按下面顺序推进：

1. 先重构 `Fault.c`
2. 再重构 `IODrivers.c`
3. 最后整理 `IO_Control.c`

原因：

- `Fault.c` 最模板化，最适合先拿到低风险收益
- `IODrivers.c` 是体积最大但业务更复杂的部分，放第二步更稳
- `IO_Control.c` 最适合在前两层稳定后做结构清理

## 12. 当前判断

当前保护驱动逻辑“能用，也有明确架构”，不是必须推倒重来。

更合适的方向是：

- 保留现有分层
- 压缩重复代码
- 收敛状态变量
- 把相同模式的故障判定和恢复逻辑抽成公共模板

这样既能瘦身，也不会把现有功能行为改乱。
