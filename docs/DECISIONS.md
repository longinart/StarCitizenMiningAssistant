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

## 012 — Windows OCR stays Uncertain

Date: 2026-11-13.

Text is read with Windows.Media.Ocr. That API is part of Windows, so no OCR package was added. The engine returns words and does not return a confidence. The data model says an OCR value stays Uncertain or Missing until a field-specific rule in a decision promotes it. This record does not add that rule. A blank image is a failure. A blank line is dropped. Any text the engine returns is Uncertain and must not feed the crack calculator.

Mass, resistance and instability are chosen only by those words on a line. A number keeps the confidence of that line. An unlabelled number is not assigned to a field. The number is parsed with the Windows culture, then the invariant culture if that fails.

## 013 — One catalog deposit confirms a signature

Date: 2026-11-13.

Decision 012 still leaves every OCR line Uncertain. A signature is a separate field. `SignatureReading` parses the digit lines. When those lines hold one number and `MatchSignature` returns exactly one deposit, that signature becomes Confirmed. The overlay may then print the deposit count and the mineral, blink a ticked mineral, and request the sound. Zero deposits, two deposits, or two different numbers stay Uncertain. No number stays Missing. Typing in the signature box is still manual entry, which is Confirmed.

This rule does not promote mass, resistance, or instability. A confirmed signature does not enter the crack check.

## 014 — A missing sample keeps the last reading

Date: 2026-11-13.

While the overlay is open, the signature rectangle and the rock rectangle are captured about twice a second. `ReadingHold` keeps the previous field when the new sample is Missing. A sample that contains a number replaces that field. An uncertain number stays uncertain. Several dark frames in a row keep the last number. Closing the overlay stops the capture. This rule does not send input to the game.

## 015 — Two identical rock reads are confirmed

Date: 2026-11-13.

Mass, resistance, and instability still come only from a line that contains that word. Decision 012 leaves one such line Uncertain. `StableReading` compares the new sample with the previous sample of that same field. When the two numbers agree, the field becomes Confirmed and can enter the crack check. One sample stays Uncertain, so the verdict stays Unknown until mass and resistance have each agreed once. A missing sample does not count as a disagreement and does not confirm the field. A number typed by the user stays Confirmed when the first sample matches it. A different number replaces it and stays Uncertain until the next sample agrees.

The rock image is scaled up to three times, without going past 2000 pixels on a side, before Windows OCR sees it. No OCR package is added, and the old buffer cast is not used. Signature digits keep their own reader.

## 016 — The overlay can sit above the game

Date: 2026-11-13.

The overlay window is topmost and drawn at 94 percent opacity so a borderless game can show behind it. The assistant window has a checkbox, Clicks pass through the overlay. While that box is ticked, the overlay's extended style includes WS_EX_TRANSPARENT, so the mouse hit test skips this window. The program does not call SendInput and does not synthesize a click. Untick the box to move or close the overlay. The choice is saved in the loadout file and starts unticked. The rectangle-placement window is unchanged: it is topmost only while a rectangle is being dragged.

## 017 — A clipped digit rectangle is not a sample

Date: 2026-11-13.

`HudNumberReader` searches the capture for the bright band that contains the most digits, so a slightly loose rectangle and a bright strip above the number still yield the line. If that band's ink touches the edge of the image, the rectangle is cutting the glyphs. The scan page and the overlay watch say to draw the rectangle a little larger. That frame is not passed to the signature hold, so a partial number does not replace a previous reading. When no digits are read, the densest ink band is used for the same edge check. A dark frame is still a missing sample, not a clip.

## 018 — Signature digits follow the bright row

Date: 2026-11-14.

The handwritten digit rules missed the paler numbers from other ships. A signature rectangle is now read from brightness, not from a ship color. The cutoff sits above the background, so black space and a brighter sky do not use the same sparkle pixel. On a bright ground such as snow, the white cores sit above the bright plateau, on the minimum channel when the strongest channel is saturated. The pill row is the digit run that is kept. A compass row in the same rectangle is a separate run and is not the sample. A bright speck inside the pin does not split that run: the gap is measured from the run's right edge. The location pin is the taller mark on the left of the digit row and is left out. When the glow joins the whole number into one wide mark, as it does for 3,400, that mark is drawn from two brighter core cutoffs so the open side of a 4 is not filled in. If both drawings are blank, a core cutoff is kept when it is one catalog deposit, or when that same number is not one digit away from a different catalog deposit. A one-digit slip such as 8,800 for 6,800 is left out. The digit row is drawn twice, once smoothed and once sharp, and Windows OCR reads both. The number is kept when the two readings agree, when one is blank, when one is the longer form of the other, or when they differ by one digit, share the rest of a non-zero run, and only one of those numbers is in the catalog. A disagreement is an empty sample, so the last good number stays. A mineral name still requires one exact catalog deposit. Decision 017's edge rejection is not used on this path: a tight box that still holds the whole number is read. No OCR package was added.

## 019 — A scan log can be switched on

Date: 2026-11-14.

