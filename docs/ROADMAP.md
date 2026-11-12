# Roadmap

Work in this order. Finish and test a milestone before starting the next one. A milestone can be split into several small changes.

## 1. Foundation — done in 0.1.0

Solution, three projects, xUnit, documentation, foundation window, file log, recognition-confidence type.

## 2. Verified crackability model — manual check is in the app

The window calls `CrackabilityCalculator`. Tests cover the SCMiner page examples, stacking, Surge at −16, Sabir, both together, craft percent, and an unconfirmed mass. OCR and the overlay still wait. Another calculator agreeing with these examples is still open; Peacefrog does not agree and is not a second source for this formula.

The working formula is the SCMiner checker, recorded in `docs/research/CRACKABILITY.md`. The equipment catalog is `data/mining-equipment.json`, with the raw pull and the gaps in `docs/research/GAME_DATA.md`.

Do this before any scanner or overlay work.

1. Collect worked examples from independent public calculators, using the same mass, resistance, instability, laser, modules, gadget and crafting inputs.
2. Record each example in `docs/research/CRACKABILITY.md` with the source, the date checked, and the Star Citizen patch the source claims.
3. Separate three questions that must not be collapsed into one number:
   - Is there enough laser power to fracture the rock?
   - How do resistance modifiers from lasers, modules, gadgets and crafting combine?
   - How unstable is the charge, and what does that do to the safe window? Instability is not a substitute for the power check.
4. Record module limits that change the answer: passive versus active, charges, one gadget on a rock, optimal distance, and multiple turrets on one rock.
5. When at least two independent sources agree on a worked example, encode that example as an xUnit fixture and implement only the part the fixture covers.
6. A formula disagreement stays unimplemented until a worked example is verified. An equipment-number disagreement uses the SCMiner value, decision 011. Do not pick a formula because it looks familiar.

The manual check window calls this engine. OCR and the overlay still wait until these tests pass.

## 3. Equipment configuration and profiles

Ships, turrets, laser heads, module slots, gadgets and crafted bonuses as data. Each turret has its own loadout. The user marks which parts they own. Profiles are saved locally.

Equipment numbers come from a versioned data file, not from literals in the calculator. Preferred sources are the Star Citizen Wiki API and `scunpacked-data`, with the dump's game build recorded. Confirm the licence and terms of any third-party API before calling it.

## 4. Calculation engine

Pure Core service. Inputs are a confirmed rock scan plus one or more turret loadouts. Outputs, per turret and for selected turrets together:

- enough power, or not enough, or unknown because a required input is missing or the formula is unverified
- power required, power available, and the margin
- adjusted resistance and instability, each with the modifiers that produced them
- warnings for active-module charges, gadget conflicts and unstable charge windows

No UI references. Tests from milestone 2 must pass unchanged or be updated in the same change with the reason recorded.

## 5. Recommendation engine

Search only equipment the user marked as available. Rank by a stated preference: fewest changes from the current loadout, or largest safety margin. Return which single turret can crack the rock, whether several turrets must work together, and which gadget or module is required. Never recommend an item the user does not have.

## 6. Screen capture

Capture a user-chosen region of the desktop with a documented Windows capture API. Save diagnostic images locally when the user asks. No game-process hooks.

## 7. OCR and image recognition

Read the mining panel with OCR plus template or anchor checks where the UI is stable. Every field is an `ObservedValue<T>`. A low-confidence read stays `Uncertain`.

## 8. Mining scan parsing

Turn recognised text into a rock scan: materials and percentages, mass, resistance, instability, signature, deposit count. Validate ranges. Reject a parse that contradicts itself instead of guessing.

## 9. Overlay

A second, compact WPF window: position, size, opacity, font size, colour for crackable / not crackable / unknown, collapsed and expanded layouts, a hotkey, and click-through when the user wants it. Test it with Star Citizen in borderless and Vulkan before calling it compatible. Unknown stays visible as unknown.

## 10. Wire recognition to calculation

The overlay calls the engine only with confirmed fields. Uncertain fields are shown as uncertain and the verdict is "unknown", not "crackable".

## 11. Settings, persistence and usability

Profiles, overlay layout, capture region and data-file updates survive a restart. Settings live under LocalAppData.

## 12. Compatibility pass

Manual checklist against a named Star Citizen patch: borderless window, overlay visibility, no interaction with the game, log file remains readable, capture region still matches the mining panel.

## 13. Packaging

A per-user installer or a portable build for other Windows machines. No admin rights required for normal use.

## Explicitly out of scope

Memory reading, injection, renderer hooks, input automation, editing game files, and copying another project's source.
