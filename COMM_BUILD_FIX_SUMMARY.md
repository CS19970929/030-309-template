# Comm Build Fix Summary

## Background

This round of changes was triggered by build failures introduced while integrating the new communication module.

Initial compiler errors:

- `TEMP_NUM` undefined in `Sci_Upper.h`
- `Comm_PortIrqHandler`, `g_comm_port1`, `g_comm_port2` undefined in `stm32f0xx_it.c`

After those were fixed, the project linked but exceeded `RW_IRAM1` on `STM32F030C8` (8 KB RAM).

## Root Cause

### 1. Header self-containment issues

- `Sci_Upper.h` used `TEMP_NUM`, but did not directly include the header that defines it.
- `stm32f0xx_it.c` used `Comm_*` symbols without directly including `Comm.h`.

The old include chain through `main.h` masked this problem before the communication refactor.

### 2. RAM growth after communication refactor

The new communication path introduced large per-port RAM usage:

- duplicate RX/TX frame buffers
- separate Modbus and ASCII parser buffers kept alive at the same time
- per-port Modbus service context
- a relatively large UART RX ring buffer

Because both USART1 and USART2 are enabled, this RAM cost was effectively doubled.

## Source Changes

### Build dependency fixes

- `Code/Source/Sci_Upper.h`
  - added `#include "DataDeal.h"` so `TEMP_NUM` is always visible where the header is used
- `Code/Drivers/stm32f0xx_it.c`
  - added `#include "Comm.h"` so IRQ handler declarations and globals are explicitly available

### Communication RAM optimization

- `Code/Source/ascii_slave.h`
  - kept `MAX_FRAME_LEN` for TX path
  - added `ASCII_RX_FRAME_LEN`
  - changed `AsciiParser.buffer` to use the smaller RX-only limit
- `Code/Source/ascii_slave.c`
  - updated parser bounds checks to use `ASCII_RX_FRAME_LEN`
- `Code/Source/Comm.h`
  - reduced `COMM_RX_RING_SIZE`
  - removed redundant `rx_buf`
  - replaced separate Modbus/ASCII parser instances with a shared union-based RX state
  - removed unused `CommRequest` and `CommResponse` structs
  - removed per-port Modbus service context from `CommPortContext`
- `Code/Source/Comm.c`
  - stopped copying complete received frames into a second buffer
  - read completed frames directly from the active parser buffer
  - moved Modbus service context to a shared static workspace
  - updated RX reset/dispatch logic to match the new shared parser storage

### Review-driven hardening

- `Code/Source/ascii_slave.c`
  - clamped `battery_num` to `CELL_MAX_NUM` before copying barcode fields
  - added explicit TX buffer capacity checks in `Build_Response_Frame`
  - updated ASCII handlers to pass output capacity through the call chain
- `Code/Source/modbus_service.c`
  - added response length validation before copying `AckLenth` into the TX buffer
  - added the missing standard header for `memcpy`
- `Code/Source/ascii_slave.h`
  - updated API signatures so frame builders/handlers receive output capacity explicitly
- `Code/Source/modbus_service.h`
  - updated API signature to include output capacity

### Transport / platform decoupling

- `Code/Source/Comm.h`
  - added `CommPlatformOps` for critical section control, timing, RS485 direction control, and event hooks
  - added `CommPortConfig` for UART, GPIO, IRQ, and baud-rate configuration
- `Code/Source/Comm.c`
  - changed transport code to consume board services through `CommPlatformOps`
  - moved port-specific UART/pin settings into `CommPortConfig`
  - isolated default STM32F0 behavior in a set of local default callbacks
- `Code/Source/modbus_proto.h`
  - added a protocol-only header for Modbus parser constants
- `Code/Source/modbus_rtu_parser.h`
  - removed direct dependency on `Sci_Upper.h`
  - switched parser buffer sizing and function-code checks to protocol-only constants
- `Code/Source/modbus_rtu_parser.c`
  - updated parsing logic to use the new protocol-only constants
- `Code/Source/Sci_Upper.h`
  - added the explicit base-type include required for standalone compilation
  - included `modbus_proto.h` so legacy Modbus symbols remain available

## Linker Result Progress

Observed progression during this fix:

- before RAM optimization: many `L6406E` / `L6407E` region overflow errors
- after RAM optimization: reduced to a single linker overflow
- latest reported state: `RW_IRAM1 size (8016 bytes) exceeds limit (8000 bytes)`

To close the remaining gap, `COMM_RX_RING_SIZE` was reduced further from `64` to `56`, which should recover the final 16 bytes across two enabled ports.

## Current Status

- compile-time symbol errors are fixed
- communication module source files compile successfully
- Keil full build now succeeds
- latest reported image size:
  - `Code=38508`
  - `RO-data=2576`
  - `RW-data=1260`
  - `ZI-data=6932`
- current build result:
  - `0 Error(s), 47 Warning(s)`

## Notes

- This was primarily a header hygiene and RAM budgeting issue, not a protocol logic regression.
- If RAM pressure returns in future changes, inspect `comm.o(.bss)` first because it is currently one of the largest contributors in `RW_IRAM1`.
- The recent decrease in `Code` size is expected. It comes from removing duplicate data paths, simplifying control flow, and allowing the linker to drop more unused split sections.
- The transport layer is more portable than before, but it is not fully board-agnostic yet. The next clean step would be moving the default STM32F0 platform callbacks out of `Comm.c` into a dedicated `comm_porting_stm32f0.*` pair.
