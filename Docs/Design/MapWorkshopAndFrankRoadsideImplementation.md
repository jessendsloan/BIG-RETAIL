# Big Retail — Map Workshop and Frank Roadside Implementation Plan

**Status:** Active design / engineering handoff

**Scope:** Reusable location setup, editor-only store authoring, Frank's
roadside prologue store, and deterministic scenario bootstrap

**Related plan:** [Campaign Opener Implementation](CampaignOpenerImplementation.md)

## Why this file exists

Frank's opening store must begin as a complete, functioning shop. Big Retail
currently knows how to construct a store during Play Mode, but it does not yet
have a safe authoring workflow for creating a prebuilt store and loading it as
clean campaign state.

This plan turns the existing runtime tools into an editor-only **Map Workshop**
and separates reusable store layout from scenario-specific starting state. The
Frank location is the first production use of that pipeline and the proving
ground for future authored stores, tutorials, challenge maps, and test scenes.

The copied gameplay scene is a migration scaffold, not the final architecture.
It lets us author Frank's map safely with systems that already work, then
extract the reusable location shell after both scenes are proven.

## Locked outcome

The completed pipeline must let the team:

1. create or duplicate a gameplay-compatible location;
2. author its exterior Tilemap and location markers in Edit Mode;
3. enter an editor-only Workshop session and build with the real construction
   and fixture tools;
4. explicitly capture the result into a versioned `StoreLayoutAsset`;
5. create a separate `StoreScenarioAsset` containing inventory, time, cash,
   deliveries, characters, and story bootstrap;
6. validate both assets without mutating the scene or template;
7. load the same result deterministically every time;
8. ship no editor-only code or Workshop mode in the player build.

Frank's store is successful when it opens already built, stocked, staffed for
the scripted beat, and ready for the player to run through the permanent retail
simulation.

## Core architectural law

> **A location supplies authored geography and policy. A layout supplies the
> built store. A scenario supplies the starting situation. Permanent runtime
> systems operate all three.**

Do not create a prologue-only construction format, inventory model, customer
simulation, checkout, receiving path, or fixture system.

## Current inspected baseline

The plan begins from verified seams in the current project:

- [`Gameplay.unity`](../../Assets/Scenes/Gameplay.unity) contains the working
  runtime shell and the main Property in one scene;
- its isometric Grid uses a 1 × 0.5 cell size;
- the current map ID is `bigretail.map.main_property`;
- `Map/Grid/MapVIsuals`, `MapAreaMask`, and `ConstructionAreaMask` are distinct
  authored Tilemaps and are included in the rotating authored presentation;
- [`GridMapHost.cs`](../../Assets/Scripts/Gameplay/Map/Unity/GridMapHost.cs)
  currently creates a nine-region `LandRegionCatalog` for every initialized
  map, which is the concrete seam that must become location-policy driven;
- [`IsometricViewHost.cs`](../../Assets/Scripts/Gameplay/Map/Unity/View/IsometricViewHost.cs)
  already snapshots and rotates authored Tilemaps from canonical logical cells;
- `CameraBounds` is a separate scene object and must remain a distinct policy
  from construction eligibility and land ownership.

These facts explain why a copied Gameplay scene is the safe first scaffold.
They are an implementation baseline, not permission to duplicate the combined
scene architecture permanently.

---

# Part 1 — Location architecture

## 1. Shared runtime shell

Every gameplay-compatible location needs the same runtime shell:

- Grid and required authored Tilemaps;
- permanent construction, wall, opening, finish, fixture, and department hosts;
- inventory, purchasing, receiving, checkout, time, customer, employee, and UI
  systems required by the selected game mode;
- camera, input, rotation, view, preview, and selection infrastructure;
- stable references to the location definition, layout asset, and optional
  scenario asset.

The scene should supply only location-specific facts:

- map identity and schema/fingerprint information;
- exterior/background art;
- playable footprint and construction mask;
- land policy;
- camera bounds and camera policy;
- entrances, delivery approach, receiving/staging points, vehicle spaces, and
  cinematic markers;
- default layout and scenario references.

## 2. Location definition and land policy

The existing main Property and Frank's store intentionally use different land
rules. Do not force both through the nine-Lot catalog.

Each location must select an explicit land policy:

