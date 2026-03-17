# Debug Checklist

## Repo-Specific Artifacts

Inspect these first when the task is about build or debug failures:

- `CommomSH367309_16series_030C8T6_C.uvprojx`
- `CommomSH367309_16series_030C8T6_C.uvoptx`
- `DebugConfig/Target_1_STM32F030C8.dbgconf`
- `Code/Drivers/startup_stm32f0xx.s`
- `Code/Source/Flash.c`
- `JLinkLog.txt`

## Known Boot and Flash Contracts

- The app uses backup registers for boot flags.
- `IapRequest_ArmLegacyFlag()` exists because shipped boards still rely on a legacy Flash flag handshake.
- Vector remap is handled in the app-side initialization when `_IAP` is enabled.
- Reset-triggered update flow depends on `u8FlashUpdateFlag` and `MCU_RESET()`.

## Failure Split

- Build fails: inspect project config, generated object list, include paths, and linker artifacts.
- Download fails: inspect J-Link settings, reset behavior, and debugger configuration before changing firmware.
- Debug cannot start without manual reset: inspect remap, RCC/SYSCFG init, startup code, and reset loops.
- Boot handoff fails: inspect flag write, flag clear, and application address assumptions.

## Change Discipline

- Avoid changing boot flags, remap, or reset logic together in one step unless required.
- If bootloader compatibility is uncertain, preserve the old handshake path and add comments.
- After changing boot flow, document the exact state machine in a repo markdown file.
