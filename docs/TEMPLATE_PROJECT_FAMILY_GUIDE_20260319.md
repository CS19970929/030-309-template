# 模板化项目族管理指南

## 目标

这份文档说明当前模板如何承接“产品族 / 板型 / 客户”三层差异，而不是继续靠复制项目目录长期演化。

## 当前模板新增的三层配置

生成新项目后，模板会自动带出：

- `config/project_vars.json`
- `config/boards/<board>.json`
- `config/customers/<customer>.json`
- `config/releases/release_lines.json`

这四份文件分别承担：

- 项目主清单
- 板级差异
- 客户差异
- 版本流转

## 推荐使用方式

### 1. 新项目初始化

示例：

```bash
task new \
  PROJECT_NAME=demo-bms \
  PRODUCT_FAMILY=bms-16s \
  BOARD_CODE=stm32f030-sh367309-v1 \
  AFE_CODE=sh367309 \
  DEVICE_CODE=STM32F030C8 \
  PROTOCOL_VARIANT=modbus \
  CUSTOMER_CODE=common
```

### 2. 板级差异放哪里

优先放：

- `config/boards/*.json`

不要优先放：

- 业务逻辑源码里的散乱注释
- 客户判断分支

### 3. 客户差异放哪里

优先放：

- `config/customers/*.json`

适合收敛：

- feature flag
- 参数组
- 协议小差异

### 4. 版本线怎么固定

模板会先带出一份：

- `config/releases/release_lines.json`

它的作用不是自动化 Git，而是先把版本线命名和热修回流规则固定下来，避免每个项目都靠临时约定。

## 对 Codex 接管的意义

这套模板化骨架的价值在于：

- Codex 能先读项目主清单，再决定改哪一层
- 板级差异和客户差异不再混在业务代码里
- 新项目不需要从零解释项目结构
- 后面更容易做自动化检查和模板生成
