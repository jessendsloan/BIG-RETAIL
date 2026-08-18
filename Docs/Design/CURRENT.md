# Big Retail — Current Design State

**Last updated:** 2026-08-17

## Current focus

Designing the merchandise circulation system, especially the seam between **Products, Suppliers, Purchasing, Receiving, Fixtures, and Stocking**.

The current immediate task is to turn the accepted opening Product/Brand set into real Supplier Offers so the purchasing prototype can operate on an actual starting economy instead of placeholders.

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

## Current implementation target

Keep the first implementation **flat and small**.

Do not build deep supplier-account progression yet.

The immediate commercial foundation should prove:

1. Product / SKU
2. Brand identity attached to each SKU
3. Supplier
4. Supplier Offer
5. Multiple suppliers offering overlapping SKUs differently
6. Supplier-specific Draft Purchase Orders behind one product-oriented Purchasing workflow

The next content pass should map the accepted 12 opening SKUs across BIG Wholesale, Central Grocery Supply, and Beacon Beverage Distribution, then assign opening pack sizes, prices, minimum implications, and delivery consequences.

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

Define the **opening supplier catalogs / Supplier Offers** against the real 12-SKU assortment:

- Which of the 12 SKUs does BIG Wholesale carry?
- Which does Central Grocery Supply carry?
- Which does Beacon Beverage Distribution carry?
- Where should offers overlap so supplier choice is meaningful?
- What opening case/pack sizes and relative price levels create the intended Cost / Flexibility / Assurance tradeoff?

Once this is locked, the purchasing prototype has real content to transact with.
