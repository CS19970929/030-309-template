# Fault.c 数据驱动重构设计文档

## 概述

将 Fault.c 中 26 个复制粘贴的故障检查函数（~1240 行代码）替换为 **1 个描述符表 + 1 个统一引擎**的结构。重构后 Fault.c 约 320 行，代码量减少 ~75%。

### 重构目标

- 消除重复代码，降低维护成本
- 保证保护逻辑与原实现完全一致
- 保持与原硬件（SH367309 AFE）的寄存器交互不变
- 保留故障记录、回调等特殊行为

---

## 架构设计

### 整体结构

```
s_faultDesc[26]          App_FaultCheck_Run(idx)     App_WarnCtrl()
  (Flash, 只读)      →      (统一引擎)         →    (26 次循环)
       │                        │
       │  ┌─ pSrcVal    ─────────┤
       │  ├─ pThreshB/S ─────────┤
       │  ├─ pCounter   ─────────┤
       │  ├─ pTimeB/S   ─────────┤
       │  ├─ u16Control ─────────┤
       │  └─ u16FaultEnum       │
       │                        ├──→ App_PubOPUPChk()
       │                        │     (PubFunc.c, 滞后比较引擎)
       │                        │
       │                        ├──→ unMdlFault_Second/Third 软件状态写回
       │                        ├──→ Fault_Flag_Second/Third 更新
       │                        └──→ FaultWarnRecord2() 首次记录
       │
       └── (特殊情况通过 u16Control 编码表达)
             ├─ OTP/UTP 虚电流门控 → VIRCUR_TYPE
             ├─ VdeltaOvp 回调    → TIMES_OFFSET(1)
             └─ OCP TimeS 偏移   → TIMES_OFFSET(2)
```

### 文件变更

| 文件 | 变更 |
|---|---|
| **Fault.c** | 删除 26 个原函数，添加 `s_faultDesc[26]`、`App_FaultCheck_Run()`、新 `App_WarnCtrl()` |
| **Fault.h** | 新增 `FaultCheckDesc` 结构体、`FAULT_CTRL_*` 宏组、`App_FaultCheck_Run()` 声明 |
| Fault.h | 原 `App_*Check()` 26 个函数声明已删除 |

---

## 描述符表 (FaultCheckDesc)

### 结构体定义 (Fault.h:381-390)

```c
typedef const struct {
    UINT16  *pSrcVal;           // 源值指针（来自 g_stCellInfoReport）
    UINT16  *pThreshB;          // EEPROM 阈值 B（跳闸阈值）
    UINT16  *pThreshS;          // EEPROM 阈值 S（恢复阈值）
    UINT16  *pCounter;          // 计数器指针（s_counters[N] 或 sys_time.xxx_cnt）
    const UINT16  *pTimeB;      // 滤波时间指针（跳闸方向）
    const UINT16  *pTimeS;      // 滤波时间指针（恢复方向）
    UINT16  u16Control;         // 控制字（位编码，见下方）
    UINT16  u16FaultEnum;       // FaultFlag 枚举值
} FaultCheckDesc;
```

每个条目 28 字节（6 指针 × 4 字节 + 2 × 2 字节），位于 Flash（RO-data 段）。

### u16Control 控制字编码

```
Bit 0-3 : FAULTREG_BIT_POS  — MDLCHGFAULT 寄存器中的 bit 位置
Bit 4-7 : FLAGREG_BIT_POS   — Fault_Flag 联合体中的 bit 位置
Bit 8   : FLAG_LOGIC        — 比较方向 (0=LOW-trigger, 1=HIGH-trigger)
Bit 9-10: TIMES_OFFSET      — TimeS 偏移 (0=无, 1=+200ticks, 2=+CurOverFaultDelay)
Bit 11-12: VIRCUR_TYPE      — 虚电流门控 (0=无, 1=充电电流, 2=放电电流)
```

### 构造宏

