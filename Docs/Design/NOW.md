# Big Retail — Now

**Updated:** 2026-08-27

This is the short active-work board. It answers **“What are we finishing
next?”** `CURRENT.md` records the broader accepted design and implementation
state; patch files preserve work that is deliberately deferred.

Keep this board narrow. When an item is complete, move its durable conclusions
to the appropriate topic document and replace it here with the next concrete
closure target.

## Current objective

Turn the opening campaign from a collection of working tools into a convincing,
economically grounded **small retailer that can visibly grow into a megastore**.

## 1. Stop camera movement from navigating the UI

WASD camera movement currently also moves through focused UI options. Camera
control and UI navigation must not consume the same keyboard input during
ordinary gameplay.

The fix must preserve intentional keyboard and controller navigation:

- WASD moves the camera without changing UI focus or selection;
- Tab, arrow keys, and controller navigation continue to operate the UI where
  supported;
- an open modal, text field, or other input-capturing interface can deliberately
  suppress camera movement;
- closing the interface restores camera input without requiring an extra click;
- pointer hover alone does not unexpectedly redirect keyboard focus.

**Done when:** Holding or tapping every WASD direction while construction and
management panels are open moves only the camera, and the same panels remain
fully navigable through their intended keyboard and controller controls.

## 2. Repair the PO and Receiving controls

The **PO** and **RCV** controls currently appear without the visible button
background used by the surrounding toolbar controls. They must read as
interactive buttons in their normal, hovered/focused, selected, and disabled
states.

The Purchasing overlay also has a blocking lifecycle bug: it closes correctly
the first time, but after reopening it, the close button no longer works and the
player becomes trapped in the PO screen.

The fix must ensure:

- PO and RCV use the same clear button-container treatment as equivalent HUD
  controls;
- hover, keyboard/controller focus, selected, and disabled states remain
  visually distinct;
- Purchasing can be opened and closed repeatedly without losing its close
  action, input routing, or underlying gameplay controls;
- closing Purchasing clears any stale overlay, focus, tooltip, or input-blocking
  state before it is opened again;
- the repeat cycle works with pointer input and intended keyboard/controller
  navigation.

**Done when:** PO and RCV are visibly recognizable as buttons, and Purchasing
passes at least five consecutive open → close cycles during one play session
without trapping the player or requiring an extra click.

## 3. Close the construction-economy gap

Foundation, sidewalk, floor, wall, finish, door, window, and demolition actions
need real construction prices. At present, campaign cash exists and is used by
purchasing, but physical construction does not spend it. Fixtures are handled
separately as ordered physical equipment in the next item.

The first implementation should provide:

- one data-owned unit price for every player-buildable construction choice;
- a live cost preview for the exact valid cells, edges, or objects in the
  current drag;
- an unaffordable preview state before the player commits;
- one atomic charge only after the complete construction edit validates;
- no partial construction or partial charge when the edit fails;
- an explicit demolition/refund rule shown to the player;
- undo/redo behavior that cannot duplicate or erase money;
- temporary v0.1 balance values that are easy to tune without rewriting tools.

Before implementation, lock these two economy decisions:

1. whether ordinary undo is a full transaction reversal while deliberate later
   demolition returns only a salvage percentage;
2. whether changing only a finish charges the new finish in full or credits the
   replaced material.

**Done when:** A player with limited campaign cash can preview, afford, build,
undo, demolish, and fail to afford every opening construction category with a
clear and consistent result.

## 4. Make fixtures ordered physical equipment

Fixtures must not appear from an unlimited construction palette. They are
physical business equipment that the player orders, receives, owns, places,
moves, stores, and may eventually resell or scrap.

The fixture-equipment loop should provide:

- a dedicated **Equipment Catalog**, separate from merchandise purchasing;
- fixture price, order quantity, availability, and delivery timing;
- free translucent planning before the equipment is owned;
- an **Order Required Equipment** action for planned layouts;
- equipment deliveries that reuse campaign cash, scheduling, delivery, and
  Receiving infrastructure without becoming merchandise POs;
