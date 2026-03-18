# Board Debug Guide

## Current status

### PC long replay

Available now:

- `task sim-replay`
- `task sim-replay-long`

Scope:

- this is not a full board-equivalent simulation
- it is a long-running replay of real shared parser code
- it produces large log files and JSONL snapshots for Codex analysis

Outputs:

- [modbus-replay.log](E:/TODO/030%20+%20309/artifacts/host-sim/modbus-replay.log)
- [modbus-replay.jsonl](E:/TODO/030%20+%20309/artifacts/host-sim/modbus-replay.jsonl)
- [modbus-replay-long.log](E:/TODO/030%20+%20309/artifacts/host-sim/modbus-replay-long.log)
- [modbus-replay-long.jsonl](E:/TODO/030%20+%20309/artifacts/host-sim/modbus-replay-long.jsonl)

### J-Link

Installed and supported by the workspace.

Current state:

- command generation is available
- VS Code launch entry is available
- actual board-attached verification still depends on your hardware connection

Launch entry:

- `Cortex-Debug: STM32F030 (J-Link)`

### ST-Link

Supported now through `OpenOCD`.

Verified locally:

- OpenOCD package exists
- `interface/stlink.cfg` exists
- `target/stm32f0x.cfg` exists
- VS Code launch entry has been added
- actual board flash was executed successfully with ST-Link + OpenOCD

Launch entry:

- `Cortex-Debug: STM32F030 (ST-Link/OpenOCD)`

Task entry:

- `mcu: flash stlink`

## How to use in VS Code

### PC one-click replay

1. Open `Run and Debug`
2. Choose:
   `Host Replay: Modbus Parser`

For long-running replay:

1. Open `Run and Debug`
2. Choose:
   `Host Replay: Modbus Parser (Long)`

### ST-Link one-click debug

1. Connect board by SWD
2. Ensure the board is powered
3. Open `Run and Debug`
4. Choose:
   `Cortex-Debug: STM32F030 (ST-Link/OpenOCD)`

Notes:

- current board debugging uses `release` firmware with debug symbols
- `Step Over` and `Step Into` are the primary actions to use at `main`
- if you are stopped in the outermost frame, `Step Out` will report that `finish` is not meaningful
- see [BOARD_DEBUG_SCENARIOS_20260318.md](E:/TODO/030%20+%20309/docs/BOARD_DEBUG_SCENARIOS_20260318.md)

### J-Link one-click debug

1. Connect board by SWD
2. Ensure the board is powered
3. Open `Run and Debug`
4. Choose:
   `Cortex-Debug: STM32F030 (J-Link)`

## How to use in terminal

### PC replay

```powershell
task sim-replay
task sim-replay-long
```

### ST-Link flash

```powershell
task flash-stlink
```

### GCC build before debug

```powershell
task build
```

## Recommended debugger choice

If you are more familiar with ST-Link, use ST-Link first.

Reason:

- OpenOCD support is already present
- STM32F0 target script is already available
- the workflow is simpler for your current board family

Use J-Link only when:

- you need SEGGER-specific features
- you already have a stable J-Link setup

## Notes

- `task build` currently uses the GCC release ELF for board-side OpenOCD debug
- `mcu: build keil` is still available for comparison with the legacy flow
- the PC long replay is for protocol and monitoring work, not for exact hardware behavior
