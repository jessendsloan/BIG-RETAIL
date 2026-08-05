# Automatic Front Door Art Drop

This four-panel door uses ten transparent PNGs: five modular pieces for each isometric wall direction. The artwork is generic and does not need wallpaper variants.

## Import contract

- Replace only the PNGs; do not replace or delete their `.png.meta` files.
- Keep the existing pixel dimensions and transparent margins exactly as exported.
- Do not add a common canvas or trim the pieces again.
- The metadata imports every piece at 100 pixels per Unity unit with a pre-aligned pivot.

## Rising Left

- `AutomaticFrontDoor_RisingLeft_Frame.png` — 212 x 364
- `AutomaticFrontDoor_RisingLeft_LeftGlass.png` — 50 x 263
- `AutomaticFrontDoor_RisingLeft_LeftDoor.png` — 56 x 266
- `AutomaticFrontDoor_RisingLeft_RightDoor.png` — 56 x 266
- `AutomaticFrontDoor_RisingLeft_RightGlass.png` — 50 x 263

## Rising Right

- `AutomaticFrontDoor_RisingRight_Frame.png` — 212 x 364
- `AutomaticFrontDoor_RisingRight_LeftGlass.png` — 50 x 263
- `AutomaticFrontDoor_RisingRight_LeftDoor.png` — 56 x 266
- `AutomaticFrontDoor_RisingRight_RightDoor.png` — 56 x 266
- `AutomaticFrontDoor_RisingRight_RightGlass.png` — 50 x 263

`Left` and `Right` describe the panel's position on screen. The fixed glass and outer frame remain stationary. Only the two center `Door` transforms will slide outward during a later animation pass.

The source file named `AutomaticFrontDoor_RisingRight_Frame.png.png` must be dropped here as `AutomaticFrontDoor_RisingRight_Frame.png` with the extra extension removed.
