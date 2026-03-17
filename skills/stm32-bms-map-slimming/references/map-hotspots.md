# Map Hotspots

## Current Repo Pattern

This repo already contains map-oriented analysis in project documents. Reuse that context instead of restarting from zero.

Known historical hotspots include:

- `iodrivers.o(i.MosCtrl_SameDoor_NoPreChg)`
- `ascii_slave.o(i.Ascii_BuildAnalogData)`
- `socenhance.o(i.CorrectionTerminal_CV)`
- `sh367309_datadeal.o(i.Refresh_Parameters)`
- `sh367309_func.o(i.Fault_ChangeToMCU)`
- `io_control.o(i.RefreshData_Drivers)`
- `sleepdeal.o(...)`
- `system_init.o(i.InitIO)`

## Low-Risk Patterns Seen In This Repo

- Remove duplicate protocol packing helpers.
- Write payload bytes directly when helper layers add more overhead than value.
- Merge communication buffers only when RX/TX lifetimes are provably non-overlapping.
- Remove duplicate communication state fields when a single source of truth is enough.

## High-Risk Areas

- SOC correction logic
- Sleep state machines
- MOS or relay drive logic
- Fault state translation paths
- Startup and initialization sequences

## Review Questions

- Is the hotspot in code size, read-only data, read-write data, or zero-init data?
- Is the optimization saving bytes by deleting duplication, or by changing behavior?
- Is the measured savings worth the regression risk?
- Does the change make future protocol work harder to reason about?
