# Project Skills Overview

This directory contains project-specific skills for the `030 + 309` STM32 BMS firmware repo.

## Skills

- `stm32-bms-protocol-adapter`: for Modbus RTU, ASCII, PYLON payloads, protocol auto-detect, and communication refactors
- `stm32-bms-map-slimming`: for `.map` analysis and low-risk firmware size reduction
- `stm32-bms-keil-debug`: for Keil, J-Link, startup, IAP, boot flags, and debug failures
- `stm32-bms-safety-review`: for review tasks focused on protection, persistence, startup, and behavioral regressions

## Why These Four

These match the repeated task shapes already visible in the repo:

- protocol refactor and adapter work
- map-based code slimming
- Keil and startup debugging
- safety-oriented regression review

## Suggested Invocation Examples

- `Use $stm32-bms-protocol-adapter to add a new ASCII command without breaking RTU handling.`
- `Use $stm32-bms-map-slimming to reduce code size by 1 KB with low regression risk.`
- `Use $stm32-bms-keil-debug to diagnose why the board only downloads after manual reset.`
- `Use $stm32-bms-safety-review to review this diff for EEPROM and wakeup regressions.`
