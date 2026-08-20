# Big Retail — Current Design State

**Last updated:** 2026-08-19

## Current focus

The opening **Products → Suppliers → Purchasing → Delivery → Receiving** loop is now integrated into `Gameplay`.

Purchasing uses the campaign clock and store cash. Placed Supplier POs become scheduled deliveries. Each ready Supplier PO claims one free 1 × 1 berth in a player-painted **Receiving Area**; orders that do not fit wait for Receiving space. Receiving a staged pallet sends its exact units into the existing backstock / overflow inventory path. The isolated lab scenes remain available for focused UI work.

A parallel narrative-design thread is now established around Milton Big and the opening campaign flow. The canonical Milton character/campaign authority is `MiltonBig.md`.

## Locked foundations

- Big Retail is a one-store retail management simulation where scale creates new bottlenecks rather than functioning as a simple level-up.
- The week is a continuous simulation calendar: Monday through Sunday. Supplier delivery schedules can use real weekdays and times.
- Products/SKUs exist independently of suppliers.
- Brands and Products are separate concepts from Suppliers.
- A supplier can offer the same SKU differently from another supplier through price, purchase-pack size, delivery timing, minimums, and assortment.
- Supplier visual identity is also separate from consumer Product/Brand identity. Trucks, shipping cases, receiving labels, and supplier UI carry supplier identity while the merchandise keeps its consumer branding.
- Purchasing should ultimately be one persistent management workspace that grows with the retailer rather than being replaced by a different late-game system.
- Purchasing is the transaction surface; Suppliers are not a second duplicate shopping system.
- A supplier may be used as a filter/lens over the same product-oriented Purchasing workspace.
- Fixtures declare what the store wants to merchandise; inventory records what the store actually has; stocking reconciles the two.
- Stocking consumes inventory. It does not directly purchase inventory.
- The current property is **96 × 96 tiles = 9,216 tiles**.
- The property is subdivided into a **3 × 3 grid of nine 32 × 32 land regions**, each containing 1,024 tiles.
- The player begins owning only the **front corner land region at the road intersection**, making the opening business literally a corner store.
- The remaining eight land regions are progression/acquisition space.
- Milton "Mr. BIG" Big should tutorialize the **first** land-region purchase; after that handoff, land acquisition should be player-directed when regions are eligible and affordable.

## Opening Product foundation

The opening convenience-oriented assortment now has 12 accepted Product Lines / opening SKUs. Full details live in `Products.md`.

1. Bright Cola — 20 oz Bottle
2. ClearSpring Pure Water — 20 oz Bottle
3. Ridgeway Original Potato Chips — Single Bag
4. ChocoMax Milk Chocolate — Bar
5. Sunburst Fruit Chews — Pack
6. Homestead White Bread — Loaf
7. Homestead Whole Milk — Jug
8. Crunch-O Corn Flakes — Box
9. CleanMax Paper Towels — Roll
10. CleanMax Dish Soap — Bottle
11. Spark Alkaline Batteries — 4-Pack
12. FreshMint Toothpaste — Tube

All opening SKUs are currently treated as **Standard** Market Position. Value / Standard / Premium are available positioning lanes, not mandatory three-piece sets for every Product Line.

Customer purchase motive is deliberately not encoded as a static Product Role. The customer / shopping trip should eventually explain *why* an item is wanted now.

## Opening Brand foundation

Consumer brand identities are now isolated in `Brands.md`.

Opening brands:

- Bright Beverage Co.
- ClearSpring
- Ridgeway Snacks
- ChocoMax
- Sunburst Candy Co.
- Homestead Foods
- Crunch-O
- CleanMax Home
- Spark
- FreshMint

Brands are recurring shelf-world identities. Supplier identity remains separate.

## Starting supplier network

### BIG Wholesale
- Owned by Milton "Mr. BIG" Big's corporate empire.
- Broad assortment.
- Same-day / within-hours delivery.
- Very flexible, very dependable, expensive.
- No meaningful early minimum.
- Functions as the player's safety net and emergency supplier.
- Visual starting identity: **burgundy / cream / gold**.

### Central Grocery Supply
- Grocery-focused regional distributor.
- Next-day delivery.
- Cheaper than BIG Wholesale.
- Moderate minimum order.
- Rewards planning.
- Visual starting identity: **green / beige / burnt orange**.

### Beacon Beverage Distribution
- Beverage specialist.
- Best opening beverage economics.
- Fixed route days, currently Tuesday and Friday.
- Least flexible of the opening three.
- Rewards specialization and calendar planning.
- Visual starting identity: **blue / white / teal**.

The canonical supplier art-direction guide is `SupplierVisualIdentity.md`. Exact production colors may still move after sprite-scale testing; the identity system and color separation are accepted.

