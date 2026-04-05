# Unity 6 — DOTS / Entities (Gene Forge)

**Last verified:** 2026-02-13
**Status:** Reference Only — Gene Forge uses MonoBehaviour for MVP; DOTS is post-MVP consideration
**Package:** `com.unity.entities` (Package Manager)

---

## Overview

**DOTS (Data-Oriented Technology Stack)** is Unity's high-performance ECS (Entity Component System)
framework designed for games with massive entity counts (1000s–10,000s).

**Gene Forge context:** The core tactical loop (isometric grid, creature turns, DNA engineering)
is implemented with MonoBehaviour + ScriptableObject. DOTS becomes relevant post-MVP if
simulation scale demands it — e.g., large procedural ecosystems, mass DNA mutation processing,
or crowd simulation in biome backgrounds.

**When DOTS would help Gene Forge:**
- Simulating 500+ background ecosystem creatures per biome scene
- Processing DNA mutation permutations across large creature populations in parallel
- Pathfinding for large-scale grid maps (100x100+)
- Burst-accelerated combat stat resolution for auto-battle features

**DON'T use DOTS for (Gene Forge MVP):**
- The core 8x8 grid combat (MonoBehaviour is sufficient and simpler)
- DNA config ScriptableObjects (no performance gain from ECS here)
- Any system heavily relying on UnityEngine UI or Cinemachine

**Note:** Entities 1.0+ (Unity 6) is a complete rewrite from 0.x. All examples below use
the Entities 1.3+ API.

---

## Installation

### Install via Package Manager

1. `Window > Package Manager`
2. Unity Registry > Search "Entities"
3. Install:
   - `Entities` (ECS core)
   - `Burst` (LLVM compiler)
   - `Jobs` (auto-installed with Entities)
   - `Mathematics` (SIMD math)

---

## Core Concepts

### 1. **Entity**
- Lightweight ID (int)
- No behavior, just an identifier

### 2. **Component**
- Data only (no methods)
- Struct implementing `IComponentData`

### 3. **System**
- Logic that operates on components
- Struct implementing `ISystem`

### 4. **Archetype**
- Unique combination of component types
- Entities with same components share archetype (cache-friendly)

---

## Basic ECS Pattern

### Define Components (Gene Forge Examples)

```csharp
using Unity.Entities;
using Unity.Mathematics;

// Grid position on the isometric battle map
public struct GridPosition : IComponentData {
    public int2 Cell; // (column, row) on the isometric grid
}

// Creature combat stats driven by DNA
public struct CreatureStats : IComponentData {
    public float HP;
    public float MaxHP;
    public float Attack;
    public float Defense;
    public float Speed;
}

// Active DNA mutation modifier applied this encounter
public struct DNAModifier : IComponentData {
    public float AttackBonus;
    public float DefenseBonus;
    public float SpeedBonus;
}

// Tag: creature belongs to the player's team
public struct PlayerCreatureTag : IComponentData { }

// Tag: creature belongs to the wild/enemy team
public struct WildCreatureTag : IComponentData { }
```

---

### Define System (Gene Forge Example)

```csharp
using Unity.Entities;
using Unity.Burst;

// Applies DNA modifier bonuses to base stats each frame (or on mutation)
[BurstCompile]
public partial struct DNAModifierSystem : ISystem {
    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        foreach (var (stats, modifier) in
            SystemAPI.Query<RefRW<CreatureStats>, RefRO<DNAModifier>>()) {

            // Apply mutation bonuses on top of base stats
            stats.ValueRW.Attack  += modifier.ValueRO.AttackBonus;
            stats.ValueRW.Defense += modifier.ValueRO.DefenseBonus;
            stats.ValueRW.Speed   += modifier.ValueRO.SpeedBonus;
        }
    }
}
```

---

### Create Creature Entities

