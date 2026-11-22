# Publishing a public ZIP

Run these commands from the repository root in PowerShell. This does not change the project files. It builds a folder that already contains the .NET runtime, so the person downloading it does not install .NET.

The app targets `net10.0-windows10.0.19041.0` and `win-x64`.

```powershell
dotnet publish src/StarCitizenMiningAssistant.App -c Release -r win-x64 --self-contained true -o publish/win-x64
```

A successful run ends with exit code 0 and `Build succeeded`. The folder `publish/win-x64` contains `StarCitizenMiningAssistant.App.exe`, the runtime files beside it, and a `data` folder. The `data` folder must stay next to the exe. That is where the ship, shop, refinery, signature, and mineral-shortcut files are read from.

Zip that folder. The version below is the `<Version>` in `Directory.Build.props`. Change the zip name when that version changes.

```powershell
Compress-Archive -Path publish/win-x64/* -DestinationPath publish/StarCitizenMiningAssistant-0.4.0-win-x64.zip -Force
```

Check the zip before uploading it:

- Unzip it to an empty folder and start `StarCitizenMiningAssistant.App.exe`.
- The window opens, and the bottom-left label matches the version in the zip name.
- `data\mining-equipment.json` is present beside the exe, inside `data`.

Upload `publish/StarCitizenMiningAssistant-0.4.0-win-x64.zip` to the GitHub release for that version. Do not upload the loose `publish/win-x64` folder.

The exe is not code-signed. Windows SmartScreen will warn on first launch. The public readme tells people to choose More info, then Run anyway.
