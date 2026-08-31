# Big Retail — Campaign Opener Implementation Plan

**Status:** Active design / engineering handoff  
**Scope:** Founder avatar, Frank's prologue store, Mr. BIG confrontation, and transition into the permanent campaign store

## Why this file exists

The campaign opener is now important enough to have its own implementation-facing source of truth.

The opener should borrow the useful structural lesson from strong playable prologues: let the player briefly **perform the real fantasy in a controlled environment**, then transition into the permanent game where they must build that operation for themselves.

The prologue is allowed to use one bespoke map and bespoke scripting, but it should primarily orchestrate **permanent Big Retail systems** rather than create a separate mini-game architecture.

## Locked narrative premise

- The campaign protagonist is **The Founder**.
- The Founder has a visible physical avatar and is the player's human representative inside the world.
- The management camera represents the player's broader authority over the company; it does not replace the Founder as a character.
- The Founder begins the campaign working in the small retail store operated by their father, **Frank**.
- Frank is an experienced small retailer. The prologue should show that he knows how to run a store rather than portray him as incompetent.
- Frank owes money to **BIG Finance**.
- The family home is tied to the debt and is at risk of foreclosure.
- Frank and Milton "Mr. BIG" Big have prior history.
- Frank deliberately calls him **Milton**, despite having been told more than once to call him **Mr. BIG**.
- Mr. BIG enters the confrontation with substantially more control than Frank. He owns the debt leverage and controls the direction of the conversation.
- It is **Mr. BIG's idea** to put Frank's child to work on one of BIG's retail properties.
- Frank does not negotiate his way into that solution, and the Founder does not invent it.
- Mr. BIG uses the arrangement to solve two problems at once: a delinquent family debt and an idle / underperforming retail property.
- Frank receives relief / time on the house debt in exchange for the Founder going to work for Mr. BIG and making the assigned property productive.
- The arrangement begins partly as punishment and leverage against Frank, but it is also a legitimate commercial opportunity for the Founder.
- The Founder ultimately turns that imposed opportunity into something far larger than either Frank or Mr. BIG initially expects.
- Mr. BIG is not the protagonist. He is catalyst, creditor, tutor, safety net, commercial opponent, and recurring foil.

## Core implementation law

> **The prologue is a curated composition of Big Retail, not a separate game.**

Do not build prologue-only versions of inventory, products, customers, employees, checkout, stocking, receiving, purchasing, or pathfinding if the permanent system can perform the same job.

---

# Phase 1 — Founder avatar foundation

## 1. Founder entity

- [ ] Add a special Founder character identity.
- [x] Founder exists as a normal world character.
- [x] Founder uses the same pathfinding / navigation framework as employees.
- [x] Founder can receive normal retail work tasks.
- [ ] Founder remains uniquely identifiable to campaign and save systems.
- [ ] Founder cannot quit.
- [ ] Founder cannot be fired.
- [ ] Founder has no normal payroll cost.
- [ ] Founder can later coexist with hired employees without a parallel labor system.

### Acceptance

The Founder can stand in a store, navigate, perform employee-compatible retail jobs, survive save/load, and remain the same character after the prologue transition.

## 2. Founder identity / appearance

Minimum viable version:

- [ ] Player can set Founder name.
- [ ] Player can choose a simple appearance / preset.
- [ ] Identity persists into the permanent campaign map.
- [ ] Dialogue can address the Founder without requiring a fixed voiced name.

Do not block the opener on a large character creator.

---

# Phase 2 — Reusable narrative / cinematic framework

## 3. Scripted character sequence support

Provide a lightweight reusable way to stage campaign beats.

Useful commands include:

- [ ] Move character to target.
- [ ] Face target.
- [ ] Play animation / pose.
- [ ] Wait.
- [ ] Trigger dialogue.
- [ ] Trigger camera action.
- [ ] Temporarily suspend normal AI.
- [ ] Restore normal AI.
- [ ] Trigger objective / campaign event.
- [ ] Skip sequence safely.

This should be reusable for later campaign moments rather than hard-coded only for the opener.

## 4. Cinematic camera support

- [ ] Temporarily disable normal gameplay camera control.
- [ ] Camera target / tracking.
- [ ] Pan.
- [ ] Zoom.
- [ ] Hold.
- [ ] Cut.
- [ ] Smooth handoff back to the normal management camera.
- [ ] Hide / restore normal gameplay UI.

### Guardrail