```c
// 设置宏（用于构建 u16Control 值）
FAULT_CTRL_FAULTREG_BIT_POS(x)  // 编码 MDLCHGFAULT bit 位置（0-15）
FAULT_CTRL_FLAGREG_BIT_POS(x)   // 编码 Fault_Flag bit 位置（0-15）
FAULT_CTRL_FLAG_LOGIC(x)        // 编码比较方向
FAULT_CTRL_TIMES_OFFSET(x)      // 编码 TimeS 偏移
FAULT_CTRL_VIRCUR_TYPE(x)       // 编码虚电流门控类型

// 读取宏（用于从 u16Control 解码，引擎内部使用）
FAULT_CTRL_GET_FAULTREG_BIT(c)
FAULT_CTRL_GET_FLAGREG_BIT(c)
FAULT_CTRL_GET_LOGIC(c)
FAULT_CTRL_GET_TIMESOFS(c)
FAULT_CTRL_GET_VIRCUR(c)
```

### 描述符表索引映射

| 索引 | 原函数 | 故障类型 | 级别 |
|---|---|---|---|
| 0 | App_CellOvp_SecondCheck | 电芯过压 | Second |
| 1 | App_CellOvp_ThirdCheck | 电芯过压 | Third |
| 2 | App_CellUvp_SecondCheck | 电芯欠压 | Second |
| 3 | App_CellUvp_ThirdCheck | 电芯欠压 | Third |
| 4 | App_BatOvp_SecondCheck | 电池组过压 | Second |
| 5 | App_BatOvp_ThirdCheck | 电池组过压 | Third |
| 6 | App_BatUvp_SecondCheck | 电池组欠压 | Second |
| 7 | App_BatUvp_ThirdCheck | 电池组欠压 | Third |
| 8 | App_MosOtp_SecondCheck | MOS 过温 | Second |
| 9 | App_MosOtp_ThirdCheck | MOS 过温 | Third |
| 10 | App_VdeltaOp_SecondCheck | 压差过压 | Second |
| 11 | App_VdeltaOp_ThirdCheck | 压差过压 | Third |
| 12 | App_IdischgOcp_SecondCheck | 放电过流 | Second |
| 13 | App_IdischgOcp_ThirdCheck | 放电过流 | Third |
| 14 | App_IchgOcp_SecondCheck | 充电过流 | Second |
| 15 | App_IchgOcp_ThirdCheck | 充电过流 | Third |
| 16 | App_CellSocUp_SecondCheck | SOC 低保护 | Second |
| 17 | App_CellSocUp_ThirdCheck | SOC 低保护 | Third |
| 18 | App_CellDisChgOtp_SecondCheck | 放电过温 | Second |
| 19 | App_CellDisChgOtp_ThirdCheck | 放电过温 | Third |
| 20 | App_CellDischgUtp_SecondCheck | 放电低温 | Second |
| 21 | App_CellDischgUtp_ThirdCheck | 放电低温 | Third |
| 22 | App_CellChgOtp_SecondCheck | 充电过温 | Second |
| 23 | App_CellChgOtp_ThirdCheck | 充电过温 | Third |
| 24 | App_CellChgUtp_SecondCheck | 充电低温 | Second |
| 25 | App_CellChgUtp_ThirdCheck | 充电低温 | Third |

索引规律：**偶数=Second 级，奇数=Third 级**（通过 `idx & 1` 选择）。

---

## 统一引擎 (App_FaultCheck_Run)

### 执行流程

