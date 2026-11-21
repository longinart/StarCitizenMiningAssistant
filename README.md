# Star Citizen Mining Assistant

A small Windows window that sits beside Star Citizen and helps you decide whether your mining ship can crack the rock you are looking at.

It watches your screen. It does not join the game. Nothing here reads the game's memory, injects into the game, or flies the ship for you.

Star Citizen is a trademark of Cloud Imperium Games. This is an unofficial fan tool and is not affiliated with Cloud Imperium Games.

## Key features

- **Signature reading.** While the signature overlay is open, the app reads the number under the radar and names the deposit when that number matches one known mineral.
- **Crack check.** Mass and resistance from the scan panel are compared with the ship loadout you saved. The overlay says whether that setup can crack the rock.
- **Composition reading.** Mineral rows under the scan panel are filled in when the name matches the mineral list. Cargo, such as `1.33 / 96 SCU`, is the ship's hold and is ignored.
- **Refinery and sell totals.** Refined SCU and a sell total are worked out from the rock size on the COMPOSITION line and from each mineral's own best price.
- **Overlays you can place.** The rock overlay and the signature overlay are separate windows. Each keeps its own size, opacity, and position.

The same window also stores named ship loadouts, a mining-equipment shop list, and the Aaron Halo Band 5 route table.

## How to download and run

You need 64-bit Windows 10 or Windows 11. You do not need to install .NET.

**[Download StarCitizenMiningAssistant-0.2.0-win-x64.zip](https://github.com/longinart/StarCitizenMiningAssistant/releases/download/v0.2.0/StarCitizenMiningAssistant-0.2.0-win-x64.zip)**

1. Click the link above and save the ZIP.
2. Right-click the ZIP and choose **Extract All**. Put it in a folder you can write to, such as `Documents\StarCitizenMiningAssistant`. Do not run the app from inside the ZIP.
3. Open that folder and double-click `StarCitizenMiningAssistant.App.exe`. Leave the other files next to it. The app will not start correctly if you move only the exe.

Windows SmartScreen may say the app is unrecognized. That happens because this build is not signed with a paid certificate. Choose **More info**, then **Run anyway**.

Windows SmartScreen may say the app is unrecognized. That happens because this build is not signed with a paid certificate. Choose **More info**, then **Run anyway**.

You do not need to install .NET. The ZIP already contains everything the app needs to start.

## Quick setup

- In **Settings**, choose the monitor the game is on.
- On **Select ship**, pick the ship, then open **Ship loadouts** and save the lasers and modules you actually have fitted.
- Open **Overlay Setup**. Turn on the rock overlay, move it where you want it, and set the size and transparency. Turn on click-through only after it is in the right place.
- Run the game in **borderless** or **windowed** mode on that same monitor, not exclusive fullscreen. The panels have to be visible. The app does not start the game.
- Open the mining screen so the scan results are showing, then open the rock overlay. It reads about twice a second while that overlay is open, and it stops when you close it.
- The label in the bottom-left corner, such as `v.0.2`, is the version you are running.

Long ore names can scroll off the scan row. **Overlay Setup → Simplify mineral names** can shorten those names in the game's English text. It asks before it writes anything. Restart the game afterward. `Data.p4k` is not changed.

## Safety and compliance

This program stays outside the game.

It does not:

- read or write the game's memory
- inject a DLL, hook DirectX or Vulkan, or modify the game executable
- send keyboard, mouse, or controller input
- automate mining, flying, or any other gameplay
- edit `user.ini` or `Data.p4k`

The only optional write is **Simplify mineral names**. After you click Yes, it updates the loose English `global.ini` for the LIVE or PTU folder it found, and it adds `g_language = english` to `user.cfg` only when that line is missing. A backup of an existing `global.ini` is kept next to it.

An uncertain reading is not treated as a confirmed fact. A crack answer uses a mass and a resistance only after each has been read the same way twice.

## License

MIT. See [LICENSE](LICENSE).

If you want to build the app from source instead of using the ZIP, the developer notes are in [AGENTS.md](AGENTS.md).
