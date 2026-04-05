# Unity 6.3 — Rendering Module Reference

**Last verified:** 2026-02-13
**Knowledge Gap:** LLM trained on Unity 2022 LTS; Unity 6 has major rendering changes

**Gene Forge context:** Gene Forge uses **URP (Universal Render Pipeline)**. All shaders,
materials, and render passes must target URP. HDRP and Built-in Pipeline code is invalid.
Isometric 3D view with Cinemachine virtual cameras. ProBuilder tiles use URP Lit materials.
Custom renderer features used for: tile highlight outlines, creature selection glow, damage
flash, and type-color overlays.

---

## Overview

Unity 6.3 LTS rendering architecture:
- **URP (Universal Render Pipeline)**: Required for Gene Forge — cross-platform, mobile-friendly
- **HDRP**: NOT used in Gene Forge
- **Built-in Pipeline**: Deprecated, do not use

---

## Key Changes from 2022 LTS

### RenderGraph API (Unity 6+)
Custom render passes now use RenderGraph instead of CommandBuffer:

```csharp
// ✅ Unity 6+ (RenderGraph) — use for Gene Forge custom passes
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
    using var builder = renderGraph.AddRasterRenderPass<PassData>("GeneForgeOutline", out var passData);
    builder.SetRenderFunc((PassData data, RasterGraphContext ctx) => {
        // Outline / highlight rendering commands
    });
}

// ❌ Old (CommandBuffer — still works but deprecated)
public override void Execute(ScriptableRenderContext context, ref RenderingData data) { }
```

### GPU Resident Drawer (Unity 6+)
Automatic batching for large numbers of tile instances:

```csharp
// Enable in URP Asset settings:
// Rendering > GPU Resident Drawer = Enabled
// Gene Forge: benefits tile grid rendering (many identical ProBuilder tile meshes)
```

---

## URP Quick Reference

### Creating / Verifying a URP Asset
1. `Assets > Create > Rendering > URP Asset (with Universal Renderer)`
2. Assign to `Project Settings > Graphics > Scriptable Render Pipeline Settings`

### URP Renderer Features (Gene Forge Uses)

```csharp
using UnityEngine.Rendering.Universal;

// Gene Forge: tile highlight outline pass
public class TileHighlightRendererFeature : ScriptableRendererFeature {
    TileHighlightRenderPass pass;

    public override void Create() {
        pass = new TileHighlightRenderPass();
        pass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData data) {
        renderer.EnqueuePass(pass);
    }
}
```

---

## Materials & Shaders

### Shader Graph (Visual Shader Editor)
Gene Forge preferred approach for creature materials and tile effects:

```csharp
// Create: Assets > Create > Shader Graph > URP > Lit Shader Graph
// Gene Forge uses: creature type-color tint, DNA highlight pulse, damage flash
```

### HLSL Custom Shaders (URP)

```hlsl
// Gene Forge URP shader template
Shader "GeneForge/URPLit" {
    Properties {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _TypeTint  ("Type Tint Color", Color) = (1,1,1,1)  // Gene Forge: creature type color
    }
    SubShader {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _TypeTint;
            CBUFFER_END

            Varyings vert(Attributes input) {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                return _BaseColor * _TypeTint;
            }
            ENDHLSL
        }
    }
}
```

---

## Lighting

### Baked Lighting (Gene Forge Tile Grid)

```csharp
// Mark ProBuilder tiles as static: Inspector > Static > Contribute GI
// Bake: Window > Rendering > Lighting > Generate Lighting
// Gene Forge: baked lighting on static tiles; dynamic lights for ability VFX only
```

### Real-Time Lights (URP)

```csharp
// Gene Forge: ability VFX point lights (flash on hit, type-colored glow)
// Keep additional light count within URP Asset "Additional Lights" limit
int lightCount = GetAdditionalLightsCount();
```

---

## Post-Processing (Volume System)

```csharp
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Gene Forge: combat scene volume — Bloom for ability glows, subtle Vignette, Color Grading
Volume volume = GetComponent<Volume>();
if (volume.profile.TryGet<Bloom>(out var bloom)) {
    bloom.intensity.value = 2.5f;  // Increase on critical hit
}

// Gene Forge: hit flash — briefly boost exposure or saturation via volume weight
volume.weight = Mathf.Lerp(0f, 1f, hitFlashTimer);
```

---

## Isometric Camera Rendering Notes

```csharp
// Gene Forge: Cinemachine virtual cameras handle framing.
// Camera projection: Orthographic or Perspective (GDD specifies).
// Sorting: Use "Sorting Layer" + "Order in Layer" for 2.5D sprite overlays if used.
// Depth write: ensure tile floors write depth so creature silhouettes clip correctly.
```

---

## Performance

### SRP Batcher (Auto-batching)
```csharp
// Enable: URP Asset > Advanced > SRP Batcher = Enabled
// Gene Forge: all tile materials should be SRP-Batcher compatible (use CBUFFER_START)
```

### GPU Instancing (Tile Grid)
```csharp
// Gene Forge: enable GPU Instancing on ProBuilder tile materials
// Batches identical tile meshes with the same material
Graphics.RenderMeshInstanced(
    new RenderParams(tileMaterial),
    tileMesh,
    0,
    tileMatrices // NativeArray<Matrix4x4>
);
```

### Occlusion Culling
```csharp
// Window > Rendering > Occlusion Culling
// Bake for static tile geometry — isometric view benefits from occlusion culling
```

---

## Gene Forge Renderer Feature Patterns

### Tile Selection Highlight

```csharp
// Pattern: render selected/highlighted tiles with stencil-based outline
// 1. TileHighlightRendererFeature enqueues pass after opaques
// 2. Pass draws outline quads at highlighted tile positions
// 3. Color determined by highlight type: move range (blue), attack range (red), selected (white)
```

### Creature Status Overlay

```csharp
// Pattern: overlay shader on creature mesh driven by status effect bitmask
// StatusOverlayShader reads _StatusFlags int and outputs tint/pulse per status type
material.SetInteger("_StatusFlags", (int)creature.ActiveStatusFlags);
```

---

## Debugging

### Frame Debugger
- `Window > Analysis > Frame Debugger` — step through draw calls, inspect state

### Rendering Debugger (Unity 6+)
- `Window > Analysis > Rendering Debugger` — live URP settings, overdraw, lighting

---

## Sources
- https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/index.html
- https://docs.unity3d.com/6000.0/Documentation/Manual/render-pipelines.html
