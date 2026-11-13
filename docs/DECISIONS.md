# Decisions

## 001 — Three projects, not one project per feature

Date: 2026-11-12.

The solution has App, Core and Infrastructure, plus Core.Tests. Logical areas that do not exist yet stay as future namespaces inside those projects. A new project needs a dependency that would otherwise point the wrong way, and a decision record.

## 002 — Plain WPF for the shell and the future overlay

Date: 2026-11-12.

The desktop UI is WPF on `net10.0-windows`. We will not adopt the Arkanis Overlay stack (WPF-hosted Blazor plus WebView2) for the mining overlay. A small status window does not need a browser control, and transparent WebView2 is awkward. Arkanis remains the reference for "a normal WPF process over the game", not for code or UI framework.

Revisit this only if a later screen genuinely needs HTML.

## 003 — External process only

Date: 2026-11-12.

The program is a separate Windows process. Future capture uses Windows capture APIs on a region the user chooses. Optional later log reading is read-only. The program does not read game memory, inject code, hook the renderer, send input, or write Star Citizen files.

Smart Citizen's overlay release notes describe screen capture plus `Game.log` and no memory access. Its localization editor writes game configuration. We take the overlay boundary and leave the file editing behind. This is not a promise about anti-cheat outcomes. It is the product rule.

## 004 — Game numbers live in data files

Date: 2026-11-12.

Lasers, modules, gadgets and materials will be loaded from versioned files with a source, a retrieval date and a game build. The calculation engine receives those values as arguments. Temporary numbers typed into C# to "try a formula" are not allowed, including inside tests, unless the test cites the source of that specific example.

## 005 — Crackability follows the SCMiner checker

Date: 2026-11-12. Updated the same day after the checker script was read and the maintainer confirmed the basic results in game.

Peacefrog, DCal and UVerse use `mass / (1 - resistance) / 5`. At 0% resistance that is `mass / 5`. SCMiner's checker uses `0.36 × mass` as the requirement and multiplies laser power by `(1 - effectiveResistance / 100)`. The 5,000 kg default on that page requires 1,800 W, not 1,000 W. The engine implements the SCMiner expressions. The other model stays documented in `docs/research/CRACKABILITY.md` and is not selectable in the app.

Crafting is an extension SCMiner does not calculate: a percent on one head multiplies that head's max power before its modules. Charge time uses `optimal_charge_rate` only. `all_charge_rates` is stored and not applied.

## 006 — Repository root is this directory

Date: 2026-11-12.

The Git repository root is `C:\Develop\SCMA`, with `StarCitizenMiningAssistant.sln` at the top. The solution is the classic `.sln` format. .NET 10's default `dotnet new sln` emits `.slnx`; that file was removed so tools and the assignment have one solution.

## 007 — MIT licence

Date: 2026-11-12.

The project is MIT so it can be shared with other Windows users. This licence does not extend to Star Citizen, to Cloud Imperium Games assets, or to source code from other overlays.

## 008 — Logging without an extra package

Date: 2026-11-12.

`IDiagnosticLog` plus a locked append to a text file is enough. Microsoft.Extensions.Logging, Serilog and a local log server can wait until we need filters, sinks or structured queries.

## 009 — Recognition results carry confidence from the start

Date: 2026-11-12.

`ObservedValue<T>` exists before OCR so later code has nowhere convenient to put a bare string. Only `IsConfirmed` authorises a calculation.

## 010 — Wiki items are the equipment catalog, with the duplicates removed

Date: 2026-11-12.

`data/mining-equipment.json` started from the Star Citizen Wiki API for `4.10.2-LIVE.12881860`. The full pull, including records left out of the catalog, is `docs/research/GAME_DATA.md`. Test entities, the MPUV arm that shares the Arbor MH1 name, size 0 lasers, ROC and ATLS GEO lasers, hand lasers, and the two module records with no modifiers are not in the JSON. The catalog heads are the size 1 and size 2 lasers for the Golem, Prospector, and MOLE.

