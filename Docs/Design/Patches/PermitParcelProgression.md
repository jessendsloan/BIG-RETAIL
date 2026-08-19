# Design Patch — Permit, Land Region & Department Progression

**Status:** Partially locked progression direction / Full progression schedule not yet locked

**Purpose:** Preserve the progression model in which Big Retail grows by qualifying for permits, acquiring more of the property, building the infrastructure required by a larger retailer, and then opening more sophisticated departments.

This patch now contains several locked map/progression facts, but it does **not** yet define the final permit ladder or department unlock schedule. It should not replace the current merchandise-circulation implementation target.

---

## Locked property foundation

The current Big Retail property is a **96 × 96 tile square**.

That gives the full property:

- **9,216 individual build tiles**;
- divided evenly into a **3 × 3 grid of land regions**;
- each land region is **32 × 32 tiles**;
- each land region therefore contains **1,024 tiles**;
- there are **9 total land regions**;
- the player begins with **1 region**;
- the remaining **8 regions** are acquired through progression.

### Terminology

Use these terms consistently:

- **Tile** — one individual construction/grid cell.
- **Land Region** — one 32 × 32 expansion block containing 1,024 tiles.
- **Property** — the entire eventual 96 × 96, 9-region site.

Avoid using `parcel` as the primary gameplay/design term for these chunks unless a later UI/narrative reason makes it useful. `Land Region` is clearer for implementation discussions.

---

## Starting region — the literal corner store

The starting 32 × 32 land region is the **front corner region at the road intersection** on the current map.

This is now the preferred opening geography because it makes the player's beginning literally a **corner store**.

The player does not begin with the entire 96 × 96 construction area commercially available. Their owned/developed site is initially only this 1,024-tile corner region.

The eventual mega-retailer therefore grows outward from the exact physical location of the original corner store. The opening footprint remains embedded in the final property as a visible history of the player's growth.

This is an important presentation and progression payoff, not merely a technical subdivision.

---

## First land purchase — Milton tutorial handoff

Milton "Mr. BIG" Big should guide the player's **first land-region acquisition**.

The intent is tutorialization, not permanent control over expansion.

The first purchase should teach the permanent interaction:

**Select eligible adjacent land region → inspect price / requirements → purchase → owned construction area expands**

This first purchase will likely happen during the early growth path toward Grocery, when the opening corner region can no longer comfortably support the next set of requirements.

Milton's involvement fits his established role:

- tutor;
- safety net;
- commercial opponent / facilitator.

He can introduce the opportunity in-world because he knows the property/business situation, rather than because the game needs a disembodied tutorial box.

After this first guided purchase, the land system should become player-directed.

---

## Player-directed acquisition after the tutorial

After Milton teaches the first purchase, the player should generally be able to acquire additional land regions **at will when they are eligible and affordable**.

The campaign should not require Milton to personally unlock or present every one of the remaining seven purchases.

The player chooses how the property grows.

A land region may be purchasable subject to concrete rules such as:

- adjacency to currently owned property;
- current permit / commercial qualification;
- purchase price;
- campaign availability where genuinely necessary;
- other future ownership rules.

The exact eligibility rule is not fully locked yet, but the design goal is clear:

> **Land acquisition is a recurring management decision after the first tutorialized purchase, not a sequence of eight scripted rewards.**

A newly acquired region does not prescribe its use.

The player may devote it to:

- parking;
- sales floor;
- receiving;
- storage;
- utilities;
- employee facilities;
- specialized department infrastructure;
- circulation;
- future expansion reserve.

That allocation decision is the gameplay reward.

---

## Core progression hypothesis

Big Retail should avoid arbitrary XP-style department unlocks where possible.

The stronger progression chain is:

**Operate current store → satisfy concrete requirements → qualify for permit / expansion capability → acquire additional land when needed → build supporting infrastructure → become capable of operating new departments → create new requirements at the next scale**

The permit is therefore not the department itself.

It is a visible progression gate that says the player's retail institution has become qualified to build or operate a more advanced class of capability.

The thing being upgraded is the **commercial institution and property**.

---

## The Anno-like loop

The useful Anno comparison is structural rather than literal.

Anno asks the player to satisfy requirements in order to move into a more capable economic tier.

Big Retail can do the same through the store itself:

**Operate current store**
→ **Satisfy capacity / infrastructure / commercial requirements**
→ **Qualify for next permit or expansion capability**
→ **Acquire more property as needed**
→ **Build new supporting systems**
→ **Open new department capability**
→ **Attract broader demand and create larger bottlenecks**
→ **Stabilize the larger store**
→ **Repeat at the next scale**

Progression should therefore be visible in the physical store rather than living only in a menu.

---

## Permits as progression gates

Permits should represent permission or qualification, not magical content licenses.

A permit may unlock one or more of the following:

- permission to acquire additional land regions;
- a larger allowed building footprint;
- additional construction types;
- specialized infrastructure;
- new department classes;
- higher-capacity parking or receiving facilities;
- specialized service spaces;
- future regulatory or professional requirements where appropriate.

The exact permit names and tiers are **not yet locked**.

A permit should normally be earned through concrete, readable store facts rather than an abstract XP meter.

