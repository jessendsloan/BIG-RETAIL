# Game Design: BIG RETAIL

This document outlines the core design specifications for **BIG RETAIL**, an isometric 2:1 pixel-art retail tycoon game. It covers the UI design, asset direction, and the tactile game feedback loop that defines the player experience as they build and manage their shopping empire.

---

## UI Design

The UI for BIG RETAIL is designed to feel tactile, structured, and warm—reminiscent of browsing a high-quality physical shopping catalog or a sleek department store directory. It avoids flat utility layouts in favor of subtle depth, warm tones, and high-contrast interactive states.

### 1. Color System
The color palette uses tinted neutrals and clear color-coded intent, ensuring no pure greys are used.

*   **Primary (UI Base):** Dark Slate Blue (`#1E222A`) — Used for HUD backdrops and secondary window framing. Tinted with deep blue-grey to maintain game palette cohesion. `(core)`
*   **Secondary (Main Panels):** Warm Cream / Sand (`#F4F1EA`) — The dominant surface color for menus, detail cards, and catalog sheets. It evokes a tactile paper/leather product catalog. `(core)`
*   **Accent (Positive Actions):** Radiant Teal (`#00BFA5`) — Exclusively reserved for high-value player interactions (e.g., placing tiles, hiring staff, completing high-profit checkout events). `(core)`
*   **Alert / Warning (High-Stakes):** Coral Red (`#FF5252`) — Used for low-funds warnings, structural blocks, shoplifting notifications, and customer service failures. `(core)`
*   **Tonal Surface Hierarchy:**
    *   *Base Layer:* Deep Slate Blue (`#1E222A`)
    *   *Container Panels:* Warm Sand (`#F4F1EA`)
    *   *Raised Content/Sub-cards:* Soft Chalk White (`#FCFAF7`)
    *   *Inputs/Inset Fields:* Deep Biscuit (`#E5DFD3`) — indented into the surface.

### 2. Typography
Typography relies on crisp proportions to separate statistics, headers, and descriptions cleanly.

*   **Display / Header Font:** Bold, geometric blocky sans-serif. Used for item names, store titles, and large monetary readouts. `(core)`
*   **Body Font:** Clean, modern medium-weight sans-serif. Used for tooltips, customer reviews, and staff contracts. `(core)`
*   **Label Font:** High-legibility monospaced or crisp pixel font. Used for small prices (`$19.99`), grid coordinates, shelf capacity indicators (`48/50`), and minor HUD statistics. `(core)`

### 3. Layout & Depth Strategy
*   **Depth Layering:** Default to overlapping catalog pages. Menus float above the lively isometric world using soft, multi-layered ambient shadows to separate interface from gameplay. `(core)`
*   **Nesting & Contrast:** Structure panel sections using tonal color shifts (e.g., a Soft Chalk White card nesting inside a Warm Sand panel) rather than thin black borders. `(core)`
*   **Spacing Rhythm:** Strictly adhere to a 4px/8px/16px/24px spacing grid. `(core)`

### 4. Components
*   **Buttons:** Thick, physical appearances. Buttons feature a 3D bevel (lighter top edge, darker bottom edge) and sink by `2px` downwards on click, accompanied by a sudden, dark-edge shadow shift to simulate a real spring-loaded button. `(core)`
*   **Cards:** Generous corner radii (`12px`) for a friendly consumer look. No interior line dividers—only clean spacing and subtle background shifts. `(core)`
*   **Inputs:** Inset fields (`Deep Biscuit`) with a slight inner shadow, making the field look "carved" into the menu. `(core)`
*   **Overlays:** Semi-transparent warm charcoal (`70%` opacity) to keep the bustling shop visible in the background when major management screens are open. `(core)`

---

## Asset Design

BIG RETAIL utilizes a vibrant, clean 2:1 isometric pixel art style. The asset pipeline is designed for high readability in dense store environments, ensuring player progression stands out immediately against distinct floor textures.

### 1. Visual Identity & Art Style
*   **Style:** 2:1 Isometric Projection (width is exactly twice the height). Sharp, hand-crafted pixel art with clean colors and deliberate shading. `(core)`
*   **Outline Treatment:** Props, characters, and shelves feature a precise, 1-pixel dark outline (`#15181F`) to pop from the floor. Floor tiles must **never** have outlines to ensure they tile seamlessly. `(core)`
*   **Color Temperature:** Sunny and warm, mimicking welcoming department store lighting. Soft, translucent warm-amber drop shadows under characters and props. `(core)`
*   **Detail Level:** Medium-low. High silhouette clarity is prioritized over internal micro-details to prevent visual noise. `(core)`

