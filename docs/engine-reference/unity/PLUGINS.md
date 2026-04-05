# Unity 6 — Packages & Plugins Reference

**Last verified:** 2026-04-04

## Gene Forge Required Packages

| Package | Purpose | MVP? |
|---------|---------|------|
| **Input System** | Modern input (grid selection, move selection, capture) | Yes |
| **URP** | Rendering pipeline (isometric 3D) | Yes |
| **TextMeshPro** | Text rendering | Yes |
| **ProBuilder** | Tile mesh prototyping | Yes |
| **Cinemachine** | Isometric camera rig | Yes |
| **Test Framework** | NUnit testing | Yes |

## Optional Packages (Post-MVP)

| Package | Purpose | When |
|---------|---------|------|
| **Addressables** | Async asset loading, memory control | When creature count > 50 |
| **Visual Effect Graph** | GPU particles (DNA mutation visuals, ability effects) | Polish phase |
| **Shader Graph** | Custom shaders (instability glitch, terrain synergy glow) | Polish phase |
| **Timeline** | Cutscenes (rival encounters, story beats) | Vertical Slice |
| **Animation Rigging** | Runtime IK for creature procedural animation | When full 3D models added |
| **Burst + Jobs** | Performance for A* pathfinding, mass damage calc | When grid > 15x15 |
| **Splines** | Campaign map paths | Vertical Slice |
| **ML-Agents** | Advanced creature AI training | Experimental |

## Quick Decision Guide

| I need... | Use... |
|-----------|--------|
| Virtual cameras | **Cinemachine** |
| Async asset loading / DLC | **Addressables** |
| Modern input | **Input System** |
| GPU particles | **Visual Effect Graph** |
| Visual shaders | **Shader Graph** |
| Cinematics | **Timeline** |
| Runtime IK | **Animation Rigging** |
| Level prototyping | **ProBuilder** |
| Performance (math) | **Burst + Jobs** |

## Packages to Avoid

| Package | Why | Use Instead |
|---------|-----|-------------|
| UGUI (Canvas) | Deprecated for new projects | UI Toolkit |
| Legacy Particle System | Deprecated | Visual Effect Graph |
| Legacy Animation | Deprecated | Animator Controller |
| Built-in Render Pipeline | No new features | URP |

**Sources:**
- https://docs.unity3d.com/6000.0/Documentation/Manual/PackagesList.html
