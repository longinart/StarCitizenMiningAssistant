# Crackability research

Status: **working model, implemented from the SCMiner checker.** Adopted 2026-11-12. The maintainer checked SCMiner's basic results in game. Where equipment numbers disagree, decision 011 uses the SCMiner value. Head laser power is the wiki `laser_power` field, which matches the SCMiner Helix I page. Peacefrog, DCal and UVerse remain a different model. They are not the engine.

## Working model

Source: the public calculator at <https://scminer.rocks/calculators/rock-crackability>. The expressions below were read from that page's script on 2026-11-12 and reimplemented in `CrackabilityCalculator`. The script was not copied into this repository.

Resistance reduces delivered power. It does not change the power the rock requires.

- Power required = `0.36 × mass`. The page default, 5,000 kg at 0% resistance, requires 1,800 W.
- Each active laser contributes `maxPower × (1 + craftPercent / 100) ×` the product of that laser's module power multipliers. The craft percent is our extension. SCMiner has no crafting field.
- A laser's resistance, instability, green-zone and charge-rate multipliers start at `1 + headPercent / 100` and are multiplied by that laser's passive modules.
- Across lasers, power is summed. The other multipliers are averaged.
- One rock gadget and one active consumable, such as Surge, multiply the fleet totals after that average. Using both multiplies both. SCMiner's suggestion list tries them separately. This engine also reports the pair.
- Effective resistance = rock resistance × the combined resistance multiplier, clamped to 0–98.
- Power delivered = summed power × `(1 − effectiveResistance / 100)`.
- The rock is crackable when delivered power is at least required power.
- Power ratio = delivered / required. At least 160% is trivial, at least 115% is comfortable, at least 100% is borderline, at least 70% needs a gadget or consumable, and below 70% needs more lasers.
- Instability and the green zone are reported. They do not change the crackable result. Green-zone width starts at 18 and is clamped to 4–65. Instability is clamped to 0–99.
- When the rock is crackable, recommended throttle is `108 / powerRatio` percent, clamped to 5–100. A 3,900 W laser against 1,800 W required is about 50%, which is the throttle on the page.
- Charge time uses `optimal_charge_rate` only. `all_charge_rates` is stored on the head and is not applied. See [GAME_DATA.md](GAME_DATA.md).
- A missing or unconfirmed mass or resistance returns Unknown. Only `ObservedValue<T>.IsConfirmed` feeds the arithmetic.

Checked against the published default table: Arbor MH1 at 2,340 W on that 5,000 kg / 0% rock is a 130% ratio and a 6,500 kg maximum. Helix I's catalog sentence of about 32,000 kg at 35% resistance does not match this checker. The tests follow the checker.

## What "crackable" has to answer

For a scanned rock and one or more turret loadouts:

1. Power available against power required to fracture.
2. Resistance after laser, module, gadget and crafting modifiers, including the order of operations.
3. Instability and the charge window. This changes how safely the rock can be charged. It is a separate output from the fracture check.
4. Limits: one gadget on the rock, active modules with a finite number of charges, optimal laser distance, and which turrets are actually firing together.
5. Crafted bonuses on the heads the user configured.

A summed "maximum theoretical laser power" is not an answer.

## Sources checked

### Peacefrog Rock Breaker

<https://rockbreaker.peacefroggaming.com/>

The page states:

`Formula: (Mass / (1 - (Resist × 0.01))) / 5`

Worked numbers shown on the page, checked by recalculation on 2026-11-12:

- Mass 50,000. Base resistance 25. Base required power = (50000 / 0.75) / 5 = 13,333.33. The page shows base required 13,333.33.
- Resistance modifier 1.250, so adjusted resistance = 25 × 1.250 = 31.25. Required power = (50000 / (1 - 0.3125)) / 5 = 14,545.45. The page shows 14,545.45.
- Instability is shown the same way: base 50 × 1.350 = 67.50. The page does not use that instability figure in the required-power line.

The page says the minimum break throttle assumes optimal laser distance. Patch version was not visible in the fetched text.

### DCal Breaker

<https://www.dystopiandynasty.org/breaker>

