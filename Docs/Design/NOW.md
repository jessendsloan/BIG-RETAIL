# Big Retail — Now

**Updated:** 2026-08-30

This is the short active-work board. It answers **“What are we finishing
next?”** `CURRENT.md` records the broader accepted design and implementation
state; patch files preserve work that is deliberately deferred.

Keep this board narrow. When an item is complete, move its durable conclusions
to the appropriate topic document and replace it here with the next concrete
closure target.

## Current objective

Turn the opening campaign from a collection of working tools into a convincing,
economically grounded **small retailer that can visibly grow into a megastore**.

## 1. Finish Frank's first complete retail transaction

The immediate vertical slice remains one complete, human-scale retail loop at
Frank's Roadside. Do not begin with a crowd simulation or broad catalog
expansion. Prove one Founder, one stocking task, one customer, and one paid
sale through the permanent systems first.

The opening foundation now proves:

- a deterministic Monday 6:45 AM Campaign start with $2,500 store cash;
- Frank's opening dialogue and the **Get to Work** handoff;
- four Ridgeway chip cases staged in the authored Receiving Area;
- direct, physical case-by-case movement from the supplier load to storage;
- an empty 15-frontage Ridgeway planogram on the center back-wall Half Shelf;
- direct one-item stocking and removal, with three bags per frontage;
- an objective transition from Receiving → stockroom → sales floor;
- layered Half Shelf art, fixture highlighting, planogram ghosts, and
  inventory-driven 1/2/3-bag shelf presentation;
- direction-specific fixture slot anchors authored through the visual Shelf
  Layout Editor; and
- completion at 45 displayed bags with the final three units retained in
  backstock;
- a reusable, employee-compatible Founder stock work order with strict grid
  routing around fixtures, walls, and doors;
- visible Founder travel to storage, case pickup, carried-case presentation,
  one-item-at-a-time shelf stocking, repeated case trips, and return of the
  opened final case; and
- a **Have Founder Stock** fixture command that replaces the instant stocking
  shortcut in Frank's campaign while retaining that shortcut as a sandbox
  fallback.

The direct-control Receiving → storage prototype remains the temporary first
half of the opener. Storage → display now runs through the permanent work seam:
the Founder performs the same physical inventory actions that a future employee
can perform.

Continue in this locked order:

1. **One Customer Journey — next.** Spawn one customer, choose one available product,
   navigate to its customer access point, take it into a basket, travel to
   checkout, pay, and leave.
2. **Staffed Checkout v1.** Make the Founder operate checkout so customer
   service is embodied work that can later be delegated to an employee.
3. **Focused Product Visual Pass.** Ridgeway chips prove the permanent display
   pipeline. Add only enough additional opening presentation to make the first
   completed transaction legible; do not expand the product universe merely
   to postpone proving the loop.
4. **Story Wrapper.** Stage Frank's debt hints around the proven opening shift,
   then add Mr. BIG's arrival, confrontation, title transition, and
   permanent-property handoff.

The dependency chain is:

**Scenario → Founder stocks → Customer shops → Staffed checkout → Revenue →
Story transition**

**Done when:** A clean Campaign begins at the authored morning time, the
Founder stocks at least one real product through an employee-compatible task,
one customer buys it at a staffed checkout, store cash increases atomically,
the customer exits, and the same opening state can be reset and replayed
deterministically.

## 2. Restore a zero-red EditMode test baseline

The complete EditMode suite currently reports **869 passed and 25 failed**.
All 122 focused tests covering Founder work state, route planning, physical
case transfer, the Half Shelf layout, Ridgeway artwork, authored slot anchors,
inventory transfer, planogram behavior, scenario reset, and opening session
flow pass. The remaining failures are stale test or Unity 6.5 fixture issues,
not failures in the opening stocking slice.

The current repair groups are:

- 3 `CellAreaBoundaryResolverTests` collection-count assertions;
- 4 `FoundationApronPreviewResolverTests` fixtures;
- 6 `FoundationAreaPreviewViewTests` fixtures;
- 4 `FoundationDemolitionAreaPreviewViewTests` fixtures;
- 4 `FoundationRuntimeHostTests` fixtures;
- 2 `DoorAssemblyViewTests` prefab-component fixtures; and
- 2 `FixtureShelfMaskGeometryTests` synthetic sprite-geometry fixtures.

