# Unity 6.3 — Physics Module Reference

**Last verified:** 2026-02-13
**Knowledge Gap:** Unity 6 physics improvements, solver changes

**Gene Forge context:** Isometric 3D grid combat on ProBuilder tile geometry. Creature
movement is grid-snapped (no physics-driven locomotion). Physics is used for:
- VFX / projectile trajectories (ability projectiles)
- Ragdoll death effects (optional)
- Environmental props (destructible tiles, knockback debris)
- Tile raycasting for mouse/cursor tile-picking

Creatures do NOT use Rigidbody for movement. Use `transform.position` lerp or
`NavMeshAgent` (non-combat) only.

---

## Overview

Unity 6.3 uses **PhysX 5.1** (improved from PhysX 4.x in 2022 LTS):
- Better solver stability
- Improved performance
- Enhanced collision detection

---

## Key Changes from 2022 LTS

### Default Solver Iterations Increased

```csharp
// Default changed from 6 to 8 iterations
Physics.defaultSolverIterations = 8;
```

### Enhanced Collision Detection

```csharp
// ✅ Unity 6: Improved CCD for fast-moving objects (ability projectiles)
rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
```

---

## Core Physics Components

### Rigidbody

```csharp
// Gene Forge: only use Rigidbody on VFX projectiles and ragdoll parts
Rigidbody rb = GetComponent<Rigidbody>();
rb.AddForce(launchDir * force, ForceMode.Impulse);

// ❌ Avoid: Direct velocity assignment
rb.velocity = new Vector3(0, 10, 0); // Only if absolutely necessary
```

### Colliders

```csharp
// Primitive colliders: Box, Sphere, Capsule — cheapest, prefer these
// Gene Forge: tile colliders = BoxCollider; creature hit colliders = CapsuleCollider
// Mesh colliders: expensive, use only for complex static ProBuilder tiles
```

---

## Raycasting

### Tile Picking (Isometric Mouse Cursor)

```csharp
// Gene Forge core pattern: convert screen pos to iso tile via raycast
public Vector2Int? ScreenPosToTile(Vector2 screenPos) {
    Ray ray = isoCamera.ScreenPointToRay(screenPos);
    int tileLayer = 1 << LayerMask.NameToLayer("Tile");

    if (Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayer)) {
        return hit.collider.GetComponent<TileCell>()?.GridPos;
    }
    return null;
}
```

### Efficient Raycasting (Avoid Allocations)

```csharp
// ✅ Non-allocating raycast
if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance)) {
    Debug.Log($"Hit: {hit.collider.name}");
}

// ✅ Multiple hits (non-allocating)
RaycastHit[] results = new RaycastHit[10];
int hitCount = Physics.RaycastNonAlloc(origin, direction, results, maxDistance);

// ❌ Avoid: RaycastAll (allocates every call)
RaycastHit[] hits = Physics.RaycastAll(origin, direction); // GC allocation!
```

### LayerMask Filtering

```csharp
// Gene Forge layers: "Tile", "Creature", "Projectile", "UI3D"
int tileLayer = 1 << LayerMask.NameToLayer("Tile");
Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, tileLayer);
```

---

## Physics Queries

### OverlapSphere (AOE Ability Detection)

```csharp
// Gene Forge: detect creatures in AOE ability radius
Collider[] results = new Collider[16];
int count = Physics.OverlapSphereNonAlloc(
    abilityOrigin,
    abilityRadius,
    results,
    1 << LayerMask.NameToLayer("Creature")
);

for (int i = 0; i < count; i++) {
    results[i].GetComponent<Creature>()?.TakeDamage(damage);
}
```

### SphereCast (Projectile Thick Raycast)

```csharp
if (Physics.SphereCast(origin, projectileRadius, direction, out RaycastHit hit, maxDistance)) {
    // Gene Forge: projectile hit detection
}
```

---

## Collision Events

### OnTriggerEnter (Projectile Hit Zone)

```csharp
// Gene Forge: ability projectile trigger on hit
void OnTriggerEnter(Collider other) {
    if (other.CompareTag("Creature")) {
        Creature target = other.GetComponent<Creature>();
        combatResolver.ResolveProjectileHit(caster, target, abilityData);
        Destroy(gameObject);
    }
}
```

### OnCollisionEnter (Physics Debris)

```csharp
void OnCollisionEnter(Collision collision) {
    // Gene Forge: debris impact SFX on tile surface
    AudioSource.PlayClipAtPoint(impactClip, collision.contacts[0].point);
}
```

---

## Knockback (Ability Effect)

```csharp
// Gene Forge: apply knockback impulse to creature ragdoll or rigidbody
void ApplyKnockback(GameObject target, Vector3 sourcePos, float force) {
    Rigidbody rb = target.GetComponent<Rigidbody>();
    if (rb != null) {
        Vector3 dir = (target.transform.position - sourcePos).normalized;
        rb.AddForce(dir * force, ForceMode.Impulse);
    }
}
```

---

## Explosion Force (AOE Ability VFX)

```csharp
void ApplyExplosionVFX(Vector3 explosionPos, float radius, float force) {
    Collider[] colliders = Physics.OverlapSphere(explosionPos, radius);
    foreach (Collider hit in colliders) {
        Rigidbody rb = hit.GetComponent<Rigidbody>();
        if (rb != null) {
            rb.AddExplosionForce(force, explosionPos, radius);
        }
    }
}
```

---

## Performance Optimization

### Physics Layer Collision Matrix
`Edit > Project Settings > Physics > Layer Collision Matrix`
- Disable Creature vs. Creature (grid handles spacing)
- Enable Tile vs. Projectile
- Enable Creature vs. Projectile
- Disable UI3D vs. everything physics

### Fixed Timestep
`Edit > Project Settings > Time > Fixed Timestep`
- Default: 0.02 (50 FPS physics)
- Gene Forge: 0.02 is fine; no physics-heavy simulation needed

### Simplified Collision Geometry
- ProBuilder tiles: use BoxCollider, not MeshCollider
- Creature hit zone: CapsuleCollider on root

---

## Tile-Based Ground Check (If Needed)

```csharp
bool IsGrounded() {
    float rayLength = 0.15f;
    return Physics.Raycast(
        transform.position,
        Vector3.down,
        rayLength,
        1 << LayerMask.NameToLayer("Tile")
    );
}
```

---

## Debugging

### Physics Debugger (Unity 6+)
- `Window > Analysis > Physics Debugger` — visualize colliders, contacts, queries

### Gizmos

```csharp
void OnDrawGizmos() {
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, abilityDetectionRadius);
}
```

---

## Sources
- https://docs.unity3d.com/6000.0/Documentation/Manual/PhysicsOverview.html
- https://docs.unity3d.com/ScriptReference/Physics.html
