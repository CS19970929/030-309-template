# 项目初始化说明

## 基础信息

- 项目：`__PROJECT_SLUG__`
- 产品族：`__PRODUCT_FAMILY__`
- 板型：`__BOARD_CODE__`
- AFE：`__AFE_CODE__`
- MCU：`__DEVICE_CODE__`
- 协议：`__PROTOCOL_VARIANT__`
- 客户：`__CUSTOMER_CODE__`

## 接入建议

1. 先明确这是不是“产品族中的一个派生项目”，不要直接复制旧项目后野化演化。
2. 板级差异先填到 `config/boards/__BOARD_CODE__.json`。
3. 客户差异先填到 `config/customers/__CUSTOMER_CODE__.json`。
4. 稳定版、研发版、热修版流转先按 `config/releases/release_lines.json` 约定。
5. 再接入统一入口：
   - `task build`
   - `task sim-protection-suite-summary`

## 不建议的做法

- 直接在业务源码里大量写客户分支判断
- 不区分板型差异和客户差异
- 热修复只改稳定版，不回流研发版
