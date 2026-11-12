# Development

Commands are run in PowerShell from the repository root, `C:\Develop\SCMA`, unless a step says otherwise.

## Machine used to create 0.1.0

- Windows 11 Pro, 64-bit, display version 25H2, build 26200.
- Git 2.56.1.windows.1.
- .NET SDK 10.0.404, selected by `global.json`. SDKs 8 and 9 are also installed and are not the target.
- The repository directory is writable by the current user.
- GitHub CLI (`gh`) is not installed. Git Credential Manager has a saved `git:https://github.com` login. Do not install `gh` unless the maintainer asks.

## First build

```powershell
dotnet restore
dotnet build
dotnet test
```

Expected result:

- `dotnet restore` completes without errors.
- `dotnet build` ends with `Build succeeded` and 0 errors.
- `dotnet test` ends with `Passed!` and a failed-test count of 0.

Then:

```powershell
dotnet run --project src/StarCitizenMiningAssistant.App
```

Expected result: a dark window opens in the centre of the screen. The title is `Star Citizen Mining Assistant` and the gold line reads `Version 0.1.0`. Close the window. The log file below contains a started line and a stopped line.

`%LocalAppData%\StarCitizenMiningAssistant\logs\app.log`

## Where code goes

- A mining rule, a formula, or a "can this be trusted?" decision goes in `src/StarCitizenMiningAssistant.Core`.
- File, network, screen and operating-system calls go in `src/StarCitizenMiningAssistant.Infrastructure`.
- Windows, overlays and button clicks go in `src/StarCitizenMiningAssistant.App`. A click handler calls Core. It does not calculate.
- A test goes in `tests/StarCitizenMiningAssistant.Core.Tests` and does not start WPF.

## Adding a package

Ask first. Say which project needs it and why the BCL is not enough. OCR, computer-vision and capture libraries wait until that milestone.

## Git

The solution file is `StarCitizenMiningAssistant.sln`. `bin/`, `obj/` and user files are ignored by the generated `.gitignore`.

Do not commit the log file from LocalAppData. Do not commit secrets.

## Data files

When game data arrives, store it under a `data/` directory with a manifest that names the source, the retrieval date and the game build. The calculation code reads the manifest. It does not contain a second copy of the numbers.