Keep this work isolated from production feature changes. Do not disable tests,
hide Console errors, or change production behavior solely to satisfy stale test
setup.

**Done when:** The complete current Unity EditMode suite is zero-red, Frank's
scenario and merchandising tests remain green, Unity compiles without new
errors, and no test has been removed or ignored.

## 3. Close the construction-economy gap

Foundation, sidewalk, floor, wall, finish, door, window, and demolition actions
need real construction prices. Campaign cash exists and is used by purchasing
and fixture equipment, but ordinary construction does not spend it.

The first implementation should provide:

- one data-owned unit price for every player-buildable construction choice;
- a live cost preview for the exact valid cells, edges, or objects in the
  current drag;
- an unaffordable preview state before the player commits;
- one atomic charge only after the complete construction edit validates;
- no partial construction or partial charge when the edit fails;
- an explicit demolition/refund rule shown to the player;
- undo/redo behavior that cannot duplicate or erase money; and
- temporary v0.1 balance values that are easy to tune without rewriting tools.

Before implementation, lock whether ordinary undo fully reverses the original
transaction while later demolition returns salvage, and whether replacing only
a finish credits the removed material.

**Done when:** A player with limited campaign cash can preview, afford, build,
undo, demolish, and fail to afford every opening construction category with a
clear and consistent result.

## 4. Establish authentic store anatomy

Use `MegastoreAnatomy.md` as the active reference. The next playable visual
milestone is a convincing **small neighborhood market**, not a prematurely
enormous supercenter. It needs:

- a readable storefront and entrance;
- a front-end band with checkout and customer service;
- clear customer circulation rather than fixtures scattered in open space;
- a sales floor separated from receiving/backstock;
- a grocery identity built from produce, perimeter refrigeration, dry-grocery
  runs, endcaps, and promotional space; and
- operational reasons for sensible adjacencies and aisle widths.

**Done when:** A screenshot without UI reads immediately as a functioning small
retail store, and its receiving, stocking, checkout, and customer circulation
can all be explained from the layout.

## 5. Expand maximum camera zoom with Lot ownership

The campaign camera should frame the store the player can actually use, not the
entire nine-Lot Property from the beginning. Maximum zoom-out must grow as the
player purchases adjacent Lots. Frank's fixed-footprint location retains its
own authored camera policy.

**Done when:** A new campaign cannot zoom far beyond its starting Lot, each Lot
purchase expands the useful zoom range, the complete Property can be framed at
full ownership, and fixed-footprint locations retain authored bounds.

## 6. Preserve the embodied-player direction

The preferred direction is **Employee Zero**: the player is a persistent Person
and owner-operator, not a separate superhuman species. Physical work should use
the same task actions as employees; direct intent and business authority are
what distinguish the player.

This is a design constraint for the next Founder task and later employee work.
Do not make separate player-only versions of stocking, receiving, checkout,
cleaning, or customer assistance.

## Integration status

GitHub `main` contains the permanent Products → Suppliers → Purchasing →
Delivery → Receiving foundation, the PO/RCV lifecycle and UI-input fixes, and
the ordered physical fixture-equipment loop documented in
`FixtureEquipment.md`.

The current branch `codex/founder-stock-task-v1` adds Frank's reusable Founder
stock work order, grid routing, visible case handling, real carried inventory,
one-item stocking beats, and the campaign fixture command. Its automated
Frank Roadside smoke run fills 45 display units from four 12-unit cases and
returns the final three units to physical backstock. It has not yet been
published to GitHub `main`.

There is separate local wall-finish work in progress. Preserve it before
changing branches or starting the construction-economy implementation.

## Not now

- supplier relationship/account depth;
- a universal permit ladder;
- the complete employee simulation beyond the single Founder task;
- late-game specialist departments;
- final Xbox accessibility conformance; and
- final balance numbers for construction or land.

These remain valid future work. They should not displace the active Frank
transaction closure target.