Do **not** create a special WASD or first-person prologue control scheme. Use Big Retail's existing world, characters, and camera language with more deliberate framing.

## 5. Dialogue presentation

Minimum:

- [ ] Speaker name.
- [ ] Dialogue text.
- [ ] Portrait support.
- [ ] Advance dialogue.
- [ ] Optional timed line.
- [ ] Optional voice stinger / short voiced line.
- [ ] Event callback after a line or conversation.

Branching dialogue is not required for the first implementation.

---

# Phase 3 — Frank's prologue store

Frank's prebuilt location, store-layout capture, and deterministic opening
scenario are specified in
[Map Workshop and Frank Roadside Implementation](MapWorkshopAndFrankRoadsideImplementation.md).

## 6. Build the one-off prologue map

Create a deliberately small functioning convenience-store environment.

Required spaces:

- [x] Small sales floor.
- [x] One checkout.
- [x] Small stockroom / backroom.
- [x] Basic receiving point / rear entrance.
- [x] Small parking lot.
- [x] Exterior storefront.
- [ ] Office / backroom surface for BIG Finance paperwork and environmental storytelling.
- [ ] Exterior arrival / parking space for Mr. BIG's vehicle.
- [ ] Interior staging space for the closing confrontation.

### Scope target

Enough geography and content to support approximately **8–15 minutes** of first-time play.

The prologue map does not need normal campaign expansion support.

## 7. Populate Frank's store with real systems

- [x] Use real Big Retail Products / SKUs.
- [x] Use real fixture assignments.
- [ ] Seed real display inventory.
- [ ] Seed real backstock.
- [x] Configure checkout through the permanent checkout system.
- [ ] Make the store operational before the player touches it.

Good opening products can come from the accepted starter universe, such as Bright Cola, ClearSpring Water, Ridgeway Chips, ChocoMax, Homestead staples, Crunch-O, and convenience essentials.

Avoid placeholder merchandise if the permanent authored products are available.

---

# Phase 4 — Playable retail prologue

## 8. Opening shift

The prologue should teach by letting the player **do retail**, not by presenting an essay.

Suggested playable beats:

- [x] Founder begins beside Frank's trailer on the authored property.
- [x] Frank points the Founder toward an ordinary opening task.
- [x] Founder handles merchandise.
- [x] Founder stocks at least one fixture.
- [ ] Customers enter.
- [ ] Founder operates checkout.
- [ ] Player completes several sales.
- [ ] At least one fixture needs replenishment.
- [ ] Founder resolves it using real inventory.
- [ ] Player sees revenue occur.

### Minimum fantasy demonstrated

**Inventory → Shelf → Customer → Checkout → Revenue**

The player should understand what a functioning tiny store feels like before being handed an empty property.

## 9. Delivery beat — recommended

If Receiving is stable enough when the opener is assembled:

- [x] A delivery arrives during the shift.
- [x] Prefer a BIG Wholesale delivery if it fits the story timing.
- [ ] Use supplier-branded truck / van art when available.
- [x] Use supplier-branded cartons / load art when available.
- [x] Founder receives or moves the merchandise.
- [x] Delivered goods enter the same real inventory path used by the campaign.

This lets the prologue preview a larger portion of the permanent chain:

**Order / Delivery → Receiving → Stocking → Sale**

---

# Phase 5 — Environmental setup for Frank's debt

## 10. Show the problem before fully explaining it

Use light environmental signals while the player works:

- [ ] BIG Finance envelope / notice.
- [ ] Past-due paperwork.
- [ ] Phone call, message, or other interruption Frank dismisses.
- [ ] Frank tells the Founder the matter is handled or otherwise avoids discussing it.

The intended impression is:

> Frank's store is operational, but the economics underneath his life are under pressure.

Do not front-load the complete foreclosure explanation in tutorial text.

---

# Phase 6 — Mr. BIG arrives

## 11. Frank character support

- [ ] Frank NPC / prefab.
- [ ] Retail-worker idle or work behavior during the prologue.
- [ ] Portrait.
- [ ] Narrative animation / staging support.
- [ ] Clean transition between worker AI and scripted conversation.

## 12. Mr. BIG character support

- [ ] Mr. BIG NPC / prefab.
- [ ] Walk / idle / talking animation support.
- [ ] Portrait using the accepted visual canon.
- [ ] Cigar treatment if practical.
- [ ] Luxury vehicle or acceptable placeholder.
- [ ] Narrative staging targets in Frank's store.

## 13. Closing-time trigger

After the playable shift:

