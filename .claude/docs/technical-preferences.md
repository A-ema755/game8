# Technical Preferences

## Engine & Language

- **Engine**: Unity 6 (6000.x) with URP
- **Language**: C# (primary)
- **Rendering**: Universal Render Pipeline (URP) — 3D Renderer, isometric camera
- **Physics**: PhysX (3D) — minimal use, grid logic handles movement/combat
- **Perspective**: Isometric 3D, fixed 45° angle, no camera rotation, height-variable tiles

## Naming Conventions

- **Classes**: PascalCase (e.g., `CreatureInstance`)
- **Public methods**: PascalCase (e.g., `GetReachableTiles()`)
- **Private fields**: camelCase with _ prefix (e.g., `_moveSpeed`)
- **Serialized fields**: camelCase (e.g., `[SerializeField] int maxHp`)
- **Events**: PascalCase (e.g., `RoundStarted`, `CreatureCaptured`)
- **Constants**: PascalCase (e.g., `MinimumDamage`, `MaxPartySize`)
- **Enums**: PascalCase (e.g., `CreatureType.Fire`, `MoveCategory.Special`)
- **Files**: PascalCase matching class (e.g., `CreatureInstance.cs`)
- **Scenes**: PascalCase (e.g., `Combat.unity`)
- **ScriptableObjects**: PascalCase (e.g., `Emberfox.asset`)
- **Config IDs**: kebab-case (e.g., `emberfox`, `flame-claw`)

## Performance Budgets

- **Target Framerate**: 60 FPS minimum
- **Frame Budget**: 16.67ms
- **Draw Calls**: [TO BE CONFIGURED]
- **Memory Ceiling**: [TO BE CONFIGURED]

## Testing

- **Framework**: Unity Test Framework (NUnit)
- **Test Modes**: EditMode (unit tests for pure C# logic), PlayMode (integration)
- **Minimum Coverage**: [TO BE CONFIGURED]
- **Required Tests**: Type chart calculations, damage formulas, capture rate math, DNA instability, grid pathfinding

## Forbidden Patterns

- No `FindObjectOfType` in hot paths — use serialized references or DI
- No hardcoded gameplay values — all values in ScriptableObjects
- No `Update()` polling when events would suffice
- No public fields — use `[SerializeField]` private fields or properties

## Allowed Libraries / Packages

- TextMeshPro (included with Unity)
- Unity Input System (new)
- ProBuilder (grid prototyping, tile meshes)
- Cinemachine (isometric camera rig)
- [Additional packages to be approved via ADR]

## Architecture Decisions Log

- ADR-001: ScriptableObjects for all config data (not JSON, not Addressables for MVP)
- ADR-002: Plain C# classes for pure logic systems (GridSystem, TurnManager, DamageCalculator, TypeChart)
- ADR-003: Minimal singletons — GameStateManager (required), AudioManager, SettingsManager (convenience). All other systems use DI or serialized references.
- ADR-004: C# events for system decoupling (not UnityEvents)
- ADR-005: JSON saves via JsonUtility (not binary serialization)
- ADR-006: Modular creature system — body parts as ScriptableObjects with slot-based equipping
- ADR-007: Type chart as a static 2D array, not ScriptableObject (performance-critical, rarely changes)