- an Owned Equipment count for every fixture definition;
- placement that consumes one owned unit;
- movement that relocates the same physical unit without charging again;
- removal that returns the unit to equipment storage instead of destroying it;
- explicit resale and scrap behavior rather than accidental deletion;
- starter equipment that prevents the opening tutorial from deadlocking while
  the first delivery is pending;
- bulk equipment pallets or a pallet-build workflow so large expansions do not
  require opening and carrying dozens of identical boxes one at a time;
- a future seam for employees to unload, assemble, and install planned
  equipment.

**Locked rule:** Fixtures are ordered, delivered physical equipment. Planning
is free; installation consumes owned equipment.

**Done when:** The player can plan a fixture layout, order its missing
equipment, receive the shipment, place the owned units, move and store them,
and redeploy them without creating, losing, or paying twice for equipment.

## 5. Establish authentic store anatomy

Use `MegastoreAnatomy.md` as the active reference. The target is not to force
players to reproduce one Walmart floor plan. The simulation should create the
same pressures that make real large stores converge on recognizable layouts.

The next playable visual milestone is a convincing **small neighborhood
market**, not a prematurely enormous supercenter. It needs:

- a readable storefront and entrance;
- a front-end band with checkout and customer service;
- clear customer circulation rather than fixtures scattered in open space;
- a sales floor separated from receiving/backstock;
- a grocery identity built from produce, perimeter refrigeration, dry-grocery
  runs, endcaps, and promotional space;
- operational reasons for sensible adjacencies and aisle widths.

**Done when:** A screenshot without UI reads immediately as a functioning small
retail store, and its receiving, stocking, checkout, and customer circulation
can all be explained from the layout.

## 6. Expand maximum camera zoom with Lot ownership

The campaign camera should frame the store the player can actually use, not
the entire nine-Lot Property from the beginning. Maximum zoom-out must grow as
the player purchases adjacent Lots.

The camera rule should provide:

- an opening zoom limit that keeps the starting corner Lot readable and useful;
- a larger maximum zoom-out after each Lot purchase, based on the current owned
  footprint rather than a fixed campaign-wide value;
- enough framing margin to understand newly purchased land without revealing
  large amounts of irrelevant or inaccessible space;
- smooth clamping when ownership changes, without needlessly snapping the
  player's current camera distance;
- full-Property framing once all nine Lots are owned;
- a fixed authored camera policy for locations without purchasable Lots, such
  as Frank's roadside store.

**Done when:** A new campaign cannot zoom out far beyond its starting Lot, each
Lot purchase visibly expands the useful zoom range, the complete Property can
be framed at full ownership, and fixed-footprint locations retain their own
authored camera bounds.

## 7. Preserve the embodied-player direction

The preferred direction is **Employee Zero**: the player is a persistent Person
and owner-operator, not a separate superhuman species. Physical work should use
the same task actions as employees; direct intent and business authority are
what distinguish the player.

This is a design constraint for later employee work, not the next large feature
to implement. New task systems should avoid making separate “player-only”
versions of stocking, receiving, checkout, cleaning, or customer assistance.

## 8. Lock the campaign opener to morning

Frank's opening objective tells the player to get the store ready for the
morning, but Campaign currently inherits a late-night simulation time. Give
Frank's Roadside a deliberate canonical opening day and time—approximately
6:30–7:00 AM—before the player receives control.

The startup rule must preserve ordinary clock progression after the opener and
must not overwrite time when loading a later campaign save, launching Sandbox,
or entering Map Workshop.

**Done when:** A new Campaign begins at the authored morning time, the objective
and lighting agree with that time, and existing saves and development workflows
retain their own clock state.

## Integration status

GitHub `main` contains the opening commercial catalog, supplier delivery and
receiving work, windows/sidewalk panel work, and the UI clarity patch through
pull request 23. No known feature bridge remains between those completed
branches.

There is separate local wall-finish work in progress. Preserve it and resolve
it before changing branches or starting the construction-economy implementation.

## Not now

- supplier relationship/account depth;
- a universal permit ladder;
- the complete employee simulation;
- late-game specialist departments;
- final Xbox accessibility conformance;
- final balance numbers for construction or land.

These remain valid future work. They should not displace the active closure
targets above.
