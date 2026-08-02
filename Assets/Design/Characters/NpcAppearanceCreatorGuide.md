# Big Retail Person Creator

Big Retail characters share one skeleton and one animation library. A visible
person is assembled from four independent appearance choices:

1. **Body silhouette** — the proportions and spacing of the 18 rounded pieces.
2. **Skin palette** — the base skin color, with automatic far-side shading.
3. **Outfit set** — coordinated shirt, trousers, footwear, accent colors, and
   optional direction-specific clothing sprites.
4. **Hair set** — coordinated front/rear hair shapes, color, and optional
   direction-specific hair sprites.

A `NpcAppearanceProfile` is the small final recipe that selects one asset from
each category. It does not duplicate the rig, animation clips, textures, or
materials.

## First use

1. Open **Big Retail > Characters > Appearance Creator > Open Creator**.
2. Click **Create / Refresh Starter Appearance Library** once.
3. Choose a saved profile, or mix a body, skin, outfit, and hair choice.
4. Save the combination as a new appearance profile.
5. Open a character prefab or select a scene character, assign it as the live
   rig preview, then apply the saved profile.
6. Use the SE, SW, NE, and NW buttons to inspect all four facings.

The starter library captures Rowan as the baseline and creates:

- two body silhouettes;
- six skin palettes;
- three outfits;
- four hairstyles;
- Rowan and Mina appearance profiles;
- a Mina character prefab for side-by-side comparison.

## Making a new outfit

Choose the closest existing outfit in the Person Creator and click
**Duplicate Outfit**. Rename the copy and edit it in the Inspector.

- **Primary Fabric** normally colors the torso and shirt sleeves.
- **Secondary Fabric** normally colors the pelvis, thighs, and shins.
- **Footwear** colors both feet.
- **Accent** colors the name badge.
- **Show Badge** controls whether the front-facing badge appears.
- **Part Styles** map every non-hair body slot to skin, fabric, footwear, or
  accent. Each entry can optionally supply SouthEast and NorthEast sprites.

The default rounded characters need no new PNGs; their shared rounded sprite is
tinted by the outfit colors. If painted clothing is added later, assign the PNG
as a Unity **Sprite** in the relevant Part Style. The same recipe system remains
valid.

## Making new hair

Choose the closest hairstyle and click **Duplicate Hair**. Edit the copy's hair
color and the front/rear shape sizes and positions. The two pieces deliberately
work together so north-facing characters can show the rear mass correctly.
Optional SouthEast and NorthEast sprites can replace either procedural piece.

## Making a new body or skin palette

Duplicate a working body before changing proportions. Body assets retain all 18
required part entries and the small bone-spacing adjustments that keep the rig
connected. Body silhouettes do not change character height or animation clips.

Skin palettes are simple shared colors. Duplicate one and change its base color;
the renderer applies a restrained depth shade to the farther side automatically.

## Texture, sprite, material, and appearance

- A **texture** is the imported image data, commonly a PNG.
- A **sprite** is the 2D renderable region, pivot, and metadata Unity creates
  from that texture.
- A **material** controls how a renderer draws a sprite through a shader.
- An **appearance** is Big Retail's coordinated body/skin/outfit/hair recipe.

The rounded starter people primarily reuse one sprite and one shared material,
then vary `SpriteRenderer` colors and transforms. That keeps population variety
cheap while leaving a clean path to painted sprites later.