The left menu has a checkbox, Log scans. It starts ticked. While it is ticked, each placed rectangle is read about once a second, whether or not the overlay is open. A changed reading is written at once. The same reading is written again after four seconds, and an empty frame after ten, so a long dark stretch does not fill the disk. Each kept frame is a bitmap plus one line in scan-log\log.tsv: time, signature or rock, file name, the reading from that frame, its confidence, and the catalog label when there is one. The line records that frame, not the number the overlay is still holding. Untick the box to stop. Closing the assistant stops it. This does not send input to the game.

## 021 — The signature is read under the radar

Date: 2026-11-16.

The live signature path is a C# port of Smart Citizen's SCO digit reader (Apache-2.0, Osiris-DevWorks/smart-citizen). Attribution is in NOTICE and third-party/smart-citizen/LICENSE. The rest of that product is not copied. The capture is one fixed patch under the radar, 38 percent across and 28 percent down, 24 percent wide and 12 percent tall, on the monitor chosen on the scan page. No signature rectangle is placed. Digit height is 11 pixels at 1600 pixels of screen height. The reader thresholds a channel, splits joined glyphs, keeps a digit row only when a comma sits before the last three digits, and matches each glyph to their saved samples. Red is tried first. Green, blue, and the strongest channel are tried as well, because on a 2560×1440 capture the red channel was only the chromatic fringe and the digit body was in green. A row that is not mostly white on that channel is ignored, so WARNING and UNKNOWN stay out. Their readout crops are the tests, and a crop named none must stay empty. A known number at 0.80 or above is kept immediately. When two readings of the same frame are that strong, the longer one is kept, so 45,000 is not reported as 4,000. A reading whose first digit matches poorly is dropped, so a broken 6 is not reported as 8,000. A known number between 0.68 and 0.80 is kept only when nothing stronger is found, so one fringed digit cannot throw away a correct 3,400. A bright sky that lifts the whole patch above the thresholds is left unread.

When that patch has no number, the same patch is tried one step up, down, left, and right. The step is 100 pixels on a 1440 pixel tall screen, and it scales with the screen height. A step that would leave the monitor is skipped. The step that returned a number is tried first on the next frame. A miss does not forget it. Show still uses that step and does not search the neighbors. When the level read is empty, the dark pill is found from its warm fill and the orange pin on its left. If that pill is tilted between about 3 and 35 degrees, the patch is turned so the pill's top edge is level, and the digits are read after that turn. The turn is smooth enough that the thin glyphs still match, which is what reads a tilted 63,000. When the pill itself is not found, a short row of digits tilted between 6 and 35 degrees is turned flat once around the middle of the row. A blank result stays blank. The two-frame watch gate is unchanged. Bright daytime snow is still left unread.

`data/scan-signatures.json` holds their signature chart, including ROC, FPS, and salvage labels, with the source and the 4.9 patch label. One chart match confirms the number and names it. Several matches stay uncertain. Zero matches fall back to the catalog rule in decision 013. While the overlay is open, a new number is kept only after the same value appears on two frames. Read text applies the number at once. A missing frame still keeps the last number. The rock rectangle is read in decision 022. The earlier brightness reader remains in the project and is not the live path. The windshield search is on the branch backup/windshield-signature-reader.

## 022 — The rock scan reads mass, resistance, and instability

Date: 2026-11-16.

Composition of the rock is not read for 1.0. Crackability uses mass and resistance. Instability is read and shown with them. It does not change whether the rock can crack.

No rock rectangle is placed. The patch is fixed on the monitor chosen on the scan page: 64 percent across, 41 percent down, 18 percent wide and 20 percent tall. On the 52 cockpit screenshots at 2560×1440, the scan-results header sat between y 614 and y 754, and the text column stayed near x 1720–1980. The three number rows sit about 110 pixels under the header, so the taller window keeps them when the header is at the bottom of that range. One step still covers a panel that sits further out. A hit is a still that has any of the three numbers. A missed resistance does not throw away a mass that was read. The mass label is also accepted when the reader saw HASS or HAS, which is how this font's M comes out. The number beside the label is used when it parses, including a trailing percent sign, unless the pale-green digits form a longer number. A shorter fragment is left blank when the row had more digit-sized marks. A zero with the diagonal stroke that separates it from the letter O stays 0. A thin 3 stays a 3 when its left side is open, and a 5 that is open on the upper right and has no lower bowl stays a 5 when it ties with a 6. An 8 is used when the glyph has a hole above a bar and a hole below it, including when the text reader saw a 0. A 6 is kept when its bowl is closed and its upper right is open. A decimal point stays in the number. A digit that does not match drops the whole number, so a short guess cannot replace it. Instability is kept when that same still has it. Other text, including ore percentages and cargo, is not a reading and is not written to the scan log.

When the patch has none of the three numbers, the same patch is tried one step up, down, left, and right. The step is 100 pixels on a 1440 pixel tall screen, and it scales with the screen height. A step that would leave the monitor is skipped. The step that found both numbers is tried first on the next frame. A miss does not forget it. Show still uses that step and does not search the neighbors. Read text, the overlay watch, and the scan log do search, and they list only the three fields. Two identical reads still confirm a field. One sample stays uncertain.
