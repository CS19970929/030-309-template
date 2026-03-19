# Map 摘要自动化指南

## 目标

这份文档说明当前工具链如何把 `GNU ld map` 文件转成 Codex 可直接读取的结构化摘要。

目标不是替代人工读 map，而是先给 Codex 和你自己一个统一的第一层判断：

- RAM 是否接近上限
- FLASH 是否接近上限
- 当前 `heap/stack` 预留是多少
- 哪些对象文件是主要占用来源

## 当前入口

```bash
task map
```

默认产物：

- [map-summary.json](/Users/cs/Downloads/work/todo/030-309-template/artifacts/map-summary.json)
- [map-summary.md](/Users/cs/Downloads/work/todo/030-309-template/artifacts/map-summary.md)

默认读取当前 `CMake/GCC` 产物：

- `artifacts/cmake/firmware-release/stm32f030_app.map`

如果你要分析旧的 `Keil` map，也可以手工指定：

```bash
task map MAP_FILE=CommomSH367309_16series_030C8T6_C.map
```

## 当前可读出的关键信息

### RAM

- 总 RAM 空间
- RAM 已使用空间
- `RAMVectorTable`
- `.data`
- `.bss`
- `heap`
- `stack`
- RAM 风险级别

### FLASH

- 总 FLASH 空间
- FLASH 已使用空间
- `.isr_vector`
- `.text`
- `.data load image`
- FLASH 风险级别

### 热点对象

当前会按对象文件聚合两类热点：

- FLASH Top
- RAM Top

这能让 Codex 在审查改动时快速看到“这次增长主要来自哪个模块”，而不是先人工翻完整 map。

## 风险分级

当前规则是：

- `< 75%`：`ok`
- `>= 75%`：`warning`
- `>= 90%`：`critical`
- `> 100%`：`critical`

这个阈值不是量产标准，只是第一层自动提醒阈值。真正的项目边界仍应结合：

- 中断栈深度
- 通信峰值场景
- 运行时临时缓冲区
- 后续版本增长空间

## 推荐工作流

如果你改了：

- 保护逻辑
- `SOC`
- 通信协议
- 存储结构
- `heap/stack`
- 编译选项

推荐顺序：

1. `task build`
2. `task map`
3. `task sim-protection-suite-summary`
4. 再决定是否进入板级验证

## 对 Codex 接管的价值

这套摘要最重要的价值是把“链接结果”从纯文本 map 变成了结构化输入。

这样 Codex 后面可以更稳定地做这些事：

- 判断 RAM/FLASH 是否接近风险区
- 判断 `heap/stack` 调整是否合理
- 判断某次改动是否带来异常增长
- 结合 PR 审查输出更高价值的 findings

## 当前边界

当前版本仍然有边界：

- 还没有做多版本趋势对比
- 还没有对 `RWX LOAD segment` 等 linker warning 做结构化归类
- 还没有建立“项目级内存预算”基线

因此它现在是“第一层自动分析”，不是最终结论系统。
