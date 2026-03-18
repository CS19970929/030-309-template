# 保护/SOC/低功耗 PC 策略仿真架构

## 目标

这份文档面向当前仓库下一阶段最重要的方向：

- 不依赖真板
- 在 `PC` 上验证 `BMS` 保护逻辑
- 在 `PC` 上验证 `SOC` 收敛与精度逻辑
- 在 `PC` 上验证低功耗进入/退出策略

重点不是“把 MCU 整机搬到 PC”，而是建立一套**策略级仿真框架**。

## 为什么要做策略级仿真

你当前最关心的是：

- 保护板安全性
- `SOC` 精度
- 功耗与低功耗策略

这三类问题有一个共同点：

- 很多核心风险在“逻辑层”
- 并不都必须先等真实硬件接入才能发现
- 非常适合用长时间、可重复、可批量的 `PC` 回放来做前置验证

所以应该优先验证：

- 判定逻辑是否正确
- 状态迁移是否正确
- 边界值和异常值下是否稳定
- 长时间回放是否漂移、抖动、振荡

而不是先追求“整机等价仿真”。

## 设计边界

### PC 上高价值可验证内容

- 保护阈值判断
- 保护滤波与恢复逻辑
- 多故障并发时优先级
- `SOC` 积分、修正、校准逻辑
- 低功耗进入条件与唤醒条件
- 状态机长期稳定性
- 极端输入下是否产生危险输出

### PC 上不能等价替代的内容

- 真实 ADC 噪声
- AFE 硬件误差
- 微安级待机电流
- 中断、GPIO、RTC 的真实硬件时序
- 电源、上电、掉电的物理行为

因此策略级仿真结论的定位应是：

- 用于前置发现逻辑风险
- 用于提升安全性和开发效率
- 不替代最终真板验证

## 当前源码结构判断

### 1. 保护逻辑

相关文件：

- [Fault.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/Fault.c)
- [Fault.h](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/Fault.h)

结构特征：

- 已经具备较强的“输入值 + 参数 + 状态位 + 计数器”判定模式
- 很多函数本质上是对 `g_stCellInfoReport` 和 `PRT_E2ROMParas` 的组合判定
- 相比 `Sleep`，硬件耦合更弱

判断：

- 这是最适合第一批抽象到 `PC` 的模块

### 2. SOC 逻辑

相关文件：

- [SocEnhance.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/SocEnhance.c)
- [SocEnhance.h](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/SocEnhance.h)

结构特征：

- 内部状态较多
- 带 EEPROM 读写耦合
- 带 OCV 表和容量参数
- 非常适合做长时间回放与误差趋势分析

判断：

- 适合第二批抽象到 `PC`
- 需要先把 EEPROM 和输入量做成平台接口或快照输入

### 3. 低功耗策略

相关文件：

- [SleepDeal.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/SleepDeal.c)
- [SleepDeal.h](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/SleepDeal.h)

结构特征：

- 状态机价值高
- 但和 GPIO、RTC、外部唤醒、中断、AFE 行为耦合更深

判断：

- 适合第三批抽象
- 先抽“策略判定层”，再把底层唤醒和 IO 动作替换为 `PC` 事件模型

## 建议的仿真分层

### 第一层：输入快照

统一把外部输入收敛为一个快照结构，而不是让业务逻辑直接读全局变量。

建议第一版结构：

```c
typedef struct
{
    uint32_t tick_ms;

    uint16_t cell_mv[16];
    uint16_t cell_max_mv;
    uint16_t cell_min_mv;
    uint16_t pack_mv;
    uint16_t vdelta_mv;

    int32_t charge_current_ma;
    int32_t discharge_current_ma;

    int16_t temp_chg_max_c_x10;
    int16_t temp_chg_min_c_x10;
    int16_t temp_dsg_max_c_x10;
    int16_t temp_dsg_min_c_x10;
    int16_t temp_mos_c_x10;

    uint8_t charger_present;
    uint8_t load_present;
    uint8_t ext_comm_active;
    uint8_t key_active;
    uint8_t wake_event;
} AppSimInputSnapshot;
```

