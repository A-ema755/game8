# Unity 6 — Addressables (Gene Forge)

**Last verified:** 2026-02-13
**Status:** Post-MVP (Addressables used after initial ship milestone)
**Package:** `com.unity.addressables` (Package Manager)

---

## Overview

**Addressables** is Unity's advanced asset management system that replaces `Resources.Load()`
with async loading, remote content delivery, and better memory control.

In **Gene Forge**, Addressables are scoped to post-MVP content delivery — creature configs,
DNA strand assets, biome tile sets, and ability VFX loaded on demand as the player progresses
through the gene-engineering campaign.

**Use Addressables for (Gene Forge):**
- Loading creature prefabs and DNA config assets on demand per combat encounter
- Streaming biome-specific isometric tile sets without keeping all in memory
- Post-MVP DLC creature packs and ability expansion sets
- Loading ability VFX bundles per biome without bloating the build

**DON'T use Addressables for:**
- Core combat loop prefabs needed immediately at game start (use direct references)
- The base isometric grid tile set (always resident)
- UI assets loaded at application startup

---

## Installation

### Install via Package Manager

1. `Window > Package Manager`
2. Unity Registry > Search "Addressables"
3. Install `Addressables`

---

## Core Concepts

### 1. **Addressable Assets**
- Assets marked as "Addressable" (assigned unique keys)
- Can be loaded by key at runtime

### 2. **Asset Groups**
- Organize assets (e.g., "Creatures/Fire", "Tiles/Wetlands", "DNA/Mutations")
- Groups determine build settings (local vs remote)

### 3. **Async Loading**
- All loading is async (non-blocking)
- Returns `AsyncOperationHandle`

### 4. **Reference Counting**
- Addressables tracks asset usage
- Must manually release assets when done

---

## Gene Forge Asset Key Conventions

| Asset Type | Key Pattern | Example |
|---|---|---|
| Creature prefab | `Creatures/{Element}/{Name}` | `Creatures/Fire/Emberveil` |
| DNA config (ScriptableObject) | `DNA/{Strand}/{Trait}` | `DNA/Flame/HeatArmor` |
| Isometric tile set | `Tiles/{Biome}` | `Tiles/Volcanic` |
| Ability VFX | `VFX/{AbilityName}` | `VFX/DNASurge` |
| Creature ability set | `Abilities/{CreatureName}` | `Abilities/Emberveil` |

---

## Setup

### 1. Mark Assets as Addressable

1. Select asset in Project window
2. Inspector > Check "Addressable"
3. Assign key (e.g., `Creatures/Fire/Emberveil`)

**OR via script (editor tooling):**
```csharp
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

AddressableAssetSettings.AddAssetEntry(guid, "Creatures/Fire/Emberveil", "Default Local Group");
#endif
```

---

### 2. Create Groups

`Window > Asset Management > Addressables > Groups`

Recommended Gene Forge groups:
- **Core** (local): Base creature set, starting biome tiles, core UI
- **Creatures-Remote**: Additional creature prefabs + DNA configs (DLC/post-MVP)
- **Tiles-Remote**: Biome-specific isometric tile bundles
- **VFX-Remote**: Ability and mutation visual effect bundles

---

## Basic Loading

### Load Creature Prefab Async

```csharp
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class CreatureSpawner : MonoBehaviour {
    async Task<GameObject> LoadCreature(string creatureKey) {
        // e.g., creatureKey = "Creatures/Fire/Emberveil"
        AsyncOperationHandle<GameObject> handle =
            Addressables.LoadAssetAsync<GameObject>(creatureKey);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded) {
            return handle.Result;
        }

        Debug.LogError($"[CreatureSpawner] Failed to load creature: {creatureKey}");
        return null;
    }
}
```

---

### Load and Instantiate Creature on Grid

```csharp
async Task<GameObject> SpawnCreatureAtCell(string creatureKey, Vector3Int gridCell) {
    // Convert grid cell to world position for isometric layout
    Vector3 worldPos = GridManager.Instance.CellToWorld(gridCell);

    AsyncOperationHandle<GameObject> handle =
        Addressables.InstantiateAsync(creatureKey, worldPos, Quaternion.identity);
    await handle.Task;

    GameObject creature = handle.Result;
    creature.GetComponent<CreatureController>().InitializeAtCell(gridCell);

    return creature;
}

void DespawnCreature(GameObject creature) {
    Addressables.ReleaseInstance(creature);
}
```

---

### Load All Creatures with Label

```csharp
async void LoadBiomeCreatures(string biomeLabel) {
    // Load all creatures tagged with e.g. "Volcanic" or "Wetlands"
    AsyncOperationHandle<IList<GameObject>> handle =
        Addressables.LoadAssetsAsync<GameObject>(biomeLabel, null);
    await handle.Task;

    foreach (var creaturePrefab in handle.Result) {
        Debug.Log($"[Addressables] Loaded creature: {creaturePrefab.name}");
    }

    Addressables.Release(handle);
}
```

---

## Asset Labels (Gene Forge Tags)

Recommended label taxonomy:

| Label | Contents |
|---|---|
| `Biome-Volcanic` | All creatures, tiles, VFX for volcanic biome |
| `Biome-Wetlands` | All creatures, tiles, VFX for wetlands biome |
| `DNA-Core` | Starter DNA strand ScriptableObjects |
| `DNA-DLC` | Post-MVP expansion DNA configs |
| `Encounter-Boss` | Boss creature prefabs + oversized tile sets |

### Load by Label

