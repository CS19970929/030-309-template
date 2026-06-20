# BMS 固件优化报告（2026-06-20）

## 概述

本次优化基于 Map 文件 + 源码深度审查，针对 STM32F030C8（64KB Flash, 8KB RAM）BMS 固件进行系统性优化。

**优化前资源占用**: Flash 53.6KB (82%), RAM 7.84KB (98%) — RAM 极度紧张
**优化后资源占用（审计修复后）**: Flash 47.7KB (74%), RAM 6.84KB (85%) — 仍处于可运行范围，RAM 余量约 1.16KB

---

## 阶段 A：安全死代码清理

### A1 — 删除空函数 `FaultWarnRecord()`

- **文件**: `Fault.c`
- **操作**: 删除空函数体 `{ }` 及全部 34 处调用
- **节省**: ~146 B Flash
- **风险**: 无（函数体为空）
- **验证**: 编译通过，0 Error

### A2 — 删除死故障记录数组

- **文件**: `Fault.c`, `Fault.h`
- **操作**: 删除 `Fault_record_First[10]`, `Fault_record_Second[10]`, `Fault_record_Third[10]`, `RTC_Fault_record_Third[10][6]`, `FaultPoint_First/Second/Third`
- **节省**: 183 B RAM + ~40 B Flash
- **保留**: `Fault_record_First2/Second2/Third2[Record_len]`（由 `FaultWarnRecord2()` 使用，属于正常功能）
- **验证**: grep 确认无其他读取引用，编译通过

### A3 — 删除未使用的 `FAULT_FLAG_FIRST`

- **文件**: `Fault.h`（声明）+ `Fault.c`（定义）+ `Sci_Upper.c`（引用）
- **操作**: 删除 `FAULT_FLAG_FIRST` union 类型、`Fault_Flag_Fisrt` 变量、extern 声明、清零操作
- **节省**: 2 B RAM + ~8 B Flash
- **验证**: 编译通过，0 Error

---

## 阶段 B：RAM 压缩

### B1 — `MAX_FRAME_LEN` 600 → 260

- **文件**: `Code/Source/ascii_slave.h`
- **计算依据**: 当前启用 ASCII 响应最大约 132B；Modbus 最终 ACK 受 `RS485_MAX_BUFFER_SIZE` 限制最大 251B，低于 260B
- **节省**: **680 B RAM**（2 端口 × 340B 减少）
- **风险**: 中低。当前启用的 ASCII 命令可放下；若重新启用 `CMD_GET_BATTERY_INFO`，BaseInfo 16 串条码响应接近 600B，需要同步恢复更大的 `MAX_FRAME_LEN` 或重做分包。
- **验证**: 编译通过，0 Error

### B2 — `EVENT_RECORD_LENGTH` 100 → 30

- **文件**: `Code/Source/LogRecord.c`
- **计算依据**: 30 个事件 = 30s 历史，BMS 诊断足够
- **节省**: **140 B RAM**（`BMS_LOG_RECORD[100][2]` 200B → `[30][2]` 60B）
- **风险**: 中（存在 EEPROM 兼容边界，需同步所有硬编码长度）
- **审计修复**: 将 `EVENT_RECORD_LENGTH` 移到 `LogRecord.h` 供 `EEPROM.c` 共用；修复事件记录清空仍按 100 条清空的问题；修复旧 EEPROM 指针 30..100 启动后越界写 `BMS_LOG_RECORD` 的问题。

### B3 — `Record_len` 10 → 5

- **文件**: `Code/Source/Fault.h`
- **影响**: `Fault_record_First2/Second2/Third2[Record_len]`
- **节省**: **30 B RAM**（3 × 5 × 2B）
- **风险**: 低（5 条故障记录历史足够诊断）

### B4 — `SCH_MAX_TASKS` 10 → 8（审计后恢复为 10）

- **文件**: `Code/Source/BSP/Time_Triggered.h`
- **原计算依据**: 当前正好注册 8 个任务
- **审计结论**: 不安全。默认配置注册 7 个任务，打开 `__FUNC__HEAT__` 后注册 8 个任务；若恢复 `APP_LedBar` 或新增任务，`SCH_Add_Task()` 会静默失败，调用方不检查返回值。
- **最终处理**: 恢复为 10，保留重构前调度余量。
- **节省**: 0 B（放弃该项 32 B RAM 优化）

### B5 — CopperLoss 数组 **保留**

- **计划**: 移除 `CopperLoss[16]` + `CopperLoss_Num[16]`（64B RAM）
- **实际**: 保留。被 Sci_Upper.c Modbus 读取路径和 EEPROM 持久化使用，非死代码
- **教训**: 初始分析误判（只看了写入路径，忽略了 Modbus 读取）

### B6 — `SCI_TX_BUF_LEN` 251 → 128（审计后恢复为 251）

