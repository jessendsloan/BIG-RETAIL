# Big Retail — Supplier & Procurement Implementation Order

**Status:** Approved design direction
**Domain:** Retail Operations & Scale → Suppliers / Purchasing / Stocking

## Goal

Build the merchandise circulation system in dependency order so the opening corner-store implementation can scale into mega-retail without replacing its core concepts.

Permanent chain:

**Product / SKU → Supplier Account → Supplier Offer → Procurement Need → Draft Purchase Order → Committed Purchase Order → Delivery → Receiving → Inventory → Fixture Demand → Stocking**

## Approved Scope Decisions

- Supplier Accounts & Relationships are part of the permanent system.
- Multiple suppliers may offer the same SKU.
- Suppliers remain meaningful economic actors rather than being flattened into a universal catalog.
- The early game starts small, while the underlying structures must support later scale.
- Do **not** implement returns/damaged freight at this stage.
- Do **not** implement product/supplier discontinuation at this stage.
- Do **not** add deep supplier-failure simulation yet.
- Stocking consumes available inventory; stocking does not directly purchase inventory.

---

## 1. Supplier Accounts & Relationships

Define what it means for the player's business to have access to a supplier.

The permanent model should have room for states such as:

- known
- available
- active
- restricted / requirements unmet

The opening implementation can remain simple: BIG Wholesale, Central Grocery Supply, and Beacon Beverage Distribution can begin as active accounts.

The account/relationship structure is where later systems can attach:

- supplier access requirements
- purchasing-volume qualifications
- better commercial terms
- preferred status
- relationship history
- negotiation eligibility

Mr. BIG's commercial relationship with the player should plug into the same supplier/account framework rather than use a separate special-case system.

## 2. Supplier Offers

Lock the permanent commercial atom:

> **Supplier X offers SKU Y in purchase pack Z, at price P, under delivery rule D.**

Minimum opening data:

- supplier
- SKU
- purchase pack
- pack quantity
- pack price
- effective unit cost
- delivery rule
- availability

The Product/SKU must not own a universal wholesale price.

Different suppliers can therefore compete to supply the same consumer-facing SKU.

## 3. Procurement Need

Define the store-side requirement independently from supplier choice.

Example:

> **Bright Cola procurement requirement: +36 units**

A Need says what the retail operation requires. It must **not** say:

> Buy two cases from Central Grocery.

Supplier selection belongs to Purchasing.

Opening logic can be simple and expand later. Early need calculation may derive from assigned fixture/display capacity compared with available and inbound inventory.

Future systems may extend the same Need with:

- reserve targets
- min/max inventory
- days of supply
- demand forecasting
- safety stock

without replacing the concept.

## 4. Draft Purchase Order Workspace

This is the first major UX design task.

The player should be able to work from store needs/products and choose supplier offers. The game then groups the chosen lines into supplier-specific draft Purchase Orders.

Example:

- BIG Wholesale — 3 lines
- Central Grocery — 14 lines
- Beacon Beverage — 6 lines

Each draft PO independently communicates:

- total cost
- purchase packs/cases
- supplier minimum status
- order cutoff
- expected arrival
- relevant warnings

The game handles clerical grouping. The player makes the commercial decisions.

The workspace must support both natural entry directions:

1. **Supplier-first:** "I am placing my Central Grocery order."
2. **Need/product-first:** "We need more cereal; who should supply it?"

Both manipulate the same Supplier Offers and Draft POs.

## 5. Committed Purchase Order Rules

Define the point where a draft order becomes a real commercial commitment.

Opening state flow:

**Draft → Placed → Scheduled / In Transit → Delivered**

Determine:

- what `Place Order` commits
- when payment is taken/committed
- whether an order may be changed before supplier cutoff
- how expected delivery time is calculated

Do not add invoices, returns, damage, or other accounting branches yet.

## 6. Delivery & Receiving Handoff

A placed order must physically reach the store before it becomes usable inventory.

Opening implementation:

- supplier vehicle arrives
- ordered cases are unloaded at the receiving point
- goods pass through Receiving
- successfully received goods become available store inventory

Later systems may add docks, pallets, unloading equipment, staging, congestion, and dedicated receivers without replacing the original flow.

Purchasing should eventually be capable of warning the player when incoming orders will stress receiving capacity.

## 7. Inventory Location States

Lock only the states the early game needs:

- **On Shelf**
- **Backstock**
- **Inbound**

These values should be visible where they are relevant to purchasing decisions.

The player should not need to leave Purchasing and manually remember shelf counts simply to decide how much stock to order.

## 8. Fixture Demand & Stocking

A fixture assignment declares what SKU belongs there and how much product the fixture wants to display.

Example:

> Bright Cola
> Display target: 24
> Currently on fixture: 9
> Fixture shortfall: 15

If compatible inventory exists elsewhere in the store, the shortfall can generate stocking work.

Opening stocking controls can later be designed around concepts such as:

- Restock Now
- Auto Restock
- Hold / Ignore

Permanent boundary:

> **Stocking moves inventory already owned by the store. Purchasing obtains inventory the store does not own.**

## 9. Contextual UI Links

After the underlying systems are stable, connect the world and Purchasing workspace.

Examples:

- Fixture → Order Stock
- Fixture → Product Detail
- Product → View Fixtures
- Product → Compare Supplier Offers
- Supplier → Catalog
- Need → Compare Offers
- Low Stock → Purchasing

These are contextual entry points into the same system, never duplicate purchasing mechanics.

## 10. Supplier Progression & Relationship Depth

Add deeper supplier gameplay only after the core merchandise circulation loop works.

Later extensions can include:

- supplier account requirements
- better terms through purchasing volume
- preferred suppliers
- category sourcing policies
- relationship progression
- new supplier discovery
- specialized distributors
- pallet purchasing
- direct manufacturer relationships
- contracts and negotiation
- late-game supplier competition for the player's business

The long arc remains:

> **You adapt to suppliers → you select suppliers → you negotiate with suppliers → suppliers adapt to you.**

---

# Immediate Design Packets

## Packet A — Commercial Foundation

1. Supplier Account
2. Supplier Offer
3. Procurement Need

## Packet B — Purchasing UX

1. Needs / Products / Suppliers views
2. Supplier-offer comparison
3. Draft PO workspace
4. Place-order flow

## Packet C — Physical Handoff

1. Delivery
2. Receiving
3. On Shelf / Backstock / Inbound inventory states
4. Fixture replenishment demand
5. Employee stocking work

Completing these three packets produces the MVP merchandise-circulation loop while preserving the architecture required for later mega-retail procurement.