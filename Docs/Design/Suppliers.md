# Big Retail — Supplier Design

**Status:** Accepted foundation; opening gray-box implemented

## Core role

Suppliers are part of Big Retail's outside economy. Products do not come from a universal magic catalog. A supplier offers a product to the player's business under specific commercial conditions.

Permanent commercial chain:

**Supplier → Supplier Offer → Purchase Order → Delivery → Inventory**

Products themselves are defined separately in `Products.md`; consumer brands are defined separately in `Brands.md`.

## Supplier tradeoff model

Suppliers are balanced primarily across three dimensions:

- **Cost** — how cheaply they can supply merchandise.
- **Flexibility** — how easily they accommodate the retailer through small orders, frequent delivery, small packs, emergency service, and low minimums.
- **Assurance** — how dependable and predictable their service is.

A supplier should rarely dominate all three at once.

Assortment/category defines supplier identity rather than functioning as a fourth stat.

## Opening suppliers

### BIG Wholesale

**Role:** emergency / convenience wholesaler

- Broad opening assortment
- Same-day delivery, usually within a few in-game hours
- No meaningful opening minimum
- Small or forgiving purchase quantities
- Very high flexibility
- Very high assurance
- Highest prices among overlapping opening offers

BIG Wholesale exists to absorb the player's uncertainty for a price. It should remain useful even after cheaper options become available.

### Central Grocery Supply

**Role:** planned grocery distributor

- Grocery-focused assortment
- Next-day delivery
- Moderate opening minimum order
- Better unit economics than BIG Wholesale
- High assurance
- Lower flexibility

Central teaches the player that planning ahead improves margin.

### Beacon Beverage Distribution

**Role:** specialist route distributor

- Beverage-focused assortment
- Best opening beverage economics
- Fixed route days: Tuesday and Friday
- Moderate opening minimum
- Lowest flexibility of the starting three

Beacon teaches that specialist suppliers can be the cheapest choice while demanding the most planning.

## Required overlap

Starting suppliers must have overlapping SKUs so supplier choice is meaningful.

Example pattern:

**Bright Cola**

- BIG Wholesale — smaller pack, highest unit cost, arrives today
- Central Grocery Supply — standard pack, lower unit cost, arrives tomorrow
- Beacon Beverage Distribution — best unit cost, next scheduled route

The player is choosing a supply arrangement for the same product, not choosing between artificial duplicate products.

The opening map is now authored against the accepted 12-SKU assortment in `Products.md`.

## Opening commercial terms — v0.1

These are implementation/playtest values, not permanent balancing law.

| Supplier | Minimum | Delivery |
|---|---:|---|
| BIG Wholesale | None | Within 3 hours |
| Central Grocery Supply | $100.00 | Next day |
| Beacon Beverage Distribution | $75.00 | Tuesday / Friday route |

| Opening SKU | BIG Wholesale | Central Grocery | Beacon Beverage |
|---|---:|---:|---:|
| Bright Cola — 20 oz Bottle | Case × 12 · $12.00 | Case × 24 · $21.00 | Case × 24 · $19.20 |
| ClearSpring Pure Water — 20 oz Bottle | Case × 12 · $8.40 | Case × 24 · $14.40 | Case × 24 · $12.96 |
| Ridgeway Original Potato Chips — Single Bag | Case × 12 · $11.40 | Case × 24 · $20.40 | — |
| ChocoMax Milk Chocolate — Bar | Case × 24 · $16.80 | Case × 48 · $29.76 | — |
| Sunburst Fruit Chews — Pack | Case × 12 · $9.00 | Case × 24 · $15.84 | — |
| Homestead White Bread — Loaf | Case × 8 · $12.00 | Case × 16 · $21.60 | — |
| Homestead Whole Milk — Jug | Case × 6 · $10.80 | Case × 12 · $19.20 | — |
| Crunch-O Corn Flakes — Box | Case × 8 · $14.40 | Case × 12 · $19.20 | — |
| CleanMax Paper Towels — Roll | Case × 12 · $12.00 | Case × 24 · $21.12 | — |
| CleanMax Dish Soap — Bottle | Case × 12 · $14.40 | Case × 24 · $25.44 | — |
| Spark Alkaline Batteries — 4-Pack | Case × 12 · $28.80 | — | — |
| FreshMint Toothpaste — Tube | Case × 12 · $18.00 | — | — |

