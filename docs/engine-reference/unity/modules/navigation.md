# Unity 6.3 — Navigation Module Reference

**Last verified:** 2026-02-13
**Knowledge Gap:** Unity 6 NavMesh improvements

**Gene Forge context:** Gene Forge uses a **custom isometric grid** (ProBuilder tiles) for
creature movement, not NavMesh pathfinding. NavMesh is NOT used for creature turn movement.
Use this module only for non-grid AI scenarios (e.g., ambient creatures in exploration,
cutscene pawns). Grid pathfinding is handled by the GDD's GridPathfinder system (A* on
tile graph). See grid-combat GDD docs for tile movement rules.

---

## Overview

Unity 6 navigation systems:
- **NavMesh**: Built-in pathfinding for AI agents
- **NavMeshComponents**: Package for runtime NavMesh building

**Gene Forge note:** Creature combat movement uses a custom A* tile graph, not NavMesh.
NavMesh may be used for overworld/exploration free-roam NPCs only.

---

## NavMesh Basics

### Bake Navigation Mesh

1. Mark walkable surfaces:
   - Select GameObject (floor/terrain)
   - Inspector > Navigation > Object tab
   - Check "Navigation Static"

2. Bake NavMesh:
   - `Window > AI > Navigation`
   - Bake tab > Click "Bake"

3. Configure settings:
   - **Agent Radius**: 0.5m default
   - **Agent Height**: 2m default
   - **Max Slope**: 45° default
   - **Step Height**: 0.4m default

---

## NavMeshAgent (AI Movement)

### Basic Agent Setup

```csharp
using UnityEngine;
using UnityEngine.AI;

public class ExplorationNPC : MonoBehaviour {
    private NavMeshAgent agent;
    public Transform wanderTarget;

    void Start() {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update() {
        agent.SetDestination(wanderTarget.position);
    }
}
```

### NavMeshAgent Properties

```csharp
NavMeshAgent agent = GetComponent<NavMeshAgent>();

agent.speed = 3.5f;
agent.acceleration = 8f;
agent.stoppingDistance = 2f;
agent.autoBraking = true;
agent.angularSpeed = 120f;
agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
```

### Check Path Status

```csharp
void Update() {
    agent.SetDestination(target.position);

    if (agent.hasPath) {
        if (agent.pathStatus == NavMeshPathStatus.PathComplete) {
            Debug.Log("Valid path");
        } else if (agent.pathStatus == NavMeshPathStatus.PathPartial) {
            Debug.Log("Partial path (destination unreachable)");
        }
    }

    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance) {
        Debug.Log("Reached destination");
    }
}
```

### Calculate Path (Don't Move Yet)

```csharp
NavMeshPath path = new NavMeshPath();
agent.CalculatePath(targetPosition, path);

if (path.status == NavMeshPathStatus.PathComplete) {
    agent.SetPath(path);
}
```

---

## NavMesh Areas (Walkable Costs)

```csharp
// Prefer walkable areas only
agent.areaMask = 1 << NavMesh.GetAreaFromName("Walkable");
```

---

## NavMesh Obstacles (Dynamic Obstacles)

```csharp
// Add: GameObject > Add Component > NavMesh Obstacle
NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();
obstacle.carving = true; // Create dynamic hole in NavMesh
```

---

## Off-Mesh Links (Jumps, Teleports)

### Detect Off-Mesh Link Traversal

```csharp
void Update() {
    if (agent.isOnOffMeshLink) {
        StartCoroutine(TraverseOffMeshLink());
    }
}

IEnumerator TraverseOffMeshLink() {
    OffMeshLinkData data = agent.currentOffMeshLinkData;
    Vector3 startPos = agent.transform.position;
    Vector3 endPos = data.endPos;
    float duration = 0.5f;
    float elapsed = 0f;

    while (elapsed < duration) {
        agent.transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
        elapsed += Time.deltaTime;
        yield return null;
    }

    agent.CompleteOffMeshLink();
}
```

---

## NavMeshComponents Package (Runtime Baking)

### Installation
`Window > Package Manager` > Add from Git URL: `com.unity.ai.navigation`

### Runtime NavMesh Baking

```csharp
using Unity.AI.Navigation;

public class NavMeshBuilder : MonoBehaviour {
    public NavMeshSurface surface;

    void Start() {
        surface.BuildNavMesh();
    }

    void UpdateNavMesh() {
        surface.UpdateNavMesh(surface.navMeshData);
    }
}
```

---

## Gene Forge: Grid Pathfinding (NOT NavMesh)

For combat creature movement, use the custom tile graph:

```csharp
// Gene Forge pattern — do NOT use NavMeshAgent for creatures in battle
// Use GridPathfinder.FindPath(Vector2Int from, Vector2Int to, int maxSteps)
List<Vector2Int> path = gridPathfinder.FindPath(creature.GridPos, targetTile, creature.Stats.SPD);
StartCoroutine(creature.WalkPath(path));
```

See: `Assets/Scripts/Grid/GridPathfinder.cs` and GDD `combat-movement.md`.

---

## Common Patterns

### Patrol Between Waypoints (Exploration NPC)

```csharp
public Transform[] waypoints;
private int currentWaypoint = 0;

void Update() {
    if (!agent.pathPending && agent.remainingDistance < 0.5f) {
        currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
        agent.SetDestination(waypoints[currentWaypoint].position);
    }
}
```

---

## Performance Tips

- **Limit Obstacle Avoidance Quality**: Use `LowQualityObstacleAvoidance` for distant agents
- **Update Frequency**: Don't call `SetDestination()` every frame if target hasn't moved
- **Area Masks**: Limit walkable areas to reduce pathfinding search space

---

## Debugging

### NavMesh Visualization
`Window > AI > Navigation > Bake tab` — Check "Show NavMesh"

### Agent Path Gizmos

```csharp
void OnDrawGizmos() {
    if (agent != null && agent.hasPath) {
        Gizmos.color = Color.green;
        Vector3[] corners = agent.path.corners;
        for (int i = 0; i < corners.Length - 1; i++) {
            Gizmos.DrawLine(corners[i], corners[i + 1]);
        }
    }
}
```

---

## Sources
- https://docs.unity3d.com/6000.0/Documentation/Manual/Navigation.html
- https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/index.html
