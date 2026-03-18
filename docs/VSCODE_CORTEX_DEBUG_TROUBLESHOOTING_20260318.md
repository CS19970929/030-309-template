# VS Code Cortex-Debug 排障与使用说明

## 结论

当前仓库里的 `ST-Link/OpenOCD` 调试链路已经具备以下条件：

- `OpenOCD` 已安装
- `arm-none-eabi-gdb` 已安装
- `ST-Link` 已实机烧录验证通过
- 工作区已包含 `cortex-debug` 启动配置
- 工作区已增加扩展推荐：`marus25.cortex-debug`、`ms-vscode.cpptools`

如果 VS Code 仍提示：

```text
Configured debug type 'cortex-debug' is not supported.
```

问题通常不在项目配置，而在当前 VS Code 窗口没有正确加载扩展。

## 已确认的本机状态

本机命令行检查结果：

- `code.cmd --list-extensions` 可见 `marus25.cortex-debug`
- `code.cmd --status` 可见当前工作区含 `cortex-debug` 启动配置
- `task flash-stlink` 已成功识别 `STLINK V2J36S7` 并完成烧录校验

这说明：

- 调试扩展已安装
- 硬件链路可用
- 工作区配置有效

## 最稳妥的恢复步骤

### 1. 完全关闭 VS Code

关闭所有该仓库相关的 VS Code 窗口，不要只关当前标签页。

### 2. 从仓库根目录重新打开

建议重新打开以下目录：

- [E:/TODO/030 + 309](E:/TODO/030%20+%20309)

### 3. 检查扩展是否启用

在 VS Code 的扩展页确认：

- `Cortex-Debug`
- `C/C++`

这两个扩展都处于启用状态，而不是 `Disable` 或 `Enable (Workspace)` 的待启用状态。

### 4. 重新尝试启动

在 `Run and Debug` 里选择：

- `Cortex-Debug: STM32F030 (ST-Link/OpenOCD)`

## 当前工作区配置要点

启动配置文件：

- [E:/TODO/030 + 309/.vscode/launch.json](E:/TODO/030%20+%20309/.vscode/launch.json)

其中已经固定：

- `serverpath` 为本机 `OpenOCD`
- `searchDir` 为本机 `OpenOCD scripts`
- `gdbPath` 为本机 `arm-none-eabi-gdb.exe`
- `preLaunchTask` 为 `mcu: build gcc release`

扩展推荐文件：

- [E:/TODO/030 + 309/.vscode/extensions.json](E:/TODO/030%20+%20309/.vscode/extensions.json)

## 终端侧替代验证

如果你怀疑只是 VS Code 扩展没加载，可以先在终端执行：

```powershell
task build
task flash-stlink
```

这两步已在本机验证通过。

## 调试前检查

上板前保持以下接线：

- `SWDIO`
- `SWCLK`
- `GND`
- `3.3V`

并确认板子已经上电。

## 当前限制

`task flash-stlink` 已验证通过，但 `VS Code -> Cortex-Debug` 是否立即可点开，仍依赖当前 VS Code 进程是否刷新了扩展注册信息。

如果报 `type 'cortex-debug' is not supported`，优先做一次完整重启 VS Code。
