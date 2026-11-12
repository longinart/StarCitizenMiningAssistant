# Crackability research

Status: **not verified, not implemented.** Checked 2026-11-12. No formula in this file is approved for the calculation engine.

The engine has to match real rocks and the better public calculators on identical inputs. Where those sources disagree, the disagreement is the result of the research. It is not resolved by choosing the formula that appears most often.

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

Fetched page text, which labels itself Star Citizen 4.10.2, shows a 5,000 kg rock at 0% resistance with dissipation threshold 1,800 W and a 3,900 W Helix I. `3900 / 1800` is about 217%, which matches the displayed power ratio. `5000 / 5` is 1,000, not 1,800. This snippet therefore does not match the Peacefrog, DCal or UVerse examples.

The surrounding page copy ("thermal dissipation", gadget bands at 70% and 115%) may be promotional text rather than the calculator's real expression. The live page source was not extracted. SCMINER is a required comparison target, and this snippet is not usable as a formula.

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

Until SCMINER and Peacefrog are compared on the same rock with the same loadout, there is no approved formula.

## Next research steps

1. Open Peacefrog, SCMINER and Rockbreaker in a browser. Enter the same three rocks (include the 8,000 kg / 95% case and the 50,000 kg / 25% case) with no modules, and write the three outputs here.
2. Repeat with one known modifier, such as a single Helix and no modules, then with two modifiers whose stacking we can see.
3. Repeat with one active module that changes power, one gadget, and a crafted laser, each in isolation.
4. Read the mort13 research write-up linked from the Breakability Chart and quote the part that justifies `0.2 * mass * (1 + resistance/100)` or a newer formula.
5. For equipment stats, take one laser from a named `scunpacked-data` or Wiki build and compare it with the calculator's laser page. Record the build id.
6. Only after two independent sources match a case, add that case as an xUnit fixture and implement that case.

## Primary data sources for stats, not for the formula

- <https://docs.star-citizen.wiki/>
- <https://api.star-citizen.wiki/>
- <https://github.com/StarCitizenWiki/API>
- <https://github.com/StarCitizenWiki/scunpacked-data>
- <https://github.com/octfx/ScDataDumper>

Item stats and the fracture formula are different problems. A correct laser power from the Wiki does not tell us how resistance is applied.