## Supplier balancing law

Supplier tradeoffs are framed around:

**Cost — Flexibility — Assurance**

Assortment/category is supplier identity, not one corner of the triangle.

Once a supplier is commercially available, the player should generally be able to order from them subject to concrete conditions such as price, pack, minimum, schedule, and future capability requirements. Relationships/contracts later may improve terms rather than functioning as XP-style permission gates.

## Narrative integration

The canonical character/campaign document is `MiltonBig.md`.

Milton "Mr. BIG" Big is more than a supplier character. He is the recurring human spine of the campaign with three simultaneous roles:

- Tutor
- Safety net
- Commercial opponent

BIG helped finance the player's beginning, owns the player's debt, and owns BIG Wholesale. Mr. BIG is charming, useful, slightly unfair, and profits from the player's dependence. The long-term relationship should shift from the player adapting to BIG toward BIG eventually wanting the player's business.

His tutorial function should usually arrive through real business offers and systems rather than detached tutorial narration. He should teach because he is financing, selling, providing, or recommending something.

His tutorial role also includes guiding the player's **first property expansion purchase**, after which the permanent land-acquisition system becomes player-directed.

The approved visual canon is a huge, bald, impeccably suited, cigar-smoking business magnate with a warm but commercially predatory presence. The approved portrait represents him around the moment he crossed his first billion dollars.

Preferred voice direction is selective character VO: written dialogue carries most content while reusable voiced tags, laughs, greetings, and rare major fully voiced lines establish the sound of Milton Big.

The campaign probably needs a real climax before releasing the player into the continuing sandbox. The exact plot remains open, but the climax should pay off the Milton/player leverage arc rather than introduce an unrelated final threat. The preferred end-state is that the player has built enough retail power to negotiate with, resist, outgrow, or redefine a major deal with Milton as a peer.

## Progression design thread — partially locked

The canonical progression patch is `Patches/PermitParcelProgression.md` (the filename is retained for continuity; the design terminology inside now uses **Land Region**).

Locked property facts:

- 96 × 96 total property.
- 9,216 tiles total.
- Nine equal 32 × 32 land regions arranged 3 × 3.
- One front-corner region owned at game start.
- Eight regions remain for later acquisition.
- Milton guides the first purchase only.
- Later eligible/affordable land acquisition is controlled by the player.

Still exploratory:

- Avoid arbitrary XP-style department unlocks where possible.
- Use **permits** as visible progression gates tied to concrete store requirements.
- Keep **permit, infrastructure, and department** as separate concepts: permission/qualification → physical capability → commercial operation.
- Let employees, suppliers, utilities, security, receiving, parking, and other systems become department requirements where appropriate rather than making one system the universal gate.
- The exact permit tiers, Grocery gate, land prices, region eligibility order, and permit-to-land relationship remain open.
- The existing `ConstructionAreaDefinition` already separates physical construction eligibility from ownership/progression/cost rules and is the preferred seam for a future land-region ownership layer.

This progression work should not interrupt the current purchasing integration target unless a gameplay chat is explicitly tasked with prototyping land-region ownership.

## Current implementation state

The supplier-backed opening implementation is flat and intentionally bounded:

- 10 authored Brands
- 12 authored opening Products / SKUs
- 3 authored Suppliers
- 24 authored Supplier Offers
- one runtime Draft Purchase Order per Supplier
- a product-first Purchasing workspace with search, category filters, a Supplier lens, offer comparisons, pack quantities, draft totals, and minimum-order feedback
- exact arrival estimates derived from the current commercial time for same-day and next-day service, plus day-only estimates for fixed routes
- a Review Orders sheet that enforces every staged Supplier minimum atomically
- immutable placed PO records with frozen lines, prices, placement time, and scheduled delivery estimate
- a placement confirmation state that clears committed drafts only after one atomic store-cash payment succeeds
- a live delivery lifecycle of **Scheduled → Ready to Receive → Received**
- a player-painted Receiving Area on owned, finished, unobstructed floor, with one cell functioning as one Supplier PO pallet berth
- stable berth reservations for ready Supplier POs; overflow orders wait until Receiving space becomes available
- occupied Receiving cells protected from erasure until their pallet has been received
- an **RCV** construction-rail tool, management overlay, couch-readable capacity status, and waiting-order feedback
- one persistent pallet view per staged Supplier PO, selecting one of four complete supplier-load sprites from its total case volume
- authored BIG Wholesale load art plus clearly named Central Grocery and Beacon Beverage replacement stubs across all four tiers
- delivery receiving through the existing fixture backstock / overflow inventory service
- a read-only Commercial Directory that switches between the 10 opening Brands and 3 opening Suppliers, deriving each card's opening assortment from the real catalog
- stub-ready image slots on Product, Brand, and Supplier assets
- isolated `PurchasingWorkspaceLab` and `CommercialDirectoryLab` scenes retained as focused review tools
- the full Purchasing workspace installed as an open/close overlay in `Gameplay`
- live campaign time and available store cash shown in the Purchasing header
- the accepted 12-product opening catalog installed as the Gameplay merchandising catalog
- rack-side purchasing replaced in the live UI by a **Supplier Deliveries** receiving panel that only receives pallets actually staged in Receiving

