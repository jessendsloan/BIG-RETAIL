# Big Retail — Current Design State

**Last updated:** 2026-08-17

## Current focus

Designing the merchandise circulation system, especially the seam between **Products, Suppliers, Purchasing, Receiving, Fixtures, and Stocking**.

## Locked foundations

- Big Retail is a one-store retail management simulation where scale creates new bottlenecks rather than functioning as a simple level-up.
- The week is a continuous simulation calendar: Monday through Sunday. Supplier delivery schedules can use real weekdays and times.
- Products/SKUs exist independently of suppliers.
- A supplier can offer the same SKU differently from another supplier through price, purchase-pack size, delivery timing, minimums, and assortment.
- Purchasing should ultimately be one persistent management workspace that grows with the retailer rather than being replaced by a different late-game system.
- Fixtures declare what the store wants to merchandise; inventory records what the store actually has; stocking reconciles the two.
- Stocking consumes inventory. It does not directly purchase inventory.

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
2. Supplier
3. Supplier Offer
4. Multiple suppliers offering overlapping SKUs differently
5. A small seeded product set using BIG Wholesale, Central Grocery Supply, and Beacon Beverage Distribution

The final Purchasing UI should wait until these relationships are concrete enough to design around.

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

Define the **flat Product + Purchasing experience** that sits on top of the supplier-offer model:

- What does the player see when opening Purchasing?
- How are Products and Suppliers reconciled in one UI?
- How does a player choose a product, compare supplier offers, choose quantity, and build supplier-specific purchase orders without clerical friction?
- How does that purchasing view later connect to receiving and fixture demand?