## 011 — SCMiner wins a conflict with the wiki

Date: 2026-11-12.

The maintainer checked SCMiner in game and rejected the wiki scan signatures. The catalog signature, instability, resistance, and deposit ladder for each material come from the SCMiner mineral table retrieved the same day. Wiki signature numbers are not stored.

Where the public SCMiner module and gadget list differs from the wiki `modifier_map`, the JSON stores the SCMiner number. Surge resistance is −16, not the wiki −15.5. SCMiner splits laser `power` from `extractPower`. Only `power` enters the crack formula. `extractPowerPercent` is stored and does not change the yes or no. The replaced fields are listed in `docs/WORK_REPORT.md`.

Head laser power stayed on the wiki `mining_laser.laser_power` values. Those match the SCMiner Helix I page at 780–3900 W, and no second SCMiner head table was found that disagreed. Klein-S1 slots stay 0, which is the SCMiner page value for a null wiki field.

## 012 — Windows OCR stays Uncertain

Date: 2026-11-13.

Text is read with Windows.Media.Ocr. That API is part of Windows, so no OCR package was added. The engine returns words and does not return a confidence. The data model says an OCR value stays Uncertain or Missing until a field-specific rule in a decision promotes it. This record does not add that rule. A blank image is a failure. A blank line is dropped. Any text the engine returns is Uncertain and must not feed the crack calculator.

Mass, resistance and instability are chosen only by those words on a line. A number keeps the confidence of that line. An unlabelled number is not assigned to a field. The number is parsed with the Windows culture, then the invariant culture if that fails.

## 013 — One catalog deposit confirms a signature

Date: 2026-11-13.

Decision 012 still leaves every OCR line Uncertain. A signature is a separate field. `SignatureReading` parses the digit lines. When those lines hold one number and `MatchSignature` returns exactly one deposit, that signature becomes Confirmed. The overlay may then print the deposit count and the mineral, blink a ticked mineral, and request the sound. Zero deposits, two deposits, or two different numbers stay Uncertain. No number stays Missing. Typing in the signature box is still manual entry, which is Confirmed.

This rule does not promote mass, resistance, or instability. A confirmed signature does not enter the crack check.

## 014 — A missing sample keeps the last reading

Date: 2026-11-13.

While the overlay is open, the signature rectangle and the rock rectangle are captured about twice a second. `ReadingHold` keeps the previous field when the new sample is Missing. A sample that contains a number replaces that field. An uncertain number stays uncertain. Several dark frames in a row keep the last number. Closing the overlay stops the capture. This rule does not send input to the game.

## 015 — Two identical rock reads are confirmed

Date: 2026-11-13.

Mass, resistance, and instability still come only from a line that contains that word. Decision 012 leaves one such line Uncertain. `StableReading` compares the new sample with the previous sample of that same field. When the two numbers agree, the field becomes Confirmed and can enter the crack check. One sample stays Uncertain, so the verdict stays Unknown until mass and resistance have each agreed once. A missing sample does not count as a disagreement and does not confirm the field. A number typed by the user stays Confirmed when the first sample matches it. A different number replaces it and stays Uncertain until the next sample agrees.

The rock image is scaled up to three times, without going past 2000 pixels on a side, before Windows OCR sees it. No OCR package is added, and the old buffer cast is not used. Signature digits keep their own reader.

## 016 — The overlay can sit above the game

Date: 2026-11-13.

The overlay window is topmost and drawn at 94 percent opacity so a borderless game can show behind it. The assistant window has a checkbox, Clicks pass through the overlay. While that box is ticked, the overlay's extended style includes WS_EX_TRANSPARENT, so the mouse hit test skips this window. The program does not call SendInput and does not synthesize a click. Untick the box to move or close the overlay. The choice is saved in the loadout file and starts unticked. The rectangle-placement window is unchanged: it is topmost only while a rectangle is being dragged.
