# 日常开发速查表

## 最省事的用法

### VS Code

- `Ctrl+Shift+B`：直接执行默认构建任务
  现在默认是 `mcu: build gcc release`
- `Terminal -> Run Task`：选择常用任务
- `Run and Debug`：选择板级调试或主机回放

### PowerShell

打开新终端后，直接使用这些短命令：

- `bbuild`：GCC 固件构建
- `brebuild`：清理后重新构建 GCC 固件
- `bflash`：`ST-Link + OpenOCD` 烧录
- `bsim`：运行主机侧解析测试
- `breplay`：运行一次主机侧回放
- `breplaylong`：运行长时间主机侧回放
- `bkeil`：执行 Keil 命令行构建

## 常见场景

### 改了 MCU 代码

```powershell
bbuild
```

如果要重新从头编译：

```powershell
brebuild
```

### 编译后下载到板子

```powershell
bbuild
bflash
```

### 改了协议解析或主机日志

```powershell
bsim
breplay
```

### 想长时间观察日志

```powershell
breplaylong
```

### 要对照旧流程

```powershell
bkeil
```

## VS Code 推荐入口

- 构建：`Ctrl+Shift+B`
- 烧录：`Terminal -> Run Task -> mcu: flash stlink`
- 主机回放：`Run and Debug -> Host Replay: Modbus Parser`
- 长跑回放：`Run and Debug -> Host Replay: Modbus Parser (Long)`
- 板级调试：`Run and Debug -> Cortex-Debug: STM32F030 (ST-Link/OpenOCD)`
