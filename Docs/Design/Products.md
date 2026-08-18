# Big Retail — Product Design

**Status:** Accepted opening foundation

## Core idea

Products are the merchandise the customer recognizes and buys.

A Product / SKU exists independently of Suppliers.

Supplier-specific case size, wholesale cost, minimums, and delivery timing do **not** belong on the Product. They belong to Supplier Offers.

## Product identity fields

For now, a SKU is defined by:

- **Brand** — who the product is from
- **Product Line** — the underlying thing being sold, such as Cola, Milk, or Toothpaste
- **Category** — where the product lives in the retail hierarchy
- **Market Position** — Value, Standard, or Premium
- **Package / Form** — what customer-facing unit is actually sold
- **Shelf Profile** — how the unit occupies fixture space; exact implementation still to be designed

## Market Position

Market Position is a lane, not a mandatory three-item set.

Current lanes:

- **Value** — price-led, low-frills
- **Standard** — mainstream, broad appeal
- **Premium** — higher-priced, more selective proposition

Not every Product Line needs Value + Standard + Premium versions.

A Product Line should occupy only the positions that create useful assortment choices.

The opening assortment is intentionally almost entirely **Standard** so the player first learns the retail loop before assortment-tier strategy expands.

## Customer motive is NOT a fixed Product Role

Do not permanently label a product as "Impulse," "Emergency," "Staple," etc.

The same SKU can be purchased for different reasons by different customers or on different trips.

Example:

- Milk may be a planned grocery purchase.
- The same milk may be an emergency replacement because a household ran out.
- The same milk may be added because the customer is already buying cereal.

Therefore:

> **Product describes what the item is. Customer / shopping-trip logic describes why it is wanted now.**

If the future customer simulation needs intrinsic product traits such as impulse appeal or substitution behavior, add them deliberately when that system is designed rather than encoding purchase motive as a static role today.

## Opening retail identity

The opener represents a small convenience-oriented store.

It is not a separate disposable product universe. It is a narrow slice of the same merchandise universe that later grocery / mega-retail assortment will deepen.

A supermarket later expands breadth and depth around these same Product Lines rather than replacing them.

## Opening Core Assortment v0.1

| Product Line | Opening SKU | Brand | Position | Package / Form |
|---|---|---|---|---|
| Cola | Bright Cola | Bright Beverage Co. | Standard | 20 oz Bottle |
| Bottled Water | ClearSpring Pure Water | ClearSpring | Standard | 20 oz Bottle |
| Potato Chips | Ridgeway Original Potato Chips | Ridgeway Snacks | Standard | Single Bag |
| Chocolate Bar | ChocoMax Milk Chocolate | ChocoMax | Standard | Bar |
| Fruit Candy | Sunburst Fruit Chews | Sunburst Candy Co. | Standard | Pack |
| White Bread | Homestead White Bread | Homestead Foods | Standard | Loaf |
| Whole Milk | Homestead Whole Milk | Homestead Foods | Standard | Jug |
| Corn Flakes | Crunch-O Corn Flakes | Crunch-O | Standard | Box |
| Paper Towels | CleanMax Paper Towels | CleanMax Home | Standard | Roll |
| Dish Soap | CleanMax Dish Soap | CleanMax Home | Standard | Bottle |
| Batteries | Spark Alkaline Batteries | Spark | Standard | 4-Pack |
| Toothpaste | FreshMint Toothpaste | FreshMint | Standard | Tube |

## Why these twelve

The opening assortment combines several kinds of believable convenience-store demand without needing bespoke "starter products":

- immediate-consumption goods: cola, water, chips, candy
- household/basic grocery needs: bread, milk, cereal
- forgotten / quick-trip essentials: paper towels, dish soap, batteries, toothpaste

The game does not need to label those reasons as Product Roles. The customer simulation can later determine purchase motive contextually.

## Assortment progression

Product progression means **assortment progression**, not SKU leveling.

As the retailer grows:

- more Product Lines become commercially viable
- existing Product Lines gain more brand alternatives
- Value / Standard / Premium alternatives appear where useful
- package/form depth increases where useful
- departments introduce products with new operational requirements

Example:

Opening Milk:
- Homestead Whole Milk — Standard

Later Milk assortment might include:
- a Value milk
- Homestead Whole Milk — Standard
- a Premium organic milk
- 2%, skim, lactose-free, alternative package sizes

The original product remains valid. The category gains depth.

## Permanent separation

**Product / SKU** = what the customer buys.

**Supplier Offer** = how a specific supplier offers that SKU to the store.

**Store Assortment** = which available SKUs the player chooses to carry.

**Fixture Assignment** = where and how the store displays an assortment SKU.

This separation should remain intact as procurement and merchandising grow.