说明：

- 第一版只保留保护、`SOC`、低功耗策略真正需要的输入
- 不追求把所有硬件寄存器都抽进来

### 第二层：策略内部状态

每个模块保留自己的内部状态，不直接暴露全局变量。

建议拆成：

- `ProtectionSimState`
- `SocSimState`
- `SleepSimState`

### 第三层：策略输出

输出应是可记录、可比较、可做回归判断的。

建议第一版结构：

```c
typedef struct
{
    uint32_t fault_first;
    uint32_t fault_second;
    uint32_t fault_third;

    uint8_t charge_mos_off;
    uint8_t discharge_mos_off;

    uint8_t soc_pct;
    uint8_t soh_pct;
    uint8_t soc_calibration_active;

    uint8_t sleep_mode;
    uint8_t sleep_status;
    uint8_t to_sleep_flag;
    uint8_t wake_reason;
} AppSimOutputSnapshot;
```

### 第四层：场景驱动器

驱动器职责：

- 从场景文件读入输入序列
- 每步喂给策略模块
- 输出 `jsonl`
- 输出摘要统计

建议目录：

- `host_sim/scenarios/protection/`
- `host_sim/scenarios/soc/`
- `host_sim/scenarios/sleep/`
- `host_sim/tools/`

## 场景设计建议

### 保护场景

第一批必须覆盖：

- 单体 OVP 触发/恢复
- 单体 UVP 触发/恢复
- 充电 OCP 触发/恢复
- 放电 OCP 触发/恢复
- 温度 OTP/UTP 触发/恢复
- 电压差过大触发/恢复
- 多故障叠加
- 阈值边缘抖动

观察项：

- 是否漏保护
- 是否误触发
- 是否误恢复
- MOS 输出是否安全

### SOC 场景

第一批必须覆盖：

- 恒流充电
- 恒流放电
- 低电流静置
- 充放切换
- 满充校准
- 低压校准
- 长时间微小偏置电流
- 断电恢复

观察项：

- 长时间漂移
- 边界跳变
- 0% / 100% 附近行为
- 校准是否收敛

### 低功耗场景

第一批必须覆盖：

- 无负载静置进入休眠
- 通讯后延时休眠
- 故障态禁止休眠
- 外部事件唤醒
- 周期唤醒后再次休眠
- 边界抖动输入

观察项：

- 是否错误进入休眠
- 是否错误保持休眠
- 是否频繁进出休眠
- 唤醒后状态是否正确

## 推荐实施顺序

### 第 1 阶段：保护策略仿真

原因：

- 风险最高
- 耦合最容易先拆
- 最直接关联保护板安全性

落地产物：

- `ProtectionSimState`
- 保护输入快照
- 第一批保护场景
- `jsonl` 输出

### 第 2 阶段：SOC 仿真

原因：

- 你明确关心 `SOC` 精度
- 长时间回放价值非常高

落地产物：

- `SocSimState`
- `SOC` 长时间回放器
- 误差趋势摘要

### 第 3 阶段：低功耗策略仿真

原因：

- 逻辑价值高
- 但硬件耦合最大

落地产物：

- `SleepSimState`
- 低功耗事件模型
- 睡眠/唤醒状态日志

## 对 Codex 接管的意义

这套架构完成后，Codex 就不只是能：

- 编译
- 跑协议解析

而是能进一步接管：

- 安全逻辑回归
- `SOC` 趋势分析
- 低功耗策略回放
- 场景批量验证
- 风险点自动摘要

这才更接近“对 BMS 核心逻辑的长期接管”。

## 下一步建议

下一步不直接大改三块业务源码，而是先做两件事：

1. 定义统一的 `AppSimInputSnapshot` / `AppSimOutputSnapshot`
2. 先从 `Fault` 提炼第一版保护策略仿真入口

这样风险最小，也最容易尽快产生第一批可用的 `PC` 安全验证结果。
