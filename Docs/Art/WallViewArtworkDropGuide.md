# Wall View Artwork Drop Guide

The wall-view system uses three display modes:

- **Walls Up** uses the existing full-height wall sprites.
- **Cutaway** uses low/base sprites for walls that obscure constructed
  foundation in the current isometric view. This includes interior walls and
  concave L-shaped boundaries when foundation continues farther back along
  the same viewing lane. Isolated full-height corner caps between two lowered
  wall runs are lowered as a continuity cleanup. Apron tiles do not count as
  foundation.
- **Walls Down** uses low/base sprites for every wall.

Walls are never disabled. If low art is unavailable, the system safely uses
the matching full-height sprite.

## Low wall PNGs

Replace these prepared placeholder PNGs in
`Assets/Art/WallSegmentArt/Low/`:

- `Default_Low_RisingLeft.png`
- `Default_Low_RisingRight.png`
- `Brick_Low_RisingLeft.png`
- `Brick_Low_RisingRight.png`
- `White_Low_RisingLeft.png`
- `White_Low_RisingRight.png`
- `Wood_Low_RisingLeft.png`
- `Wood_Low_RisingRight.png`

For each file, use the same canvas dimensions and alignment as the matching
full-height sprite. Keep the wall base in the same place and make the area
above the cut transparent.

## Mode button PNGs

Replace these prepared placeholder PNGs in
`Assets/Art/UI/WallView/Icons/`:

- `WallView_WallsUp.png`
- `WallView_Cutaway.png`
- `WallView_WallsDown.png`

The buttons appear as a horizontal strip in the bottom-right corner. Square,
transparent PNGs are recommended; the UI displays each icon in a 44 x 44 slot.

## Important replacement rule

Overwrite the PNG files in place. Do not delete, rename, or replace their
`.meta` files. The existing metadata preserves the sprite references, pivots,
and import settings, so Unity will update automatically when the PNG contents
change.

If a PNG or its metadata is accidentally recreated, run
`Big Retail > Walls > Refresh Wall View Artwork` once inside Unity.
