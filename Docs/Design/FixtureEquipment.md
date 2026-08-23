# Fixture Equipment

Fixtures are purchased physical equipment. The player may plan a store layout
for free, but installing a fixture consumes one owned equipment module.

## Player loop

1. Open the Fixtures tool and turn on **Plan Layout**.
2. Place translucent fixture plans without spending cash.
3. Open the **Equipment Catalog**. Browse by category, set quantities, or add
   every uncovered plan requirement to the equipment-order draft.
4. Place the equipment order. The store pays immediately from store cash.
5. Equipment becomes ready after its configured game-time lead.
6. A placeholder equipment pallet claims space in an operational Receiving
   Area through the same reservation system used by merchandise deliveries.
7. Open **RCV** and choose **Receive Equipment** to move staged shipments into
   owned equipment storage.
8. Choose **Install Ready**, or leave plan mode and place owned fixtures
   individually. Each installation consumes one module.
9. Removing an installed fixture returns that same module to equipment
   storage. Moving a fixture is therefore store and re-place, with no purchase.

## System boundary

- **BIG Wholesale is the exclusive fixture-equipment supplier.** The player
  browses one BIG-owned Equipment Catalog, stages one BIG equipment draft,
  and receives BIG-branded equipment pallets. There is no fixture-supplier
  comparison or selection.
- Equipment orders are separate from supplier purchase orders and do not
  create merchandise lines. BIG's equipment exclusivity does not require the
  deferred supplier-account or relationship systems.
- The Equipment Catalog is its own full-screen workspace. The compact fixture
  drawer is limited to selecting, planning, opening the catalog, and installing.
- Receiving equipment is an RCV action, not a shortcut in the fixture drawer.
- Equipment and merchandise share the simulation clock, store cash, and
  Receiving capacity.
- Receiving load identities include their source, so supplier order `1` and
  equipment order `1` cannot collide.
- Plans use the authoritative fixture placement rules and reserve their
  footprints against other plans.
- Direct placement and removal are equipment-aware construction history
  actions. Undo returns modules to storage; redo consumes them again.

## Opening catalog

| Equipment | Price | Delivery lead |
| --- | ---: | ---: |
| Half Shelf | $160 | 120 game minutes |
| Standard Shelf | $240 | 120 game minutes |
| Backstock Shelf | $320 | 120 game minutes |
| Basic Checkout Counter | $850 | 120 game minutes |

These values are initial tuning, not final balance. The catalog is authored in
`Assets/Design/Equipment/FixtureEquipmentCatalog.asset` and can grow alongside
the fixture catalog.

## Current scope

- Equipment starts at zero owned modules so the complete loop is exercised.
- Arrivals use BIG Wholesale's authored one-to-four-carton supplier loads.
  The generic BIG-red placeholder remains as a safe fallback if that artwork
  is unavailable.
- Orders, plans, and owned quantities currently live for the running Gameplay
  session. Save/load persistence is a later integration seam.
- **Install Ready** is a convenience batch in this first pass; its individual
  placements are not yet recorded as one combined undo transaction.
- Assembly labor, selling/scrapping, supplier terms, and employee automation
  are deliberately outside this pass.