- **Purchasable Lots** — the Mr. BIG Property uses nine 32 × 32 Lots, starting
  with the front corner and expanding through purchases.
- **Fixed Footprint** — Frank Roadside grants its complete authored footprint
  and has no Lot-purchase UI or progression.
- **Future authored policy** — later challenge or sandbox locations may choose
  a full map, a custom region set, or another explicit provider.

`GridMapHost` and related construction checks must depend on the selected
policy instead of assuming that every map owns a `LandRegionCatalog`.

## 3. Camera policy belongs to the location

Camera bounds are not interchangeable with construction or ownership masks.
Each location needs its own authored camera bounds and zoom policy.

- The Mr. BIG Property expands maximum zoom-out from the currently owned Lot
  footprint and reaches full-Property framing at complete ownership.
- Frank Roadside uses fixed bounds appropriate to its smaller authored map.
- Rotation and view framing must remain valid at every supported zoom level.
- Purchasing land changes the permitted range; it should not snap the camera
  unless the current position or zoom has become invalid.

This rule is also tracked as an active closure item in [NOW](NOW.md).

---

# Part 2 — Authored data

## 4. `StoreLayoutAsset`

The layout asset records the physical store independent of a particular
opening shift. It should use plain serializable records and stable definition
IDs rather than scene-object references wherever practical.

Required data:

- schema version;
- map/location ID and map fingerprint;
- authored layout ID and display name;
- logical grid origin and orientation;
- owned Lots or fixed-footprint entitlement;
- foundations and sidewalks;
- floor cells and floor-finish IDs;
- wall edges, wall type, and both face-finish IDs;
- doors and windows, including orientation;
- installed fixtures and uninstalled fixture plans, each with stable fixture
  instance IDs, definition IDs, cells, and rotation;
- department/area assignments;
- receiving-area cells;
- any other permanent placed construction needed to reproduce the store.

Layout coordinates must remain logical grid coordinates. Do not serialize
screen positions, transient view objects, preview state, undo history, or
scene-instance IDs.

## 5. `StoreScenarioAsset`

The scenario asset describes a repeatable starting situation layered on top of
a layout. It must not be folded into the layout merely because Frank's opener
is the first consumer.

Required data:

- schema version and scenario ID;
- referenced location and layout IDs;
- starting day, time, simulation speed, and store cash;
- fixture planograms and merchandising assignments;
- display inventory and backstock inventory;
- checkout configuration;
- scheduled and ready delivery state where required;
- Founder, Frank, customer, employee, and Mr. BIG spawn markers/roles;
- vehicle, arrival, cinematic, and interaction markers;
- opening objectives and story-state flags;
- deterministic random seed where simulation startup requires one.

The first Frank scenario should be named clearly, for example
`FrankOpeningShiftScenario`, and remain reusable for reset and automated tests.

## 6. Stable identity and versioning

Both assets must follow these non-negotiable rules:

- use stable catalog IDs for products, finishes, fixtures, doors, windows, and
  other definitions;
- use stable instance IDs when later records refer to a placed object;
- store a schema version and reject unsupported versions with a useful error;
- store and verify a map fingerprint before applying coordinates;
- serialize records in a deterministic order;
- never silently substitute an unknown definition ID;
- never mutate the template asset while running, resetting, or testing a
  scenario;
- migrate deliberately when a schema changes instead of guessing.

---

# Part 3 — Map Workshop

## 7. Workshop shape

The Map Workshop is an **editor-only authoring workflow that runs the real
store tools in Play Mode**. It is not a shipping Game Mode and should not appear
in campaign or sandbox menus.

The Workshop should launch a normal gameplay-compatible scene with an
editor-only flag that:

- suppresses construction prices, campaign objectives, land purchases,
  deliveries, and other unwanted simulation side effects;
- exposes all definitions needed for authoring;
- allows the real construction, finish, opening, fixture, area, undo, and redo
  paths to operate;
- enables explicit layout capture and validation;
- makes dirty/unsaved state unmistakable.

The Tile Palette remains the correct tool for decorative exterior art. The
Workshop owns simulated store state.

## 8. Workshop window

Provide one focused editor window with:

- location/scene selector;
- layout selector;
- optional scenario selector;
- **Build / Enter Workshop**;
- **Test Scenario**;
- **Save As New Layout**;
- **Update Selected Layout**;
- **Validate**;
- **Reload From Asset**;
- clear dirty-state indicator;
- exact validation errors that identify the record, coordinate, and missing or
  conflicting definition;
