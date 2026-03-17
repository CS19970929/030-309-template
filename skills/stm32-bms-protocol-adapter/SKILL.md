---
name: stm32-bms-protocol-adapter
description: Maintain and extend the communication stack for this STM32F0 + SH367309 BMS firmware. Use when working on Modbus RTU, ASCII/PYLON frames, protocol auto-detection, RS485 TX/RX flow, field mapping in `bms_comm_data_adapter.c`, or when a change touches `Comm.c`, `ascii_slave.c`, `modbus_service.c`, `modbus_rtu_parser.c`, `Sci_Upper.c`, or protocol review documents.
---

# STM32 BMS Protocol Adapter

Use this skill to change the protocol path without breaking the existing register service and pack data mapping.

Read [references/protocol-context.md](references/protocol-context.md) before making a non-trivial change.

## Workflow

1. Identify the layer being changed.
- Transport and IRQ path: `Comm.c`, `Comm.h`, ISR glue, RS485 DE/RE switching.
- RTU framing and dispatch: `modbus_rtu_parser.c`, `modbus_service.c`.
- Register semantics: `Sci_Upper.c`, EEPROM/Flash side effects.
- ASCII/PYLON payload: `ascii_slave.c`, `bms_comm_data_adapter.c`.

2. Preserve the current split of responsibilities.
- Keep IRQ handlers thin. Do not move frame parsing or business logic back into interrupts.
- Keep `modbus_service` as a bridge into the legacy register service instead of re-implementing register behavior.
- Keep `bms_comm_data_adapter` as the only place that converts internal BMS data to protocol-specific fields.

3. Treat field units as part of the external contract.
- Verify internal units before editing any encoder.
- Call out every conversion explicitly in code review notes or change docs.
- If protocol docs and code disagree, document the chosen mapping in the repo.

4. Validate on both protocol correctness and firmware behavior.
- Check happy path frames.
- Check invalid address, invalid checksum/CRC, timeout, and half-frame reset behavior.
- Check EEPROM/Flash side effects still happen at the expected point in the response flow.

## Required Checks

- For RTU changes, verify `0x03`, `0x06`, and `0x10`.
- For ASCII changes, verify `0x60`, `0x61`, `0x62`, and `0x63`.
- For protocol auto-detect changes, verify `RTU -> ASCII -> RTU` switching on the same port.
- For unit-mapping changes, state internal unit, protocol unit, scale, sign, and saturation behavior.
- For stateful changes, review timeout reset and residual buffer cleanup.

## Output Expectations

- Summarize which protocol layer changed.
- List the externally visible frame or field changes.
- List firmware side effects or regression risks.
- If the change is substantial, add or update a repo document near the touched area.

## References

- [references/protocol-context.md](references/protocol-context.md)
