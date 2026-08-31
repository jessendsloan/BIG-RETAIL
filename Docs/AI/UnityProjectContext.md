# Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: `C:\Users\jesse\Unity Projects\BIG RETAIL`
- Last analyzed: 2026-08-01
- Last analyzed commit: `24f7f3f463c95d06b95283f09e6da336d2c32939`
- Current branch during analysis: `agent/department-planning-core`
- The project is a 2D isometric retail-building game with model-owned map, foundation, floor, wall, department, merchandise, and inventory domains plus Unity presentation and construction-tool layers.

## Confirmed Environment

- Unity version: 6000.5.1f1 (`0d9463e84828`)
- Render pipeline: Universal Render Pipeline 17.5.0
- Input system: Unity Input System 1.19.0 with a project-owned `PlayerInput` asset
- Target platforms: Unknown; no platform-specific first-party assemblies were identified during this pass.

## Important Packages And Frameworks

| Area | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Rendering | Universal Render Pipeline 17.5.0 | Confirmed | `Packages/manifest.json`, `ProjectSettings/GraphicsSettings.asset` |
| Input | Unity Input System 1.19.0; `PlayerInput` owns camera and construction actions | Confirmed | `Packages/manifest.json`, `Assets/Scripts/Input/CameraInput.cs`, `Assets/Scripts/Input/BigRetailInput.inputactions` |
| UI | UI Toolkit runtime construction toolbar plus uGUI/EventSystem support | Confirmed | `Assets/UI/Construction/PC/ConstructionToolbar.uxml`, `Assets/Scripts/Gameplay/Construction/Unity/UI/PC/ConstructionToolbarDocumentHost.cs`, `Assets/Scenes/Gameplay.unity` |
| Tests | Unity Test Framework 1.7.0 with first-party EditMode assemblies | Confirmed | `Packages/manifest.json`, `Assets/Tests/EditMode/` |
| Networking | No first-party runtime networking use found | Likely | `Packages/manifest.json`, first-party code search |

## Directory Structure

| Path | Purpose | Confidence | Evidence |
| --- | --- | --- | --- |
| `Assets/Scripts/Gameplay/Map/Domain` | Grid positions, edges, and map definitions | Confirmed | First-party code and assembly definition |
| `Assets/Scripts/Gameplay/Map/Foundations` | Foundation state and construction rules | Confirmed | First-party code and assembly definition |
| `Assets/Scripts/Gameplay/Map/Floors` | Floor state, finishes, and construction rules | Confirmed | First-party code and assembly definition |
| `Assets/Scripts/Gameplay/Map/Walls` | Wall state, finishes, construction, and reversible edits | Confirmed | First-party code and assembly definition |
| `Assets/Scripts/Gameplay/Map/View` | Engine-free isometric projection and wall-presentation rules | Confirmed | First-party code and assembly definition |
| `Assets/Scripts/Gameplay/Map/Unity` | Runtime hosts and Unity presentation systems | Confirmed | First-party code and assembly definition |
| `Assets/Scripts/Gameplay/Construction/Unity` | Player construction tools, input bridges, previews, and PC toolbar presenters | Confirmed | First-party code |
| `Assets/Tests/EditMode` | Domain and Unity integration tests | Confirmed | Test assemblies and fixtures |

## Assembly Boundaries

| Assembly | Responsibility | Key references | Notes |
| --- | --- | --- | --- |
| `BigRetail.Map.Domain` | Engine-free map primitives and definitions | None/engine-free | Base map layer |
| `BigRetail.Map.Foundations` | Engine-free foundation state and rules | Map Domain, Map Construction | Runtime domain |
| `BigRetail.Map.Floors` | Engine-free floor state, finishes, and rules | Map Domain and related map domains | Runtime domain |
| `BigRetail.Map.Walls` | Engine-free wall state, finishes, and rules | Map Domain and related map domains | Runtime domain |
| `BigRetail.Map.View` | Engine-free projection and presentation selection | Map Domain | `noEngineReferences: true` |
| `BigRetail.Map.Unity` | Unity hosts, tilemap views, and wall sprite views | Domain, Construction, Walls, Floors, View, Foundations | Presentation/integration layer |
| `Assembly-CSharp` | Construction controllers and UI Toolkit presenters | Auto-referenced project assemblies | No construction-specific asmdef currently bounds this code |

## Scenes And Startup Flow

- Build scenes: only `Assets/Scenes/SampleScene.unity` is enabled in serialized Build Settings.
- Likely active development scene: `Assets/Scenes/Gameplay.unity`; it contains the runtime map, input, construction, wall, floor, and toolbar composition.
- Additional scene: `Assets/Scenes/StartScreen.unity`.
- Scene loading flow: Unknown. The serialized Build Settings do not currently describe a production start-to-gameplay flow.

## Architecture

