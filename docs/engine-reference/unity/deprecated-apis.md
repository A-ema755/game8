# Unity 6 — Deprecated APIs

**Last verified:** 2026-04-04

Quick lookup: **Don't use X** → **Use Y instead**

## Input

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `Input.GetKey()` | `Keyboard.current[Key.X].isPressed` | New Input System |
| `Input.GetKeyDown()` | `Keyboard.current[Key.X].wasPressedThisFrame` | New Input System |
| `Input.GetMouseButton()` | `Mouse.current.leftButton.isPressed` | New Input System |
| `Input.GetAxis()` | `InputAction` callbacks | New Input System |
| `Input.mousePosition` | `Mouse.current.position.ReadValue()` | New Input System |

## UI

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `Canvas` (UGUI) | `UIDocument` (UI Toolkit) | Production-ready in Unity 6 |
| `Text` component | `TextMeshPro` or UI Toolkit `Label` | Better rendering |
| `Image` component | UI Toolkit `VisualElement` with background | More flexible |

## Rendering

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `CommandBuffer.DrawMesh()` | RenderGraph API | URP/HDRP render passes |
| `OnPreRender()` / `OnPostRender()` | `RenderPipelineManager` callbacks | SRP compatibility |
| Built-in Render Pipeline | URP or HDRP | No new features |

## Physics

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `Physics.RaycastAll()` | `Physics.RaycastNonAlloc()` | Avoid GC allocations |

## Asset Loading

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `Resources.Load()` | Addressables | Better memory control (post-MVP for us) |
| Synchronous asset loading | `Addressables.LoadAssetAsync()` | Non-blocking |

## Scripting

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `WWW` class | `UnityWebRequest` | Modern async networking |
| `Application.LoadLevel()` | `SceneManager.LoadScene()` | Scene management |
| `FindObjectOfType()` | `FindFirstObjectByType()` or serialized refs | Performance |

## Particles

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| Legacy Particle System | Visual Effect Graph | GPU-accelerated |

## Quick Migration Patterns

```csharp
// ❌ Deprecated input
if (Input.GetKeyDown(KeyCode.Space)) { Jump(); }
// ✅ New Input System
if (Keyboard.current.spaceKey.wasPressedThisFrame) { Jump(); }

// ❌ Deprecated asset loading
var prefab = Resources.Load<GameObject>("Creatures/Emberfox");
// ✅ Addressables (post-MVP)
var handle = Addressables.LoadAssetAsync<GameObject>("Creatures/Emberfox");
var prefab = await handle.Task;

// ❌ Deprecated UI
GetComponent<Text>().text = "HP: 78";
// ✅ UI Toolkit
rootVisualElement.Q<Label>("hp-label").text = "HP: 78";
```

**Sources:**
- https://docs.unity3d.com/6000.0/Documentation/Manual/deprecated-features.html
