# Windows 终端直接使用说明

## 结论

如果你在 PowerShell 里遇到下面这种报错：

```powershell
task : 无法将“task”项识别为 cmdlet、函数、脚本文件或可运行程序的名称
```

通常不是没安装，而是当前终端没有刷新 `PATH`。

## 以后怎么直接用

最稳妥的方式：

1. 关闭当前 PowerShell
2. 重新打开一个新的 PowerShell 或 VS Code 终端
3. 在仓库根目录执行：

```powershell
task doctor
task sim-modbus
task build
```

## 当前窗口立刻恢复的方法

在仓库根目录执行：

```powershell
. .\scripts\windows_refresh_dev_path.ps1
```

注意：
- 前面的 `. ` 不能省略，这是 PowerShell 的 dot-source 用法
- 执行后会立刻刷新当前窗口的 `PATH`

刷新完成后再执行：

```powershell
task doctor
task sim-modbus
task build
```

## 当前已经配置好的工具路径

脚本会补这些目录：

- `C:\Users\Administrator\AppData\Local\Microsoft\WinGet\Links`
- `C:\Program Files\CMake\bin`
- `C:\Program Files (x86)\Arm GNU Toolchain arm-none-eabi\14.2 rel1\bin`
- `C:\Program Files\SEGGER\JLink_V818`
- `C:\Users\Administrator\AppData\Local\Microsoft\WinGet\Packages\xpack-dev-tools.openocd-xpack_Microsoft.Winget.Source_8wekyb3d8bbwe\xpack-openocd-0.12.0-7\bin`

## 仓库里最常用的命令

```powershell
task doctor
task sim-modbus
task build
C:\Keil_v5\UV4\UV4.exe -b CommomSH367309_16series_030C8T6_C.uvprojx -j0
```

## VS Code 里怎么用

如果你不想在终端里敲命令，可以直接用：

- [tasks.json](E:/TODO/030%20+%20309/.vscode/tasks.json)

入口在：

1. `Terminal`
2. `Run Task`
3. 选择：
   `pc: doctor`
   `pc: host sim (cmake)`
   `mcu: build gcc release`
   `mcu: build keil`

## 如果你还遇到找不到命令

先执行：

```powershell
. .\scripts\windows_refresh_dev_path.ps1
```

如果仍然不行，再把下面三条输出贴给我：

```powershell
Get-Command task
Get-Command cmake
Get-Command arm-none-eabi-gcc
```
