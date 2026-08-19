# Design Patch — Permit, Property Parcel & Department Progression

**Status:** Promising progression direction / Not yet locked

**Purpose:** Preserve the emerging progression model in which Big Retail grows by qualifying for permits, gaining access to more of the property, building the infrastructure required by a larger retailer, and then opening more sophisticated departments.

This patch is intentionally exploratory. It should inform later progression design, but it does not replace the current merchandise-circulation implementation target.

## Core hypothesis

Big Retail should avoid arbitrary XP-style department unlocks where possible.

A stronger progression chain may be:

**Meet store requirements → qualify for permit → gain new construction / expansion rights → build supporting infrastructure → become capable of operating new departments → create new requirements at the next scale**

The permit is therefore not the department itself.

It is a visible progression gate that says the player's retail institution has become qualified to build or operate a more advanced class of store capability.

## Why this fits Big Retail

Big Retail is already built around one persistent property and the idea that every new capability consumes land, money, labor, utilities, receiving capacity, customer access, or attention.

A permit-and-property progression model turns those existing systems into the progression requirements themselves.

Instead of:

> Reach Level 8 → Grocery unlocked

The game can say, in effect:

> This site now has the parking, receiving, utilities, operating history, and other qualifications needed for the next commercial permit.

The player then physically expands the site and builds what the new retail capability requires.

Progression becomes visible in the store rather than living only in a menu.

## The Anno-like loop

The useful Anno comparison is structural rather than literal.

Anno asks the player to satisfy requirements in order to move into a more capable economic tier.

Big Retail could do the same through the store itself:

**Operate current store**
→ **Satisfy capacity / infrastructure / commercial requirements**
→ **Qualify for next permit**
→ **Acquire or unlock more property / construction rights**
→ **Build new supporting systems**
→ **Open new department capability**
→ **Attract broader demand and create larger bottlenecks**
→ **Stabilize the larger store**
→ **Qualify for the next expansion tier**

The thing being upgraded is not a population class. It is the commercial institution and property.

## Permits as progression gates

Permits should represent permission or qualification, not magical content licenses.

A permit may unlock one or more of the following:

- the right to acquire an adjacent property parcel;
- a larger allowed building footprint;
- additional construction types;
- specialized infrastructure;
- new department classes;
- higher-capacity parking or receiving facilities;
- specialized service spaces;
- future regulatory or professional requirements where appropriate.

The exact permit names and tiers are not yet decided.

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

## Permit → Infrastructure → Department

The strongest version of the model keeps these concepts separate.

### Permit

**Are we allowed / qualified to build or operate this class of capability?**

### Infrastructure

**Can this physical property actually support it?**

### Department

**Have we built and staffed the commercial operation itself?**

This avoids a permit functioning as a one-click department unlock.

Example direction:

### Grocery

Possible requirements might include:

- an early food-retail or commercial-expansion permit;
- sufficient customer parking / arrival capacity;
- adequate receiving access;
- basic food storage capability;
- required refrigeration or utility support for refrigerated categories;
- suitable grocery fixtures;
- access to appropriate suppliers;
- enough general or trained labor to operate the department.

The exact Grocery requirements are not locked. The important point is that Grocery appears because the store has become capable of supporting Grocery.

### Electronics

A later department might additionally care about:

- a more advanced commercial permit;
- electrical / display capability;
- security capability;
- specialized fixtures;
- supplier access;
- employees capable of operating the department.

### Pharmacy / advanced services

A much later specialty department could require a stronger combination of:

- specialized permit / authorization;
- secure or restricted space;
- professional staff;
- supporting infrastructure;
- sufficient store scale and customer demand.

These examples establish the pattern, not final content requirements.

## Employees are a requirement, not necessarily the whole progression ladder

An earlier idea considered making increasingly capable employees the direct equivalent of Anno population tiers.

The current stronger direction is a mix:

- **Permits** gate classes of expansion or construction.
- **Property and infrastructure** determine what the site can physically support.
- **Employees** determine what the store can actually operate.
- **Suppliers** determine what merchandise can be sourced.
- **Customers / demand** determine whether the new capability is commercially worthwhile.

Specialized employees can therefore be required for specialized departments without forcing the entire progression game to revolve around an employee-tier ladder.

General retail labor might operate the opening store, while later departments require trained associates, specialists, technicians, or professionals as appropriate.

## Property parcels / section unlocking

A strong companion idea, inspired by the territorial progression feel of Parcel Simulator, is to divide the full property into **unlockable or purchasable sections** rather than handing the player the entire usable site immediately.

