# Big Retail — Merchandise Circulation Implementation Order

**Status:** Checkpoints 1–5 accepted; Checkpoint 6 placement and scheduling implemented in the Purchasing lab, with campaign economy integration pending
**Domain:** Products / Brands / Suppliers / Purchasing / Receiving / Stocking

## Goal

Build the opening merchandise loop in small permanent checkpoints. Each checkpoint should prove one relationship the player will actually use before adding the next layer.

Opening vertical slice:

**Brand → Product / SKU → Supplier Offer → Draft Purchase Order → Placed Order → Delivery / Receiving → Inventory → Fixture → Stocking → Customer Sale**

The opening implementation stays intentionally flat. Deeper supplier-account and relationship systems are preserved separately in `Patches/SupplierAccountsAndRelationships.md` and are not part of this build sequence.

---

## Current Design Rules

- Products / SKUs exist independently of Suppliers.
- Brands and Suppliers are separate concepts.
- A Brand is the consumer identity attached to a SKU.
- A Supplier Offer connects a Supplier to a SKU under specific commercial terms.
- The same SKU may have multiple Supplier Offers.
- Wholesale price, purchase-pack size, and availability belong to Supplier Offers, not Products.
- Opening minimums and delivery timing are Supplier-wide terms resolved through each Offer.
- Purchasing is the transaction surface.
- Suppliers are not a second duplicate shopping system; a Supplier can later act as a filter / management context over Purchasing.
- Customer purchase motive is not a fixed Product Role. The Product describes what the item is; customer / trip logic later explains why it is wanted.
- Stocking moves inventory the store already owns. Purchasing obtains inventory the store does not yet own.
- The week is continuous Monday–Sunday simulation time, and Supplier delivery rules use that calendar.

---

# Checkpoint 1 — Brands + Products

**Implementation status:** Complete

Create the authored consumer-world foundation before procurement.

## BrandDefinition

Minimum opening data:

- stable ID
- display name
- identity / presentation metadata as needed by UI or art later

## ProductDefinition / SKU

Minimum opening data:

- stable ID
- Brand reference
- Product Line
- Category
- Market Position: Value / Standard / Premium
- Package / Form
- Shelf Profile reference or placeholder

Do not put Supplier, wholesale price, case size, or delivery timing on Product.

## Seed accepted opening content

Author the 10 opening Brands and 12 opening SKUs already defined in `Brands.md` and `Products.md`.

The checkpoint is complete when the game can reliably enumerate the real opening products and resolve their Brand identities.

---

# Checkpoint 2 — Suppliers

**Implementation status:** Complete

Create the three opening Supplier definitions.

## SupplierDefinition

Minimum opening data:

- stable ID
- display name
- supplier identity / category tags
- minimum-order rule
- delivery rule

Opening Suppliers:

1. **BIG Wholesale** — broad, same-day / within-hours, highly flexible, expensive
2. **Central Grocery Supply** — grocery-focused, next-day, cheaper, moderate minimum
3. **Beacon Beverage Distribution** — beverage specialist, best beverage economics, Tuesday / Friday route

Do not implement supplier relationship tiers, contracts, negotiation, or account progression here.

---

# Checkpoint 3 — Supplier Offer Matrix

**Implementation status:** Complete with v0.1 playtest balance

Connect the Product world to the Supplier world.

## SupplierOfferDefinition

Minimum opening data:

- Supplier reference
- SKU reference
- purchase-pack quantity
- purchase-pack price
- effective unit cost, calculated where appropriate
- delivery behavior / rule reference
- availability

The permanent commercial atom is:

> **Supplier X offers SKU Y in purchase pack Z, at price P, under delivery rule D.**

Build the actual opening Supplier Offer matrix across the accepted 12 SKUs.

Design goal:

- BIG should feel broad.
- Central should feel like a serious grocery alternative.
- Beacon should feel narrow and meaningfully specialized.
- Several SKUs must overlap across Suppliers so Cost / Flexibility / Assurance creates real choices.

Stop and inspect this matrix before building Purchasing. The first economy should make sense as content before the UI is asked to present it.

---

# Checkpoint 4 — Draft Purchase Orders

**Implementation status:** Complete

Create runtime purchasing state only after Supplier Offers exist.

## Runtime concepts

- `DraftPurchaseOrder`
- `PurchaseOrderLine`
- `PurchasingService` or equivalent domain owner

Rules:

- Every Draft PO belongs to exactly one Supplier.
- A line references a Supplier Offer plus a quantity of purchase packs.
- Adding an offer for BIG creates / updates the BIG draft.
- Selecting the Central offer for the same SKU creates / updates the Central draft instead.
- The domain calculates order totals and validates Supplier minimums.

The important test is:

> One SKU has several offers, and choosing one offer creates the correct Supplier-specific commercial consequence.

Do not build final UI yet.

---

# Checkpoint 5 — Gray-Box Product-First Purchasing UI

**Implementation status:** Accepted in the isolated Purchasing lab

Build one functional Purchasing workspace before polishing it.

The canonical purchasing interaction is product-oriented:

> **Product → available Supplier Offer → quantity → Supplier-specific Draft PO**

A product row / card should be able to show, at minimum:

- Brand / product identity
- Product name
- Package / Form
- selected Supplier Offer
- purchase-pack quantity
- pack price and/or effective unit cost
- expected arrival
- quantity controls

If a SKU has multiple offers, the Supplier / offer control exposes those alternatives.

If only one offer is relevant, do not require a meaningless extra selection.

The workspace may show multiple Supplier-specific Draft POs simultaneously.

Example:

- BIG Wholesale — draft total
- Central Grocery — draft total / minimum status
- Beacon Beverage — draft total / minimum status

Important UI rule:

> Do not build a separate Supplier catalog purchasing flow.

A future Suppliers management screen may open this same Purchasing workspace with a Supplier filter applied.

Playtest this gray-box interaction before visual polish.

---

# Checkpoint 6 — Place and Schedule Orders

**Implementation status:** Placement and scheduling implemented in the isolated lab; campaign money commitment remains an integration boundary

Turn a Draft PO into a real order.

Opening state flow:

**Draft → Placed → Scheduled / In Transit → Delivered**

Define only what the opening game needs:

- `Place Order` commits the PO
- money is paid / committed according to the current simple economy
- Supplier minimum validation is enforced
- delivery time is calculated from the Supplier's rule and the current weekday / time

The isolated lab currently anchors its commercial clock at **Monday, 9:00 AM** so every opening Supplier's temporal consequence can be reviewed together. Campaign integration will provide the real current time and money authority.

Opening scheduling currently uses only authored promises:

- same-day service adds its lead hours;
- next-day service advances one calendar day at the same time;
- fixed routes select the next authored route day without inventing an arrival hour.

An order placed on a route day rolls to the following route until explicit route cutoffs are authored. Cutoff times should not be guessed or hidden in presentation code.

Examples:

- BIG Wholesale: arrives later the same simulated day
- Central Grocery: arrives the next day
- Beacon Beverage: arrives on the next Tuesday / Friday route

Do not add invoices, credit terms, backorders, returns, damage, or contract logic here.

---

# Checkpoint 7 — Delivery / Receiving / Owned Inventory

A placed order must physically arrive before it becomes usable inventory.

Opening flow:

**Placed Order → Supplier Vehicle Arrival → Receiving → Owned Inventory**

Keep this first pass simple.

The checkpoint is complete when:

- the Supplier's scheduled delivery occurs
- the ordered SKU quantities arrive through Receiving
- those quantities become inventory owned by the store
- Purchasing / inventory state can distinguish inbound from received stock

Later receiving capacity, pallets, docks, staging, and congestion extend this flow rather than replace it.

---

# Checkpoint 8 — Fixture Assignment + Stocking + First Sale

Connect owned inventory to the sales floor.

A Fixture assignment declares:

- which SKU belongs there
- how much display capacity the Fixture currently wants for that SKU

Stocking then moves available owned inventory to the Fixture.

Permanent boundary:

> **Purchasing obtains inventory. Stocking moves inventory.**

The opening vertical slice is complete when a real accepted SKU can travel through the entire chain:

> Bright Cola exists as a branded Product → a Supplier offers it → the player orders it → it arrives → it becomes owned inventory → it is stocked onto an assigned Fixture → a Customer purchases it.

---

# Checkpoint 9 — Contextual Links and Supplier Management Surface

Only after the core loop works, connect convenient entry points.

Examples:

- Fixture → Product detail / Order Stock
- Product → View Fixtures
- Product → compare Supplier Offers
- Supplier → open Purchasing filtered to that Supplier
- low stock → open Purchasing on that Product

A dedicated Suppliers screen, when added, is a **commercial relationship / supplier-management surface**, not another place to duplicate the buying workflow.

Deep Supplier Accounts & Relationships remain deferred until the opening Purchasing loop proves itself.

---

# Explicitly Deferred

Do not pull these into the first merchandise-circulation slice:

- Supplier Accounts & Relationship tiers
- contracts
- negotiation
- credit terms / invoices
- preferred-supplier policies
- pallet / truckload economics
- supplier reliability simulation
- shortages / backorders
- returns / damage
- product discontinuation
- manufacturer-direct sourcing
- private label
- auto-replenishment
- deep forecasting
- brand loyalty simulation

Preserve strong ideas as patches instead of widening the active build.

---

# Build Discipline

Use the project workflow rule: one boundary at a time.

For this system, the intended order is:

1. **Brands + 12 Products**
2. **3 Suppliers**
3. **Supplier Offer matrix**
4. **Draft PO domain**
5. **Gray-box product-first Purchasing UI**
6. **Place / schedule orders**
7. **Delivery / Receiving / inventory**
8. **Fixture stocking + first customer sale**
9. **Contextual links / Supplier management surface**

After each checkpoint: compile, test, playtest where relevant, and keep the accepted checkpoint stable before adding the next boundary.

## Overengineering test

If a proposed feature does not change what the player can **see, choose, pay, wait for, receive, stock, or sell** in the opening vertical slice, it probably belongs in a patch instead of the active implementation.