- [ ] Customers leave.
- [ ] Store reaches a closed state.
- [ ] Founder and Frank remain.
- [ ] Normal simulation is paused, constrained, or handed to the narrative sequence.
- [ ] Mr. BIG's vehicle arrives.
- [ ] Confrontation sequence begins.

---

# Phase 7 — Frank / Mr. BIG confrontation

## 14. Required power structure

This scene should preserve the following authority relationship:

- [ ] Frank knows why Mr. BIG is there.
- [ ] Mr. BIG controls the pace and direction of the conversation.
- [ ] Mr. BIG already knows the relevant financial facts.
- [ ] Frank attempts explanation, deflection, or another extension.
- [ ] Frank calls him **Milton**.
- [ ] Mr. BIG corrects him: **Mr. BIG**.
- [ ] The house / foreclosure leverage becomes clear.
- [ ] Mr. BIG establishes that Frank has already received accommodation before.
- [ ] Mr. BIG brings the Founder into the conversation.
- [ ] Frank immediately objects to involving his child.
- [ ] Mr. BIG reveals that he has a retail property that needs to earn.
- [ ] Mr. BIG states that the Founder is going to run it.
- [ ] Frank refuses / objects.
- [ ] Mr. BIG makes the alternative clear: accept the arrangement or foreclosure proceeds.
- [ ] Founder accepts / steps into the imposed opportunity.

### Non-negotiable scene rule

> **Mr. BIG conceived the arrangement and dictates its terms.**

The scene should not read as Frank successfully pitching a plan to him.

---

# Phase 8 — Title and campaign transition

## 15. Title transition

After the arrangement is accepted:

- [ ] End the Frank-store prologue.
- [ ] Present the **BIG RETAIL** title at the chosen dramatic beat.
- [ ] Load / transition to the permanent campaign property.

The title should mark the point at which the story stops being principally about Frank's existing store and becomes **The Founder's retail story**.

## 16. Transfer persistent campaign state

- [ ] Same Founder name.
- [ ] Same Founder appearance.
- [ ] Same Founder identity / save key.
- [ ] Frank debt / foreclosure campaign state initialized.
- [ ] Mr. BIG relationship / story state initialized.
- [ ] Main campaign opening funds initialized.
- [ ] Correct starting land region ownership initialized.
- [ ] Appropriate supplier availability initialized.

---

# Phase 9 — Arrival at the permanent property

## 17. Short handoff scene

- [ ] Founder appears at the real campaign property.
- [ ] Mr. BIG appears with the Founder.
- [ ] Property visibly reads as small / unimpressive compared with the Founder's future potential.
- [ ] Mr. BIG summarizes the arrangement in broad business terms.
- [ ] Success is linked to making his property productive and keeping Frank's foreclosure pressure contained.
- [ ] Mr. BIG leaves.

Do not create another long cinematic here. This scene hands the player the game.

## 18. Seamless management-camera reveal

- [ ] Cinematic framing begins near the characters.
- [ ] Mr. BIG exits.
- [ ] Founder remains physically visible.
- [ ] Camera rises / zooms into the normal Big Retail management view.
- [ ] Standard UI appears.
- [ ] Construction / management controls become available.
- [ ] Founder remains present as Employee #1.
- [ ] Player receives control.

Opening objective:

> **Open the Store**

---

# Phase 10 — Founder as Employee #1

## 19. Required early jobs

Founder should use employee-compatible implementations for the basic jobs required by the opening slice, including as available:

- [x] Stocking.
- [x] Receiving / inventory movement.
- [ ] Checkout.
- [ ] Cleaning.
- [ ] Other basic opening labor tasks.

Do not create Founder-exclusive versions of jobs that normal employees will eventually perform.

## 20. Delegation payoff

When the player hires employees:

- [ ] Employees can take over work previously done by Founder.
- [ ] Founder remains a valid world character.
- [ ] Founder remains directly selectable / controllable according to the final labor UX.
- [ ] The store can eventually operate without Founder performing routine physical labor.

This is an important expression of Big Retail's scale fantasy:

**Founder does everything → Founder delegates → Founder manages the machine.**

---

# Phase 11 — Sandbox compatibility

## 21. Reuse the Founder architecture

Sandbox should not need the Frank / Mr. BIG prologue.

- [ ] Sandbox creates or selects a Founder.
- [ ] Founder spawns as Employee #1.
- [ ] Same gameplay implementation as campaign.
- [ ] Campaign debt / Frank story state is absent or replaced by sandbox settings.
- [ ] Prologue scene is skipped.

