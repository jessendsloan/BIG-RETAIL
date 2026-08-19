# Inbound Supplier Delivery Artwork Drop Guide

The first physical delivery slice uses one **1 × 1 pallet per supplier purchase order**. The purchase order remains the exact manifest; its visible carton stack is a readable summary of the order's total case count.

## Art needed

Four transparent PNG sprites are enough for the finished opening pass:

1. `InboundPallet.png` — one shared empty 1 × 1 isometric pallet.
2. `BIGWholesaleCarton.png` — BIG Wholesale shipping carton.
3. `CentralGroceryCarton.png` — Central Grocery Supply shipping carton.
4. `BeaconBeverageCarton.png` — Beacon Beverage Distribution shipping carton.

The game builds the stack. Do not draw separate one-box, two-box, or three-box pallet images.

## Composition

- Match the game's current isometric camera angle.
- Keep the complete object inside a tightly cropped transparent canvas.
- Show one closed shipping carton, not a consumer product package.
- Keep all three supplier cartons at the same apparent dimensions and viewpoint.
- Supplier markings should remain readable at normal gameplay zoom without relying on tiny text.
- Use each supplier's identity colors, but preserve enough light/dark contrast to read against pavement and store flooring.
- Avoid baked ground shadows extending far outside the object. A compact contact shadow is acceptable.

## Unity import and assignment

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Single`
- Mesh Type: `Full Rect`
- Filter Mode: match the accepted game-art treatment
- Compression: `None` while reviewing
- Pivot: bottom center, placed at the visual ground contact point

Assign `InboundPallet.png` to **Inbound Delivery View System → Pallet Sprite** in `Gameplay`.

Assign each supplier carton to **Delivery Box Sprite** on its supplier asset:

- `Assets/Design/Purchasing/Suppliers/BIGWholesale.asset`
- `Assets/Design/Purchasing/Suppliers/CentralGrocery.asset`
- `Assets/Design/Purchasing/Suppliers/BeaconBeverage.asset`

The renderer normalizes sprite width, so the PNGs do not need identical pixel dimensions. Matching canvas scale and pivot placement will still make review easier.

## Current gameplay rules

| Purchase-order case count | Visible cartons |
|---:|---:|
| 1–3 | 1 |
| 4–7 | 2 |
| 8+ | 3 |

Each ready supplier PO owns a separate pallet and staging cell. Receiving that PO removes its pallet and passes its exact units into the existing rack/overflow inventory path.

This is intentionally a presentation layer, not full pallet-capacity or truckload procurement simulation. It leaves room for a later receiving zone and employee hauling job without replacing the purchase-order model.
