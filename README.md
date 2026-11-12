# Star Citizen Mining Assistant

External, read-only Windows desktop assistant for Star Citizen mining. The product goal is a compact overlay that can answer one question accurately:

**Can I crack this rock with my current mining equipment?**

This repository is the foundation. Screen recognition, the crackability engine, equipment profiles and the in-game overlay are not implemented yet.

Star Citizen is a trademark of Cloud Imperium Games. This project is an unofficial fan tool and is not affiliated with Cloud Imperium Games.

## Requirements

- Windows 11 64-bit
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (this repo pins the 10.0 feature band in `global.json`)
- Git for Windows

No paid API keys are required.

## Open and build

Run these commands from the repository root in PowerShell:

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/StarCitizenMiningAssistant.App
```

A successful build ends with `Build succeeded` and no errors. A successful test run reports every test passed. `dotnet run` opens a small desktop window titled **Star Citizen Mining Assistant** and shows version **0.1.0**.

Startup lines are appended to:

`%LocalAppData%\StarCitizenMiningAssistant\logs\app.log`

## Layout

```
StarCitizenMiningAssistant.sln
src/StarCitizenMiningAssistant.App              WPF desktop shell
src/StarCitizenMiningAssistant.Core             Domain types, no UI and no file access
src/StarCitizenMiningAssistant.Infrastructure   Logging and, later, capture and storage
tests/StarCitizenMiningAssistant.Core.Tests     xUnit tests for Core
docs/                                           Architecture, roadmap and research
```

Read `docs/ARCHITECTURE.md` before changing the structure. Persistent instructions for coding agents are in `AGENTS.md`.

## Safety boundary

The assistant stays outside the game process. It may later read the user's own screen and, if we add it, read-only game log text. It does not read game memory, inject code, send gameplay input, modify game files, or hook the renderer.

## Current status

Version 0.1.0 is a buildable shell plus the recognition-confidence type that later OCR results must use. The crackability formula is still unverified. See `docs/research/CRACKABILITY.md`.

## License

MIT. See `LICENSE`.