The integrated campaign-side foundation provides:

- a deterministic Monday-starting simulation clock installed in `Gameplay`
- opening store cash and checkout revenue
- fixture planograms, display inventory, physical backstock, stocking, and receiving
- a temporary fixed-case service retained only as a compatibility fallback; the live Gameplay UI no longer presents its product-order buttons
- the opening campaign presentation and the first land-region ownership/progression foundation

The temporary fixture ordering shortcut keeps wholesale case cost and retail unit price on the Product only for graybox compatibility. Permanent supplier prices, purchase-pack quantities, minimums, and delivery rules remain owned by Supplier Offers.

The implemented seam proves:

1. Product / SKU
2. Brand identity attached to each SKU
3. Supplier
4. Supplier Offer
5. Multiple suppliers offering overlapping SKUs differently
6. Supplier-specific Draft Purchase Orders behind one product-oriented Purchasing workflow
7. Review, minimum validation, placement, and Supplier-rule scheduling

### Opening assortment map

| Supplier | Opening assortment | Count |
|---|---|---:|
| BIG Wholesale | All opening SKUs | 12 |
| Central Grocery Supply | All opening SKUs except Spark Batteries and FreshMint Toothpaste | 10 |
| Beacon Beverage Distribution | Bright Cola and ClearSpring Water | 2 |

Opening balance values are **v0.1 playtest numbers**, not permanent economic law. The exact pack and price matrix lives in `Suppliers.md`.

Supplier minimum and delivery rules are supplier-wide in this opening model. Purchase pack, pack price, effective unit cost, and availability belong to each Supplier Offer.

## Deferred for later

- Supplier Accounts & Relationships — preserved as a design patch in `Patches/SupplierAccountsAndRelationships.md`
- Full permit / department progression schedule — continuing design lives in `Patches/PermitParcelProgression.md`
- Exact Milton/campaign climax plot — broad climax principle is preserved in `MiltonBig.md`
- Negotiations
- Contracts
- Credit terms and invoices
- Pallet / truckload procurement quantities and capacity simulation; the current pallet is a physical per-PO receiving prop
- Supplier reliability simulation
- Shortages / backorders
- Returns / damage
- Product discontinuation
- Private label
- Manufacturer-direct purchasing
- Full auto-replenishment

## Next design question

Playtest the integrated loop in the actual campaign scene:

- Is choosing the Product before the Supplier Offer natural?
- Are pack size, unit cost, case price, and delivery timing legible enough to compare?
- Does the Supplier lens feel like a useful filter rather than a duplicate store?
- Are multiple staged Supplier POs understandable before review?
- Do the v0.1 offers make BIG feel flexible and expensive, Central planned and economical, and Beacon specialized and schedule-bound?
- Does the review sheet make **when each order arrives** obvious enough to drive the Supplier decision?
- Does an unmet Supplier minimum explain clearly why placement is blocked?
- Does Purchasing use the live campaign clock rather than a lab-only Monday 9:00 AM value?
- Does placement spend store cash atomically and reject an unaffordable batch without partially committing orders?
- Do scheduled arrivals become receivable inventory through the existing backstock/receiving seam?
- Is painting and erasing the Receiving Area understandable with mouse and controller input?
- Does **occupied / total** Receiving capacity make the physical bottleneck legible from the couch?
- When two Suppliers arrive, do their two separate pallets and occupied berths make the receiving burden immediately legible?
- When Receiving is full, is it clear that additional ready Supplier POs are waiting for space rather than lost?

The next step is a hands-on Gameplay review of the new Receiving Area at the
campaign's intended camera distance and UI scale. Delivery access, supplier
vehicles, unloading labor, travel paths, and richer dock logic remain later
extensions; they should build on the current **ready PO → reserved berth →
received inventory** seam rather than replace it. Removing the dormant fixed-case
compatibility service and deciding how Purchasing is first introduced remain
follow-up campaign-integration work.

Parallel story question:

- What are the exact first 20–30 minutes of the campaign, from Milton's opportunity through the player's first functioning retail loop?