- confirmation before overwriting an existing asset.

Saving must be explicit. Exiting Play Mode must not silently overwrite an
asset, because an accidental or half-finished session must remain disposable.

## 9. Capture behavior

Capture reads canonical simulation/model state after a clean validation pass.
It must not scrape sprites or infer layout from visual children when an
authoritative model already exists.

Capture should:

1. freeze authoring input;
2. validate location identity and required hosts;
3. collect records from permanent state services;
4. normalize logical coordinates and deterministic ordering;
5. validate the candidate layout in memory;
6. write a new or deliberately selected asset;
7. mark the asset dirty and save through Unity's editor asset workflow;
8. report a concise capture summary and any warnings.

## 10. Load behavior

Layout loading must be transactional and side-effect free. The recommended
order is:

1. verify location ID, fingerprint, and schema version;
2. resolve every referenced definition ID;
3. validate coordinates, duplicates, overlaps, and required support;
4. stop before mutation if any error exists;
5. clear the current authored store state through canonical services;
6. apply entitlement/owned-footprint state;
7. restore foundations, sidewalks, floors, walls, and finishes;
8. restore doors and windows;
9. restore installed fixtures and their stable instance IDs;
10. restore uninstalled fixture plans without creating orders, inventory, or
    deliveries;
11. restore departments and receiving areas;
12. rebuild views, masks, pathfinding, and derived caches once;
13. publish one completion event after the whole store is valid.

Loading a template must not spend cash, create purchase history, record undo
commands, fire campaign construction objectives, or award progression.

## 11. Scenario bootstrap order

After the layout is complete, scenario loading should:

1. validate the complete scenario and all cross-references;
2. restore time, cash, and simulation settings;
3. apply fixture planograms and checkout configuration;
4. seed display and backstock inventory;
5. create delivery state;
6. spawn characters and vehicles at authored markers;
7. initialize story state and objectives;
8. rebuild dependent simulation state;
9. release input or begin the opening sequence.

A failed scenario validation must leave the loaded layout intact and report the
failure without half-applying inventory or story state.

---

# Part 4 — Frank Roadside

## 12. Scene strategy

Begin with a copy of the current `Gameplay` scene named clearly, such as
`FrankRoadside`. This is the safest first scaffold because it retains known-good
hosts, catalogs, UI, input, cameras, Grid/Tilemaps, and permanent retail
systems.

For the first pass:

- keep shared systems, cameras, UI, hosts, views, previews, and Grid structure;
- replace only the authored exterior/environment layer;
- assign a new stable map ID, for example `bigretail.map.frank_roadside`;
- select the fixed-footprint land policy;
- author Frank-specific bounds and markers;
- attach `FrankStoreLayout` and `FrankOpeningShiftScenario`;
- validate the copied scene and the original `Gameplay` scene after every
  extraction step.

Only after both scenes work should duplicated runtime objects be moved into a
shared scene, prefab, installer, or location-creation command. Do not perform a
full scene-framework rewrite before the first Frank map is playable.

## 13. Environment direction

Frank's location should feel like an older independent roadside shop, inspired
by the atmosphere of a worn rural commercial strip rather than copied from
another game's map.

Target traits:

- a 96 × 32 construction footprint, arranged as three 32 × 32 authoring
  sections but granted as one fixed-footprint location at runtime;
- dirt/gravel road and worn parking apron;
- weeds, scrub, trees, drainage, utility poles, and uneven roadside edges;
- a modest older storefront and small parking area;
- rear delivery access and a believable small receiving point;
- a clear exterior arrival/parking position for Mr. BIG;
- interior and exterior cinematic staging markers;
- enough surrounding land to frame the scene without suggesting normal Lot
  expansion.

Atmosphere can be borrowed; geography and recognizable landmarks should be
original to Big Retail.

## 14. Scene hierarchy contract

Preserve the existing functional separation under `Map/Grid`:

- `MapVIsuals` — authored ground, road, dirt, vegetation, and decorative map
  art;
- `MapAreaMask` — cells that belong to the location;
- `ConstructionAreaMask` — cells on which construction is permitted;
- view Tilemaps — foundations, sidewalks, floors, walls, finishes, doors,
  windows, fixtures, departments, receiving, and other simulated state;
