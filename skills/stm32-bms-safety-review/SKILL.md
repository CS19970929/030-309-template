---
name: stm32-bms-safety-review
description: Review changes in this STM32 BMS firmware for behavioral regressions and safety risk. Use when a task touches protection thresholds, fault propagation, sleep logic, EEPROM persistence, Flash/IAP flow, communication side effects, startup sequencing, or any code that can change charging, discharging, wakeup, or fault handling behavior.
---

# STM32 BMS Safety Review

Use this skill for review-style tasks where the main goal is to find bugs, unsafe assumptions, and missing validation.

Read [references/risk-checklist.md](references/risk-checklist.md) before reviewing a diff.

## Review Order

1. Find user-visible or hardware-visible behavior changes first.
- Charge/discharge gating.
- Fault escalation and clearing.
- Sleep/wakeup decisions.
- EEPROM/Flash persistence and reset behavior.

2. Then inspect integrity risks.
- Invalid defaults.
- Unit conversion mistakes.
- Partial-write or delayed-write side effects.
- Infinite retry or boot-stall conditions.
- Shared-state races between IRQ and main loop.

3. Then inspect validation coverage.
- What exact scenario proves this change?
- What bad input or timeout path is still untested?
- Is there an existing repo document that should be updated to capture the new contract?

## Review Output Format

- Put findings first, ordered by severity.
- Reference concrete files and line numbers when available.
- Keep summary short and secondary.
- If there are no findings, say that explicitly and mention residual validation gaps.

## Required Checks

- Persistence: boot defaults, EEPROM repair-back, Flash write timing, reset flags.
- Safety state machines: sleep, forced wake, hiccup, protection latching.
- Protocol side effects: command acceptance, invalid input rejection, deferred persistence.
- Startup robustness: bounded retry, failure exit path, watchdog or reset loops.

## References

- [references/risk-checklist.md](references/risk-checklist.md)
