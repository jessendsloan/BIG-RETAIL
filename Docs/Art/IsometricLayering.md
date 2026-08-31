# Isometric Layering Contract

Big Retail uses one display-cell depth rule for walls, fixtures, deliveries,
characters, and anything a character carries. A world object must not choose a
fixed global sorting order merely because its internal artwork has multiple
layers.

## World Depth

The current view projection converts logical cells into display cells. World
depth is the display cell's `X + Y` value. Smaller display depth is closer to
the viewer and therefore receives the larger sorting order.

`IsometricRenderOrderResolver` owns the shared numeric contract:

- cell occupant: `200 - (display depth * 2)`;
- wall between cells: the reserved odd order between the two cell orders;
- wall pylons: a separate higher band used only for wall seams;
- UI and explicit editing overlays: separate presentation bands, never world
  depth substitutes.

The doubled cell step is intentional. It leaves one sorting order between two
neighboring cell centers so their shared wall can sit between them.

`WallRenderOrderResolver` consumes this central contract for wall boundaries,
presentation heights, pylons, and equal-depth directional seam priority. It is
not a second world-depth authority.

## Static World Objects

Fixtures, delivery pallets, and other cell-owned views resolve their root order
from their presentation cell when their view is built. Multi-layer fixtures use
a root `SortingGroup`; shelf and product orders are local offsets inside it.

## Moving World Objects

Characters and other moving world objects use `IsometricDepthSortingGroup`.
The component reads the map cell beneath its ground-contact anchor during
`LateUpdate`, then places the complete root `SortingGroup` into the cell-occupant
order. The anchor is normally the character root at its feet.

Body parts and carried objects remain children of that group:

- body-part orders describe anatomy only;
- a carried object's order describes whether it is behind or in front of the
  torso and hands;
- neither may use its local order to compete directly with walls or fixtures.

Future worker and customer spawners must configure this component with the
active `IsometricViewHost` and coordinate tilemap. Use a nonzero root offset
only for two world objects that deliberately share one cell; do not use offsets
to repair incorrect anchors.

## Visual Rule of Thumb

The feet decide world depth. The object's children decide internal depth.
Overlays decide interaction emphasis. These are three separate questions and
must remain separate in code and authored art.