Fetched text dated as data from 2026-11-08. It says the formula is a community formula and is not official from CIG:

`required laser power = mass ÷ (1 − resistance) ÷ 5`

Worked example on that page: mass 8,000 kg, resistance 95%, no resistance modifier, required power 32,000.

Check: if 95% is the fraction 0.95, (8000 / 0.05) / 5 = 32,000. That matches.

The same text says several lasers add their power, and resistance effects combine multiplicatively. The example given is +25% and −30% producing −12.5%, which is `1.25 × 0.70 = 0.875`. It says one gadget acts on the whole rock, instability is scaled in the same way as resistance, and suggestion loadouts use non-crafted items. Crafting applies only to lasers the user sets. Warning thresholds for instability and the green zone are described as estimated.

### UVerse fracture calculator

<https://uverse.space/mining/fracture>

Fetched text shows:

`Required = ( Mass ÷ ( 1 − AdjResist ÷ 100 ) ) ÷ 5`

Worked example: mass 8,500, adjusted resistance 28.0, required 2,361.1.

Check: (8500 / (1 - 0.28)) / 5 = 2,361.11. That matches.

The page text calls this "CIG's published mining model" and also says module power buffs are not fully factored, so a Rieger-style setup can crack more than shown. Those two statements are both recorded. The "published by CIG" claim is not confirmed by a primary CIG document in this research pass. Do not treat it as official.

### mort13 / Breakability Chart

<https://www.reddit.com/r/starcitizen/comments/1oo9teo/the_theory_of_mass_resistance_and_laserpower/>

<https://mort13.github.io/BreakabilityChart/>

The post states:

`P_needed = 0.2 * mass * (1 + (resistance / 100))`

At 0% resistance this is `mass / 5`, the same as the Peacefrog formula. At 50% resistance it is `mass * 0.30`. The Peacefrog formula at 50% is `mass * 0.40`. They are different models. The chart page says breakability depends on mass and resistance, and that the calculations come from the author's research. The research write-up itself was not retrieved in this pass. A comment on the post discusses multiplicative stacking of resistance reduction. That comment is not a verification.

### SCMINER

<https://scminer.rocks/calculators/rock-crackability>

The page default is a 5,000 kg rock at 0% resistance, threshold 1,800 W, and a 3,900 W laser at a 217% ratio. `0.36 × 5000 = 1800` and `3900 / 1800 = 2.166…`. That is the working model above. It does not match Peacefrog's `mass / 5` at 0% resistance, which would be 1,000 W. The maintainer's in-game checks follow this page, so the engine follows this page.

### Other calculators, not yet opened in source form

- <https://aogaming.tools/star-citizen/rock-breaker/> states that CIG has not published the exact formula and that combined turret power is a modelling choice.
- <https://www.dystopiandynasty.org/breaker> is already included above as DCal.

## Agreement and conflict

| Input | Peacefrog / DCal / UVerse shape | mort13 shape | SCMINER snippet |
| --- | --- | --- | --- |
| Any mass, 0% resistance | mass / 5 | mass / 5 | snippet showed mass × 0.36 for one example |
| 8,000 kg, 95% resistance | 32,000 | 3,120 | not checked |
| 50,000 kg, 25% resistance | 13,333.33 | 12,500 | not checked |
| Resistance modifiers | Peacefrog multiplies the rock resistance by a factor. DCal multiplies the factors together. | not verified | not verified |
| Instability | reported separately from required power | described as a player choice, not the break check | page also shows a green-zone width |

The working engine is the SCMiner checker. Peacefrog stays in this file as a recorded disagreement. A crafted head that disagrees with the `craftPercent` power multiplier, or a size 0 laser whose power scale is confirmed, is the next correction. Those gaps are listed in [GAME_DATA.md](GAME_DATA.md).

## Primary data sources for stats, not for the formula

- <https://docs.star-citizen.wiki/>
- <https://api.star-citizen.wiki/>
- <https://github.com/StarCitizenWiki/API>
- <https://github.com/StarCitizenWiki/scunpacked-data>
- <https://github.com/octfx/ScDataDumper>

Item stats and the fracture formula are different problems. A correct laser power from the Wiki does not tell us how resistance is applied.
