# Standard shelf art drop

The standard 2x1 shelf is symmetrical across a half turn, so the fixture
system expects two complete world sprites plus one catalog icon:

- `Fixture_2x1_StandardShelf01_RisingLeft.png`
- `Fixture_2x1_StandardShelf01_RisingRight.png`
- `Fixture_2x1_StandardShelf01_Icon.png`

Each directional PNG must contain the complete fixture. Use a transparent
background and keep both directional canvases aligned consistently. The
installer reuses Rising Left and Rising Right for their opposite 180-degree
views, so no duplicate exports are required.

For each directional world sprite, place the custom pivot on the lowest point
where the fixture base touches the floor. Ignore transparent canvas padding:
the pivot belongs on the actual bottom/front floor-contact point of the art.
The presentation system places that pivot on the front corner of the rotated
footprint, giving multi-tile fixtures the same authored-anchor behavior as
wall sprites. The catalog icon remains centered and does not use this world
pivot convention.

Copy only the PNG contents into the prepared files while Unity is closed; keep
the existing `.meta` files. Then run **Big Retail > Fixtures > Install Initial
Shelf Placement** once. The installer will swap the pylon placeholder for the
complete shelf automatically. If either directional image is missing, the safe
pylon fallback remains active. The dedicated icon is optional, but preferred.