### 2. Retail Floor Specifications
Different floor tiles serve as the primary tool for players to define "Zoning" within their superstore. Each tile type conveys a distinct retail feel, acoustic identity, and target customer behavior.

```
       /\
      /  \
     /    \  128px Width
    \      /
     \    /
      \  /  64px Height
       \/
```

#### A. Polished Concrete (Bulk / Warehouse Zone) `(core)`
*   **Visual Design:** Large, light-grey slabs with fine joint lines. Shows subtle, dusty tire tracks and scuff marks, evoking a wholesale warehouse store (e.g., Costco). Matte, non-reflective finish.
*   **Color Palette:** Dominant: Slate Grey (`#8D9096`), Secondary: Cool Grey (`#B0B3B8`), Accent: Safety Yellow (`#FFD54F`) used for loading-bay border markings.
*   **Grid Style:** Large 2x2 meter tiles (combining to fill a 128x64px footprint).
*   **Store Zone Function:** Wholesale aisles, shipping & receiving bays, discount electronics.

#### B. Linoleum Tiles (Standard Grocery Zone) `(core)`
*   **Visual Design:** Bright off-white/cream base with a subtle speckled terrazzo texture. Clean, sterile grout lines dividing the tile into a 4x4 mini-grid.
*   **Color Palette:** Dominant: Cream White (`#F5F4F0`), Secondary: Pale Blue (`#E3EFF5`), Accent: Mint Green (`#A2D5C6`) for aisle number borders.
*   **Grid Style:** Standard 1x1 meter square tiles arranged diagonally.
*   **Store Zone Function:** Fresh produce, dairy, household goods, and standard cashier aisles.

#### C. Plush Carpet (Boutique Fashion Zone) `(core)`
*   **Visual Design:** Dense, textured deep-navy carpet. Features a soft, noise-dampened fiber look without visible seams, creating a continuous, cozy lounge appearance.
*   **Color Palette:** Dominant: Deep Navy (`#1A2B4C`), Secondary: Royal Indigo (`#2E446E`), Accent: Soft Gold (`#D4AF37`) for elegant brass dividing trim.
*   **Grid Style:** Completely seamless; tiles merge beautifully to hide grid lines.
*   **Store Zone Function:** Designer apparel, jewelry, cosmetic counters, and fitting rooms.

#### D. Hardwood Planks (High-End / Organic Zone) `(core)`
*   **Visual Design:** Rich golden oak planks aligned along the long isometric diagonal. Features clean satin grain patterns and thin, dark-brown gaps between boards.
*   **Color Palette:** Dominant: Honey Oak (`#9E7B56`), Secondary: Warm Amber (`#7D5F3E`), Accent: Fresh Leaf Green (`#76C075`) for accent zone dividers.
*   **Grid Style:** Horizontal plank layout emphasizing the long diagonal of the isometric tile.
*   **Store Zone Function:** Premium organic foods, artisanal bakeries, craft coffee shops.

### 3. Scale & Proportion
*   **Standard Tile Size:** `128 x 64` pixels. `(core)`
*   **Character Scale:** Humans (customers, workers) stand approximately `2.5` tiles tall (about `160px` tall in orthographic view). `(core)`
*   **Prop Scale:** Shelves, refrigerators, and checkout stands fit within integer grid footprints (e.g., `1x1`, `1x2`, or `2x2` tiles) and stand up to `2` tiles high. `(core)`

---

## Game Feedback (Polishment)

The core feedback philosophy of BIG RETAIL is **Tactile Resonance**. Every placement, customer footstep, and monetary transaction must feel physical, satisfying, and acoustic. High contrast between the floor zones is achieved through specialized step sounds and cart audio.

### 1. Genre Profile
**Tactical Tycoon with High-Energy micro-moments.** The game utilizes responsive, physical micro-feedbacks (squash & stretch on placements) and highly stylized, comforting audio (the crisp "cha-ching" of modern registers, the varying resonance of footsteps across concrete and carpet).

### 2. Interaction Map

