# Big Retail Population Appearance System

Big Retail uses one shared `Person` prefab, skeleton, movement presenter, and
animation library. Population data controls how that shared person looks.
Customer behavior, employee jobs, spawning, pathfinding, and visual testing
remain separate systems.

## Active project structure

- `Assets/Prefabs/Characters/Core/Person.prefab` is the shared person body and
  rig. It is not a named character, an employee, or a customer.
- `Assets/Animations/Characters/Core/` contains the shared idle, walk, and
  Animator Controller.
- `Assets/Art/Characters/Appearance/Catalog/` contains the central appearance
  and population-definition library.
- `Assets/Art/Characters/Appearance/Population Definitions/` decides which
  choices each population is allowed to receive.
- `Assets/Art/Characters/Appearance/Defaults/` holds optional exact fallback
  appearances, not the normal population.
- `Assets/Art/Characters/Experiments/` contains quarantined visual experiments
  that are not part of the active character pipeline.

## Population definitions

A population definition is a reusable set of appearance rules. It has:

1. A display name, such as Customer or Store Employee.
2. A broad behavior family: Customer or Employee.
3. Man and Woman generation weights. A zero weight disables that category.
4. A Men appearance pool with allowed body, skin, outfit, and hair assets.
5. A Women appearance pool with its own allowed body, skin, outfit, and hair
   assets.

Men and Women are not separate gameplay populations. A Customer remains a
Customer and an Employee remains an Employee; the two pools only control how
each gender may look. This keeps behavior definitions simple while allowing,
for example, different employee uniforms or customer hair choices for men and
women.

Multiple definitions may share the same behavior family. For example, Store
Employee and Manager can both use Employee behavior while receiving different
outfit pools.

## Population Definitions tool

Open **Big Retail > Population > Definitions**.

The tool only authors population data:

1. Select, add, or duplicate a population type.
2. Set its display name and Customer or Employee behavior family.
3. Set how frequently the population generates men and women.
4. Select the **Men** or **Women** appearance tab.
5. Add or remove Body, Skin, Outfit, and Hair assets from that gender's pool.
6. Resolve any missing, incompatible, or duplicate asset warnings.

The tool does not create individual people, expose random seeds, save named
characters, preview the rig, or test animations. Those responsibilities belong
to gameplay generation and a separate character test area.

## Population Previewer

Open **Big Retail > Population > Previewer**.

The previewer is a read-only showroom with two sources:

1. **Population Definition** shows only the choices currently authorized to
   spawn for Customer, Employee, or another population.
2. **Appearance Library** shows every saved Body, Skin, Outfit, and Hair asset
   from the authoring folders, including assets not yet assigned to a
   population.
3. Choose Man or Woman and browse only compatible options.
4. Generate weighted population examples or random library combinations.
5. Check the assembled person in all four facings.

The displayed person lives in Unity's hidden preview scene. The previewer does
not place a GameObject in the active scene, save a named person, or change the
shared `Person.prefab`. Animation testing remains a separate future tool.

## Appearance Creator

Open **Big Retail > Population > Appearance Creator**.

The creator authors the reusable ingredients used by population definitions:

1. Choose Body, Skin, Outfit, or Hair.
2. Select an existing asset as a safe starting point.
3. Edit it while watching a temporary, category-focused preview.
4. Save the working copy as a new reusable asset, or deliberately update the
   selected asset.
5. Add the finished asset to one or more Population Definitions when it is
   ready to enter simulation generation.

Body assets explicitly define Man or Woman. Outfit and Hair assets declare
whether they support men, women, or everyone. Skin palettes work across both.
The creator never modifies scenes, the shared Person prefab, animation clips,
or movement code.

Hair editing keeps two dependable core shapes for the main front/rear mass.
The **Optional Silhouette Layers** section can then add named sweeps, fringes,
tufts, tapers, side locks, or buns. Every layer has a depth, shade, and separate
South East/North East transform; the preview updates immediately while these
values are adjusted.

**Save Changes to Selected** updates the existing asset while preserving its
identity, so every Population Definition referencing it receives the update.
**Save as New Asset** creates an independent library entry and leaves the
selected asset untouched. New assets appear in the Previewer's Appearance
Library but cannot spawn until added to a Population Definition.

## Starter style packs

Open **Big Retail > Population > Setup > Add Masculine Style Pack** to install
the first coordinated expansion without resetting existing population work.
The command is safe to run more than once and adds each choice only once.

The pack adds two same-height masculine silhouettes (Lean and Broad), three
masculine hairstyles, two customer outfits, and one employee uniform. It adds
them only to the Men pools for Customer and Store Employee; the Women pools
are left unchanged. **Repair Starter Content** also restores this pack, so a
later repair cannot silently remove these supported choices.

## How the simulation creates a person

1. Gameplay chooses a population definition.
2. Runtime generation selects Man or Woman from that population's weights.
3. The definition supplies body, skin, outfit, and hair assets exclusively
   from the matching Men or Women appearance pool.
4. Runtime generation selects one valid combination.
5. The appearance is applied to the shared `Person` rig.
6. The rig and animations remain unchanged; only appearance data changes.

Employees can later preserve the exact generated result in save data, while
temporary customers can receive new appearances as they enter the store.

## Appearance asset types

- A **body type** defines Man or Woman and changes safe proportions and
  spacing of the shared body pieces. It does not create a separate skeleton.
- A **skin palette** supplies skin color and automatic depth shading.
- An **outfit set** coordinates shirt, trousers, footwear, badges, and optional
  painted sprites.
- A **hair set** coordinates the core front/rear hair mass, color, and optional
  silhouette layers such as crown tufts, side sweeps, fringes, tapers, or a
  bun. Each optional layer can have separate South East and North East poses;
  west-facing views still come from mirroring. These pieces follow the Head
  bone and therefore require no new skeleton bones or animation clips. A hair
  set may support men, women, or everyone.

Outfits and hair are sets because they affect several body pieces together.
This prevents population generation from combining incompatible fragments.
