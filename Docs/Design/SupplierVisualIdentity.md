# Big Retail — Supplier Visual Identity

**Status:** Accepted opening direction; exact production colors remain adjustable after in-game art tests

## Purpose

Suppliers should be visually recognizable before the player reads their names.

Their identity should appear consistently across the physical supply chain:

- delivery vehicles;
- shipping cartons / cases;
- pallet or freight markings later;
- receiving-area labels;
- supplier badges and filters in Purchasing;
- future supplier-management UI.

The goal is both flavor and simulation readability. A busy Receiving area should communicate which company delivered which freight at a glance.

## Core separation

**Product branding and supplier branding are different layers.**

A customer-facing SKU such as **Bright Cola** keeps the visual identity of Bright Beverage Co. on the shelf.

The outer shipping case, delivery vehicle, manifest label, or receiving marker may carry the identity of **BIG Wholesale**, **Central Grocery Supply**, or **Beacon Beverage Distribution**.

Do not recolor consumer packaging to match the supplier that delivered it.

Conceptually:

**Consumer package = Brand / Product identity**

**Shipping layer = Supplier identity**

This allows the sales floor and the supply chain to feel like two connected commercial worlds.

---

# Opening supplier palettes

These colors are the current art-direction starting point, not untouchable production values. They should be adjusted if sprite-scale readability or contrast testing requires it.

## BIG Wholesale

**Commercial identity:** powerful, polished, broad, immediate, expensive

| Role | Color | Hex |
|---|---|---|
| Primary | Deep Burgundy | `#7A1F2B` |
| Secondary | Warm Cream | `#F3E6CF` |
| Accent | Gold | `#C89B3C` |

### Visual language

- Bold, heavy **BIG** wordmark.
- Broad burgundy bands or large stamped marks.
- Gold should be an accent, not the dominant carton color.
- Shapes should feel confident, expensive, and slightly self-important.
- Branding can be cleaner and more polished than the other opening suppliers.

### Shipping carton direction

Preferred starter treatment:

- kraft-cardboard base;
- wide burgundy supplier band;
- large BIG stamp / logo;
- cream or light product-information label;
- small gold accent where sprite scale permits.

A stack of BIG cartons should be identifiable primarily by the burgundy band and oversized BIG mark.

### Vehicle direction

- Burgundy-dominant corporate livery.
- Cream body panels or large cream logo field.
- Gold accent stripe / trim used sparingly.
- Should feel like a company with money and a strong corporate identity.

**Read:** "Milton sent one of his trucks."

---

## Central Grocery Supply

**Commercial identity:** practical, regional, dependable, planned grocery distribution

| Role | Color | Hex |
|---|---|---|
| Primary | Grocery Green | `#2F6B3F` |
| Secondary | Warm Beige | `#E8DDC6` |
| Accent | Burnt Orange | `#C77A2B` |

### Visual language

- Functional rather than flashy.
- Clean horizontal bands and standardized labeling.
- Professional grocery / food-distribution appearance.
- Less visual ego than BIG; more emphasis on organization and reliability.

### Shipping carton direction

Preferred starter treatment:

- kraft-cardboard base;
- green horizontal band or corner marking;
- compact Central Grocery Supply mark;
- standardized product / quantity label;
- small burnt-orange routing or category accent where useful.

Central cartons should look like they belong in an organized regional distribution network.

### Vehicle direction

- Green-and-beige fleet livery.
- Straightforward rectangular graphics.
- Orange used only as a small identifying accent.
- Should look dependable and workmanlike rather than premium.

**Read:** "The planned grocery order is here."

---

## Beacon Beverage Distribution

**Commercial identity:** beverage specialist, route delivery, crisp, energetic

| Role | Color | Hex |
|---|---|---|
| Primary | Route Blue | `#1F5F8B` |
| Secondary | Clean White | `#F7F9FA` |
| Accent | Teal | `#2AA7A1` |

### Visual language

- Clean and cool.
- More energetic than Central.
- Stripes, waves, or route-like directional graphics are appropriate.
- The identity should suggest beverages without depicting a specific consumer brand.

### Shipping carton direction

Preferred starter treatment:

- kraft-cardboard or beverage-case base;
- crisp blue side band / stripe;
- Beacon mark in white or light field;
- teal route or handling accent;
- product label remains distinct from supplier branding.

Later beverage freight may naturally use trays, shrink-wrap, or specialized cases, but the opening system does not require those asset types.

### Vehicle direction

- Blue-dominant beverage-route livery.
- White logo panels.
- Teal directional / wave accent.
- Should read visually lighter and more category-specialized than Central or BIG.

**Read:** "The drink route is here."

---

# Shared shipping-case system

The opening implementation does **not** need bespoke illustrated freight art for every Supplier Offer.

Use a reusable visual structure:

1. **Base case/carton asset**
2. **Supplier visual layer** — color band / logo / marking style
3. **Product label** — SKU identity, quantity, or abbreviated product information
4. Optional later **handling/category marking**

Example:

**BIG Wholesale carton**
- burgundy BIG band
- label: `Bright Cola — 12`

**Central Grocery carton**
- green Central band
- label: `Homestead Whole Milk — 12`

The supplier layer explains **where the case came from**. The SKU label explains **what is inside**.

This approach lets a small art set represent many Supplier Offers without making every offer a unique shipping-box sprite.

---

# Readability rules

- Supplier identity should survive small isometric sprite scale.
- Primary recognition should come from **large color regions and silhouette/pattern**, not tiny text.
- Do not rely on color alone where a large logo mark, band orientation, or pattern can reinforce identity.
- Keep the three opening primary colors strongly separated: **burgundy / green / blue**.
- Product labels should remain readable against the supplier treatment.
- Avoid covering most of a carton with saturated supplier color; kraft cardboard should remain a useful neutral base.
- Supplier UI badges should reuse the same primary identity colors so world freight and management UI reinforce one another.

## Suggested pattern distinction

Color is the first cue; pattern can become the second cue:

- **BIG Wholesale:** broad solid band / oversized stamp
- **Central Grocery Supply:** orderly horizontal band / standardized label block
- **Beacon Beverage Distribution:** narrow route stripe / wave or directional motif

This helps preserve recognition for players who have difficulty distinguishing colors and prevents the art from depending entirely on hue.

---

# Scope guardrail

For the first playable merchandise loop, the visual system only needs to prove:

- the three suppliers look different;
- their trucks are distinguishable;
- their freight is distinguishable in Receiving;
- the same supplier identity can appear in Purchasing UI;
- the product itself keeps its own consumer brand identity.

Do **not** require yet:

- bespoke carton art for every SKU;
- pallet-specific supplier families;
- multiple historical supplier logos;
- detailed vehicle fleets;
- uniforms for supplier drivers;
- full corporate brand manuals;
- animated signage or elaborate warehouse markings.

Those can grow from this system if the simulation later needs them.

## Design law

> **A supplier should be recognizable as a company in both the interface and the physical world, while the merchandise remains recognizable as the product the customer buys.**
