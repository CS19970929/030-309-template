# 应用层 Code Review 修复记录

## 本轮修复范围

- `SleepDeal` 状态机分发修复
- `CS resistor` 参数统一校验与缩放系数刷新
- 温度校准恢复默认时的 EEPROM 索引修复
- 首次 AFE 初始化增加有限重试，避免启动死循环
- `main()` 不可达旧主循环删除

## 关键改动

### 1. `SleepDeal` 状态机恢复完整分发

- `App_SleepDeal()` 现在覆盖：
  - `SLEEP_HICCUP_SHIFT`
  - `SLEEP_HICCUP_OVERCUR`
  - `SLEEP_HICCUP_OVDELTA`
  - `SLEEP_HICCUP_CBC`
  - `SLEEP_HICCUP_FORCED`
  - `SLEEP_HICCUP_VCELLOVP`
  - `SLEEP_HICCUP_VCELLUVP`
  - `SLEEP_HICCUP_NORMAL_SELECT`
  - `SLEEP_HICCUP_NORMAL_L1/L2/L3`
  - `SLEEP_HICCUP_TEST`
- 默认分支从“回退到 `NORMAL_SELECT`”调整为“回到 `SHIFT`”，避免异常休眠原因被错误吞掉。
- `SleepDeal_Forced()` 的退出条件修复为 “`L1/L2/L3` 全部未置位时退出”。

### 2. `CS resistor` 参数安全收口

- 新增公共接口：
  - `DataDeal_IsCsResConfigValid()`
  - `DataDeal_RefreshCsResScale()`
- 统一在以下路径刷新 `g_u32CS_Res_AFE`：
  - 上电初始化
  - EEPROM 启动加载
  - `Sci_WrRegs_0x10_SystemElement()`
  - `Sci_WrReg_0x06_Reset_OtherCanAdd()`
  - `SH367309_UpdataAfeConfig()`
  - `InitShortCur()`
- 当 EEPROM 中 `u16Sys_CS_Res` 或 `u16Sys_CS_Res_Num` 为 `0` 时：
  - RAM 中回退到默认值
  - 拉起 EEPROM 写回标志
  - 触发 `ERROR_EEPROM_STORE`
- 串口写系统参数时，如果 `CS resistor` 参数非法，直接返回 `RS485_ERROR_DATA_INVALID`，不再接收该写入。

### 3. 温度校准恢复默认修复

- `Sci_WrReg_0x06_Reset_CalibCoef()` 中 `0x55AE` 分支原先把温度通道默认值写入了错误的 EEPROM 索引。
- 现在 EEPROM 写回索引与 RAM 索引保持一致，避免污染其他校准项。

### 4. 首次 AFE 初始化防卡死

- `InitData_E2prom()` 首次上电路径把无限 `do...while(ret == false)` 改成有限重试。
- 当前重试次数为 `5` 次。
- 失败后触发 `ERROR_AFE1` 并退出初始化函数，不再把系统永久卡在启动阶段。

## 额外说明

- 为了让后续补丁工具可持续修改，本轮把部分历史源文件转换成了 `UTF-8`：
  - `Code/Source/DataDeal.c`
  - `Code/Source/SleepDeal.c`
  - `Code/Source/EEPROM.c`
  - `Code/Source/ShortFunc.c`
  - `Code/Source/SH367309_DataDeal.c`
- 这会导致本次 diff 里出现较多“注释编码修正”类变化，但功能改动只集中在上述修复点。
