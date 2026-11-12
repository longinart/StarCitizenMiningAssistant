# Work report

Session started 2026-11-12 while the maintainer was away. This file is the checkpoint log for that session.

## Checkpoint: disk

`C:` had 97.66 GB free (368.08 GB used). Stop line was under 5 GB free. The session continued.

## Scope

Finish the manual crack check, prefer SCMiner on conflicts, keep only Golem, Prospector, and MOLE heads, then add image OCR only if that stayed small. Star Citizen and Easy Anti-Cheat were not run. No commit was made.

## Checkpoint: catalog

`data/mining-equipment.json` now has 37 materials from the SCMiner mineral table in the public calculator chunk, retrieved 2026-11-12. Agricium signature is 3885, with deposits 3885 through 38850. Quantainium signature is 3170. Wiki signatures are not stored. Resistance and instability in that table matched the wiki fractions already written in `docs/research/GAME_DATA.md`.

Heads are still the 13 size 1 and size 2 ship lasers. Size 0, ROC, ATLS GEO, hand lasers, and test entities stayed out. Head wattages were not replaced. The wiki `laser_power` for Helix I is 780–3900, which is the SCMiner page value, and no second SCMiner head table was found.

Module and gadget modifiers were replaced from the SCMiner list in chunk `0r77ugxvok-47.js` where they differed. The crack check uses `power`, not `extractPower`. These are the changes that can move a yes or no:

| Item | What changed |
| --- | --- |
| Surge | resistance −15.5 → −16 |
| Brandt | resistance 15.5 → 16 |
| Lifeline | resistance −15.5 → −15 |
| Rime | resistance −24.8 → −25 |
| Deluge | resistance −15.5 → −16, laser power 0 → 15, extract power −15 |
| Overrun | resistance −24.8 → −25, laser power 0 → 2, extract power −15 |
| Clearcut | laser power 0 → 15 |
| Forel | laser power 50 → 0, extract power 50, resistance 15.5 → 15 |
| FLTR, FLTR-L, FLTR-XL | laser power −15/−10/−5 → 0; those numbers are now extract power. Inert 20/23/24 → −8/−16/−24 |
| XTR, XTR-L, XTR-XL | laser power cleared; the old power numbers are extract power. Green zone 15/22/25 → 10/18/25 |
| Vaux, Vaux-C2, Vaux-C3 | laser power +15/+20/+25 → −15/−10/−5. Charge rate cleared. The old positive numbers are extract power |
| Focus, Focus II | green zone 30 → 20 and 37 → 30 |
| Torrent, Torrent II, Torrent III | charge rate 30/35/45 → 20/30/40. Green zone −10/−3/−1 → −3/−2/−1 |
| Torpid | charge rate 60 → 80. Overcharge −60 → 60. Overcharge is stored and is not in the crack formula |

Sabir stayed resistance −50, instability 15, green zone 50. Gadget clustering was stored and is not used in the crack formula.

## Checkpoint: calculator and window

`CrackabilityCalculator` is the SCMiner checker: required power is 0.36 × mass, resistance reduces delivered power, lasers sum power and average the other factors, then one gadget and one active module multiply the fleet. Craft percent multiplies that head only. The window calls this through the view model. The click handler does not contain the formula.

The window takes mass, resistance, optional instability, optional signature, ship, up to three turrets, craft percent, passive modules, one gadget, and one active module. Surge is the default active module. A signature such as 7770 names the SCMiner material and deposit. It does not change the crack verdict.

## Checkpoint: tests and the window

`dotnet test` from `C:\Develop\SCMA`: 17 passed, 0 failed.

`dotnet build`: succeeded, 0 warnings.

The app was started and closed with `CloseMainWindow`. Exit code 0. The log recorded `Loaded equipment catalog 4.10.2-LIVE.12881860.` and then `stopped`.

## Checkpoint: recognition

Image OCR and the overlay were not started. The crack-check catalog change was already the review point for this session, and an OCR engine would have been a new Windows API path with no mining-panel image to check against. That is the next session, after you accept or correct the module table above.

## Left for the maintainer

- Read the table above. The important case is Forel, FLTR, Vaux, and XTR: SCMiner does not treat the old wiki power percent as crack power.
- Open the app with `dotnet run --project src/StarCitizenMiningAssistant.App`. A 5000 kg, 0% resistance rock on Helix I should be Trivial. Signature 7770 should name Agricium deposit 2.
- Nothing was committed.
