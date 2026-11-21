# Architecture

Version 0.1.0. This is the structure to follow until a new decision is recorded in `docs/DECISIONS.md`.

## Why three projects

The assignment lists many logical components. A solo developer does not need an empty project for each of them. They start as namespaces and move into their own project only when the dependency boundary is real.

| Logical component | Home in 0.1.0 | Later home |
| --- | --- | --- |
| Desktop window | App | App |
| Overlay window | App, separate window | App |
| Application services / composition | App startup | App |
| Screen capture | Infrastructure still of a user rectangle | Infrastructure |
| OCR and image matching | Infrastructure `WindowsOcrReader`, behind `IScreenReader` | Infrastructure |
| Recognition results | `Core.Recognition` | Core |
| Mining data | `data/mining-equipment.json`, parsed in Core from a string | refresh stays a versioned file; App reads it |
| Aaron Halo Band 5 routes | `data/aaron-halo-band5.json`, parsed in Core from a string | the file is BandHopper's precomputed Band 5 table |
| Equipment profiles | not built | Core model, Infrastructure persistence |
| Crackability | `Core.Calculation` | Core, pure functions |
| Recommendations | not built | Core, uses the crackability engine and the user's available equipment |
| Settings | loadout file, edited on the Settings page | same file |
| Logging | `IDiagnosticLog` in Core, file log in Infrastructure | same |
| Tests | `Core.Tests` | one test project per production project that gains behaviour |

## Dependency direction

```
App
  -> Core
  -> Infrastructure
Infrastructure
  -> Core
Core
  -> (base class library only)
Core.Tests
  -> Core
```

Core has no reference to WPF, Windows Forms, screen-capture APIs, or the file system. It parses catalog JSON only when the caller passes the text in. A calculation test must run with `dotnet test` and no window.

The WPF `App` class is the composition root. It constructs the log and the main window. Window event handlers stay thin: they call a Core or application service, they do not contain mining rules.

## Projects

### StarCitizenMiningAssistant.App

`net10.0-windows10.0.19041.0`, WPF. The window opens on Golem, Prospector, and Mole. The chosen ship moves to the top. A single-laser ship shows one head and its module slots. The Mole shows its three turrets side by side, and extra turrets start switched off. One gadget sits under the turrets. Each turret lists the combined catalog stats of its head, craft percent and fitted modules: power, minimum power, resistance, instability, optimal charge rate, all charge rates, charge-window size, overcharge rate, shatter, inert materials, extract power, and optimal and maximum range. Extract power stays separate from laser power. The open head, module and gadget lists show that item's own catalog numbers, including an active module's charges and duration. A higher power, charge rate, window, or extract figure is green. A lower resistance or instability figure is green. A left menu switches between ship selection, the loadout, and the signature list. The loadout page can store several named setups in `%LocalAppData%\StarCitizenMiningAssistant\loadout.json`. Saving under a new name leaves the other names unchanged. The screen that was open is restored on the next start, including an older file that had only one setup. Save configuration stores that file and opens the mineral list. That list is the catalog's deposit signatures. A tick marks a mineral for the future overlay. Clicking a name shows the numbers, and shift-click copies them. The crack calculator stays in Core. A second window, opened from the Overlay menu, lists each saved loadout as Crackable, Difficult, Uncrackable, or Unknown. Its position and size are stored in the same loadout file. The main window does not ask for a rock. The overlay does. A rock-rectangle reading keeps the confidence from that scan: only a confirmed mass and resistance change the verdict, and an uncertain scan stays Unknown. Typing over a box makes that field manual again. The Settings page holds click-through, the scan log, the monitor picker, and the signature and rock stills. Scan areas stores one signature rectangle and one rock rectangle per monitor in that same file. Show still copies those pixels into a bitmap under `%LocalAppData%\StarCitizenMiningAssistant\captures`. The window reads `data/mining-equipment.json` from the output folder.

