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