```csharp
using Unity.Entities;
using Unity.Mathematics;

public partial class CreatureEntitySpawner : SystemBase {
    protected override void OnUpdate() {
        var em = EntityManager;

        // Create a creature entity at grid cell (3, 2)
        Entity creature = em.CreateEntity();
        em.AddComponentData(creature, new GridPosition { Cell = new int2(3, 2) });
        em.AddComponentData(creature, new CreatureStats {
            HP = 80f, MaxHP = 80f,
            Attack = 25f, Defense = 15f, Speed = 8f
        });
        em.AddComponentData(creature, new PlayerCreatureTag());
    }
}
```

---

## Hybrid ECS (MonoBehaviour + ECS)

### Baker — Convert Creature GameObject to Entity

This is the recommended hybrid approach for Gene Forge post-MVP: author creatures as
GameObjects in the editor, then bake them to entities at runtime for performance-critical
simulation passes.

```csharp
using Unity.Entities;
using UnityEngine;

// Authoring component — attach this in the Inspector on creature prefabs
public class CreatureAuthoring : MonoBehaviour {
    public float hp;
    public float attack;
    public float defense;
    public float speed;
    public bool isPlayerOwned;
}

// Baker — converts authoring data to ECS components
public class CreatureBaker : Baker<CreatureAuthoring> {
    public override void Bake(CreatureAuthoring authoring) {
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        AddComponent(entity, new CreatureStats {
            HP       = authoring.hp,
            MaxHP    = authoring.hp,
            Attack   = authoring.attack,
            Defense  = authoring.defense,
            Speed    = authoring.speed
        });

        if (authoring.isPlayerOwned)
            AddComponent<PlayerCreatureTag>(entity);
        else
            AddComponent<WildCreatureTag>(entity);
    }
}
```

---

## Queries

### Query All Player Creatures

```csharp
foreach (var (stats, gridPos) in
    SystemAPI.Query<RefRO<CreatureStats>, RefRO<GridPosition>>()
        .WithAll<PlayerCreatureTag>()) {

    Debug.Log($"Player creature at cell {gridPos.ValueRO.Cell} — HP: {stats.ValueRO.HP}");
}
```

---

### Query with Entity Access

```csharp
foreach (var (stats, entity) in
    SystemAPI.Query<RefRW<CreatureStats>>().WithEntityAccess()) {

    if (stats.ValueRO.HP <= 0f) {
        // Mark for removal
        Debug.Log($"Creature {entity} has fainted");
    }
}
```

---

### Filter by Team Tag

```csharp
// Process only wild/enemy creatures
foreach (var stats in
    SystemAPI.Query<RefRW<CreatureStats>>().WithAll<WildCreatureTag>()) {
    // Wild creature AI logic
}
```

---

## Jobs (Parallel Execution)

### Parallel DNA Mutation Processing

```csharp
using Unity.Entities;
using Unity.Burst;

// Apply a pending DNA mutation to all creatures in parallel
[BurstCompile]
public partial struct ApplyMutationJob : IJobEntity {
    public float AttackDelta;
    public float DefenseDelta;

    void Execute(ref CreatureStats stats, in DNAModifier modifier) {
        stats.Attack  += AttackDelta + modifier.AttackBonus;
        stats.Defense += DefenseDelta + modifier.DefenseBonus;
    }
}

[BurstCompile]
public partial struct MutationApplicationSystem : ISystem {
    public void OnUpdate(ref SystemState state) {
        var job = new ApplyMutationJob {
            AttackDelta  = 5f,
            DefenseDelta = 2f
        };
        job.ScheduleParallel(); // All creatures processed simultaneously
    }
}
```

---

## Burst Compiler

```csharp
using Unity.Burst;

[BurstCompile] // 10-100x faster than regular C# for numeric-heavy logic
public partial struct StatResolutionSystem : ISystem {
    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        // Combat stat math, DNA permutation scoring, pathfinding heuristics
    }
}
```

