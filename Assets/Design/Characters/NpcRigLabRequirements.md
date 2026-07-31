# NPC Rig Lab — First Contract

## Goal

Prove a Project Hospital-like, Unity-native cutout character before
building the customer and employee population systems.

The first milestone is one canonical mannequin with:

- one reusable 20-bone skeleton;
- 18 visible body pieces;
- two authored isometric directions;
- horizontal mirroring for the other two directions;
- a stable floor-level world root;
- placeholder art that can be replaced without rebuilding the rig.

## Direction contract

| Displayed facing | Authored artwork | Mirrored |
|---|---|---:|
| SouthEast | SouthEast | No |
| SouthWest | SouthEast | Yes |
| NorthEast | NorthEast | No |
| NorthWest | NorthEast | Yes |

The minimum canonical art kit is therefore:

`18 parts × 2 authored directions = 36 sprites`

## Bone contract

1. Root
2. Pelvis
3. SpineLower
4. Chest
5. Neck
6. Head
7. ShoulderFar
8. UpperArmFar
9. ForearmFar
10. HandFar
11. ShoulderNear
12. UpperArmNear
13. ForearmNear
14. HandNear
15. ThighFar
16. ShinFar
17. FootFar
18. ThighNear
19. ShinNear
20. FootNear

`Root` is the NPC's world position and stays between the feet at floor
level. Only the child visual root is mirrored.

## Visible-part contract

Back-to-front baseline:

1. HairRear
2. UpperArmFar
3. ForearmFar
4. HandFar
5. ThighFar
6. ShinFar
7. FootFar
8. Pelvis
9. Torso
10. Neck
11. Head
12. HairFront
13. ThighNear
14. ShinNear
15. FootNear
16. UpperArmNear
17. ForearmNear
18. HandNear

Near/Far names remain stable in both authored directions. They describe
visual depth relative to the camera, not left/right anatomy.

## Art requirements

Each sprite must:

- have a transparent background;
- contain only its named body piece;
- match the canonical character's scale and proportions;
- preserve identical canvas alignment within one direction;
- include hidden overlap beneath adjacent pieces;
- avoid baked floor shadows;
- avoid accessories in the mannequin pass;
- use consistent lighting, outline, and pixel density.

The approved SouthEast and NorthEast assembled characters are the source
of truth. Generated individual pieces must be checked against those
masters before they enter the rig.

## Art-kit intake

The reusable skeleton and the replaceable character appearance are
separate assets. `NpcRigArtKit` owns the complete 36-sprite appearance,
while `NpcCutoutRig` owns bones, slots, facing, and animation.

Create the canonical intake with:

`Big Retail > Characters > Art Kit > Create Canonical Employee Art Kit`

Unity creates:

```text
Assets/Art/Characters/CanonicalEmployee/
├── CanonicalEmployeeArtKit.asset
├── SouthEast/
└── NorthEast/
```

Each direction folder accepts one Sprite asset named exactly after each
visible-part identifier:

```text
HairRear
UpperArmFar
ForearmFar
HandFar
ThighFar
ShinFar
FootFar
Pelvis
Torso
Neck
Head
HairFront
ThighNear
ShinNear
FootNear
UpperArmNear
ForearmNear
HandNear
```

The file extension is irrelevant; the imported Unity Sprite name must
match. A single sliced source sheet is also valid when its individual
Sprite names match this list.

Select `CanonicalEmployeeArtKit.asset` to see the compact 18-by-2 intake
inspector. Its buttons can:

1. populate matching sprites from the two canonical folders;
2. apply the kit to `CanonicalNpcRig.prefab`;
3. validate missing or duplicate sprite assignments.

Partial kits are safe. Missing pieces continue to show mannequin
placeholders, which allows SouthEast to be proved before NorthEast
artwork exists.

## Unity ownership

The Unity-native prototype owns:

- transform hierarchy and pivots;
- sprite attachments and draw order;
- direction selection and mirroring;
- Animator integration;
- future idle, walk, reach, and interaction clips;
- future movement-speed synchronization;
- whole-character isometric sorting.

Final art determines the bind-pose offsets. Placeholder positions in
`NpcRigDefinition` are only for testing the hierarchy.

## Asset placement

Character work uses normal project folders:

- `Assets/Art/Characters/`
- `Assets/Animations/Characters/`
- `Assets/Prefabs/Characters/`
- `Assets/Scripts/Gameplay/Characters/`

No `Resources` folder is required. Character prefabs and artwork should
use direct Unity references unless a later runtime-loading requirement
proves otherwise.

## First experiment boundary

The lab proves:

1. Generate the canonical prefab.
2. Inspect the 20 bones and 18 slots.
3. Replace placeholders with one SouthEast art set.
4. Create an in-place idle and walk cycle.
5. Flip-test SouthWest.
6. Add NorthEast art and flip-test NorthWest.
7. Add one stop-and-reach interaction.

Population spawning, pathfinding, jobs, outfit variety, and production
optimization remain outside this first experiment.

## Rounded employee proof

`Big Retail > Characters > Create Rounded Employee - Rowan` creates or
updates the rounded procedural employee at:

```text
Assets/Prefabs/Characters/Prototype/RoundedEmployeeRowan.prefab
```

The same operation generates a looping idle clip, a looping in-place
walk clip, and an Animator Controller under:

```text
Assets/Animations/Characters/Prototype/
```

The controller exposes one float parameter named `Speed`. Values above
`0.05` transition to `Walk`; lower values return to `Idle`. The clips do
not move the character root. A future movement presenter owns world
translation and supplies the speed value.

Headless visual validation is available through:

```text
BigRetail.Characters.Editor.NpcRigLabBatch.GenerateAndRenderRowan
```

It regenerates Rowan and writes four sampled walk poses to
`Logs/RowanWalkPreview.png` without changing a gameplay scene.