```
App_FaultCheck_Run(idx)
  │
  ├─ 1. 从 s_faultDesc[idx] 读取描述符
  │    ├─ 源值、阈值、计数/时间指针
  │    └─ 解码 u16Control → bit 位置、逻辑、偏移、虚电流类型
  │
  ├─ 2. 选择保护级别
  │    ├─ idx&1==0 (even): unMdlFault_Second + Fault_Flag_Second
  │    └─ idx&1==1 (odd):  unMdlFault_Third  + Fault_Flag_Third
  │
  ├─ 3. OTP/UTP 虚电流门控
  │    ├─ VIRCUR_TYPE!=0 且 AFE 位未设置时
  │    └─ 检查充电/放电电流 ≤1 → 跳过本次检查
  │
  ├─ 4. 配置 SPUBOPUPCHK 结构体
  │    ├─ pSrcVal → u16ChkVal
  │    ├─ pThreshB → u16OPValB (跳闸阈值)
  │    ├─ pThreshS → u16OPValS (恢复阈值)
  │    ├─ pCounter → i16ChkCnt (计数器指针)
  │    ├─ pTimeB → u16TimeCntB (跳闸滤波时间)
  │    ├─ pTimeS → u16TimeCntS (恢复滤波时间)
  │    ├─ TIMES_OFFSET(1) → u16TimeCntS += 200
  │    ├─ TIMES_OFFSET(2) → u16TimeCntS += CurOverFaultDelay
  │    └─ 读取 MDLCHGFAULT bit → u8FlagBit
  │
  ├─ 5. 调用 App_PubOPUPChk(&t) 执行滞后比较
  │
  └─ 6. 处理结果
       ├─ 将 t.u8FlagBit 写回 unMdlFault_Second/Third 对应 bit
       ├─ t.u8FlagBit==1: 设置 Fault_Flag 位
       │   └─ 首次设置 (原为0): 调用 FaultWarnRecord2() + 回调
       └─ t.u8FlagBit==0: 清除 Fault_Flag 位
           └─ 首次清除 (原为1): 调用恢复回调
```

---

## 特殊情况处理

### 1. OCP 第二级 — 外部计数器

OCP Second 级使用 `sys_time.occ2_cnt`（充电）和 `sys_time.odc2_cnt`（放电）作为计数器。
这些计数器被 ShortFunc.c 等其他模块共享。

**描述符中**: `pCounter = (UINT16 *)&sys_time.occ2_cnt`（充电）/ `&sys_time.odc2_cnt`（放电）

### 2. OCP 第二级 — 硬编码滤波时间

OCP Second 级的 `TimeCntB` 不使用 EEPROM 参数，而是固定 500ms（100 × 5ticks）。

**描述符中**: `pTimeB = &s_u16OcpSecondTimeB`（指向静态常量 `(100 * 5)`）

### 3. OCP 第二/三级 — TimeS 偏移 (+CurOverFaultDelay)

OCP Second/Third 级的恢复滤波时间需要额外增加 `CurOverFaultDelay`（3000ticks = 30s）。

**控制字**: `FAULT_CTRL_TIMES_OFFSET(2)` → 引擎内 `t.u16TimeCntS += CurOverFaultDelay`

### 4. VdeltaOvp 第三级 — 系统错误回调

VdeltaOvp 触发和恢复时需要调用 `System_ERROR_UserCallback()`。

**控制字**: `FAULT_CTRL_TIMES_OFFSET(1)` → 引擎内 `t.u16TimeCntS += 200u`，同时触发回调：
- 设置时: `System_ERROR_UserCallback(ERROR_VDEATLE_OVER)`
- 清除时: `System_ERROR_UserCallback(ERROR_REMOVE_VDEATLE_OVER)`

### 5. OTP/UTP 虚电流门控

OTP/UTP 保护需要检查是否存在相应方向的电流，避免空载时误触发。

**控制字**: 
- `FAULT_CTRL_VIRCUR_TYPE(1)` = 充电方向, 门控电流源 `g_stCellInfoReport.u16Ichg`
- `FAULT_CTRL_VIRCUR_TYPE(2)` = 放电方向, 门控电流源 `g_stCellInfoReport.u16IDischg`

**门控条件**: AFE 位未设置 `&&` 电流 ≤ 1 → 跳过本次检查

### 6. MDLCHGFAULT 与 Fault_Flag — bit 位置差异

MDLCHGFAULT 寄存器和 Fault_Flag 联合体中，部分故障的 bit 位置**不同**:

| 故障 | MDLCHGFAULT bit | Fault_Flag bit | 原因 |
|---|---|---|---|
| VdeltaOvp | bit 10 (`b1VcellDeltaBig`) | bit 11 (`VdeltaOvp`) | 硬件/软件寄存器定义不同 |
| MosOTp | bit 13 (`b1TmosOtp`) | bit 10 (`MosOTp`) | 同上 |
| CellDsgOTp_Third | bit 7 (`b1CellDischgOtp`) | bit 8 (`CellDsgOTp_Third`) | `FAULT_FLAG_THIRD` 温度位顺序不同 |
| CellChgUTp_Third | bit 8 (`b1CellChgUtp`) | bit 7 (`CellChgUTp_Third`) | 同上 |