Possible qualification facts include:

- parking / arrival capacity;
- receiving capability;
- utility capacity;
- building size or developed floor area;
- employee facilities;
- operational staffing capability;
- store revenue, profitability, or sustained operating history;
- existing department operation;
- safety / security capability;
- customer throughput;
- property ownership.

Not every permit should require every fact.

---

## Permit → Infrastructure → Department

Keep these concepts separate.

### Permit

**Are we allowed / qualified to build or operate this class of capability?**

### Infrastructure

**Can this physical property actually support it?**

### Department

**Have we built, supplied, and staffed the commercial operation itself?**

This avoids a permit functioning as a one-click department unlock.

### Grocery example direction

Grocery may ultimately require some combination of:

- sufficient parking / arrival capacity;
- adequate receiving access;
- food storage capability;
- refrigeration / utility support where appropriate;
- suitable fixtures;
- grocery supplier access;
- enough labor;
- enough land / building area;
- an appropriate early permit or commercial qualification.

The exact Grocery requirements remain open.

The important design law is that Grocery appears because the store has become **capable of supporting Grocery**, not because an arbitrary level number was reached.

---

## Employees are a requirement, not the whole progression ladder

An earlier idea considered making increasingly capable employees the direct equivalent of Anno population tiers.

The stronger current direction is mixed:

- **Permits** gate classes of expansion or construction.
- **Property and infrastructure** determine what the site can physically support.
- **Employees** determine what the store can actually operate.
- **Suppliers** determine what merchandise can be sourced.
- **Customers / demand** determine whether the new capability is commercially worthwhile.

Specialized employees can therefore be required for specialized departments without forcing the entire progression game to revolve around an employee-tier ladder.

---

## Relationship between permits and land regions

The exact interaction is still open, but the likely direction is a **mixed model**.

Permits and land solve different problems:

- **Permit:** what the retailer is qualified or allowed to do.
- **Money:** whether the retailer can afford the expansion.
- **Land:** where the retailer can physically do it.

Possible rules include:

- early permit progression makes the first adjacent region purchasable;
- later permits raise the number/class of regions the player may acquire;
- some regions may simply become available once adjacent and affordable;
- department requirements may themselves create the practical need for more land.

Do not hard-code every department unlock to a land purchase. The systems should interact without becoming identical.

---

## Existing implementation seam

The current map code already contains a useful conceptual separation.

`Assets/Scripts/Gameplay/Map/Construction/ConstructionAreaDefinition.cs` defines cells that are physically eligible for construction while explicitly noting that **ownership, progression, cost, conflicts, and other rules may still reject a construction request**.

This is a strong seam for the region system.

A future ownership/progression layer can potentially sit above the authored construction area:

**GridMap / ConstructionArea**
→ what physically exists and could ever be built on

**Land Region ownership**
→ which 32 × 32 region(s) the player currently owns

**Progression / permit rules**
→ which unowned adjacent region(s) may currently be purchased

**Construction rules**
→ whether a particular action is legal on an owned tile

This patch does not prescribe the final class architecture, but gameplay implementation should preserve this separation.

---

## Gameplay implementation handoff

If this system is prototyped now, the smallest useful vertical slice is:

1. Represent the 96 × 96 property as nine authored 32 × 32 land regions.
2. Mark the front corner region as owned at game start.
3. Treat the other eight regions as unowned.
4. Reject construction on unowned tiles even if those tiles belong to the broader `ConstructionAreaDefinition`.
5. Expose at least one adjacent unowned region as purchasable for testing.
6. Purchasing that region changes ownership and immediately makes its tiles eligible for normal construction rules.
7. Keep price / permit qualification data-driven or replaceable; do not bake the final progression ladder into this first prototype.
8. Do not implement eight scripted Milton events. Milton only needs to be compatible with tutorializing the first purchase later.

This proves the permanent property boundary without prematurely solving the full campaign.

---

## Design law candidate

> **Advanced departments should become available because the player has built an institution capable of supporting them, not merely because the player reached an arbitrary level.**

Permits can provide the visible progression ladder while infrastructure, property, labor, suppliers, and demand provide the simulation requirements underneath it.

---

## Explicitly not locked yet

- exact permit names;
- number of permit tiers;
- which departments belong to each permit tier;
- exact Grocery permit / infrastructure requirements;
- exact permit qualification numbers;
- whether revenue / profit should ever be a hard permit requirement;
- exact employee qualification system;
- exact land-region purchase prices;
- whether every future region must always be adjacent;
- whether every permit exposes exactly one region or several;
- exact order in which the remaining eight regions become available;
- how sandbox mode exposes permits / land compared with story mode;
- whether special departments need distinct regulatory permits versus general store-expansion permits.

## Next progression-design pass

The next useful design session should answer:

1. **What exactly triggers / qualifies the first Grocery expansion?**
2. **Which of the two regions adjacent to the starting corner can the player buy first — either one by choice, or one specific tutorial region?**
3. **What are the broad permit / store stages after Grocery?**
4. **At each stage, what new land, infrastructure, and department opportunities become possible?**

Only after those are clear should the full permit schedule be locked.
