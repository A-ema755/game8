# Unity 6 — Breaking Changes

**Last verified:** 2026-04-04
**Adapted from CCGS template for Gene Forge project**

This document tracks breaking API changes and behavioral differences between Unity 2022 LTS
(likely in model training) and Unity 6 (current version). Organized by risk level.

## HIGH RISK — Will Break Existing Code

### Input System — Legacy Input Deprecated
**Versions:** Unity 6.0+

```csharp
// ❌ OLD: Input class (deprecated)
if (Input.GetKeyDown(KeyCode.Space)) { }

// ✅ NEW: Input System package
using UnityEngine.InputSystem;
if (Keyboard.current.spaceKey.wasPressedThisFrame) { }
```

**Migration:** Install Input System package, replace all `Input.*` calls with new API.

### URP Renderer Feature API Changes
**Versions:** Unity 6.0+

```csharp
// ❌ OLD: ScriptableRenderPass.Execute signature
public override void Execute(ScriptableRenderContext context, ref RenderingData data)

// ✅ NEW: Uses RenderGraph API
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
```

**Migration:** Update custom render passes to use RenderGraph API.

## MEDIUM RISK — Behavioral Changes

### Addressables — Asset Loading Returns
**Versions:** Unity 6.2+

Asset loading failures now throw exceptions by default instead of returning null.

```csharp
// ❌ OLD: Silent null on failure
var handle = Addressables.LoadAssetAsync<Sprite>("key");
var sprite = handle.Result; // null if failed

// ✅ NEW: Throws on failure, use try/catch
try {
    var handle = Addressables.LoadAssetAsync<Sprite>("key");
    var sprite = await handle.Task;
} catch (Exception e) {
    Debug.LogError($"Failed to load: {e}");
}
```

### Physics — Default Solver Iterations Changed
**Versions:** Unity 6.0+

Default solver iterations increased for better stability. Check `Physics.defaultSolverIterations` if you rely on old behavior.

## LOW RISK — Deprecations (Still Functional)

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| UGUI (Canvas) | UI Toolkit | UI Toolkit now production-ready |
| Legacy Particle System | Visual Effect Graph | GPU-accelerated |
| Old Animation System | Animator Controller (Mecanim) | State machine |
| `Resources.Load()` | Addressables | Better memory control |
| `WWW` class | `UnityWebRequest` | Modern async networking |
| `Application.LoadLevel()` | `SceneManager.LoadScene()` | Scene management |

## Gene Forge Relevance

For our isometric 3D creature game:
- **Input System**: Required — we use it for grid movement, move selection, capture controls
- **RenderGraph**: Not needed MVP — no custom render passes planned
- **Addressables**: Post-MVP — using Resources/ for MVP ConfigLoader
- **Physics changes**: Low impact — grid-based movement, minimal PhysX usage
- **UGUI vs UI Toolkit**: Decision needed — combat UI, party management, Creature Forge all need UI framework choice

## Migration Checklist

- [ ] Replace all `Input` class usage with Input System package
- [ ] Add exception handling to any Addressables calls (post-MVP)
- [ ] Test physics behavior if using PhysX for anything
- [ ] Choose UI Toolkit or UGUI for Gene Forge UI systems

**Sources:**
- https://docs.unity3d.com/6000.0/Documentation/Manual/upgrade-guides.html
