# Design Patch — Supplier Accounts & Relationships

**Status:** Parked / Deferred

**Purpose:** Preserve the deeper supplier-relationship idea without making it part of the current implementation target.

This patch is not active design. The current game should begin with a flat supplier model. Revisit this only after the basic Product → Supplier Offer → Purchase Order loop is proven.

## Core idea

A Supplier exists independently as a company. A Supplier Account represents that supplier's commercial relationship with the player's store.

The distinction would be:

- **Supplier** — who the company is.
- **Supplier Account** — our current commercial relationship with that company.
- **Supplier Offer** — what that supplier will sell us, in what pack, at what price, and under what delivery rules.
- **Relationship History / Qualification** — the business facts that may unlock better commercial opportunities later.

The intent is to let the same supplier treat a tiny new retailer differently from a major account without contaminating Product/SKU data or replacing Supplier Offers.

## Relationship philosophy

Avoid a gamey `Supplier Reputation: 73/100` meter.

Supplier relationships should primarily respond to observable business facts such as:

- purchasing volume;
- length of relationship;
- consistency of business;
- infrastructure/capability requirements;
- future payment history if credit terms are ever added;
- willingness to make volume commitments;
- strategic importance of the player's account.

Relationship progression should unlock **commercial opportunities**, not arbitrary stat bonuses.

Examples:

- lower minimum order;
- additional delivery day;
- volume-pricing program;
- larger purchase packs / pallet access;
- better payment terms;
- negotiation access;
- special service options.

## Possible account states

A future version could distinguish:

- **Known** — the player knows the supplier exists.
- **Available** — the supplier is willing to open an account if requirements are satisfied.
- **Active** — the player can place purchase orders.

Opening an account should remain an abstraction, not paperwork gameplay.

## Possible account standing

A compact descriptive standing could summarize how commercially important the player has become to that supplier:

- New Account
- Established Account
- Preferred Account
- Key Account
- Strategic Account

Standing should describe the relationship, while current commercial qualification can remain separate.

Example:

- **Standing:** Key Account
- **Current Volume Tier:** Tier 2

This prevents a long-term relationship from disappearing just because current volume temporarily drops, while still allowing volume-sensitive terms to change.

## Supplier-specific qualification

Different suppliers should care about different business facts.

Examples:

- a beverage distributor rewards beverage volume;
- a grocery distributor rewards overall grocery volume and consistency;
- a manufacturer may reward commitment to its product line;
- a large freight supplier may require receiving infrastructure and high order minimums;
- BIG may offer unusually favorable service access because Milton Big has already embedded the player in the BIG ecosystem.

The relationship framework can be shared while qualification rules remain supplier-specific.

## Opening supplier relationship flavor

If this patch is eventually activated, the opening trio could begin differently:

### BIG Wholesale

- Account already active.
- Possibly begins at a stronger standing than a normal new retailer would receive.
- Narrative explanation: Milton arranged it.
- This reinforces BIG's role as tutor, safety net, and commercially self-interested patron.

### Central Grocery Supply

- Active account.
- New / standard commercial standing.
- Standard pricing, normal minimums, next-day service.

### Beacon Beverage Distribution

- Active account.
- New / standard commercial standing.
- Best beverage economics but fixed route service and its own minimums.

## Long-term relationship arc

The broader supplier progression fantasy could eventually move through:

**You adapt to suppliers**
→ **You choose suppliers**
→ **You negotiate with suppliers**
→ **Suppliers adapt to you**

The system should allow the player's sourcing decisions to reshape supplier leverage over time.

Products generate purchasing volume. Purchasing volume can create supplier leverage. Supplier leverage can improve purchasing options. Better purchasing changes product economics.

## UI concept if activated later

A supplier detail screen could eventually show a compact account summary such as:

```text
CENTRAL GROCERY SUPPLY
Established Account

Current Terms
Next-day delivery
$150 minimum PO
Standard Commercial Pricing

Business
This week: $1,624
Average weekly purchases: $1,410

Available Opportunity
Volume Grocery Program
Reach $2,000/week average
→ 3% grocery discount
```

This should remain secondary to the main Purchasing workflow. Supplier relationships should deepen the commercial network, not create a parallel relationship minigame.

## Explicitly not active yet

Do not implement this patch during the current flat purchasing phase.

The current implementation remains:

**Product / SKU → Supplier → Supplier Offer → Purchase Order**

with the three opening suppliers differentiated by cost, flexibility, assurance, assortment, pack size, and delivery schedule.

Revisit this patch only when the flat purchasing loop is proven and deeper supplier progression would add useful decisions rather than setup complexity.
