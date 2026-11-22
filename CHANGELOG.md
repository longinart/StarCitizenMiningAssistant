# Changelog

## 0.4.0

- Calculate, Rescan, and the lock stay on a row under the tabs, including when Rock details and Composition are both closed.
- A short press on Rescan reads composition again. Hold it and, after a quarter of a second, the button reads Rescan all and a bar fills. At half a second it reads mass, resistance, and instability too. The numbers on screen stay until that new read is confirmed.
- Settings can turn off auto-scan for rock details and for composition separately. Rescan still reads the part you asked for.
- Overlay Setup sets the rock window width and height. Width can go down to 315.
- The rock window is one solid rounded rectangle. The fade toward the bottom is gone, and the overlay type and controls are smaller.
- Calculate shows the crack rows again, including after they have faded and the verdict has not changed.
- A crack row shows the loadout name and Crackable, Difficult, or Uncrackable. Click the row for the module and gadget names, the power ratio, and the throttle. That detail window has a close button and the same rounded style as the overlay.
- The bottom-left label is `v.0.4`.

## 0.3.0

- Composition locks after the same reading repeats five times, and only while the HUD says FRACTURE MODE or EXTRACTION MODE.
- Rescan takes a new composition if that lock was wrong.
- The rock overlay keeps one size when rows appear.
- A new signature, or a frame with no scan panel, opens the lock and starts the count again.
- The bottom-left label is `v.0.3`.

## 0.2.0

First public zip. It reads the mining screen, checks whether a saved loadout can crack the rock, and can shorten long ore names after you confirm.
