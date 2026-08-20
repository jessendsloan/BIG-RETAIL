# Inbound Supplier Delivery Artwork Drop Guide

The first physical delivery slice uses one **1 × 1 pallet per supplier purchase order**. The purchase order remains the exact manifest; one of four complete pallet-load sprites provides a readable summary of its total case count.

## Opening sprite set

Each supplier owns four transparent PNGs containing the complete pallet and carton arrangement:

- `BigWholesalePalletLoad1.png` through `BigWholesalePalletLoad4.png`
- `CentralGroceryPalletLoad1.png` through `CentralGroceryPalletLoad4.png`
- `BeaconBeveragePalletLoad1.png` through `BeaconBeveragePalletLoad4.png`

BIG Wholesale currently has its authored opening set. Central and Beacon use clearly named copies of the BIG artwork as temporary stubs. Replace those PNGs in place when their artwork is ready; their Unity references will remain intact.

## Composition

- Keep all four tiers on the same transparent canvas with the pallet grounded at the same location.
- Match the game's current isometric camera angle.
- Tier 1 should show one carton, tier 2 two cartons, tier 3 three cartons, and tier 4 four cartons.
- Supplier markings should remain recognizable at normal gameplay zoom without relying on tiny text.
- Use each supplier's identity colors while preserving enough contrast to read against pavement and store flooring.
- Avoid baked ground shadows extending far outside the object. A compact contact shadow is acceptable.

## Unity import and assignment

The project imports these images as single sprites with transparency, no mipmaps, and a bottom-center pivot. The opening supplier assets expose four **Delivery Load** artwork slots:

- `Assets/Design/Purchasing/Suppliers/BIGWholesale.asset`
- `Assets/Design/Purchasing/Suppliers/CentralGrocery.asset`
- `Assets/Design/Purchasing/Suppliers/BeaconBeverage.asset`

Use **Big Retail → Merchandise → Refresh Supplier Delivery Load Artwork** if an empty slot ever needs to be restored from the standard filenames. Existing non-empty artwork assignments are preserved.

The renderer normalizes the complete load to one Receiving Area cell. Matching canvas dimensions and pallet placement across all twelve PNGs prevents visual movement when the selected tier changes.

## Current gameplay rules

| Purchase-order case count | Supplier-load sprite |
|---:|---:|
| 1–3 | Load 1 |
| 4–7 | Load 2 |
| 8–11 | Load 3 |
| 12+ | Load 4 |

Each ready supplier PO owns a separate pallet and reserves one free player-designated Receiving Area cell. Orders beyond the painted capacity wait until a berth becomes available. Receiving that PO removes its load and passes its exact units into the existing rack/overflow inventory path.

This is intentionally one-PO-per-berth presentation, not full pallet-capacity or truckload procurement simulation. It leaves room for later docks, supplier vehicles, unloading labor, and employee hauling without replacing the purchase-order model.
