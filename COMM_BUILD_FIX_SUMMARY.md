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

## Linker Result Progress

Observed progression during this fix:

- before RAM optimization: many `L6406E` / `L6407E` region overflow errors
- after RAM optimization: reduced to a single linker overflow
- latest reported state: `RW_IRAM1 size (8016 bytes) exceeds limit (8000 bytes)`

To close the remaining gap, `COMM_RX_RING_SIZE` was reduced further from `64` to `56`, which should recover the final 16 bytes across two enabled ports.

## Current Status

- compile-time symbol errors are fixed
- communication module source files compile successfully
- last change intended to clear the final 16-byte RAM overflow has been applied
- full rebuild verification after the final ring-buffer adjustment still needs to be confirmed in Keil

## Notes

- This was primarily a header hygiene and RAM budgeting issue, not a protocol logic regression.
- If RAM pressure returns in future changes, inspect `comm.o(.bss)` first because it is currently one of the largest contributors in `RW_IRAM1`.
