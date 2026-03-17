# Protocol Context

## Scope

This repo is an STM32F0 BMS firmware with a split communication stack:

- `Comm.c/.h`: common port context, protocol auto-detection, shared TX/RX flow
- `modbus_rtu_parser.c/.h`: RTU frame parsing
- `modbus_service.c/.h`: dispatch bridge into legacy register service
- `Sci_Upper.c/.h`: register semantics and existing business logic
- `ascii_slave.c/.h`: ASCII frame parsing and response generation
- `bms_comm_data_adapter.c/.h`: convert internal BMS data into PYLON-facing payloads

## Expected Architecture

- Keep interrupts limited to byte movement, error cleanup, and simple state changes.
- Keep protocol parsing in the main loop.
- Keep register mapping in the legacy service layer unless a deliberate migration is being done.
- Keep protocol payload building separate from raw BMS data ownership.

## Data and Unit Traps

- Internal values and protocol values do not share the same units.
- `u16VCellTotle` is stored in `0.01V` and exported as `mV` in the current PYLON mapping.
- Current uses signed semantics at the protocol boundary even if the source is split into charge/discharge fields internally.
- Temperature encoding uses absolute temperature at the protocol side for PYLON-related paths.
- Unsupported fields are sometimes encoded as `0xFFFF` and must not be silently replaced with guessed values.

## Validation Focus

- RTU function coverage: `0x03`, `0x06`, `0x10`
- ASCII command coverage: `0x60`, `0x61`, `0x62`, `0x63`
- Port switching: one port should recover cleanly when protocol type changes between frames
- Error paths: invalid address, checksum/CRC failure, timeout, and half-frame cleanup
- Side effects: EEPROM and Flash actions that are intentionally deferred until response handling completes
