# Star Citizen Mining Assistant

An external Windows desktop utility designed to assist with Star Citizen mining. The application overlays the game to provide real-time information, helping you assess rock crackability, track deposit signatures, and manage refinery expectations.

## 📋 Features

*   **📡 Signature Scanning:** Uses OCR to read deposit signatures from the radar HUD, identifying minerals when they match the provided signature list.
*   **🔨 Automated Crackability:** Reads rock mass and resistance from scan results to determine if your current ship configuration can fracture the rock.
*   **📊 Composition Analysis:** Uses OCR to detect Total SCU and mineral percentages directly from the scan interface.
*   **💰 Refinery & Pricing:** Calculates potential yields and profit based on selected refinery methods and current market prices.
*   **🛠️ Loadout Management:** Saves and switches between ship loadouts, including installed heads, modules, and gadgets.
*   **🖼️ Configurable Overlays:** Separate, customizable windows for signature and rock data, supporting independent position, opacity, and click-through settings.

## ⬇️ How to Download and Run

1.  **Download:** Get the latest `StarCitizenMiningAssistant-x.x.x.zip` from the [Releases](../../releases) page.
2.  **Extract:** Extract the ZIP folder to a convenient location on your computer.
3.  **Run:** Open the folder and run `StarCitizenMiningAssistant.App.exe`.
4.  *Note:* You do not need to install .NET. All required dependencies are included.

> **Windows SmartScreen:** Because this is an independent project, you may see an "Unknown publisher" warning when launching the app for the first time. To continue, click **"More info"** and then **"Run anyway."**

## ⚙️ Quick Setup

1.  **System:** In the app Settings, select the monitor where you run Star Citizen.
2.  **Loadout:** On the "Select ship" page, configure your ship, laser heads, and installed modules to match your in-game setup.
3.  **Overlay:** In "Overlay Setup," enable the desired overlays. Position them over the game window and set your transparency preferences.
4.  **Game Settings:** Run Star Citizen in **Borderless Windowed** mode on the same monitor configured in the app.
5.  **Mining:** Once in Mining Mode, open your scan results panel. The overlay will automatically detect and read the data while the overlay window is active.

*Tip: Scan readings are most accurate when the HUD contrast is high. Darker game environments often improve OCR recognition.*

## 🛡️ Safety and Compliance

This application is a read-only, external tool.

*   It does **not** inject code, read game memory, hook rendering APIs, or modify game files.
*   It does **not** send keyboard, mouse, or controller input to the game.
*   It does **not** perform gameplay automation.
*   The only write operation is an optional setting to simplify in-game mineral names in `global.ini`, which the user must explicitly confirm.

## ⚖️ License

MIT. See `LICENSE`.
