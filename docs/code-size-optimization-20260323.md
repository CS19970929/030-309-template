# Code Size Optimization 2026-03-23

## 背景

基于当前分支生成的 `CommomSH367309_16series_030C8T6_C.map` 分析，当前镜像体积如下：

- `Total RO Size`: `51056` bytes
- `Total RW Size`: `6568` bytes
- `Total ROM Size`: `51532` bytes

链接后占用最大的对象主要是：

- `sci_upper.o`: `8800 code / 764 RO / 244 RW`
- `fault.o`: `6682 code / 470 RO`
- `eeprom.o`: `3346 code / 364 RO / 1172 RW`
- `socenhance.o`: `2938 code / 252 RO / 252 RW`
- `i2c_afe1.o`: `2324 code / 74 RO / 368 RW`
- `sleepdeal.o`: `2210 code / 190 RO`

同时，`map` 已显示链接器已移除 `697` 个未使用段，共 `36517` bytes，说明当前工程已经启用未引用代码裁剪，继续通过“删文件”获取收益很有限。

## 本次优化

### 1. `Fault.c` 重复保护检测逻辑收敛

`Fault.c` 内大量 `SecondCheck/ThirdCheck` 函数具有相同骨架：

- 组装 `SPUBOPUPCHK`
- 调用 `App_PubOPUPChk`
- 回写模型故障位
- 记录故障历史
- 置位/清除二三级故障锁存

本次将这部分统一收敛到两个内部辅助函数：

- `Fault_RunCheckCore`
- `Fault_RunCheckWithActivation`

外部函数名和调用点保持不变，仅改为薄包装调用，降低重复指令展开，目标是在不改变行为的前提下压缩 `fault.o` 的代码段。

### 2. Keil 工程切换到偏体积优化

在 `CommomSH367309_16series_030C8T6_C.uvprojx` 中将：

- `<oTime>1</oTime>` 调整为 `<oTime>0</oTime>`

该修改用于让编译器优先考虑代码尺寸而非执行时间，属于对功能无侵入的体积优化。

### 3. `Sci_Upper.c` 协议分发层收敛

第二轮继续处理 `sci_upper.o` 的协议调度层，重点覆盖：

- `Sci_Deal_ReadRegs_0x03`
- `Sci_Deal_WrReg_0x06`
- `Sci_Deal_WrRegs_0x10`

本次没有改动具体寄存器写入实现，只调整入口分发方式：

- 将 `0x03` 的地址归一化逻辑收敛为统一查表函数
- 将 `0x06` 的单寄存器写指令改为表驱动分发
- 将 `0x10` 的多寄存器写指令拆为三类分发

三类分发分别为：

- 校准地址范围判断
- 保护参数和 SN 这类需要保留地址参数的显式分支
- 其余固定入口使用表驱动映射

这样做的目标是压缩大 `switch-case` 展开，同时保留现有处理函数、参数和外部行为不变。

## 风险与说明

- `Fault.c` 只做了内部收敛，未改动外部接口、调用顺序和故障枚举。
- `App_VdeltaOp_ThirdCheck` 的附加告警回调逻辑保留，仍在故障置位/恢复时触发。
- 过流二级保护仍分别使用 `sys_time.occ2_cnt` / `sys_time.odc2_cnt` 作为计数器，保持原策略不变。

## 当前环境限制

当前工作区没有可直接调用的 `armcc` / `clang` / `gcc`，因此本次无法在本地重新编译并给出新的 `map` 对比值。后续建议在带 Keil 编译环境的机器上重新生成 `map`，重点观察：

- `fault.o` 的 `Code (inc. data)` 是否明显下降
- 总 `ROM Size` 的变化
- `sci_upper.o` 是否仍为第一大热点

## 后续建议

如果继续做第二轮压缩，优先级建议如下：

1. `Sci_Upper.c`
   该文件仍是当前最大热点，当前只收敛了入口分发层，后续可继续处理 `ACK_0x03` 读取回包中的重复组包逻辑。
2. `EEPROM.c`
   关注默认参数表、重复写读流程和软 I2C 重复序列。
3. `SocEnhance.c` / `SleepDeal.c`
   继续收敛状态机中重复分支。