The rock overlay and the signature overlay are two windows in this project, both owned by the main window. Neither is a browser host and neither is injected into Star Citizen. That choice is recorded in decision 002. Arkanis Overlay demonstrates that a separate WPF process can sit over the game; we are not using its Blazor/WebView2 stack, because a compact status overlay does not need a web view and transparent WebView2 is a known source of pain. Both are topmost. The rock window is a transparent layered window: a solid rounded title and a background that fades toward the sides and the bottom. Its opacity slider is stored in the loadout file and starts at 94 percent. The signature window is only the confirmed reading, such as 2x Stileron, with its own font, color, opacity, and position in that same file. A positioning preview draws a green outline and sample text, and it stays clickable while the click-through box is ticked. Locking the signature window hides that preview and lets clicks pass through when the box is ticked. The rock window still has its own click-through flag. The watch that reads the screen still runs only while the rock overlay is open. Decision 016 and decision 024.

### StarCitizenMiningAssistant.Core

`net10.0`. Currently:

- `ApplicationInfo` — name and version read from the assembly.
- `ObservedValue<T>` and `RecognitionConfidence` — the rule that missing and uncertain data cannot be used as facts.
- `IDiagnosticLog` — the logging port. Core does not open files.
- `Equipment.MiningCatalog` — ships, heads, modules, and gadgets parsed from a JSON string.
- `Equipment.MiningItemCatalog` — shop rows for mining items and vehicles, parsed from `data/mining-items.json`. Item shops are the CStone tables. Vehicle shops stay the stored UEX reports. Search matches the name or the category and the card shows the four cheapest buys. The item page lists every stored shop.
- `Refining.RefineryCalculator` and `Refining.CommodityPricing` — refined SCU and sell totals from `data/refinery-data.json`. An unconfirmed composition row is not priced. OCR composition stays uncertain.
- `Calculation.CrackabilityCalculator` — the SCMiner checker, including craft percent and the gadget / active-module comparison.
- `Calculation.LoadoutComparison` — one confirmed rock against each saved loadout. The row is Crackable, Difficult, Uncrackable, or Unknown, and it names the boost that the crack needs.
- `Capture.CaptureRectangle` — a signature rectangle and a rock rectangle, stored per monitor. The live rock scan does not use the rectangle. Core does not capture pixels.
- `Recognition.IScreenReader` — the OCR port. `OcrDocument` keeps every returned line Uncertain. `RockOcrFields` assigns a number to mass, resistance, or instability only when that word is on the line, and it does not raise the confidence. A panel is present when any of those three numbers was read. A composition share and the total SCU stay uncertain. Decision 022 and 026.
- `Recognition.ScanPatch` — the fixed signature patch under the radar, as a fraction of the chosen monitor. A miss can shift it one step, and a step that leaves the monitor is skipped. Decision 021.
- `Recognition.RockPatch` — the fixed scan-results patch. A miss shifts it one step, the same step as the signature. Decision 022.
- `Recognition.CompositionPatch` — the share list under that panel, same column, read with the scan-results panel. A parsed share is written into the overlay. A miss uses the same step. Decision 026.
- `Recognition.RockDigitReader` — the pale-green mass, resistance, and instability digits. A decimal stays a decimal and a trailing percent sign is not a digit. A diagonal stroke through a zero stays 0. `RockPanel` keeps the longer of the label's number and the digit shapes, and leaves a shorter fragment blank when the row had more digits.
- `Recognition.SignatureAim` — remembers the shift that last returned a number and tries it first. Decision 021.
- `Recognition.HudSignatureReader` — reads that patch by matching glyphs. A comma before the last three digits is required. A tilted pill is turned level before the digits are read, and a slanted row is turned flat once when the pill is not found. Decision 021.
- `Recognition.ScanSignatureChart` — `data/scan-signatures.json`. One match confirms the signature. Several matches stay uncertain.
- `Recognition.RepeatedReading` — while the overlay watches, a new signature is kept only after two identical frames. Decision 021.
- `Recognition.SignatureReading` — one digit line becomes a confirmed signature when the chart has one match, or when the chart has none and the catalog has one deposit. Decision 013 and decision 021. Any other reading stays Uncertain or Missing.
- `Recognition.ReadingHold` — a missing sample keeps the previous signature or rock field. A sample that contains a number replaces it. Decision 014. The overlay timer in the App calls this about twice a second and stops when the overlay closes.
- `Recognition.ScanLogPolicy` — while Log scans is ticked, a placed rectangle is written to `scan-log` with the reading from that frame. A repeat waits. A rock line lists mass, resistance, and instability only. Decision 019 and decision 022.
- `Recognition.StableReading` — two identical labelled samples confirm mass, resistance, or instability. One sample stays Uncertain. Decision 015. `WindowsOcrReader.ReadLabelsAsync` scales the rock image up to three times before Windows OCR. The live signature path uses `HudSignatureReader`. Decision 021. `SignatureOcr` remains and is not the live path.
- `Recognition.SignatureWatch` — a confirmed signature against the catalog. The hit is labelled with the deposit count, such as 2 × Bexalite. A ticked mineral blinks. Sound is requested only when the user enabled it. An uncertain signature produces no hit.
- `Recognition.RockScan` — keeps a rock field at the confidence it was saved with. An uncertain scan is not a confirmed mass or resistance.
- `Recognition.HudNumberReader` — the earlier bright-core digit rules. Signature capture no longer uses it. Decision 018.
- `Recognition.SignatureDigitStrip` — finds the bright digit row in a signature rectangle and leaves out the taller location pin. Ship color is not used. A glow that joins the digits into one wide mark is read from the bright core, so an open 4 stays open. A blank drawing can be filled from that core when the number is one catalog deposit.
- `Recognition.SignatureDigits` — keeps a signature number only when two readings of that frame agree. A disagreement is an empty sample. Decision 018.