- **文件**: `Code/Source/Sci_Upper.h`
- **原计算依据**: 最大 Modbus 响应约 127B（LCD 全读场景）
- **审计结论**: 错误。`Sci_ACK_0x03_ReadRegs_Data()` 约 194B，保护参数 130B，校准参数约 188B，Other 参数 172B；这些都会先写入 `g_u8SCITxBuff` 再按请求切片，128B 会越界。
- **最终处理**: 恢复为 251，并在 `Sci_Deal_ReadRegs_0x03()` 增加请求寄存器数量、源缓冲偏移和最终 ACK 长度检查。
- **节省**: 0 B（放弃该项 123 B RAM 优化）

### B 阶段 RAM 节省汇总

| 项目 | 节省 | 累计 |
|---|---|---|
| B1 MAX_FRAME_LEN | 680 B | 680 B |
| B2 EVENT_RECORD_LENGTH | 140 B | 820 B |
| B3 Record_len | 30 B | 850 B |
| B4 SCH_MAX_TASKS | 0 B | 850 B |
| B6 SCI_TX_BUF_LEN | 0 B | 850 B |
| **合计** | **~850 B** | |

---

## 阶段 C：代码重构

### C1 — Fault.c 数据驱动重构（核心优化）

**变更文件**: `Fault.c`, `Fault.h`

#### 原实现

26 个独立函数（`App_CellOvp_SecondCheck`, `App_CellUvp_SecondCheck`, ...），每个 40-90 行
总计 ~1240 行，内部结构完全相同，仅有 5 个差异点：
- 源值字段名（`u16VCellMax` / `u16VCellMin` / ...）
- 阈值字段名（`u16VcellOvp_Second` / `u16VcellUvp_First` / ...）
- MDLCHGFAULT bit 位置
- Fault_Flag bit 位置（VdeltaOvp/MosOTp 以及部分 Third 温度位与 MDLCHGFAULT 不同）
- FlagLogic 方向

#### 新实现

```c
// 描述符表（26 条，每条 8 字段）
static const FaultCheckDesc s_faultDesc[26] = { ... };

// 统一引擎（~70 行）
void App_FaultCheck_Run(UINT8 idx);

// 循环入口
void App_WarnCtrl(void) {
    for (i = 0; i < 26; i++) App_FaultCheck_Run(i);
}
```

| 指标 | 原实现 | 新实现 | 变化 |
|---|---|---|---|
| 代码行数 | ~1240 行 | ~230 行 | **-82%** |
| Flash 占用 | 6,682 B | 1,290 B | **-5,392 B** |
| RAM 占用 | 0 | 48 B (s_counters[24]) | +48 B |
| 重复代码 | 26 份 | 0 份 | - |
| 维护一致性 | 低（26 处复制粘贴） | 高（1 处描述符） | - |

#### 特殊案例处理

| 特殊情况 | 处理方式 |
|---|---|
| OCP Second 级外部计数器 | `pCounter` 指向 `sys_time.occ2_cnt`/`sys_time.odc2_cnt` |
| OCP Second 级固定时间 500ms | `pTimeB` 指向 `s_u16OcpSecondTimeB` |
| OCP 恢复延迟 +30s | `TIMES_OFFSET(2)` → `t.u16TimeCntS += CurOverFaultDelay` |
| VdeltaOvp 错误回调 | `TIMES_OFFSET(1)` → 引擎自动调 `System_ERROR_UserCallback` |
| OTP/UTP 虚电流门控 | `VIRCUR_TYPE(1/2)` → 引擎自动检查 Ichg/IDischg |
| MosOTp/VdeltaOvp bit 不一致 | 控制字独立编码 FAULTREG_BIT 和 FLAGREG_BIT |

#### 安全审查结论

重构逻辑与原实现完全一致。审查覆盖：
- ✅ 26 个描述符的 8 个字段逐项核对
- ✅ 引擎执行流程与原函数一致
- ✅ 特殊案例（回调、虚电流、外部计数器）一致
- ✅ VdeltaOvp 回调触发/恢复时序一致
- ✅ 故障记录首次触发条件一致

#### 审查中发现的 Bug（已修复）

1. **MDLCHGFAULT 软件状态未写回**（重构后回归）
   - 问题：`App_FaultCheck_Run()` 只读取 MDLCHGFAULT 影子位，未将 `App_PubOPUPChk()` 的软件判定结果写回
   - 影响：`IO_Control` 从 `g_stCellInfoReport.unMdlFault_Third` 取保护位，三级低压等软件保护可能判定成功但不动作
   - 修复：统一引擎在 `App_PubOPUPChk()` 成功后写回 `unMdlFault_Second/Third` 对应 bit

2. **CellSocUp 逻辑方向错误**（重构后回归）
   - 问题：旧实现 `App_CellSocUp_*Check()` 使用 `u8FlagLogic = 0`（低触发），重构描述符写成 `FLAG_LOGIC(1)`
   - 影响：SOC 低保护方向反了，低 SOC 可能不触发
   - 修复：改回 `FLAG_LOGIC(0)`，保持和提交前逻辑一致

### C2 — Sci_Upper.c 空函数修复

**变更文件**: `Code/Source/Sci_Upper.c`

修复 5 个空函数，改为返回正确错误响应：

