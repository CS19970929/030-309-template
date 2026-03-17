---
name: stm32-bms-map-slimming
description: Analyze firmware size and prioritize safe reductions for this STM32F0 BMS project. Use when inspecting `.map` files, Keil build output, large protocol code, RAM pressure in communication contexts, or when a user asks to slim code size without destabilizing protection, sleep, SOC, or MOS control logic.
---

# STM32 BMS Map Slimming

Use this skill when the task is not just "make it smaller", but "make it smaller without breaking a safety-oriented firmware".

Read [references/map-hotspots.md](references/map-hotspots.md) first.

## Workflow

1. Start from evidence, not intuition.
- Read the current `.map` file and build summary.
- Identify the largest functions and the largest object files.
- Separate code-size pressure from data-size pressure.

2. Rank hotspots by regression risk.
- Prefer protocol packing helpers, duplicated buffer logic, and local helper inlining/folding.
- Be cautious with SOC correction, sleep state machines, MOS control, fault transitions, and startup paths.

3. State the expected savings and the reason they are plausible.
- Mention the affected function or object names.
- Explain whether the savings come from removing duplication, shrinking buffers, reducing call overhead, or deleting dead code.

4. Rebuild and compare artifacts after each meaningful change.
- Report code/data/BSS deltas if available.
- If only source-level reasoning is possible, say so explicitly.

## Decision Rules

- Do not chase single-digit byte savings in high-coupling safety logic unless there is no safer alternative.
- Prefer reversible, localized changes over cross-module rewrites.
- If the repo already contains a size-analysis document, update it instead of creating another parallel narrative.

## Output Expectations

- List top hotspots first.
- Separate low-risk, medium-risk, and high-risk optimization options.
- For each accepted change, note the validation method and the remaining uncertainty.

## References

- [references/map-hotspots.md](references/map-hotspots.md)
