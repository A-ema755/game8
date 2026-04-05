# Unity 6.3 — Networking Module Reference

**Last verified:** 2026-02-13
**Knowledge Gap:** Unity 6 uses Netcode for GameObjects (UNet deprecated)

**Gene Forge context:** Gene Forge v1 is **single-player only**. This module is provided for
reference if multiplayer (async PvP or co-op) is added in a future milestone. Do not
implement networking in the core game without explicit GDD approval. The server-authoritative
patterns here are still useful as a mental model for the turn-based combat authority model
(local GameManager acts as "server").

---

## Overview

Unity 6 networking options:
- **Netcode for GameObjects** (RECOMMENDED): Official Unity multiplayer framework
- **Mirror**: Community-driven (UNet successor)
- **Photon**: Third-party service (PUN2)
- **Custom**: Low-level sockets

**UNet (Legacy)**: Deprecated, do not use.

---

## Netcode for GameObjects

### Installation
1. `Window > Package Manager`
2. Search "Netcode for GameObjects"
3. Install `com.unity.netcode.gameobjects`

---

## Basic Setup

### NetworkManager

```csharp
using Unity.Netcode;

public class CustomNetworkManager : MonoBehaviour {
    void Start() {
        NetworkManager.Singleton.StartHost();   // Server + client
        // OR
        NetworkManager.Singleton.StartServer(); // Dedicated server
        // OR
        NetworkManager.Singleton.StartClient(); // Client only
    }
}
```

---

## NetworkObject (Networked GameObjects)

### Mark GameObject as Networked

1. Add `NetworkObject` component to GameObject
2. Must be at root of prefab (not nested)
3. Register prefab in `NetworkManager > NetworkPrefabs List`

### Spawn Network Objects

```csharp
using Unity.Netcode;

public class GameManager : NetworkBehaviour {
    public GameObject creaturePrefab;

    // Gene Forge (future): spawn a creature for a player
    [ServerRpc(RequireOwnership = false)]
    public void SpawnCreatureServerRpc(ulong clientId) {
        GameObject creature = Instantiate(creaturePrefab);
        creature.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }
}
```

---

## NetworkBehaviour (Networked Scripts)

```csharp
using Unity.Netcode;

public class Creature : NetworkBehaviour {
    public override void OnNetworkSpawn() {
        if (IsOwner) {
            // Only run on owner's client
        }
    }

    void Update() {
        if (!IsOwner) return;
    }

    [ServerRpc]
    void SubmitActionServerRpc(int abilityIndex, Vector2Int targetTile) {
        // Gene Forge (future): validate and process creature turn action on server
    }
}
```

---

## Network Variables (Synchronized State)

### NetworkVariable<T>

```csharp
using Unity.Netcode;

public class Creature : NetworkBehaviour {
    // Gene Forge (future): sync live HP across clients
    private NetworkVariable<int> currentHP = new NetworkVariable<int>(100);

    public override void OnNetworkSpawn() {
        currentHP.OnValueChanged += OnHPChanged;
    }

    void OnHPChanged(int oldValue, int newValue) {
        hpBar.UpdateDisplay(newValue);
    }

    [ServerRpc]
    public void TakeDamageServerRpc(int damage) {
        currentHP.Value -= damage;
    }
}
```

### NetworkVariable Permissions

```csharp
// Server writes, clients read (default)
private NetworkVariable<int> hp = new NetworkVariable<int>();

// Owner writes (e.g., local creature action state)
private NetworkVariable<int> actionState = new NetworkVariable<int>(
    default,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Owner
);
```

---

## RPCs (Remote Procedure Calls)

### ServerRpc (Client to Server)

```csharp
[ServerRpc]
void SelectAbilityServerRpc(int slotIndex) {
    // Runs on server — validate ability selection
}

// Call from client owner:
SelectAbilityServerRpc(2);
```

### ClientRpc (Server to All Clients)

```csharp
[ClientRpc]
void PlayAbilityVFXClientRpc(Vector3 position, int abilityId) {
    // Gene Forge (future): trigger ability VFX on all clients
    vfxManager.PlayAbility(abilityId, position);
}
```

### RPC Parameters

```csharp
// ✅ Supported: Primitives, structs, strings, arrays
[ServerRpc]
void SubmitMoveServerRpc(Vector2Int tilePos) { }

// ❌ Not supported: MonoBehaviour, GameObject (use NetworkObjectReference)
```

---

## Network Ownership

```csharp
if (IsOwner)       { /* This client owns this NetworkObject */ }
if (IsServer)      { /* Running on server */ }
if (IsClient)      { /* Running on client */ }
if (IsLocalPlayer) { /* This is the local player object */ }

// Transfer ownership (server only)
GetComponent<NetworkObject>().ChangeOwnership(newOwnerClientId);
```

---

## NetworkObjectReference (Pass GameObjects in RPCs)

```csharp
[ServerRpc]
void AttackCreatureServerRpc(NetworkObjectReference targetRef) {
    if (targetRef.TryGet(out NetworkObject target)) {
        target.GetComponent<Creature>().TakeDamageServerRpc(attackDamage);
    }
}

// Call:
AttackCreatureServerRpc(targetCreature.GetComponent<NetworkObject>());
```

---

## Server-Authoritative Pattern (RECOMMENDED)

```csharp
// Gene Forge mental model: local GameManager == server authority for turn processing
// Client submits intent → server validates → server applies → clients display

public class Creature : NetworkBehaviour {
    private NetworkVariable<Vector3> worldPosition = new NetworkVariable<Vector3>();

    void Update() {
        if (IsOwner) {
            // Client: send tile selection intent
        }
        // All clients: display synced position
        transform.position = worldPosition.Value;
    }

    [ServerRpc]
    void RequestMoveServerRpc(Vector2Int targetTile) {
        if (gridPathfinder.IsValidMove(GridPos, targetTile)) {
            worldPosition.Value = GridToWorld(targetTile);
        }
    }
}
```

---

## Connection Events

```csharp
void Start() {
    NetworkManager.Singleton.OnClientConnectedCallback  += OnClientConnected;
    NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
}

void OnClientConnected(ulong clientId)    { Debug.Log($"Client {clientId} connected"); }
void OnClientDisconnected(ulong clientId) { Debug.Log($"Client {clientId} disconnected"); }
```

---

## Performance Tips

- Use `NetworkVariable` for state that changes infrequently (HP, status effects)
- Batch multiple changes before syncing (end-of-turn state flush)
- Use delta compression for large data

---

## Debugging

- `Window > Analysis > Network Profiler` — bandwidth, RPC calls, variable updates
- `NetworkManager > Network Simulator` — simulate latency / packet loss

---

## Sources
- https://docs-multiplayer.unity3d.com/netcode/current/about/
- https://docs-multiplayer.unity3d.com/netcode/current/learn/bossroom/