- preview Tilemaps — transient tool feedback only;
- host objects — authoritative runtime services and presentation bridges.

The exact current names may be normalized later, but their responsibilities
must remain distinct. Decorative edits must not be used as an implicit source
of truth for ownership or construction eligibility.

## 15. Division of work for the first map

Jesse's safe handoff is deliberately narrow:

> Edit only `Map/Grid/MapVIsuals` in the copied Frank scene to create the new
> exterior map.

Codex/engineering then owns:

- copying and naming the scene;
- confirming the Grid and Tilemap hierarchy;
- updating map ID and location policy;
- rebuilding `MapAreaMask` and `ConstructionAreaMask` from the finished map;
- setting camera bounds and rotation-safe framing;
- authoring entrances, delivery, vehicle, character, and cinematic markers;
- generating or updating the layout and scenario assets;
- validating views, systems, build inclusion, and the original Gameplay scene.

This keeps the art edit comfortable while ensuring every hidden layer and
runtime contract is updated by someone who has inspected the full scene.

---

# Part 5 — Implementation order

## Phase A — Data and validation foundation

- [x] Define versioned layout and scenario record types.
- [x] Define stable location identity, fingerprint, and land-policy interface.
- [x] Implement definition resolution and complete preflight validation.
- [x] Add deterministic serialization/order rules.
- [x] Add EditMode tests for valid and invalid assets.

## Phase B — Runtime loader

- [x] Load a tiny hand-authored layout into a test location.
- [x] Restore every construction category through canonical state services.
- [ ] Rebuild derived views and caches once after the transaction.
- [x] Prove bootstrap loading creates no cost, undo, history, or objective
  side effects.
- [x] Prove repeated reset produces identical state.

The first integration proof runs against the real Frank Roadside runtime
composition. It restores foundations, sidewalks, finished floors, finished
walls, a window opening, a fixture with stable identity, a department, and a
Receiving cell; loads the same template twice; compares the complete canonical
snapshots; verifies the template asset is unchanged; and confirms construction
undo/redo history remains untouched. The loader calls permanent state services
directly, outside tool, price, purchase-history, and campaign-objective paths.

For safety, this first loader requires the layout's authored land entitlement
to match the location's current policy exactly. That is the correct behavior
for Frank's fixed footprint. Explicit mutable owned-Lot restoration for the
main Property remains a later loader extension before a Property template is
introduced.

## Phase C — Workshop capture and editor UI

- [x] Add the editor-only Workshop launch flag.
- [x] Add the focused Workshop window and explicit save/update flow.
- [x] Capture canonical runtime state into a layout asset.
- [x] Add dirty-state and overwrite protection.
- [x] Complete a build → save → reload → build round trip.

Open **Big Retail → Map Workshop → Open Workshop** to select a location scene
and optional existing layout, then enter a real Sandbox-backed Play Mode
Workshop. The distinct editor-only flag grants the established unrestricted
land policy, suppresses Campaign opening objectives through the existing
session mode, and selects unlimited construction undo/redo. Saving remains an
explicit action: new layouts cannot overwrite an existing path, updates require
confirmation, and reload warns before discarding a dirty runtime draft.

Capture reads only permanent runtime model hosts, creates a canonical snapshot,
and runs the complete location-aware validator before asset persistence. The
integration proof builds the tiny Frank layout, captures it, creates and
reimports a real `StoreLayoutAsset`, rejects an accidental second create at the
same path, rebuilds from the saved asset, and compares the complete canonical
state. Scenario selection and **Test Scenario** remain visibly reserved for
Phase D rather than implying an incomplete scenario bootstrap is ready.

## Phase D — Scenario bootstrap

- [ ] Add inventory, planogram, checkout, time, cash, delivery, spawn, and story
  records.
- [ ] Add transactional scenario validation and loading.
- [ ] Add reset and deterministic startup tests.
- [ ] Prove a seeded store can stock, sell, checkout, and earn revenue.

## Phase E — Frank Roadside production use

- [x] Duplicate `Gameplay` into the Frank scaffold.
- [x] Replace `MapVIsuals` with the dirt-road environment.
- [x] Configure fixed land policy, masks, camera bounds, and base markers.
- [ ] Author and validate `FrankStoreLayout`.
- [ ] Author and validate `FrankOpeningShiftScenario`.
- [ ] Run the complete opening retail loop in the Frank scene.

