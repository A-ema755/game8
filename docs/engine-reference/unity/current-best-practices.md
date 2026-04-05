# Unity 6 — Current Best Practices

**Last verified:** 2026-04-04
**Adapted from CCGS template for Gene Forge project**

Modern Unity 6 patterns that may not be in the LLM's training data.

## Project Setup

- **Use URP** for Gene Forge (isometric 3D, cross-platform, good performance)
- **Use C# 9+ features** (record types, pattern matching, init-only properties)

## Scripting

### Async/Await for Asset Loading
```csharp
// ✅ Modern async pattern
public async Task<GameObject> LoadCreatureAsync(string key) {
    var handle = Addressables.LoadAssetAsync<GameObject>(key);
    return await handle.Task;
}
```

### C# 9 Features Available
```csharp
// ✅ Pattern matching (useful for type chart, AI decisions)
var result = creature.PrimaryType switch {
    CreatureType.Fire => 1.2f,
    CreatureType.Water => 0.8f,
    _ => 1.0f
};
```

## Input

### Use Input System Package (Not Legacy Input)
```csharp
// ✅ Input Actions (rebindable, cross-platform)
using UnityEngine.InputSystem;

public class CombatInput : MonoBehaviour {
    private PlayerControls controls;

    void Awake() {
        controls = new PlayerControls();
        controls.Combat.SelectTile.performed += ctx => OnTileSelected(ctx);
        controls.Combat.UseMove.performed += ctx => OnMoveSelected(ctx);
    }
}
```

## UI

### UI Toolkit for Runtime UI (Production-Ready)
```csharp
// ✅ UI Toolkit
using UnityEngine.UIElements;

public class CreatureInfoPanel : MonoBehaviour {
    void OnEnable() {
        var root = GetComponent<UIDocument>().rootVisualElement;
        var hpBar = root.Q<ProgressBar>("hp-bar");
        hpBar.value = creature.CurrentHP / creature.MaxHP;
    }
}
```

## Performance

### Use Burst Compiler + Jobs for Heavy Work
```csharp
// ✅ Burst-compiled job (for A* pathfinding, damage calculations at scale)
[BurstCompile]
struct PathfindingJob : IJob {
    public NativeArray<int> TileHeights;
    public int2 Start;
    public int2 End;
    public NativeList<int2> ResultPath;

    public void Execute() {
        // A* implementation here — runs 20-100x faster than managed C#
    }
}
```

### GPU Instancing for Grid Tiles
```csharp
// ✅ Thousands of tiles with minimal draw calls
Graphics.RenderMeshInstanced(
    new RenderParams(tileMaterial),
    tileMesh,
    0,
    tileMatrices
);
```

## Gene Forge Specific Best Practices

| System | Recommendation |
|--------|---------------|
| **Grid rendering** | GPU Instancing for tile meshes |
| **Pathfinding** | Burst + Jobs if grid > 15x15 |
| **Type chart** | Static array (ADR-007), no SO needed |
| **Creature configs** | ScriptableObjects via Resources/ (MVP), Addressables (post-MVP) |
| **Combat UI** | UI Toolkit recommended for Creature Forge, move selection |
| **Input** | Input System package with Input Actions asset |
| **Camera** | Cinemachine virtual camera for isometric rig |
| **Tile meshes** | ProBuilder for prototyping, replace with authored meshes later |

## Summary: Unity 6 Tech Stack for Gene Forge

| Feature | Use This | Avoid This |
|---------|----------|------------|
| **Input** | Input System package | `Input` class |
| **UI** | UI Toolkit | UGUI (Canvas) |
| **Rendering** | URP + RenderGraph (if custom passes needed) | Built-in pipeline |
| **Assets** | Resources/ (MVP), Addressables (post-MVP) | Hardcoded paths |
| **Performance** | Burst + Jobs for hot paths | Coroutines for heavy work |
| **Camera** | Cinemachine | Manual camera scripts |

**Sources:**
- https://docs.unity3d.com/6000.0/Documentation/Manual/BestPracticeGuides.html