**Burst Restrictions:**
- No managed references (classes, strings, UnityEngine.Object)
- Only blittable types (structs, primitives, `Unity.Mathematics` types)
- No exceptions, no `Debug.Log` in hot paths

---

## Entity Command Buffers (Structural Changes)

### Deferred Creature Spawn / Despawn

```csharp
using Unity.Entities;

public partial struct EncounterSpawnSystem : ISystem {
    public void OnUpdate(ref SystemState state) {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Queue creature spawns without modifying the entity list during iteration
        foreach (var spawner in SystemAPI.Query<GridSpawnerData>()) {
            Entity newCreature = ecb.CreateEntity();
            ecb.AddComponent(newCreature, new GridPosition { Cell = spawner.SpawnCell });
            ecb.AddComponent(newCreature, new WildCreatureTag());
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
```

---

## Dynamic Buffers (Grid Path / Move Range)

### Define Move Range Buffer

```csharp
// Each creature entity can store its reachable cells for the current turn
public struct ReachableCell : IBufferElementData {
    public int2 Cell;
}
```

### Populate and Read Move Range

```csharp
// Set reachable cells for a creature
var buffer = EntityManager.AddBuffer<ReachableCell>(creatureEntity);
buffer.Add(new ReachableCell { Cell = new int2(3, 2) });
buffer.Add(new ReachableCell { Cell = new int2(4, 2) });
buffer.Add(new ReachableCell { Cell = new int2(3, 3) });

// Query move range during Input System selection validation
foreach (var moveRange in SystemAPI.Query<DynamicBuffer<ReachableCell>>()) {
    foreach (var cell in moveRange) {
        Debug.Log($"Reachable: {cell.Cell}");
    }
}
```

---

## Tags (Zero-Size Components)

```csharp
public struct PlayerCreatureTag : IComponentData { }  // Player-owned creature
public struct WildCreatureTag   : IComponentData { }  // Wild/enemy creature
public struct FaintedTag        : IComponentData { }  // Out of combat
public struct SelectedTag       : IComponentData { }  // Currently selected by Input System
public struct HasActedTag       : IComponentData { }  // Already took a turn this round
```

---

## System Ordering (Gene Forge Turn Loop)

```csharp
[UpdateBefore(typeof(DNAModifierSystem))]
public partial struct TurnInitSystem : ISystem { }    // Reset per-turn state

[UpdateAfter(typeof(DNAModifierSystem))]
public partial struct CombatResolutionSystem : ISystem { } // Resolve attacks after stats applied

[UpdateAfter(typeof(CombatResolutionSystem))]
public partial struct FaintCheckSystem : ISystem { }  // Check HP, apply FaintedTag
```

---

## Migration from MonoBehaviour (Reference)

```csharp
// MonoBehaviour approach (Gene Forge MVP):
public class CreatureController : MonoBehaviour {
    public float speed;
    void Update() {
        // Move creature along path
    }
}

// DOTS approach (post-MVP simulation scale):
public struct CreatureStats : IComponentData {
    public float Speed;
}

[BurstCompile]
public partial struct CreatureMovementSystem : ISystem {
    public void OnUpdate(ref SystemState state) {
        float dt = SystemAPI.Time.DeltaTime;
        foreach (var (transform, stats) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRO<CreatureStats>>()) {
            // Move along grid path — Burst-compiled, runs on job threads
            transform.ValueRW.Position += new float3(0, 0, stats.ValueRO.Speed * dt);
        }
    }
}
```

---

## Debugging

### Entities Hierarchy Window

`Window > Entities > Hierarchy`

- Shows all active creature/grid entities and their components
- Filter by `PlayerCreatureTag`, `WildCreatureTag`, `FaintedTag`, etc.

### Entities Profiler

`Window > Analysis > Profiler > Entities`

- System execution times (useful for profiling DNA mutation passes)
- Memory usage per archetype

---

## Sources
- https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/index.html
- https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/index.html
- https://learn.unity.com/tutorial/entity-component-system