The player begins with a modest portion of the eventual retail property.

As the business qualifies for expansion, adjacent sections of the larger site can become available.

This creates a physical progression story:

**tiny store on a small site**
→ **neighborhood retailer**
→ **supermarket footprint**
→ **large-format store**
→ **massive super-retail property**

The early store remains visible as a small part of the final institution.

## Why parcels are valuable

Property unlocking reinforces several existing Big Retail goals at once:

- Growth is physically visible.
- Land remains a scarce strategic resource.
- The player cannot solve every early problem by spreading across an enormous empty map.
- Each expansion creates an immediate parking-versus-building-versus-logistics decision.
- Old layout decisions remain embedded in the final store.
- The player develops attachment and spatial memory as the property grows.
- A new parcel is a meaningful reward without needing an abstract currency beyond money / qualification.

A newly acquired section does not prescribe its use.

The player may choose to spend it on:

- parking;
- sales floor;
- receiving;
- storage;
- utilities;
- employee facilities;
- specialized department infrastructure;
- circulation;
- future expansion reserve.

That decision is the gameplay.

## Relationship between permits and parcels

Several structures are possible and remain open:

### Model A — Permit unlocks parcel acquisition

The player satisfies permit requirements, receives the next expansion permission, and may then purchase one or more adjacent property sections.

### Model B — Property acquisition helps qualify for permit

The player purchases land first, then develops enough capacity to qualify for the next commercial permit.

### Model C — Mixed

Some permits increase legal / construction capability while property sections are bought independently when adjacency, money, and campaign conditions allow.

The mixed model may ultimately be strongest because permits and land solve different design problems, but this is not yet locked.

## Parcel geometry is deliberately undecided

Do not treat **96** as a locked parcel count or a final grid shape.

The conversation that produced this idea included the possibility of dividing the map into sections, but the correct parcel size, count, shape, and progression cadence need to be chosen against the actual Big Retail map and construction scale.

Possible structures could include:

- a rectangular parcel grid;
- larger authored expansion blocks;
- irregular property sections shaped around roads / access;
- a mostly regular grid with a few special edge parcels.

The player should think in meaningful pieces of property, not hundreds of tedious micro-purchases.

## Existing implementation seam

The current map code already contains a useful conceptual separation.

`ConstructionAreaDefinition` defines cells that are physically eligible for construction while explicitly noting that **ownership, progression, cost, conflicts, and other rules may still reject a construction request**.

That means a future parcel / ownership layer can potentially sit above the authored construction area rather than redefining physical map eligibility.

This patch does not prescribe an implementation yet, but the existing boundary is compatible with the idea.

## Example visible progression objective

A permit can function as a clear near-term goal:

```text
EXPANDED COMMERCIAL PERMIT

Parking Capacity        80 / 100
Receiving Capability    Met
Employee Facilities     Met
Store Revenue            $38,400 / $40,000
Grocery Department       Operational

2 requirements remaining
```

The important emotional effect is that the player sees a concrete system requirement and immediately knows what kind of store improvement could move them forward.

If the missing requirement is parking, the progression problem becomes a real property problem:

> Where do I put twenty more spaces without damaging the rest of the operation?

That is much more aligned with Big Retail than filling an XP bar.

## Design law candidate

> **Advanced departments should become available because the player has built an institution capable of supporting them, not merely because the player reached an arbitrary level.**

Permits can provide the visible progression ladder while infrastructure, property, labor, suppliers, and demand provide the simulation requirements underneath it.

## Explicitly not locked yet

- exact permit names;
- number of permit tiers;
- which departments belong to each tier;
- whether Grocery itself requires a named permit or is enabled by a more general commercial permit;
- exact permit qualification numbers;
- whether revenue / profit should ever be a hard requirement;
- exact employee qualification system;
- exact property parcel count;
- exact parcel dimensions or shape;
- whether parcels must be adjacent;
- whether every permit unlocks land;
- whether land is purchased with money, granted through campaign progression, or uses a mixed rule;
- how sandbox mode exposes permits / land compared with story mode;
- whether special departments need distinct regulatory permits versus general store-expansion permits.

## Next design pass when revisited

The next useful progression-design session should answer three questions in order:

1. **What are the broad stages of the store?**
   - corner store;
   - grocery / neighborhood market;
   - supermarket;
   - big-box / superstore;
   - mega retail.

2. **What property / infrastructure facts prove the store is ready to move from one stage to the next?**

3. **What new permits, parcels, and department opportunities should each transition expose?**

Only after those are clear should we choose exact permit names, parcel dimensions, or numeric thresholds.
