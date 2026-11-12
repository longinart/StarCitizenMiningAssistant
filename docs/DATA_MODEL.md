# Data model

Nothing in this document is a Star Citizen statistic. It describes the shapes the code will use. Numbers arrive later, in versioned data files, each with a source.

## Confidence

Recognition and any other fallible input use `ObservedValue<T>`:

| Confidence | Meaning | May a calculation use it? |
| --- | --- | --- |
| `Missing` | The field was not found. | No. |
| `Uncertain` | A candidate value exists and might be wrong. | No. Show it as uncertain. |
| `Confirmed` | The user accepted it, or a recogniser met the confirmation rule for that field. | Yes. |

`IsConfirmed` is the only check. A missing resistance stored as the integer default `0` is still missing. Zero can also be a real resistance, so callers must not treat "default value" as "no data".

Manual entry by the user is `Confirmed`. OCR starts as `Uncertain` or `Missing` until a field-specific rule, reviewed in a decision record, promotes it.

## Rock scan

A future `RockScan` carries observed fields, not bare numbers:

- mass, in kilograms
- resistance, in the unit the verified formula expects, recorded explicitly
- instability
- signature
- deposit count
- composition entries: material identity plus percentage
- the game patch the scan was interpreted against, when known

The engine refuses to give a crackable/not-crackable verdict while mass or resistance is not confirmed, or while the formula version required for those inputs is unverified.

## Equipment

Model these as separate types so a turret does not inherit another turret's modules:

- `MiningLaserHead` — identity, size, power curve, optimal and maximum distance, resistance modifier, instability modifier, module slot count, crafting bonus
- `MiningModule` — identity, passive or active, charges if active, modifiers, activation condition
- `MiningGadget` — identity and rock-wide modifiers. Sources currently say one gadget is attached to a rock. That limit stays a data rule, not an assumption buried in a loop.
- `TurretLoadout` — one laser head, its modules, and which modules are switched on
- `EquipmentProfile` — the turrets the user configured, plus the set of heads, modules and gadgets they actually own

Crafting bonuses attach to the laser the user configured. They are not invented for a recommendation.

A modifier stores its operator as data (`add` or `multiply`, once the research file says which) and its source item. The engine can explain a result by listing those modifiers.

## Game data files

Planned layout, not created yet:

```
data/
  manifest.json          source, retrieved date, game build, formula version
  lasers.json
  modules.json
  gadgets.json
  materials.json
```

Candidate sources, none of them wired in:

- [Star Citizen Wiki API](https://api.star-citizen.wiki/) and [its docs](https://docs.star-citizen.wiki/)
- [scunpacked-data](https://github.com/StarCitizenWiki/scunpacked-data)
- [ScDataDumper](https://github.com/octfx/ScDataDumper), for producing our own dumps from files the user already has

Check the licence before redistributing a dump. Prefer storing the attributes we use and a link to the source build, rather than copying an entire unpacked tree into this repository.

## Calculation result

A future result is explicit about ignorance:

- `Crackable`
- `NotCrackable`
- `Unknown` — missing input, uncertain input, or an unverified mechanic

Each result names the formula version, the turrets included, the power available, the power required when the formula can produce it, and the warnings (charges, instability, distance, gadget). A recommendation is a separate object and may only contain items from the user's available set.

## What is deliberately absent

No laser powers, no module percentages, and no fracture formula are stored in this milestone. The research notes explain why.