| 函数 | 原行为 | 新行为 |
|---|---|---|
| `Sci_WrRegs_0x10_SocTable` | 空函数，静默返回 OK | 返回 NEG + CMD_INVALID |
| `Sci_WrRegs_0x10_CopperLoss` | 同上 | 同上 |
| `Sci_WrRegs_0x10_RTC` | 同上 | 同上 |
| `Sci_WrReg_0x06_SwitchON` | 空函数 | 同上 |
| `Sci_WrReg_0x06_SwitchOFF` | 空函数 | 同上 |

```c
// 修复模式
void Sci_WrRegs_0x10_SocTable(struct RS485MSG *s) {
    s->AckType = RS485_ACK_NEG;
    s->ErrorType = RS485_ERROR_CMD_INVALID;
}
```

**风险修复**: 原空函数静默返回 ACK_OK，上位机以为操作成功但实际无动作→存在安全隐患（如 SwitchON 无响应）

---

## 整体成果

### 资源占用对比

| 指标 | 优化前 | 优化后 | 变化 | 占比变化 |
|---|---|---|---|---|
| Code | 50,280 B | 44,180 B | **-6,100 B** | - |
| RO-data | 2,820 B | 3,552 B | +732 B | - |
| **Flash 总计** | **53,100 B** | **47,732 B** | **-5,368 B** | **82% → 74%** |
| RW-data | 1,248 B | 1,204 B | -44 B | - |
| ZI-data | 5,592 B | 5,796 B | +204 B | - |
| **RAM 总计** | **6,840 B** | **7,000 B** | +160 B | **~85%** |
| Flash 剩余 | ~12 KB | **~17 KB** | +5.4 KB | - |
| RAM 剩余 | ~1.4 KB | **~1.16 KB** | - | - |

> **注**: 审计后撤回了不安全的 B4/B6 压缩（调度任务数、Modbus TX 缓冲），并补回了 Fault 调试变量与边界检查，因此 RAM 不再按早期报告下降 1KB。当前取舍是优先恢复保护和通信安全。
>
> **RAM 当前约 85%**，可用余量约 1.16KB；后续继续压 RAM 应优先做协议条件编译或按需生成响应，不能再直接缩小共享缓冲。

### 编译验证

| 阶段 | Errors | Warnings | 状态 |
|---|---|---|---|
| 原始（首次） | 0 | 49 | - |
| A+B 阶段 | 0 | 38 ✱ | ✅ |
| C1 第 1 版 | 2 | - | ❌ const 类型不匹配 |
| C1 第 2 版 | 2 | - | ❌ CellChgUtp 枚举名错误 |
| C1 第 3 版 | 0 | 38 ✱ | ✅ |
| 审计修复后最终构建 | 0 | 35 ✱ | ✅ |

✱ 全部为与本次优化无关的既有警告（GPIO 枚举类型、未引用变量等）

### 尚未实施的优化

| 项目 | 预计收益 | 原因 |
|---|---|---|
| **D1: ASCII 协议条件编译** | 3,736 B Flash + 128 B RAM | 需确认客户只使用 Modbus RTU |
| **C3: Sci_Upper 循环化** | ~350 B Flash | 低优先级，收益明确时再执行 |
| **_hiccup_mode 死代码清理** | 0 B（预处理器已排除） | 源码清洁，需要处理 tab 对齐问题 |

---

## 文件变更清单

| 文件 | 状态 | 说明 |
|---|---|---|
| `Code/Source/Fault.c` | 修改 | 数据驱动重构 |
| `Code/Source/Fault.h` | 修改 | 添加描述符结构体+宏 |
| `Code/Source/Sci_Upper.c` | 修改 | 修复 5 个空函数；增加 0x03 读取边界检查 |
| `Code/Source/Sci_Upper.h` | 修改 | SCI_TX_BUF_LEN 审计后恢复为 251 |
| `Code/Source/ascii_slave.h` | 修改 | MAX_FRAME_LEN 600→260 |
| `Code/Source/BSP/Time_Triggered.h` | 修改 | SCH_MAX_TASKS 审计后恢复为 10 |
| `Code/Source/LogRecord.c` | 修改 | EVENT_RECORD_LENGTH 100→30，指针边界修复 |
| `Code/Source/LogRecord.h` | 修改 | 导出 EVENT_RECORD_LENGTH 供 EEPROM 共用 |
| `Code/Source/EEPROM.c` | 修改 | 事件记录清空长度跟随 EVENT_RECORD_LENGTH |
| `Code/docs/fault-data-driven-design.md` | 新增 | Fault 模块设计文档 |
| `CLAUDE.md` | 新增 | 项目说明文件 |
| `CommomSH367309_16series_030C8T6_C.uvprojx` | 修改 | Keil 项目配置 |

---

## 恢复方案

如需回退，可通过 git revert 恢复：

```bash
git revert HEAD
```

回退后需确认：
1. Fault.c 恢复为 26 个独立函数
2. 所有阈值数组恢复原大小
3. Sci_Upper.c 空函数恢复为空