Do not build separate player-character architectures for Campaign and Sandbox.

---

# Phase 12 — Save / load and development shortcuts

## 22. Persist opener state

Save at minimum:

- [ ] Founder customization.
- [ ] Founder world identity.
- [ ] Campaign story phase.
- [ ] Frank debt / foreclosure status.
- [ ] Mr. BIG campaign state.
- [ ] Prologue-complete flag.
- [ ] Current campaign objective.
- [ ] Permanent property state.

## 23. Development / skip controls

For development:

- [ ] Skip whole prologue.
- [ ] Jump to major prologue beats.
- [ ] Start directly at the permanent property.
- [ ] Re-run confrontation without replaying the whole shift.

For release:

- [ ] Allow dialogue / cinematic skipping.
- [ ] Decide later whether repeat campaigns may skip the complete playable prologue.

---

# End-to-end acceptance flow

A clean campaign start should eventually support:

**Create Founder**  
↓  
**Load Frank's store**  
↓  
**Perform real retail tasks**  
↓  
**Serve customers / make sales**  
↓  
**Close store**  
↓  
**Mr. BIG arrives**  
↓  
**Frank / Mr. BIG confrontation**  
↓  
**Mr. BIG assigns Founder to his retail property**  
↓  
**BIG RETAIL title**  
↓  
**Load permanent campaign property**  
↓  
**Same Founder appears with Mr. BIG**  
↓  
**Mr. BIG hands off the property and leaves**  
↓  
**Camera rises into management view**  
↓  
**Objective: Open the Store**  
↓  
**Founder immediately participates in normal employee-compatible work**

---

# Engineering priority

## P0 — Foundation before the opener can work

- [ ] Founder entity / identity.
- [x] Founder integration with employee-compatible jobs.
- [x] Basic stocking.
- [ ] Basic checkout.
- [ ] Customers.
- [ ] Dialogue / event sequencing framework.
- [ ] Scene or map transition support.

## P1 — Assemble the playable prologue

- [ ] Frank's store map.
- [ ] Frank NPC.
- [ ] Mr. BIG NPC.
- [ ] Opening shift scripting.
- [ ] Closing confrontation.
- [ ] Permanent-property arrival scene.
- [ ] Cinematic-to-management camera handoff.

## P2 — High-value polish

- [ ] BIG delivery during the prologue.
- [ ] Supplier-branded vehicles / cartons / load art.
- [ ] Better character animation.
- [ ] Voice stingers / selective voiced lines.
- [ ] Environmental debt storytelling.
- [ ] Music / ambience / title treatment.
- [ ] Camera polish.

## P3 — Later campaign continuation

- [ ] Frank recurring appearances.
- [ ] Mr. BIG recurring campaign triggers.
- [ ] House-debt progression.
- [ ] Eventual release of the house lien / foreclosure threat.
- [ ] Later reversal of commercial leverage between Founder and Mr. BIG.

---

# Existing-system reuse targets

The current Gameplay foundation already contains authored Products, Suppliers, Supplier Offers, Purchasing, scheduled supplier deliveries, Receiving Areas, physical staged supplier loads, backstock / overflow receiving, checkout revenue, a campaign clock, and the opening merchandising catalog.

The opener should build on those systems wherever practical rather than reproducing them.

In particular, the prologue is a good presentation layer for proving that the permanent systems can support a curated first-time-user sequence.

---

# Open design questions that should not block engineering foundation

These remain intentionally open until the scene is written / staged more precisely:

- Exact Frank-store layout and exterior setting.
- Exact dialogue wording.
- Exact reason / history behind Frank's original BIG Finance debt.
- Exact legal / financial abstraction used for the house debt.
- Exact title-card timing.
- Whether the BIG Wholesale delivery occurs before or during the playable shift.
- How much Founder appearance customization ships initially.
- Whether the Founder has a voiced personality or remains mostly player-authored through silence / minimal response.
- Whether Dad's store remains visitable later or exists only as a campaign narrative location.

These questions can be resolved without changing the foundational architecture above.

---

# Scope guardrail

The opener is allowed to have:

- one bespoke prologue map,
- bespoke story scripting,
- bespoke dialogue,
- bespoke cinematic staging.

It should **not** require bespoke replacements for the real game systems.

The expensive work should be reusable character, narrative, and campaign infrastructure. Frank's store itself should remain intentionally small and inexpensive to build.
