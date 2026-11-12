# Game data for the mining assistant

Retrieved 2026-11-12 from the [Star Citizen Wiki API](https://api.star-citizen.wiki/). Item and vehicle records report game version `4.10.2-LIVE.12881860`.

This file is the working catalog. Numbers below are copied from that API response. They are not estimates. Where a second public source disagrees, the disagreement is written next to the value.

SCMiner signature bases are from the mineral table embedded in the public calculator, labeled `4.10.2-live.12881860` in that file. Deposit columns on SCMiner are that base multiplied by 1 through 10. The wiki list has one signature number and no deposit-count table.

## Problems found while collecting this

- The same display name can appear more than once. `Arbor MH1 Mining Laser` has three records. `Mining_Laser_GRIN_Arbor_S1` is the ship laser at 117-2340 W. `Mining_Laser_GRIN_Arbor_S1_Test_Active_1` repeats that power and is a test entity. `Mining_Laser_MPUV_Arm` is also named Arbor MH1 and reports 1.85-1850 W. `Impact II` includes `Mining_Laser_TEST_Best` at 2-2000 W beside the real `Mining_Laser_THCN_Impact_S2` at 1015-4060 W.
- Size 0 lasers (ROC, ATLS GEO, and the hand tools) report maximum power below 1 in the same `laser_power` field where ship lasers report 2340-4930. That scale is not usable for the ship crack formula until it is checked in game. They are listed separately and are not in the ship catalog.
- Six ship ores have no wiki signature, including Quantainium and Gold. The app uses the SCMiner signature. Agricium is 4000 on the wiki and 3885 in the catalog.
- Helix I `description_data` still says mining laser power 630-3150. The structured `mining_laser.laser_power` field says 780-3900, which is the value SCMiner shows and the one used here.
- Surge resistance is -15.5 in the wiki `modifier_map`. The catalog uses SCMiner's −16. Power is +50% in both. Other module and gadget fields that differed are overwritten in `data/mining-equipment.json` and listed in `docs/WORK_REPORT.md`. SCMiner `extractPower` is stored separately and is not laser power in the crack check.
- Every ship head has `all_charge_rates`. Some also have `optimal_charge_rate`, and the two numbers differ. Helix I has `all_charge_rates` 30 and no `optimal_charge_rate`. SCMiner's Helix I page says charge acceleration 0%. The crack yes/no does not use charge rate. Both fields are stored. The charge-time estimate uses `optimal_charge_rate` only.
- MOLE `crew.min` is 4 and `crew.max` is 2. Those two fields are stored as the API returned them.
- Two module records have no modifiers: `Mining Modules` (`mining-modules`) and `ROC Module` (`roc-module`, class `Mining_Modules_VehicleMod_ROCds`). They are listed and excluded from the calculator catalog.
- Gadget modifier values are mixed: some are numbers (`-70`) and some are strings (`"-50%"`, `"+15%"`). The catalog treats both as percentage points after stripping a trailing percent sign.
- The wiki flags Hadanite, Dolivine, Janalite and Aphorite as on-foot mineables. Beradom, Feynmaline and Glacosite are the ground-vehicle mineables. Aphorite has no signature.

## Mining ships

ROC, ROC-DS, ATLS GEO and the ATLS GEO colour variants are in the API and are left out of this ship list, as requested. Their lasers and their mineable signatures are in the later sections.

Hardpoints below were walked on the base hull. Alliance and Wikelo variants exist in the same version. Their hardpoints were not walked separately.

| Ship | Slug | Role | Crew | Cargo | Ore capacity | Stock mining layout |
| --- | --- | --- | --- | --- | --- | --- |
| MISC Prospector | misc-prospector | Light Mining | 1-1 | 0 | 32 | 1 turret, size 1, stock Arbor MH1 |
| MOLE | argo-mole | Medium Mining | min 4, max 2 | 32 | 96 | 3 turrets (front, right, left), size 2, stock Arbor MH2 on each |
| Golem | drak-golem | Starter / Light Mining | 1-1 | 0 | 32 | 1 turret, size 1, stock Pitman |

Also present, same version, not given a separate hardpoint walk:

- MISC Prospector Alliance (`misc-prospector-btala`)
- MISC Prospector Wikelo Work Special (`misc-prospector-collector-indust`)
- MOLE Alliance (`argo-mole-btala`)
- Golem Alliance (`drak-golem-btala`)
- Golem Wikelo Work Special (`drak-golem-collector-indust`)

## Ship mining heads

Power is `mining_laser.laser_power` minimum and maximum. Modifiers are percentage points from `modifier_map`. A blank cell means the API omitted that modifier. Module slots use the same rule.

| Name | Class | Slug | Size | Power min-max | Slots | Optimal m | Max m | Resistance | Instability | Green zone | Optimal charge rate | All charge rates | Inert |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Arbor MH1 Mining Laser | Mining_Laser_GRIN_Arbor_S1 | arbor-mh1-mining-laser | 1 | 117-2340 | 1 | 60 | 180 | 25 | -35 | 40 |  | 30 | -30 |
| Helix I Mining Laser | Mining_Laser_THCN_Helix_S1 | helix-i-mining-laser | 1 | 780-3900 | 2 | 15 | 45 | -30 |  | -40 |  | 30 | -30 |
| Hofstede-S1 Mining Laser | Mining_Laser_SHIN_Hofstede_S1 | hofstede-s1-mining-laser | 1 | 130-2600 | 1 | 45 | 135 | -30 | 10 | 60 | 20 | 30 | -30 |
| Impact I Mining Laser | Mining_Laser_THCN_Impact_S1 | impact-i-mining-laser | 1 | 520-2600 | 2 | 45 | 135 | 10 | -10 | 20 | -40 | 30 | -30 |
| Klein-S1 Mining Laser | Mining_Laser_SHIN_Klein_S1 | klein-s1-mining-laser | 1 | 468-3120 | null | 45 | 135 | -45 | 35 | 20 |  | 30 | -30 |
| Lancet MH1 Mining Laser | Mining_Laser_GRIN_Lancet_S1 | lancet-mh1-mining-laser | 1 | 624-3120 | 1 | 30 | 90 |  | -10 | -60 | 40 | 30 | -30 |
| Pitman Mining Laser | Mining_Laser_DRAK_Golem_S1 | pitman-mining-laser | 1 | 780-3900 | 2 | 40 | 45 | 25 | 35 | 40 | -40 | 40 | -40 |
| Arbor MH2 Mining Laser | Mining_Laser_GRIN_Arbor_S2 | arbor-mh2-mining-laser | 2 | 145-2900 | 2 | 90 | 270 | 25 | -35 | 40 |  | 40 | -40 |
| Helix II Mining Laser | Mining_Laser_THCN_Helix_S2 | helix-ii-mining-laser | 2 | 1232.5-4930 | 3 | 30 | 90 | -30 |  | -40 |  | 40 | -40 |
| Hofstede-S2 Mining Laser | Mining_Laser_SHIN_Hofstede_S2 | hofstede-s2-mining-laser | 2 | 406-4060 | 2 | 60 | 180 | -30 | 10 | 60 | 20 | 40 | -40 |
| Impact II Mining Laser | Mining_Laser_THCN_Impact_S2 | impact-ii-mining-laser-3 | 2 | 1015-4060 | 3 | 60 | 180 | 10 | -10 | 20 | -40 | 40 | -40 |
| Klein-S2 Mining Laser | Mining_Laser_SHIN_Klein_S2 | klein-s2-mining-laser | 2 | 870-4350 | 1 | 60 | 180 | -45 | 35 | 20 |  | 40 | -40 |
| Lancet MH2 Mining Laser | Mining_Laser_GRIN_Lancet_S2 | lancet-mh2-mining-laser | 2 | 1087.5-4350 | 2 | 45 | 135 |  | -10 | -60 | 40 | 40 | -40 |

`Optimal charge rate` is `optimal_charge_rate`. `All charge rates` is `all_charge_rates`. A blank cell means that key was absent. Klein-S1 `module_slots` is null in the API. SCMiner's Klein-S1 page says 0 slots. The catalog uses 0.

## Heads kept out of the ship catalog

| Name | Class | Slug | Size | Power min-max | Why it is separate |
| --- | --- | --- | --- | --- | --- |
| Arbor MHV Mining Laser | Mining_Laser_GRIN_Arbor_S0 | arbor-mhv-mining-laser | 0 | 0.16-0.8 | Size 0. Power scale does not match ship lasers. |
| Lawson Mining Laser | Mining_Laser_SHIN_Klein_S0 | lawson-mining-laser | 0 | 0.16-0.8 | Size 0. Power scale does not match ship lasers. |
| S0 Helix | Mining_Laser_THCN_Helix_S0 | s0-helix | 0 | 0.15-1 | Size 0. Power scale does not match ship lasers. |
| S00 Hofstede | Mining_Laser_SHIN_Hofstede_S0 | s00-hofstede | 0 | 0.16-0.8 | Size 0. Power scale does not match ship lasers. |
| Arbor MH1 Mining Laser | Mining_Laser_GRIN_Arbor_S1_Test_Active_1 | arbor-mh1-mining-laser-2 | 1 | 117-2340 | Class name marks it as a test entity. |
| Arbor MH1 Mining Laser | Mining_Laser_MPUV_Arm | arbor-mh1-mining-laser-3 | 1 | 1.85-1850 | Class is an MPUV arm, not the Greycat Arbor S1, despite the display name. |
| Impact II Mining Laser | Mining_Laser_TEST_Best | impact-ii-mining-laser-2 | 2 | 2-2000 | Class name marks it as a test entity. |

Size 0 assignment from the vehicle hardpoint walk: Arbor MHV is the stock ROC laser. Lawson is the stock ATLS GEO laser. `S00 Hofstede` and `S0 Helix` are not mounted on those vehicles in the walked loadouts. They are the hand-tool lasers in this item list.

## Mining modules

`power_modifier` is a fraction on the wiki. 0.5 means +50% laser power, which is how SCMiner applies Surge. Other modifiers are percentage points. Active modules have charges and a duration in seconds. Passive modules are permanent while fitted.

| Name | Slug | Active or passive | Charges | Duration s | Power fraction | Other modifiers |
| --- | --- | --- | --- | --- | --- | --- |
| Brandt Module | brandt-module | Active | 5 | 60 | 0.35 | resistance=15.5, shatter_damage=-30 |
| Clearcut Module | clearcut-module | Active | 6 | 30 |  | optimal_charge_window_size=30 |
| Deluge Module | deluge-module | Passive | 1 |  |  | resistance=-15.5 |
| FLTR Module | fltr-module | Passive | 1 |  | -0.15 | all_charge_rates=20, inert_materials=20 |
| FLTR-L Module | fltr-l-module | Passive | 1 |  | -0.1 | all_charge_rates=23, inert_materials=23 |
| FLTR-XL Module | fltr-xl-module | Passive | 1 |  | -0.05 | all_charge_rates=24, inert_materials=24 |
| Focus II Module | focus-ii-module | Passive | 1 |  | -0.1 | optimal_charge_window_size=37 |
| Focus III Module | focus-iii-module | Passive | 1 |  | -0.05 | optimal_charge_window_size=40 |
| Focus Module | focus-module | Passive | 1 |  | -0.15 | optimal_charge_window_size=30 |
| Forel Module | forel-module | Active | 6 | 60 | 0.5 | resistance=15.5, overcharge_rate=-60 |
| Lifeline Module | lifeline-module | Active | 3 | 15 |  | resistance=-15.5, laser_instability=-20, overcharge_rate=60 |
| Mining Modules | mining-modules | excluded | | | | class `Mining_Modules` has no modifiers |
| Optimum Module | optimum-module | Active | 5 | 60 | -0.15 | laser_instability=-10, overcharge_rate=-80 |
| Overrun Module | overrun-module | Passive | 1 |  |  | resistance=-24.8 |
| Rieger Module | rieger-module | Passive | 1 |  | 0.15 | optimal_charge_window_size=-10 |
| Rieger-C2 Module | rieger-c2-module | Passive | 1 |  | 0.2 | optimal_charge_window_size=-3 |
| Rieger-C3 Module | rieger-c3-module | Passive | 1 |  | 0.25 | optimal_charge_window_size=-1 |
| Rime Module | rime-module | Active | 10 | 20 | -0.15 | resistance=-24.8, shatter_damage=-10 |
| ROC Module | roc-module | excluded | | | | class `Mining_Modules_VehicleMod_ROCds` has no modifiers |
| Stampede Module | stampede-module | Active | 6 | 30 | 0.35 | laser_instability=-10, shatter_damage=-10 |
| Surge Module | surge-module | Active | 7 | 15 | 0.5 | resistance=-15.5, laser_instability=10 |
| Torpid Module | torpid-module | Active | 5 | 60 |  | optimal_charge_rate=60, overcharge_rate=-60, shatter_damage=40 |
| Torrent II Module | torrent-ii-module | Passive | 1 |  |  | optimal_charge_window_size=-3, optimal_charge_rate=35 |
| Torrent III Module | torrent-iii-module | Passive | 1 |  |  | optimal_charge_window_size=-1, optimal_charge_rate=45 |
| Torrent Module | torrent-module | Passive | 1 |  |  | optimal_charge_window_size=-10, optimal_charge_rate=30 |
| Vaux Module | vaux-module | Passive | 1 |  | 0.15 | optimal_charge_rate=-20 |
| Vaux-C2 Module | vaux-c2-module | Passive | 1 |  | 0.2 | optimal_charge_rate=-15 |
| Vaux-C3 Module | vaux-c3-module | Passive | 1 |  | 0.25 | optimal_charge_rate=-5 |
| XTR Module | xtr-module | Passive | 1 |  | -0.15 | optimal_charge_window_size=15, all_charge_rates=5, inert_materials=5 |
| XTR-L Module | xtr-l-module | Passive | 1 |  | -0.1 | optimal_charge_window_size=22, all_charge_rates=5.75, inert_materials=5.75 |
| XTR-XL Module | xtr-xl-module | Passive | 1 |  | -0.05 | optimal_charge_window_size=25, all_charge_rates=6, inert_materials=6 |

## Mining gadgets

One gadget is attached to a rock. Values are as the API returned them.

| Name | Slug | Class | Modifiers as returned |
| --- | --- | --- | --- |
| BoreMax | boremax | Mining_Gadget_THCN_BoreMax | instability=-70, resistance=10% |
| Okunis | okunis | Mining_Gadget_SHIN_Okunis | optimal_charge_rate=100, optimal_charge_window=+50% |
| OptiMax | optimax | Mining_Gadget_GRIN_OptiMax | optimal_charge_window=-30%, resistance=-30% |
| Sabir | sabir | Mining_Gadget_SHIN_Sabir | instability=+15%, optimal_charge_window=+50%, resistance=-50% |
| Stalwart | stalwart | Mining_Gadget_THCN_Stalwart | instability=-35, optimal_charge_rate=50, optimal_charge_window=-30% |
| Waveshift | waveshift | Mining_Gadget_GRIN_WaveShift | instability=-35, optimal_charge_rate=-30, optimal_charge_window=+100% |

## Scan signatures

The comparison below is the wiki pull. The app does not use the wiki signature column. `data/mining-equipment.json` stores the SCMiner base, the deposit ladder `base × 1` through `base × 10` when SCMiner published those ten values, and the SCMiner resistance and instability. Resistance in the wiki API is a fraction: 0.95 is 95% on the mining scanner. Those resistance and instability numbers matched the SCMiner `rawStats` on 2026-11-12.

| Material | Where | Wiki signature | SCMiner base | Instability | Resistance |
| --- | --- | --- | --- | --- | --- |
| Agricium (Ore) | ship | 4000 | 3885 | 350 | 50% (0.5) |
| Aluminum (Ore) | ship | MISSING | 4285 | 0 | -40% (-0.4) |
| Aphorite | on foot | MISSING | 3000 | 0 | 0% (0) |
| Aslarite (Raw) | ship | 4000 | 3840 | 700 | 50% (0.5) |
| Beradom | ground vehicle | 4000 | 4000 | 0 | 0% (0) |
| Beryl (Raw) | ship | 4900 | 3540 | 350 | 65% (0.65) |
| Bexalite (Raw) | ship | MISSING | 3600 | 600 | 60% (0.6) |
| Borase (Ore) | ship | MISSING | 3570 | 40 | 30% (0.3) |
| Copper (Ore) | ship | 4700 | 4240 | 50 | -70% (-0.7) |
| Corundum (Raw) | ship | 4700 | 4225 | 50 | 10% (0.1) |
| Dolivine | on foot | 3000 | 3000 | 0 | 10% (0.1) |
| Feynmaline | ground vehicle | 4000 | 4000 | 200 | 0% (0) |
| Glacosite | ground vehicle | 4000 | 4000 | 0 | 10% (0.1) |
| Gold (Ore) | ship | MISSING | 3585 | 550 | 50% (0.5) |
| Hadanite | on foot | 3000 | 3000 | 200 | 0% (0) |
| Hephaestanite (R) | ship | 4700 | 4180 | 400 | -30% (-0.3) |
| Iron (Ore) | ship | 4700 | 4270 | 50 | -40% (-0.4) |
| Janalite | on foot | 4000 | 3000 | 300 | 30% (0.3) |
| Laranite (Raw) | ship | 4750 | 3825 | 400 | 50% (0.5) |
| Lindinium (Ore) | ship | 3400 | 3400 | 1000 | 95% (0.95) |
| Quantainium (Raw) | ship | MISSING | 3170 | 1000 | 95% (0.95) |
| Quartz (Raw) | ship | 4700 | 4210 | 50 | -70% (-0.7) |
| Raw Ice | ship | 4000 | 4300 | 0 | -50% (-0.5) |
| Raw Ouratite | ship | 4000 | 3370 | 600 | 60% (0.6) |
| Raw Silicon | ship | 4700 | 4255 | 50 | -20% (-0.2) |
| Riccite (Ore) | ship | 4700 | 3385 | 850 | 95% (0.95) |
| Savrilium (Ore) | ship | 3200 | 3200 | 1000 | 95% (0.95) |
| Stileron (Ore) | ship | 4700 | 3185 | 870 | 60% (0.6) |
| Taranite (Raw) | ship | MISSING | 3555 | 700 | 50% (0.5) |
| Tin (Ore) | ship | 4700 | 4195 | 0 | -20% (-0.2) |
| Titanium (Ore) | ship | 4700 | 3855 | 0 | 10% (0.1) |
| Torite (Ore) | ship | 3200 | 3900 | 550 | 25% (0.25) |
| Tungsten (Ore) | ship | 4700 | 3870 | 0 | -40% (-0.4) |

SCMiner deposit readings for a material are `base × 1` through `base × 10`. That pattern holds for the Agricium, Aluminium and Aslarite rows checked in the source table. It is not a wiki field.

