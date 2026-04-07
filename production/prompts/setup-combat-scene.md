# Implementation Prompt: Combat Scene Setup

## Agent

Invoke **team-ui** skill: `/team-ui Combat Scene Setup`

Team composition for this feature:
- **ux-designer** — Validate camera framing, UI layout in scene, grid readability
- **ui-programmer** — Set up UIDocument, attach controllers, configure UI Toolkit panels
- **art-director** — Review isometric camera angle, grid tile placeholder visuals, lighting

Additionally consult:
- **godot-specialist** → **unity-specialist** agent for scene hierarchy best practices
- **technical-artist** agent for URP lighting and tile material setup

> **Note**: This is scene assembly, not code. Most scripts already exist — this prompt wires them into a Unity scene. Heavy use of Unity MCP tools for GameObject creation, component attachment, and scene configuration.

## Objective

Create the **Combat scene** — a Unity scene with isometric camera, grid visual, UI Document overlay, and all controller components wired up. This is the runtime environment where combat plays out. All logic exists in scripts; this prompt assembles them into a playable scene.

## Authoritative Design Source

- `design/gdd/combat-ui.md` §3 — Layout regions, camera requirements
- `design/gdd/grid-tile-system.md` §3 — Isometric grid visual, tile dimensions, height rendering
- `CLAUDE.md` — Isometric 3D, fixed 45° angle, URP rendering

## What Already Exists

### Scripts (all implemented):
- `Assets/Scripts/UI/CombatController.cs` — Scene orchestrator (after Combat Controller prompt)
- `Assets/Scripts/UI/CombatHUDController.cs` — Root UI controller
- All UI panel controllers (7 files in `Assets/Scripts/UI/`)
- All combat logic (TurnManager, DamageCalculator, etc.)
- `Assets/Scripts/Gameplay/Grid/GridSystem.cs` — Pure C# grid (needs visual representation)

### UI assets (implemented):
- `Assets/UI/Combat/CombatHUD.uxml` — Root UXML document
- `Assets/UI/Combat/CombatHUD.uss` — Stylesheet

### Missing scene elements:
- No `Combat.unity` scene exists yet
- No camera rig configured
- No grid tile GameObjects or prefabs
- No UIDocument GameObject in scene

## Scope — What to Create

### 1. `Assets/Scenes/Combat.unity`

**Scene hierarchy:**

```
Combat (scene root)
├── CombatManager                    [CombatController]
│   └── (serialized refs to UI panels and grid visual)
├── Camera
│   └── IsometricCamera              [Camera, URP settings]
│       └── (Cinemachine virtual cam, 45° isometric angle)
├── Lighting
│   ├── DirectionalLight             [Light, shadows]
│   └── AmbientProbe
├── Grid
│   └── GridVisual                   [MonoBehaviour: GridVisualizer]
│       └── (spawns tile GameObjects at runtime from GridSystem data)
├── UI
│   └── CombatHUD                    [UIDocument → CombatHUD.uxml]
│       └── (CombatHUDController attached)
└── CreatureContainer                [Empty parent for spawned creature GameObjects]
```

### 2. `Assets/Scripts/UI/GridVisualizer.cs`

**MonoBehaviour** that renders GridSystem data as 3D tile GameObjects.

**Responsibilities:**
- On `Initialize(GridSystem grid)`: spawn tile prefab at each grid position
- Convert grid coords to world coords (isometric projection)
- Set tile height from `TileData.Height`
- Apply material based on `TileData.Terrain` type
- Expose `HighlightTile(Vector2Int, Color)` and `ClearHighlights()` for TileHighlightController

### 3. `Assets/Prefabs/Tile.prefab`

**Simple tile prefab:**
- Cube scaled to (1, 0.2, 1) as placeholder
- Default material (gray)
- Collider for mouse raycasting

### 4. `Assets/Prefabs/CreaturePlaceholder.prefab`

**Placeholder creature visual:**
- Capsule or sphere with type-colored material
- TextMeshPro label showing creature name
- Billboard to always face camera

### 5. Camera configuration

- Orthographic camera at 45° rotation (isometric)
- Camera position: elevated, looking down at grid center
- URP renderer settings for clean tile rendering
- No camera rotation (fixed angle per CLAUDE.md)

## Method

Use Unity MCP tools extensively:
1. `unity_scene_new` → create Combat scene
2. `unity_gameobject_create` → build hierarchy
3. `unity_component_add` → attach scripts and components
4. `unity_component_set_property` → configure camera, lights
5. `unity_asset_create_prefab` → create tile and creature prefabs
6. `unity_material_create` → tile materials per terrain type
7. `unity_scene_save` → persist scene

## Constraints

- **URP rendering** — use URP-compatible materials and lights
- **Isometric 3D** — orthographic camera, 45° angle, no rotation
- **Grid coord → world coord** formula: standard isometric projection
- **Placeholder art only** — cubes for tiles, capsules for creatures. Real art is post-MVP
- **No Cinemachine dependency for MVP** — simple orthographic camera setup. Cinemachine is approved but not required yet
- Follow collaboration protocol: Question → Options → Decision → Draft → Approval

## Verification

- Open Combat scene in Unity Editor
- Camera shows isometric view of grid area
- UI overlay visible with all 4 panels (even if empty)
- Run Play mode: grid tiles spawn, creatures appear at start positions
- Screenshots via `unity_screenshot_scene` and `unity_screenshot_game`

## Branch

Create branch `feature/Combat-Scene` from `main`.
