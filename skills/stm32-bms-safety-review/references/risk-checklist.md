# Risk Checklist

## Highest-Risk Change Categories

- Charge or discharge enable logic
- Fault level mapping or clearing rules
- Sleep, hiccup, forced wake, and wake-source logic
- EEPROM default repair, write-back, and delayed persistence
- Flash update flags and IAP handoff
- Protocol writes that mutate persistent configuration

## Common Regression Patterns In This Repo

- Wrong unit conversion after protocol or calibration changes
- Restoring defaults to the wrong EEPROM index
- Infinite retry in startup or AFE initialization
- Invalid configuration accepted into RAM and only failing later
- Shared buffer or shared state corruption between IRQ and main loop
- Size optimization that weakens readability in already fragile state logic

## Review Questions

- What external hardware behavior changes if this branch executes?
- What happens on invalid input, zero input, and power-loss timing?
- Is the failure fail-safe, fail-open, or undefined?
- Does the code preserve prior bootloader or field-device compatibility?
- Is the validation evidence stronger than "build passes"?
