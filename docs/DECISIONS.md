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

## 005 — No crackability formula in 0.1.0

Date: 2026-11-12.

Public calculators do not yet give one verified model. Peacefrog, DCal and UVerse publish examples that match `required power = mass / (1 - resistance) / 5` when resistance is a fraction. A mort13 write-up publishes `0.2 * mass * (1 + resistance/100)`, which matches the first formula only at 0% resistance. An SCMINER page snippet for 5,000 kg at 0% resistance shows a 1,800 threshold, which matches neither (mass / 5 would be 1,000). Details and links are in `docs/research/CRACKABILITY.md`.

The engine stays unwritten until milestone 2 locks sourced examples.

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
