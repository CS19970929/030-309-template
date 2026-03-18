# Host Replay And Logging Guide

## Purpose

This host-side tool reuses the real `modbus_rtu_parser.c` code and adds:

- frame replay from a text scenario
- unified log output through `APP_LOG`
- JSONL snapshots for long-running inspection

It is the first step toward "same-code PC simulation plus long-term monitoring".

## What Is Shared Today

The following code is shared between MCU and PC:

- [modbus_rtu_parser.c](E:/TODO/030%20+%20309/Code/Source/modbus_rtu_parser.c)
- [modbus_rtu_parser.h](E:/TODO/030%20+%20309/Code/Source/modbus_rtu_parser.h)
- [app_log.h](E:/TODO/030%20+%20309/Code/Source/BSP/app_log.h)

The host adds only runtime glue:

- [app_log_runtime.c](E:/TODO/030%20+%20309/host_sim/support/app_log_runtime.c)
- [modbus_parser_host_replay.c](E:/TODO/030%20+%20309/host_sim/tools/modbus_parser_host_replay.c)

## How To Run

In the repository root:

```powershell
task sim-replay
```

Generated outputs:

- [modbus-replay.log](E:/TODO/030%20+%20309/artifacts/host-sim/modbus-replay.log)
- [modbus-replay.jsonl](E:/TODO/030%20+%20309/artifacts/host-sim/modbus-replay.jsonl)

## Scenario Format

Default input file:

- [modbus_frames.txt](E:/TODO/030%20+%20309/host_sim/scenarios/modbus_frames.txt)
- [modbus_invalid_frames.txt](E:/TODO/030%20+%20309/host_sim/scenarios/modbus_invalid_frames.txt)

Rules:

- one Modbus frame per line
- hex bytes separated by spaces
- lines beginning with `#` are ignored

Recommendation:

- use `modbus_frames.txt` for long-running replay and monitoring
- use `modbus_invalid_frames.txt` only when you want to inspect invalid-frame handling

Example:

```text
01 03 00 10 00 02 44 09
01 06 00 05 12 34 95 78
```

## Output Meaning

### Log file

The log file is human-readable and records:

- cycle index
- line index
- byte index
- parser length
- expected frame length
- final parse result

### Snapshot file

The snapshot file is JSONL and records one line per replayed frame.

Fields:

- `cycle`
- `line`
- `result`
- `length`
- `expected_length`
- `frame`

## VS Code Entry

You can run it from:

- [tasks.json](E:/TODO/030%20+%20309/.vscode/tasks.json)
- [launch.json](E:/TODO/030%20+%20309/.vscode/launch.json)

Use:

- `pc: host replay`
- `Host Replay: Modbus Parser`

## Why This Matters

This tool gives you three things immediately:

- repeatable PC-side replay
- larger logs than MCU serial logs
- stable snapshots for Codex to inspect during debugging

## Next Step

The next stage is to keep pulling more business logic behind the same boundary:

1. protocol handling
2. state snapshots
3. platform abstraction for time, storage, and IO
