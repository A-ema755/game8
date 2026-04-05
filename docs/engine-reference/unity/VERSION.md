# Unity Engine -- Version Reference

| Field | Value |
|-------|-------|
| **Engine Version** | Unity 6 (6000.x) |
| **Render Pipeline** | Universal Render Pipeline (URP) — 3D Renderer (isometric) |
| **Project Pinned** | 2026-04-04 |
| **Last Docs Verified** | 2026-04-04 |
| **LLM Knowledge Cutoff** | May 2025 |

## Knowledge Gap Warning

The LLM's training data likely covers Unity up to ~2022 LTS / early Unity 6 previews.
Unity 6 (6000.x) introduced significant changes that the model may not know about.
Always cross-reference this directory before suggesting Unity API calls.

## Key Unity 6 Changes from 2022 LTS

- RenderGraph is now mandatory for custom URP/HDRP render passes
- New Input System is the recommended default (old Input Manager deprecated)
- UI Toolkit recommended over UGUI for editor and runtime UI
- DOTS/ECS (Entities package) is production-ready
- Addressables v2 with improved async loading
- Burst compiler improvements for SIMD

## Verified Sources

- Official docs: https://docs.unity3d.com/6000.0/Documentation/Manual/
- Migration guide: https://docs.unity3d.com/6000.0/Documentation/Manual/UpgradeGuide6.html
- URP docs: https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest
