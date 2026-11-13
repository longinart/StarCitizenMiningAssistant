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

### B. Compare every named loadout to one rock — in Core

`LoadoutComparison` runs the existing calculator once per saved loadout. Each row is Crackable, Difficult, or Uncrackable. When a boost is what makes it crackable, the row names it: the active module, the gadget, or both. A missing field is Unknown, not Crackable. Instability and charge-window size stay on the row. They do not turn an uncrackable Hofstede into a crack. The overlay does not show this yet.

### C. Overlay window — in the app

The Overlay menu opens a second WPF window. The user moves it and resizes it, and that place is saved. It shows one row per named loadout from B. Mass, resistance and instability are typed in until the scan area exists. Click-through waits until the window is readable over the game.

### D. Two capture regions — in the app

Scan areas lets the user place one rectangle on the signature list and another on the rock panel. Each monitor keeps its own rectangles in the loadout file. Show still captures that rectangle and saves a bitmap for diagnosis. No text is read yet, and there are no game hooks.

### E. OCR — in the app

Scan areas can read the placed rectangle. No OCR package was added. Interface digits are read from the bright core of each glyph, because Windows OCR misses this small font. Every line is an `ObservedValue<T>`. Decision 012 does not promote a line, so a reading stays Uncertain and does not change the crack check.

### F. Signature watch — in the app

A confirmed signature is matched to the catalog. The overlay prints the mineral name beside it. Minerals ticked on the signature page blink. A sound plays only when that box is ticked, and not again for the same mineral. An uncertain OCR line does not match or blink.

### G. Rock scan into the comparison — in the app

Read text on the rock rectangle fills the overlay. A confirmed mass and resistance run the loadout comparison. An uncertain or missing field stays Unknown and does not produce Crackable. Typing in a box again counts as manual entry, which is confirmed.

### H. Game check — waiting on the layout

The program has no path that sends keyboard, mouse, or controller input. The overlay is a normal window. The placement window is topmost only while a rectangle is being dragged, and it closes on Enter or Escape. Still to confirm on the borderless layout: both rectangles land on the right panels, and the overlay stays where it was placed.

## Later

A per-user installer or a portable build. No admin rights for normal use. Ranking equipment the user does not own stays out.

## Explicitly out of scope

Memory reading, injection, renderer hooks, input automation, editing game files, and copying another project's source.