This yields 24 opening Supplier Offers: BIG carries 12 SKUs, Central carries 10, and Beacon carries 2.

## Product separation

A Product/SKU is the consumer-facing item. It does not own one universal purchase price.

A Supplier Offer means:

> This supplier sells this SKU in this purchase pack, at this price, under this delivery rule.

Opening Supplier Offer data expresses:

- Supplier
- SKU
- Purchase-pack quantity
- Purchase-pack price
- Effective unit cost
- Availability

The opening Supplier owns its shared minimum-order and delivery rule. Each Offer resolves those consequences through its Supplier. This keeps repeated terms in one authored place while leaving room for offer-specific exceptions later if playtesting demonstrates a real need.

## Supplier availability philosophy

The opening system should protect supplier choice rather than hide it behind relationship progression.

Once a supplier is commercially available to the store, the player can place ordinary orders with them subject to that supplier's concrete order conditions.

Useful restrictions are things such as:

- category / department compatibility
- required store capability later (for example refrigeration or pallet-capable receiving)
- minimum order
- delivery schedule
- pack size
- price

Future relationships and contracts may improve or bend those terms, but should not become an XP-style permission gate for ordinary supplier choice.

## Purchasing interaction rule

**Purchasing is the transaction surface. Suppliers are not a second shopping system.**

Canonical buying flow:

**Product → choose Supplier Offer → choose quantity → add to supplier-specific Purchase Order**

If a SKU has multiple Supplier Offers, Purchasing exposes the available choices and their consequences such as:

- unit cost
- purchase pack
- arrival timing
- minimum/order constraint

A supplier can also be used as a filter/lens over the same Purchasing workspace.

Example:

> Open Central Grocery → View Products

This opens Purchasing filtered to Central. It does **not** open a second duplicated supplier catalog interface.

## Supplier directory and future management UI

The opening Commercial Directory now provides a deliberately read-only Supplier view. It answers:

- Who are the opening Suppliers?
- What is each Supplier's specialty?
- What are their current delivery rules and minimums?
- Which opening Products do they carry?

This directory does not place orders and does not duplicate Purchasing.

A dedicated Suppliers screen still has a valid long-term role, but its purpose is to manage / understand the companies themselves rather than duplicate purchasing.

It can eventually answer:

- Who supplies us?
- What categories do they serve?
- What are their delivery rules and minimums?
- What agreements or contracts exist?
- What relationship / account terms exist later?

The current read-only directory is the minimal opening form. Deeper management should wait until agreements, relationships, or account terms provide something real to manage.

## Weekly calendar

Big Retail uses a continuous Monday-through-Sunday simulation calendar.

Supplier timing should be expressed in concrete player-facing language such as:

- Arrives today at approximately 3:00 PM
- Arrives tomorrow
- Next route: Friday

The calendar creates understandable procurement tradeoffs without requiring the player to interpret abstract lead-time math.

## Mr. BIG

Milton "Mr. BIG" Big owns BIG and BIG Wholesale and is tied to the player's startup debt.

He is simultaneously:

- a tutor,
- a stable commercial safety net,
- and a commercial opponent.

His tutorial role should usually arrive disguised as a useful business offer. He likes the player and wants them to succeed because their success is profitable to him.

The intended player feeling is approximately:

> "Oh, this bastard. ...Okay, what've you got?"

BIG Wholesale is therefore both a real gameplay tool and a narrative expression of the player's early dependence on Mr. BIG's ecosystem.

BIG Wholesale should remain persistently useful: expensive convenience is a legitimate strategic option, not merely a tutorial trap that disappears once the player learns better planning.

## Opening scope rule

Keep the first supplier implementation flat.

Do **not** build these yet:

- supplier relationship tiers
- negotiation
- contracts
- credit terms
- complex reliability
- shortages or backorders
- returns or damage
- discontinuation
- pallets or truckloads
- private label
- manufacturer-direct deals

The relationship concept is preserved separately as a future patch under `Patches/SupplierAccountsAndRelationships.md`.

These are future extensions of the same supplier model, not requirements for the opening implementation.

## Core design law

The supplier system should never collapse into:

> Buy from whoever has the lowest price.

The intended question is:

> Which supply arrangement best fits the retail operation I have built and the problem I need to solve right now?
