# Big Retail — Current Design State

**Last updated:** 2026-08-18

## Current focus

The opening **Products → Suppliers → Purchasing** commercial foundation now exists as authored data and a playable workspace through PO placement and supplier scheduling.

The current review boundary is deliberately after order placement but before campaign spending or physical delivery. Purchasing can now expose the time consequence of each supplier choice without pretending that inventory has arrived.

## Locked foundations

- Big Retail is a one-store retail management simulation where scale creates new bottlenecks rather than functioning as a simple level-up.
- The week is a continuous simulation calendar: Monday through Sunday. Supplier delivery schedules can use real weekdays and times.
- Products/SKUs exist independently of suppliers.
- Brands and Products are separate concepts from Suppliers.
- A supplier can offer the same SKU differently from another supplier through price, purchase-pack size, delivery timing, minimums, and assortment.
- Purchasing should ultimately be one persistent management workspace that grows with the retailer rather than being replaced by a different late-game system.
- Purchasing is the transaction surface; Suppliers are not a second duplicate shopping system.
- A supplier may be used as a filter/lens over the same product-oriented Purchasing workspace.
- Fixtures declare what the store wants to merchandise; inventory records what the store actually has; stocking reconciles the two.
- Stocking consumes inventory. It does not directly purchase inventory.

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

### Central Grocery Supply
- Grocery-focused regional distributor.
- Next-day delivery.
- Cheaper than BIG Wholesale.
- Moderate minimum order.
- Rewards planning.

### Beacon Beverage Distribution
- Beverage specialist.
- Best opening beverage economics.
- Fixed route days, currently Tuesday and Friday.
- Least flexible of the opening three.
- Rewards specialization and calendar planning.

## Supplier balancing law

Supplier tradeoffs are framed around:

**Cost — Flexibility — Assurance**

Assortment/category is supplier identity, not one corner of the triangle.

Once a supplier is commercially available, the player should generally be able to order from them subject to concrete conditions such as price, pack, minimum, schedule, and future capability requirements. Relationships/contracts later may improve terms rather than functioning as XP-style permission gates.

## Narrative integration

Milton "Mr. BIG" Big is more than a supplier character.

He is a recurring campaign spine with three simultaneous roles:

- Tutor
- Safety net
- Commercial opponent

BIG helped finance the player's beginning, owns the player's debt, and owns BIG Wholesale. Mr. BIG is charming, useful, slightly unfair, and profits from the player's dependence. The long-term relationship should shift from the player adapting to BIG toward BIG eventually wanting the player's business.

## Current implementation state

The opening implementation is flat and intentionally bounded:

- 10 authored Brands
- 12 authored opening Products / SKUs
- 3 authored Suppliers
- 24 authored Supplier Offers
- one runtime Draft Purchase Order per Supplier
- a product-first Purchasing workspace with search, category filters, a Supplier lens, offer comparisons, pack quantities, draft totals, and minimum-order feedback
- exact arrival estimates derived from the current commercial time for same-day and next-day service, plus day-only estimates for fixed routes
- a Review Orders sheet that enforces every staged Supplier minimum atomically
- immutable placed PO records with frozen lines, prices, placement time, and scheduled delivery estimate
- a placement confirmation state that clears committed drafts without creating inventory
- a read-only Commercial Directory that switches between the 10 opening Brands and 3 opening Suppliers, deriving each card's opening assortment from the real catalog
- stub-ready image slots on Product, Brand, and Supplier assets
- isolated `PurchasingWorkspaceLab` and `CommercialDirectoryLab` scenes; no campaign or gameplay scene integration yet

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
- Negotiations
- Contracts
- Credit terms and invoices
- Pallet / truckload procurement
- Supplier reliability simulation
- Shortages / backorders
- Returns / damage
- Product discontinuation
- Private label
- Manufacturer-direct purchasing
- Full auto-replenishment

## Next design question

Playtest and critique the placement and scheduling pass:

- Is choosing the Product before the Supplier Offer natural?
- Are pack size, unit cost, case price, and delivery timing legible enough to compare?
- Does the Supplier lens feel like a useful filter rather than a duplicate store?
- Are multiple staged Supplier POs understandable before review?
- Do the v0.1 offers make BIG feel flexible and expensive, Central planned and economical, and Beacon specialized and schedule-bound?
- Does the review sheet make **when each order arrives** obvious enough to drive the Supplier decision?
- Does an unmet Supplier minimum explain clearly why placement is blocked?

If that interaction survives review, connect its explicit seams to the campaign clock and economy before beginning **Checkpoint 7: Delivery / Receiving / Owned Inventory**.