```csharp
// Preload all assets for the volcanic biome encounter
Addressables.LoadAssetsAsync<GameObject>("Biome-Volcanic", null);
```

---

## Remote Content (Post-MVP DLC)

### Setup Remote Groups

1. Create new group: `Window > Addressables > Groups > Create New Group > Packed Assets`
2. Group Settings:
   - **Build Path**: `ServerData/[BuildTarget]`
   - **Load Path**: `http://yourcdn.com/geneforge/content/[BuildTarget]`

### Build Remote Content

1. `Window > Asset Management > Addressables > Build > New Build > Default Build Script`
2. Upload `ServerData/` folder to CDN
3. Gene Forge loads creature/DNA expansion packs from remote server

---

## Preloading / Caching

### Preload Encounter Assets Before Combat

```csharp
async Task PreloadEncounterAssets(string biomeLabel) {
    // Download all assets for the upcoming encounter without loading into memory
    AsyncOperationHandle handle = Addressables.DownloadDependenciesAsync(biomeLabel);
    await handle.Task;

    // Encounter assets are now cached — load instantly when combat begins
    Addressables.Release(handle);
}
```

### Check Download Size (DLC Pack)

```csharp
async Task<long> GetDLCPackSize(string dlcLabel) {
    AsyncOperationHandle<long> handle = Addressables.GetDownloadSizeAsync(dlcLabel);
    await handle.Task;

    long sizeInBytes = handle.Result;
    Debug.Log($"[DLC] Pack size: {sizeInBytes / (1024 * 1024)} MB");

    Addressables.Release(handle);
    return sizeInBytes;
}
```

---

## Memory Management

### Release Assets After Encounter

```csharp
// Release all creature handles when leaving an encounter
Addressables.Release(handle);

// Release instantiated creature objects
Addressables.ReleaseInstance(creatureGameObject);
```

---

## Asset References (Inspector-Assigned)

### Use AssetReference for DNA Config ScriptableObjects

```csharp
using UnityEngine.AddressableAssets;

public class CreatureDefinition : ScriptableObject {
    // Assign DNA strand config in Inspector
    public AssetReference dnaStrandConfig;
    public AssetReference abilitySetConfig;

    public async Task<DNAStrandConfig> LoadDNAConfig() {
        AsyncOperationHandle<DNAStrandConfig> handle =
            dnaStrandConfig.LoadAssetAsync<DNAStrandConfig>();
        await handle.Task;
        return handle.Result;
    }

    public void ReleaseDNAConfig() {
        dnaStrandConfig.ReleaseAsset();
    }
}
```

---

## Scenes

### Load Biome Scene Additively

```csharp
async Task LoadBiomeScene(string biomeSceneKey) {
    // e.g., biomeSceneKey = "Scenes/VolcanicBiome"
    AsyncOperationHandle<SceneInstance> handle =
        Addressables.LoadSceneAsync(biomeSceneKey, LoadSceneMode.Additive);
    await handle.Task;

    SceneInstance biomeScene = handle.Result;

    // Later — unload when leaving biome
    await Addressables.UnloadSceneAsync(handle).Task;
}
```

---

## Common Patterns

### Lazy Loading Creature Configs

```csharp
Dictionary<string, AsyncOperationHandle<GameObject>> _loadedCreatures = new();

async Task<GameObject> GetCreaturePrefab(string creatureKey) {
    if (!_loadedCreatures.ContainsKey(creatureKey)) {
        var handle = Addressables.LoadAssetAsync<GameObject>(creatureKey);
        await handle.Task;
        _loadedCreatures[creatureKey] = handle;
    }
    return _loadedCreatures[creatureKey].Result;
}
```

---

### Cleanup on Encounter End

```csharp
void OnEncounterComplete() {
    foreach (var handle in _loadedCreatures.Values) {
        Addressables.Release(handle);
    }
    _loadedCreatures.Clear();
}
```

---

## Content Catalog Updates (Live Patches)

### Check for New Creature/DNA Packs

```csharp
async void CheckForContentUpdates() {
    AsyncOperationHandle<List<string>> handle = Addressables.CheckForCatalogUpdates();
    await handle.Task;

    if (handle.Result.Count > 0) {
        Debug.Log("[Addressables] New Gene Forge content available");
        await Addressables.UpdateCatalogs(handle.Result).Task;
    }

    Addressables.Release(handle);
}
```

---

## Performance Tips

- **Preload** the starting biome's creature set during the loading screen
- **Release** creature assets immediately after an encounter ends
- Use **biome labels** to batch-load related creatures, tiles, and VFX together
- **Cache** remote DNA expansion packs for offline play

---

## Debugging

### Addressables Event Viewer

`Window > Asset Management > Addressables > Event Viewer`

- Shows all load/release operations
- Memory usage per creature/DNA config asset
- Reference counts

### Addressables Profiler

`Window > Asset Management > Addressables > Profiler`

- Real-time asset usage per encounter
- Bundle loading stats for biome tile sets

---

## Migration from Resources

```csharp
// OLD: Resources.Load (synchronous, blocks frame)
GameObject prefab = Resources.Load<GameObject>("Creatures/Emberveil");

// NEW: Addressables (async, non-blocking)
var handle = Addressables.LoadAssetAsync<GameObject>("Creatures/Fire/Emberveil");
await handle.Task;
GameObject prefab = handle.Result;
```

---

## Sources
- https://docs.unity3d.com/Packages/com.unity.addressables@2.0/manual/index.html
- https://learn.unity.com/tutorial/addressables
