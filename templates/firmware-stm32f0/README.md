# __PROJECT_SLUG__

## 项目概览

- 产品族：`__PRODUCT_FAMILY__`
- 板型：`__BOARD_CODE__`
- AFE：`__AFE_CODE__`
- MCU：`__DEVICE_CODE__`
- 协议：`__PROTOCOL_VARIANT__`
- 客户：`__CUSTOMER_CODE__`

## 目录意图

- `config/project_vars.json`
  项目主清单，记录产品族、板型、AFE、协议和客户信息。
- `config/boards/__BOARD_CODE__.json`
  板级差异配置入口。
- `config/customers/__CUSTOMER_CODE__.json`
  客户差异配置入口。
- `config/releases/release_lines.json`
  稳定版 / 研发版 / 热修版流转说明。
- `docs/PROJECT_SETUP.md`
  当前项目初始化与接入说明。

## 使用建议

1. 共性逻辑尽量放在核心层，不要直接复制源码分叉。
2. 板级差异优先收敛到 `boards` 配置和平台适配层。
3. 客户差异优先收敛到 `customers` 配置，不要散落在业务代码里。
