# Big Retail — Supplier Design

**Status:** Accepted foundation; opening implementation intentionally flat

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

The exact opening supplier-to-product map should now be authored against the accepted 12-SKU opening assortment in `Products.md` rather than against placeholders.

## Product separation

A Product/SKU is the consumer-facing item. It does not own one universal purchase price.

A Supplier Offer means:

> This supplier sells this SKU in this purchase pack, at this price, under this delivery rule.

Opening Supplier Offer data should be able to express:

- Supplier
- SKU
- Purchase-pack quantity
- Purchase-pack price
- Effective unit cost
- Delivery rule
- Availability

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

## Future Suppliers management UI

A dedicated Suppliers screen still has a valid long-term role, but its purpose is to manage / understand the companies themselves rather than duplicate purchasing.

It can eventually answer:

- Who supplies us?
- What categories do they serve?
- What are their delivery rules and minimums?
- What agreements or contracts exist?
- What relationship / account terms exist later?

The opening implementation may keep this screen minimal or defer it until it has enough management content to justify itself.

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