The finalized location baseline validates a complete 96 × 32 construction
mask, frames the camera from the authored map footprint, and supplies
rotation-aware stable markers for the store center, roadside arrival, and rear
service approach. Exact character and cinematic staging remains scenario work
and should be positioned after the prebuilt store footprint is authored.

## Phase F — Shared-shell extraction

- [ ] Compare the proven Gameplay and Frank scenes.
- [ ] Extract only genuinely identical runtime objects and setup.
- [ ] Add an editor command such as **Create / Validate Location** to prevent
  future hierarchy drift.
- [ ] Revalidate both scenes and a player build after every extraction step.
- [ ] Wire Frank's scene into the campaign opener and permanent-property
  transition.

---

# Part 6 — Validation and acceptance

## 16. Required automated coverage

### EditMode

- [ ] Layout record round trip preserves canonical equality.
- [ ] Deterministic capture produces stable ordering.
- [ ] Unknown definition IDs are rejected.
- [ ] Unsupported schema versions and wrong map fingerprints are rejected.
- [ ] Duplicate cells, edges, instance IDs, and invalid overlaps are rejected.
- [ ] Wall faces, openings, fixtures, departments, and receiving records retain
  orientation and ownership correctly.
- [ ] Scenario cross-references resolve against the selected layout.
- [ ] Editor-only assemblies do not leak into runtime assemblies.

### PlayMode

- [ ] A tiny layout loads into a clean scene with correct canonical state and
  views.
- [ ] Capture → clear → reload reproduces the same layout.
- [ ] Five consecutive scenario resets produce the same store, inventory, time,
  characters, and story state.
- [ ] Template assets remain byte-for-byte/logically unchanged after runtime
  loading and coordinate-origin shifts.
- [ ] Bootstrap generates no cash charges, undo entries, purchasing history, or
  construction objectives.
- [ ] Frank's seeded loop supports Inventory → Shelf → Customer → Checkout →
  Revenue.
- [ ] Original Gameplay remains valid after Frank-scene changes.

### Build validation

- [ ] Both gameplay scenes compile and open without Console errors.
- [ ] Required scenes and runtime assets are included in the build.
- [ ] Map Workshop UI and editor assemblies are absent from the player.
- [ ] No runtime asset depends on `UnityEditor`.
- [ ] Campaign transition selects the correct location, layout, and scenario.

## 17. End-to-end acceptance flow

The authoring path is complete when this sequence works:

1. Create or duplicate a compatible location.
2. Author its exterior Tilemap and markers.
3. Enter Map Workshop.
4. Build with the permanent tools.
5. Validate and explicitly capture the layout.
6. Author the scenario.
7. Reload from a clean state.
8. Run the same opening shift five times with identical startup.
9. Build the player with no editor-only dependency.

Frank's store is complete when a clean campaign can:

1. Load Frank Roadside.
2. Restore the prebuilt store.
3. Seed merchandise and checkout.
4. Spawn Founder and Frank.
5. Let the player stock, serve, sell, and earn revenue.
6. Stage Mr. BIG's arrival and confrontation.
7. Transition to the permanent Property with the same Founder.

---

# Deferred until the first production round trip works

- a universal room/building editor;
- live collaborative map authoring;
- arbitrary player-facing Workshop access;
- automatic save on Play Mode exit;
- a large character or cinematic editor;
- generalized procedural map generation;
- support for every imaginable future location policy;
- broad shared-shell refactors that are not required by both proven scenes.

These may become worthwhile later. They must not delay the first safe,
repeatable Frank Roadside authoring pipeline.

## Source references

The implementation should follow current Unity guidance for
[Play Mode](https://docs.unity3d.com/Manual/GameView.html),
[ScriptableObject](https://docs.unity3d.com/Manual/class-ScriptableObject.html),
[script serialization](https://docs.unity3d.com/Manual/script-serialization.html),
[assembly definitions](https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html),
[Tilemaps](https://docs.unity3d.com/Manual/Tilemap.html), and the
[Unity Test Framework](https://docs.unity3d.com/Packages/com.unity.test-framework@latest).

Those references support the workflow; this document remains Big Retail's
accepted project-specific source of truth.
