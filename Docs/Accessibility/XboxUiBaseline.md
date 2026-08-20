# Xbox UI Accessibility Baseline

This document records the accessibility baseline for BIG RETAIL's gameplay UI.
It is a design and regression target, not a certification claim.

## Current baseline

- Gameplay UI text authored in `ConstructionToolbar.uss` uses a tiered 15-28 px
  scale at the 1920 x 1080 reference size. Captions and supporting copy remain
  compact while key values, dialogue, headers, and actions stay prominent.
- Icon-only controls include contextual text and expose the same context on
  pointer hover and keyboard or controller focus. That context appears in a
  fixed HUD dock so it cannot cover construction panels.
- Focus changes use a combination of a high-visibility border, background, and
  scale change instead of color alone.
- Decorative UI motion is brief, player-initiated, and has an
  `is-reduced-motion` style seam that removes transition timing.
- Important text is presented on solid or near-opaque panels rather than
  directly over the moving game world.
- Current controls use Unity's default sans-serif runtime font.

## Required follow-up systems

The current patch improves the default experience, but the following work is
still required before describing the game as conforming to the Xbox
Accessibility Guidelines:

1. Add a console or Large UI profile, calibrate actual rendered glyph height
   at 1080p, and make that profile the default on Xbox. USS `font-size` is not
   a direct measurement of rendered body height.
2. Add a player-facing UI scale setting that reaches 200 percent without loss
   of content or functionality and reflows without two-axis scrolling.
3. Add high-contrast presets and measure every important text/control state,
   including disabled and selected states, against its rendered background.
4. Add screen narration and programmatic names, roles, values, and descriptions
   for every interactive element.
5. Complete controller-only, keyboard-only, and focus-order testing across all
   menus, overlays, dialogs, and gameplay panels.
6. Connect the `is-reduced-motion` root class to a saved accessibility setting
   and apply that preference to every future UI animation.
7. Validate localization, long labels, and 200 percent text at 1080p, 1440p,
   4K, ultrawide, and Xbox streaming/mobile presentation sizes.
8. Add safe-area validation for television overscan and platform UI overlays.
9. Add a saved setting that can hide the contextual control-hint dock for
   players who already know the controls or prefer a quieter HUD.

## Microsoft references

- [XAG 101: Text display](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/101)
- [XAG 102: Contrast](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/102)
- [XAG 112: UI navigation](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/112)
- [XAG 113: UI focus handling](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/113)
- [XAG 114: UI context](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/114)
- [XAG 117: Visual distractions and motion settings](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/117)