控制字中分别编码两个 bit 位置：
```c
FAULT_CTRL_FAULTREG_BIT_POS(10) | FAULT_CTRL_FLAGREG_BIT_POS(11)  // VdeltaOvp
FAULT_CTRL_FAULTREG_BIT_POS(13) | FAULT_CTRL_FLAGREG_BIT_POS(10)  // MosOTp
FAULT_CTRL_FAULTREG_BIT_POS(7)  | FAULT_CTRL_FLAGREG_BIT_POS(8)   // CellDsgOTp_Third
FAULT_CTRL_FAULTREG_BIT_POS(8)  | FAULT_CTRL_FLAGREG_BIT_POS(7)   // CellChgUTp_Third
```

---

## 门控逻辑参考

### FLAG_LOGIC 比较方向

| 类型 | FLAG_LOGIC | 跳闸条件 | 恢复条件 | 应用 |
|---|---|---|---|---|
| HIGH-trigger | 1 | `value >= ThreshB` | `value <= ThreshS` | OVP, OCP, OTP, VdeltaOvp |
| LOW-trigger | 0 | `value <= ThreshB` | `value >= ThreshS` | UVP, UTP, SocLow |

### SPUBOPUPCHK 内部逻辑 (PubFunc.c)

```c
// u8FlagBit == (1 - u8FlagLogic): "非故障态" → 检查是否应跳闸
if (value >= OPValB)          // 值进入跳闸区
    counter++[到 TimeCntB] → u8FlagBit = u8FlagLogic (进入故障态)
else                          // 值回到阈值以下
    counter--

// u8FlagBit != (1 - u8FlagLogic): "故障态" → 检查是否应恢复
if (value <= OPValS)          // 值进入恢复区
    counter++[到 TimeCntS] → u8FlagBit = 1 - u8FlagLogic (退出故障态)
else                          // 值回到阈值以上
    counter--
```

---

## 维护指南

### 如何添加新的故障类型

假设需要添加"电芯压差第二级"保护：

1. 在 `FaultFlag` 枚举末尾添加枚举值（注意保持枚举顺序与 EEPROM 布局一致）

2. 在 EEPROM 参数结构体 `PRT_E2ROM_PARAS` 中添加对应阈值字段

3. 调整 `s_counters` 数组大小；或在描述符表末尾追加一行

4. 修改 `App_WarnCtrl()` 中的循环上限 26 → 新总数

### 如何修改现有故障的阈值来源

仅需修改描述符表中对应的指针：

```c
// 将 CellOvp_SecondCheck 的源值从 u16VCellMax 改为 u16VCellAvg
{ &g_stCellInfoReport.u16VCellAvg,  // 改这里
  &PRT_E2ROMParas.u16VcellOvp_Second, ...
```

### 如何修改滤波时间算法

只需修改 `App_FaultCheck_Run()` 中对应的 TimeS 偏移逻辑，无需改动 26 个位置。

### 需要特别注意的约束

- **E2P 参数结构体布局不可变**：`PRT_E2ROM_PARAS` 与 EEPROM 地址 1:1 映射，删除/重排字段会破坏持久化数据
- **FaultFlag 枚举值不可重排**：数值写入故障记录数组，重排会改变记录含义
- **s_counters[] 大小同步**：每加一个故障类型需要对应增加一个计数器
- **App_WarnCtrl 循环上限**：必须与描述符表条目数一致

---

## 资源占用

| 项目 | 大小 | 位置 |
|---|---|---|
| `s_faultDesc[26]` | 26 × 28 = 728 B | Flash (RO-data) |
| `s_counters[24]` | 48 B | RAM (ZI-data) |
| `App_FaultCheck_Run()` | ~400 B | Flash (Code) |
| 原来 26 个函数 | ~6600 B | Flash (Code, 已删除) |
| **净节省** | **~5400 B** | **Flash** |