| Interaction | Tier | Importance | Camera | Time | Transform | Visual | Audio | Input | Rationale |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Place Floor Tile** | core | Light | — | — | Subtle squash/stretch on tile arrival (`10%` overshoot, `0.1s`). | Tiny dust puff particles (`4-6` pixels) at tile corners. | Satisfying, wet acoustic "slap" or wooden "thud" depending on tile type. | Snap-to-grid cursor response. | Gives tile laying a heavy, physical building feel. |
| **Customer Walk: Concrete** | core | Minor | — | — | Subtle character bounce. | Tiny scuff-dust particles on step. | Echoey, hard-soled shoe click-clack and heavy, hollow shopping cart rumble. | — | Emphasizes the wide, industrial, and loud nature of bulk zones. |
| **Customer Walk: Linoleum** | core | Minor | — | — | Subtle character bounce. | — | Squeaky, rubber-soled sneaker squeaks and crisp, lightweight cart wheels. | — | Creates a bright, busy supermarket atmosphere. |
| **Customer Walk: Carpet** | core | Minor | — | — | Smooth, glided bobbing. | — | Deep, dampened, and barely audible soft thuds; completely silent cart wheels. | — | Delivers a hushed, high-end, premium acoustic atmosphere. |
| **Customer Walk: Hardwood** | core | Minor | — | — | Standard character bounce. | — | Satisfying, warm wooden hollow "clop-clop" footstep sounds. | — | Conveys high-quality, craft, and organic material warmth. |
| **Checkout Purchase** | core | Medium | — | — | Cash register bounces vertically (`1.2x` height scale) and settles. | Floating numeric text (`+$24.99`) rising in Electric Teal. | A delightful dual-tone "Ching-Ring!" sound effect with random pitch (`±10%`). | — | Maximizes the core loop's psychological reward of earning cash. |
| **Zone Unlock / Level Up** | core | Heavy | Subtle 2D screen shake (decaying exponentially over `0.4s`). | Tiny freeze-frame (`0.05s`) on achievement trigger. | Massive scale-up pop on the unlocked region. | Shower of colored paper confetti particles falling from screen top. | Uplifting retail-bell chime fanfare. | Soft screen rumble haptics. | Puts a grand punctuation mark on major progression milestones. |

### 3. Feedback Sequences

#### Tile Placement Sequence:
`Tile Selected` (cursor highlights grid cell) ➔ `Mouse Click` (instant snap) ➔ `Tile Squash` (horizontal scale `1.15`, vertical `0.85` at `0ms`) ➔ `Dust Puffs` (bursts at grid vertices at `10ms`) ➔ `Tile Stretch` (overshoot to vertical `1.10` at `50ms`) ➔ `Acoustic Drop Sound` (synchronized with peak stretch at `60ms`) ➔ `Settle` (returns to normal scale `1.0` at `120ms`).

#### Checkout Transaction Sequence:
`Customer arrives at register` ➔ `Cashier scans item` (beeps, small red flash on register) ➔ `Customer hands over payment` ➔ `Register pop-up` (scales up to `1.2` instantly) ➔ `Floating Text spawn` (rising and fading over `0.6s`) ➔ `Cash register chime` (with `±12%` randomized pitch).

### 4. Assets Needed

| Asset Name | Tier | Type | Style Reference | Palette | Usage Context | Approximate In-Scene Size |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Polished Concrete Tile** | core | Sprite (Tile) | Smooth gray industrial warehouse floor | Slate Grey / Safety Yellow | Base ground for wholesale zones | `128 x 64 px` (isometric tile) |
| **Linoleum Grid Tile** | core | Sprite (Tile) | Speckled supermarket floor grid | Cream White / Mint Green | Base ground for general grocery zones | `128 x 64 px` (isometric tile) |
| **Plush Carpet Tile** | core | Sprite (Tile) | Seamless luxury dark-blue hotel carpet | Deep Navy / Soft Gold | Base ground for high-end boutique zones | `128 x 64 px` (isometric tile) |
| **Hardwood Plank Tile** | core | Sprite (Tile) | Golden wood grain diagonal planks | Honey Oak / Amber Wood | Base ground for organic and cafe zones | `128 x 64 px` (isometric tile) |
| **Tile Placement Dust** | core | Particle Sprite | Small, circular 2x2 px smoke puffs | Creamy Tan (`#EADCC9`) | Instantiates at tile vertices on placement | `4 x 4 px` per particle |
| **floating Cash Text** | core | UI Font / Mesh | Vibrant, readable numeric font | Electric Teal (`#00BFA5`) | Spawns above register on checkout | `36pt` display size |
| **Modern Cash Register** | core | Sprite (Prop) | Compact checkout counter with screen | Slate Blue / Teal Screen | Point of sale placement | `1 x 2` tiles footprint |