| Pattern | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Domain/presentation separation | Mutable map state lives in plain C#; MonoBehaviours synchronize Unity views | Confirmed | `WallState.cs`, `WallViewSystem.cs`, `FloorState.cs`, `FloorTilemapViewSystem.cs` |
| Runtime composition hosts | Focused hosts create and expose subsystem state and services | Confirmed | `GridMapHost.cs`, `FoundationRuntimeHost.cs`, `FloorRuntimeHost.cs` |
| Event-driven views | Views subscribe to model changes and isometric orientation changes | Confirmed | `WallViewSystem.cs`, `FloorTilemapViewSystem.cs`, `IsometricViewHost.cs` |
| UI presenter/view split | UI Toolkit wrappers expose intent; MonoBehaviour presenters bind services | Confirmed | `ConstructionToolbarView.cs`, `ConstructionToolbarPresenter.cs`, `ConstructionToolbarDocumentHost.cs` |
| Logical map rotation | Map data stays canonical while a shared projection rotates presentation and targeting | Confirmed | `IsometricViewHost.cs`, `IsometricViewProjection.cs` |

## Coding Conventions

- Namespace style: feature-based `BigRetail.<Area>...` namespaces.
- Serialized fields: private `[SerializeField]`, often with headers/tooltips; mutable public state is exposed through read-only properties.
- Formatting: Allman braces, explicit types, wrapped argument lists, and lifecycle subscriptions paired in enable/disable methods.
- Async: no dominant first-party async convention identified in the sampled systems.
- Comments/docs: XML summaries describe ownership boundaries; comments explain constraints and non-obvious rollback/lifecycle behavior.

## Testing And Validation

- EditMode tests: present across map domains and Unity presentation integrations.
- PlayMode tests: none found during this pass.
- CI/build validation: no repository CI instructions found. `Docs/DevelopmentWorkflow.md` expects Unity compilation, Editor playtesting, and tests before committing.

## Available Unity Tooling

| Capability | Status | Evidence |
| --- | --- | --- |
| Unity Editor connection/Console/scene/test APIs | unavailable | No Unity MCP tools are exposed in the current Codex session. |
| Repository inspection and editing | available | Local workspace access |
| Command-line Unity executable | unverified | Not established during onboarding |

## Important Constraints

- Preserve the model/presentation boundary; wall visibility must not alter `WallState` or save semantics.
- Prefer event-driven refreshes over per-frame wall scans.
- Avoid broad scene serialization changes; the active `Gameplay.unity` scene already has uncommitted user work.
- Preserve the active uncommitted department-planning and construction-toolbar changes.
- Human Play Mode and visual acceptance remain required by `Docs/DevelopmentWorkflow.md`.

## Unknowns And Confidence

- The intended production build-scene order and target platforms are unknown.
- No connected Unity Editor API is available, so Console state and Play Mode behavior cannot be inspected directly from this session.
- The wall art currently exposes full-height directional sprites only; a low-wall visual state would require additional authored sprites or a separate rendering treatment.

## Source Files Inspected

- `ProjectSettings/ProjectVersion.txt`
- `ProjectSettings/GraphicsSettings.asset`
- `ProjectSettings/EditorBuildSettings.asset`
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `Docs/DevelopmentWorkflow.md`
- `Assets/Scripts/Gameplay/Map/Unity/GridMapHost.cs`
- `Assets/Scripts/Gameplay/Map/Unity/View/IsometricViewHost.cs`
- `Assets/Scripts/Gameplay/Map/Unity/Walls/WallViewSystem.cs`
- `Assets/Scripts/Gameplay/Map/Unity/Walls/WallSegmentView.cs`
- `Assets/Scripts/Gameplay/Map/Unity/Floors/FloorRuntimeHost.cs`
- `Assets/Scripts/Gameplay/Map/Unity/Floors/FloorTilemapViewSystem.cs`
- `Assets/Scripts/Gameplay/Map/Unity/Foundations/FoundationRuntimeHost.cs`
- `Assets/Scripts/Gameplay/Map/View/WallPresentationSelection.cs`
- `Assets/Scripts/Gameplay/Construction/Unity/UI/PC/ConstructionToolbarView.cs`
- `Assets/Scripts/Gameplay/Construction/Unity/UI/PC/ConstructionToolbarPresenter.cs`
- `Assets/Scripts/Gameplay/Construction/Unity/UI/PC/ConstructionToolbarDocumentHost.cs`
- `Assets/UI/Construction/PC/ConstructionToolbar.uxml`
- `Assets/Scenes/Gameplay.unity`
- Representative EditMode tests and assembly definitions under `Assets/Tests/EditMode` and `Assets/Scripts/Gameplay`

<!-- unity-onboarding:generated:end -->

## Required Isometric Layering Contract

All isometric world presentation must use the shared depth contract documented
in `Docs/Art/IsometricLayering.md`.

- `IsometricRenderOrderResolver` is the single numeric authority for displayed
  cell depth. Fixtures, deliveries, construction previews, walls, characters,
  customers, and workers must not invent independent world sorting formulas.
- Moving actors and other mobile world objects use
  `IsometricDepthSortingGroup`, configured with the active
  `IsometricViewHost`, coordinate Tilemap, and a ground-contact anchor at the
  object's feet.
- A root `SortingGroup` determines the object's place in the world. Child
  renderer orders determine only internal layering such as body parts, carried
  cases, fixture layers, and products.
- Walls consume the central cell-depth contract through
  `WallRenderOrderResolver`, which owns only wall-boundary and wall-seam rules.
- UI emphasis, selection glows, and construction previews use explicit overlay
  bands; they must not be used to repair incorrect world-depth anchors.

When adding a customer, worker, bot, movable pallet, or carried prop, verify its
world sorting against a nearer wall and a farther wall before considering its
presentation complete.
