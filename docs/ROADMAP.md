# Roadmap

Work in this order. Finish and test a milestone before starting the next one. A milestone can be split into several small changes.

## 1. Foundation — done in 0.1.0

Solution, three projects, xUnit, documentation, foundation window, file log, recognition-confidence type.

## 2. Verified crackability model — done for the SCMiner checker

`CrackabilityCalculator` is covered by the tests: the SCMiner page examples, stacking, Surge at −16, Sabir, both together, craft percent, and an unconfirmed mass. The window no longer asks for a rock by hand. One unnamed loadout is saved. Peacefrog does not agree and is not a second source for this formula.

The formula stays the one in `docs/research/CRACKABILITY.md`. A new rock number still needs a source. An uncertain reading stays Unknown.

## Next sequence

This is the order for the overlay. Finish and test one item before the next. The process stays external: screen capture of a region the user places, no game memory, no injected input.

### A. Named loadouts — in the app

The loadout page stores more than one setup, each with a name the user types. One loadout is the whole ship: on the Mole, that is all three turrets plus the gadget. Saving a new name leaves the other names unchanged. Switching names loads that saved setup. The current screen is kept separately and restored on the next start. An older file with one setup still loads, under the name Loadout.

### B. Compare every named loadout to one rock

Core only, with tests. The same confirmed mass, resistance and instability runs through the existing calculator once per saved loadout. Each row is Crackable, Difficult, or Uncrackable. When a boost is what makes it crackable, the row names it: Surge, the gadget, or both. A missing field is Unknown, not Crackable.

Instability and charge-window size stay on the row as the reason a crackable Hofstede setup is the cleaner one for a small rock. They do not turn an uncrackable Hofstede into a crack. No new formula.

### C. Overlay window

A second WPF window, not a browser view. The user moves it and resizes it. Position and size are saved. It shows one row per named loadout from B. It does not read the screen yet, so the table can be checked before OCR exists. Click-through waits until the window is readable over the game.

### D. Two capture regions

The user places one rectangle on the signature list and another on the rock panel, where mass, resistance and instability are drawn. Different monitors get different rectangles. The rectangles are saved. A still image can be saved for diagnosis. No game hooks.

### E. OCR

Ask before adding an OCR package. Infrastructure implements a Core interface. Every signature and every rock number is an `ObservedValue<T>`. A low-confidence read stays Uncertain.

### F. Signature watch

A confirmed signature is matched to the catalog. The overlay prints the mineral name beside it. Minerals ticked on the signature page blink. A sound plays only when the user has turned that sound on.

### G. Rock scan into the comparison

Confirmed mass, resistance and instability from the rock rectangle run B and fill the overlay. Uncertain fields stay Unknown and do not produce Crackable.

### H. Game check

Manual check on the user's borderless layout: both rectangles land on the right panels, the overlay stays where it was placed, and the process sends no input to the game.

## Later

A per-user installer or a portable build. No admin rights for normal use. Ranking equipment the user does not own stays out.

## Explicitly out of scope

Memory reading, injection, renderer hooks, input automation, editing game files, and copying another project's source.
