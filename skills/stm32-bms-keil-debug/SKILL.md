---
name: stm32-bms-keil-debug
description: Diagnose build, startup, download, debug, and boot handoff issues for this Keil-based STM32F0 BMS firmware. Use when the task involves `.uvprojx`, `.uvoptx`, `DebugConfig`, startup files, vector remap, IAP/app switching, Flash update flags, J-Link behavior, or symptoms such as "cannot download", "cannot debug", "stuck after reset", or "bootloader/app handoff fails".
---

# STM32 BMS Keil Debug

Use this skill for toolchain and boot-path problems that are specific to this repo's Keil and STM32F0 setup.

Read [references/debug-checklist.md](references/debug-checklist.md) before changing startup or boot code.

## Workflow

1. Separate the failure phase.
- Build-time: project file, include path, object generation, linker output.
- Download-time: J-Link, reset behavior, debugger config.
- Early boot: vector remap, startup assembly, IAP entry, watchdog/reset loop.
- Runtime handoff: backup register flags, Flash marker, app reboot sequence.

2. Prefer inspecting the current repo artifacts before proposing fixes.
- Read `Flash.c`, startup files, debug config, and recent logs first.
- If a previous repo document already explains the symptom, build on it.

3. Preserve boot compatibility.
- `IapRequest_ArmLegacyFlag()` exists for shipped boards. Do not remove this path casually.
- Treat backup-register flags and Flash flags as external contracts with the bootloader.

4. When touching reset or remap code, explain the exact before/after execution path.

## Required Checks

- Confirm whether the failure happens before or after `main()`.
- If Flash update or boot flags are involved, verify both write path and clear path.
- If vector remap is involved, verify address assumptions and interrupt table length.
- If the issue is debugger-only, avoid firmware changes until the tool configuration has been inspected.

## Output Expectations

- State the failing phase.
- Name the exact files and symbols involved.
- Separate tool configuration fixes from firmware fixes.
- If the change modifies boot behavior, add a short repo document describing the boot contract.

## References

- [references/debug-checklist.md](references/debug-checklist.md)
