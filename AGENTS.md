# Instructions for coding agents

These instructions apply to every session that changes this repository, including OpenCode, Cursor and local models. Read them before editing. Then read `docs/ARCHITECTURE.md`, `docs/DECISIONS.md` and `docs/ROADMAP.md`. If the task touches mining numbers or game mechanics, also read `docs/DATA_MODEL.md` and `docs/research/CRACKABILITY.md`.

The human maintainer is technically capable and can run PowerShell, Git and .NET commands. Explain where a command should be run and what a successful result looks like. Do not assume professional C# experience, and do not dump several competing technology choices when one practical choice will do.

## What this product is

A Windows desktop mining assistant for Star Citizen. The most important feature is an accurate answer to whether the user's configured equipment can fracture the scanned rock.

The assistant is external and read-only.

Allowed later:

- Capture of the user's own screen.
- OCR and image matching, with an explicit confidence level on every reading.
- Reading plain-text game logs, read-only, if a future task adds it.
- A separate topmost window drawn by this process.
- Game data downloaded from documented public sources and stored as updateable data files.

Forbidden:

- Reading or writing the game process memory.
- DLL injection, code injection, or hooking DirectX, Vulkan or the game executable.
- Sending keyboard, mouse or controller input to the game.
- Gameplay automation.
- Modifying Star Citizen files, including `user.ini`, `Data.p4k` and the install directory.
- Undocumented private game APIs.
- Copying source code from other projects into this repository. Public projects may be studied for approach and licensing constraints. Arkanis Overlay is PolyForm Noncommercial; do not copy it. Smart Citizen is a separate product; do not copy it.

If a requested change would cross that boundary, stop and ask.

## How to work

1. Read the docs listed above and the code you are about to change.
2. Take one small, verifiable task. Leave unrelated code alone.
3. Keep mining calculations, recognition and recommendation logic out of WPF event handlers. Put them in `StarCitizenMiningAssistant.Core` behind a clear type.
4. Write or update an automated test for behaviour you change in Core.
5. From the repository root, run `dotnet test`. Fix failures caused by your change.
6. Review the diff yourself before handing it back. Check names, nullability, and whether uncertain data can be mistaken for a confirmed fact.
7. Update `docs/ARCHITECTURE.md` and `docs/DECISIONS.md` when the architecture changes. Update `docs/ROADMAP.md` when a milestone moves.
8. Ask before a major architectural change, a new project, a new dependency, system-wide software installation, changes outside this repository, paid services, or a switch away from C# / WPF / .NET 10 / xUnit.

Prefer the smallest design that stays testable. Do not add a package for something the BCL already does. Do not add empty projects "for later".

Never silently remove or replace existing behaviour. If a behaviour must change, say so in the task summary and cover it with a test.

Do not invent Star Citizen statistics, equipment properties, patch numbers or mining formulas. A number from memory is not a source. If the sources disagree, record the disagreement and leave the engine unimplemented until a worked example is verified. Uncertain game data stays in versioned data files with a source, a verification date and a patch label. It is not scattered through C# constants.

## Commands

Run these from the repository root.

```powershell
dotnet restore
dotnet build
dotnet test
```

Success means the process exit code is 0, the build reports `Build succeeded`, and the test summary reports that all tests passed. Do not claim success from a partial or failed run.

The desktop app:

```powershell
dotnet run --project src/StarCitizenMiningAssistant.App
```

## Repository map

- `src/StarCitizenMiningAssistant.App` — WPF shell only.
- `src/StarCitizenMiningAssistant.Core` — domain model and future calculation engines. No WPF, no file system, no screen capture.
- `src/StarCitizenMiningAssistant.Infrastructure` — logging now; capture, OCR adapters and persistence later.
- `tests/StarCitizenMiningAssistant.Core.Tests` — xUnit tests for Core.
- `docs/research` — sourced notes. Notes are not permission to hardcode a formula.

Dependency direction is App → Infrastructure → Core, and App → Core. Tests → Core. Core does not reference the other projects.

## Recognition rule

Every recognised field uses `ObservedValue<T>` and `RecognitionConfidence` (`Missing`, `Uncertain`, `Confirmed`). Only `IsConfirmed` may feed a calculation. An uncertain OCR guess is still uncertain when the text looks plausible.

## Subagents

Use the primary agent as the coordinator. Add a specialist only for a bounded task such as mechanics research, calculation review, UI work, or test review. Do not let two agents edit the same files at the same time.

## Current milestone

Foundation only (version 0.1.0). The next milestone is the verified crackability model, described in `docs/ROADMAP.md` and `docs/research/CRACKABILITY.md`. Do not start OCR or the overlay until the calculation engine has tests that match sourced examples.
