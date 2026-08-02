# Big Retail Population Appearance System

Big Retail uses one shared `Person` prefab, skeleton, movement presenter, and
animation library. Population data controls how that shared person looks.
Customer behavior, employee jobs, spawning, and pathfinding remain separate
gameplay systems.

## Active project structure

- `Assets/Prefabs/Characters/Core/Person.prefab` is the shared person body and
  rig. It is not a named character, an employee, or a customer.
- `Assets/Animations/Characters/Core/` contains the shared idle, walk, and
  Animator Controller.
- `Assets/Art/Characters/Appearance/Catalog/` lists reusable appearance
  choices.
- `Assets/Art/Characters/Appearance/Population Definitions/` decides which
  choices customers and employees are allowed to receive.
- `Assets/Art/Characters/Appearance/Defaults/` holds optional exact fallback
  appearances, not the normal population.
- `Assets/Art/Characters/Experiments/` contains quarantined visual experiments
  that are not part of the active character pipeline.

## How the simulation creates a person

1. Gameplay chooses a population definition, such as Customer or Store
   Employee.
2. The definition supplies allowed body, skin, outfit, and hair choices plus
   their relative weights.
3. A seed selects one valid combination deterministically.
4. The resulting appearance is applied to the shared `Person` rig.
5. The rig and animations remain unchanged; only appearance data changes.

A seed is simply a repeatable random identity. It lets a hire candidate remain
the same person after saving and loading, while throwaway shoppers can receive
new seeds whenever they are generated.

## The four appearance choices

1. **Body silhouette** changes proportions and spacing of the shared body
   pieces.
2. **Skin palette** supplies the skin color and automatic depth shading.
3. **Outfit set** coordinates shirt, trousers, footwear, badge rules, and
   optional painted sprites.
4. **Hair set** coordinates front and rear hair shapes, color, and optional
   painted sprites.

Outfits and hair are sets because they affect several body pieces together.
This prevents random generation from combining incompatible fragments.

## Population Studio

Open **Big Retail > Characters > Population Studio > Open Population Studio**.
The studio is primarily a testing and authoring window:

1. Choose the Appearance Catalog.
2. Choose Customer or Store Employee.
3. Change the seed to generate repeatable samples.
4. Preview SouthEast, SouthWest, NorthEast, and NorthWest on the shared rig.
5. Adjust the population definition or its allowed assets when the generated
   crowd does not match the intended role.

Saving an exact appearance is optional. It is useful for a default, a story
character, a retained hire candidate, or a repeatable test; normal populations
should be generated from their definitions.

## Adding appearance content

Duplicate a known-good Body, Skin, Outfit, or Hair asset from Population
Studio, edit the copy in the Inspector, then add it to the Appearance Catalog
and to every Population Definition that may use it.

- **Primary Fabric** normally colors the torso and shirt sleeves.
- **Secondary Fabric** normally colors the pelvis, thighs, and shins.
- **Footwear** colors both feet.
- **Accent** colors the name badge.
- **Show Badge** controls the front-facing badge.
- **Part Styles** map every body slot to skin, fabric, footwear, or accent.

The current art style uses reusable procedural body-part sprites. Painted
clothing or hair can later provide SouthEast and NorthEast sprites without
changing the population recipe system.

## Texture, sprite, material, and appearance

- A **texture** is imported image data, commonly a PNG.
- A **sprite** is the renderable 2D region and pivot Unity creates from a
  texture.
- A **material** controls how a renderer draws a sprite through a shader.
- An **appearance** is Big Retail's coordinated body, skin, outfit, and hair
  recipe.