Future namespaces, added only with the milestone that needs them:

- `Mining` — rock scan model.
- `Recommendation` — search over equipment the user marked as available.

### StarCitizenMiningAssistant.Infrastructure

`net10.0-windows10.0.19041.0`. `FileDiagnosticLog` appends lines to `%LocalAppData%\StarCitizenMiningAssistant\logs\app.log`. If that folder cannot be created, startup uses `NullDiagnosticLog` and the window still opens. `DisplayMonitors` lists screens. `RegionImage` copies one rectangle with `BitBlt` and can save that bitmap beside the log. `WindowsOcrReader` implements `IScreenReader` with Windows.Media.Ocr. The live signature capture is the radar patch, read by `HudSignatureReader`. A miss shifts that patch one step, and a tilted pill is turned level before the digits are read. `SignatureOcr` remains and is not the live path. It does not hook the game. An OCR line stays Uncertain, per decision 012. A signature that matches one catalog deposit is confirmed by decision 013.

### StarCitizenMiningAssistant.Core.Tests

xUnit. Tests the confidence rule, the catalog parse, and the crackability fixtures. New calculation behaviour needs tests in this project before any UI calls it.

## External compatibility

Two public companions were reviewed for the safety boundary, not copied:

- [Arkanis Overlay](https://github.com/ArkanisCorporation/ArkanisOverlay) is a separate .NET WPF application (WPF-hosted Blazor and WebView2). Its published philosophy excludes cheats and automation. License: PolyForm Noncommercial 1.0.0. We do not reuse its code.
- [Smart Citizen](https://github.com/Osiris-DevWorks/smart-citizen) documents an optional overlay that captures the screen and reads `Game.log`, and states that the overlay does not touch game memory or send input. The same application also edits localization files. This project will not edit game files. The mining-signature overlay is a useful later reference for "read the screen, show a result", which is a different feature from crackability.

No third-party write-up is a guarantee about Easy Anti-Cheat. The practical rule for this product is the one above: stay a normal Windows process, capture the desktop or a window the user selected, and do not hook the game.

## Data

Equipment statistics live in `data/mining-equipment.json` with a source and a game patch label. The formula lives in `CrackabilityCalculator` and is described in `docs/research/CRACKABILITY.md`. See `docs/DATA_MODEL.md`.

## Testing strategy

- Core behaviour: xUnit, no UI, deterministic inputs.
- Calculation fixtures: one test per sourced worked example, with the source URL and the patch label in the test name or a comment. A fixture is added only after the example has been checked by hand.
- Infrastructure: small tests around files in a temporary directory, when persistence exists.
- App: start the executable and confirm the log line. UI automation is not part of the foundation.
- Recognition: tests must include uncertain and missing cases, not only a clean screenshot.

Run `dotnet test` from the repository root after every change.

## Development order

The order in `docs/ROADMAP.md` is binding. The calculation engine and its tests come before screen capture and before the overlay. OCR output enters the engine only through `ObservedValue<T>`.
