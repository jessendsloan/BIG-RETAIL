# Big Retail Character Studio

Big Retail characters share one skeleton, one movement presenter, and one
animation library. Character Studio controls only how a person looks.
Pathfinding, customer behavior, employee jobs, and spawning remain separate
systems.

## The character pipeline

The system has four clear layers:

1. **Character Library** - the central catalog of reusable character assets
   and population templates.
2. **Character Template** - the rules for one population role. The Customer
   template permits customer outfits; the Store Employee template permits
   approved uniforms.
3. **Appearance Selection** - one exact body, skin, outfit, and hair
   combination generated from a template and seed.
4. **Appearance Profile** - a saved selection with a person's name. Gameplay
   can assign this exact recipe to the shared rig later.

A random seed is repeatable. The same template, seed, locks, and library data
produce the same appearance. Body, skin, outfit, and hair use independent
random streams, so locking one category does not reshuffle the others.

## Daily workflow

1. Open **Big Retail > Characters > Character Studio > Open Character
   Studio**.
2. Choose **Customer** or **Store Employee**.
3. Enter a seed or click **Next**, then click **Randomize Unlocked Choices**.
4. Lock any feature you want to preserve while trying more seeds.
5. Choose exact allowed options from the Body, Skin, Outfit, and Hair lists.
6. Select a rig and click **Preview Current Recipe**. Use SE, SW, NE, and NW
   to inspect it.
7. Name the person and click **Save as New Person**.
8. Click **Assign Saved Person to Rig** only when the rig should permanently
   reference that profile.

Preview is intentionally temporary. Assigning a saved person is an explicit,
persistent operation.

## Starter content

Expand **Starter Content & Asset Authoring** and click **Repair / Refresh
Starter Content** after first import or when the baseline content needs repair.
This uses the existing rounded person rig as a source, without treating Rowan
as the architecture. Rowan and Mina remain ordinary saved example people.

The starter catalog contains:

- two body silhouettes;
- six skin palettes;
- four outfits;
- four hairstyles;
- Customer and Store Employee templates;
- Rowan and Mina appearance profiles;
- a Mina comparison prefab.

## The four appearance choices

1. **Body silhouette** - proportions and spacing of the 18 rounded pieces.
2. **Skin palette** - a base skin color with automatic depth shading.
3. **Outfit set** - a coordinated shirt, trousers, footwear, badge rule, and
   optional direction-specific clothing sprites.
4. **Hair set** - coordinated front and rear hair shapes, color, and optional
   direction-specific sprites.

Keeping outfits and hair as coherent sets is deliberate. A uniform is more
than one shirt color, and a hairstyle is more than one isolated hair piece.

## Creating new assets

Use the duplication buttons under **Starter Content & Asset Authoring** to copy
a known-good body, skin, outfit, or hair asset, then edit the copy in its
Inspector. Add the finished choice to the central Character Library and to each
Character Template that should be allowed to generate it.

For outfits:

- **Primary Fabric** normally colors the torso and shirt sleeves.
- **Secondary Fabric** normally colors the pelvis, thighs, and shins.
- **Footwear** colors both feet.
- **Accent** colors the name badge.
- **Show Badge** controls the front-facing badge.
- **Part Styles** map each body slot to skin, fabric, footwear, or accent.

The rounded characters need no new PNGs. They reuse a rounded sprite and vary
its color and transform. Painted clothing or hair can be introduced later by
assigning SouthEast and NorthEast sprites to the relevant part style without
replacing the recipe system.

## Texture, sprite, material, and appearance

- A **texture** is imported image data, commonly a PNG.
- A **sprite** is the 2D renderable region, pivot, and metadata Unity creates
  from that texture.
- A **material** controls how a renderer draws a sprite through a shader.
- An **appearance** is Big Retail's coordinated body, skin, outfit, and hair
  recipe.

This separation keeps population variety inexpensive while allowing the art
direction to grow beyond rounded procedural pieces later.
