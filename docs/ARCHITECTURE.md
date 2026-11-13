# Architecture

Version 0.1.0. This is the structure to follow until a new decision is recorded in `docs/DECISIONS.md`.

## Why three projects

The assignment lists many logical components. A solo developer does not need an empty project for each of them. They start as namespaces and move into their own project only when the dependency boundary is real.

| Logical component | Home in 0.1.0 | Later home |
| --- | --- | --- |
| Desktop window | App | App |
| Overlay window | not built | App, separate window type |
| Application services / composition | App startup | App |
| Screen capture | not built | Infrastructure |
| OCR and image matching | not built | Infrastructure, behind a Core interface |
| Recognition results | `Core.Recognition` | Core |
| Mining data | `data/mining-equipment.json`, parsed in Core from a string | refresh stays a versioned file; App reads it |
| Equipment profiles | not built | Core model, Infrastructure persistence |
| Crackability | `Core.Calculation` | Core, pure functions |
| Recommendations | not built | Core, uses the crackability engine and the user's available equipment |
| Settings | not built | Infrastructure |
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

`net10.0-windows`, WPF. The window opens on Golem, Prospector, and Mole. The chosen ship moves to the top. A single-laser ship shows one head and its module slots. The Mole shows its three turrets side by side, and extra turrets start switched off. One gadget sits under the turrets. Each turret lists the combined catalog stats of its head, craft percent and fitted modules: power, minimum power, resistance, instability, optimal charge rate, all charge rates, charge-window size, overcharge rate, shatter, inert materials, extract power, and optimal and maximum range. Extract power stays separate from laser power. The open head, module and gadget lists show that item's own catalog numbers, including an active module's charges and duration. A higher power, charge rate, window, or extract figure is green. A lower resistance or instability figure is green. A left menu switches between ship selection, the loadout, and the signature list. Save configuration stores the setup in `%LocalAppData%\StarCitizenMiningAssistant\loadout.json` and opens the mineral list. That list is the catalog's deposit signatures. A tick marks a mineral for the future overlay. Clicking a name shows the numbers, and shift-click copies them. The crack calculator stays in Core. The window does not ask for a rock. The window reads `data/mining-equipment.json` from the output folder.

The overlay, when it exists, will be another window in this project: a separate topmost window owned by this process. It will not be a browser host and it will not be injected into Star Citizen. That choice is recorded in decision 002. Arkanis Overlay demonstrates that a separate WPF process can sit over the game; we are not using its Blazor/WebView2 stack, because a compact status overlay does not need a web view and transparent WebView2 is a known source of pain.

### StarCitizenMiningAssistant.Core

`net10.0`. Currently:

- `ApplicationInfo` — name and version read from the assembly.
- `ObservedValue<T>` and `RecognitionConfidence` — the rule that missing and uncertain data cannot be used as facts.
- `IDiagnosticLog` — the logging port. Core does not open files.
- `Equipment.MiningCatalog` — ships, heads, modules, and gadgets parsed from a JSON string.
- `Calculation.CrackabilityCalculator` — the SCMiner checker, including craft percent and the gadget / active-module comparison.

Future namespaces, added only with the milestone that needs them:

- `Mining` — rock scan model.
- `Recommendation` — search over equipment the user marked as available.

### StarCitizenMiningAssistant.Infrastructure

`net10.0`. `FileDiagnosticLog` appends lines to `%LocalAppData%\StarCitizenMiningAssistant\logs\app.log`. If that folder cannot be created, startup uses `NullDiagnosticLog` and the window still opens.

Capture and persistence will land here, implementing interfaces defined in Core when those interfaces exist.

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
