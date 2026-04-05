# Campaign Map

## 1. Overview

The campaign map is a branching node-based progression screen divided into five habitat zones. Each zone presents 8–20 nodes arranged in a directed acyclic graph where players choose their path through mandatory encounters, optional diversions, and landmark events. Nodes are connected by visible paths; branching points let the player commit to one route at a time. Zones unlock sequentially after the previous zone's climax (rival battle or trophy creature) is cleared. The MVP ships with one fully-realized zone — Verdant Basin — containing 8–12 nodes.

## 2. Player Fantasy

The player feels like an expedition leader charting unknown wilderness. Every fork in the path is a meaningful choice: take the safe trainer route for guaranteed RP, or risk a nest encounter for an egg with rare innate DNA. The map communicates the world's ecology at a glance — predators patrol certain corridors, migration paths pulse with moving icons, and completed nodes show trophy badges. By the end of a zone, the player should feel they've genuinely explored a living habitat rather than advanced through a menu.

## 3. Detailed Rules

### Zone Structure
- Each zone is a separate MapZone asset containing an ordered list of MapNode assets.
- Nodes connect via directed edges forming a DAG. Multiple paths may converge at the zone boss node.
- The player can only traverse edges in the forward direction; backtracking to previous nodes is allowed for revisiting (non-combat nodes only; encounter nodes become inert after completion).

### Node Types
| Node Type | Icon | Repeatable | Description |
|-----------|------|-----------|-------------|
| Wild Encounter | Paw print | No | 1–3 wild creatures; can capture. Drops DNA materials + RP. |
| Trainer Battle | Figure silhouette | No | NPC trainer; cannot capture. Drops RP + story beat. |
| Nest | Egg | No | Protect eggs from predators (wave encounter). Rewards creature egg. |
| Trophy | Star | No | Optional superboss. Extremely tough. Unique DNA recipe reward. |
| Research Station | Flask | Yes | Safe zone: heal, DNA mods, party swap, Pokedex, store. |
| DNA Vault | Padlock | No | Requires environmental puzzle. Contains Forbidden Mods + vault guardian fight. |
| Environmental Puzzle | Gear | No | Field ability puzzle; rewards shortcut unlock or hidden content. |

### Path Rules
- At least one valid path from zone start to zone boss must exist that includes only Wild Encounter + Research Station nodes (accessible without field abilities or special traps).
- Trophy nodes are always optional side branches.
- DNA Vault nodes are always behind Environmental Puzzle nodes.

### Zone Boss Node
- Final node of every zone is either a Rival Battle (story fight) or a Trophy node.
- Clearing the boss node unlocks the next zone and awards a zone completion badge.

### Verdant Basin Layout (MVP)
```
[Start/Station] -- [Wild] -- [Wild] -- [Station]
                        |                   |
                    [Trainer]           [Nest]
                        |                   |
                    [Wild] ----------- [Wild]
                                          |
                                    [Station] -- [Trophy(opt)]
                                          |
                                     [Rival Boss]
```
Total nodes: 10 (2 optional). 3 mandatory Research Stations.

### Node State Machine
```csharp
public enum NodeState { Locked, Available, Completed, InProgress }

[System.Serializable]
public class MapNode
{
    public string nodeId;
    public NodeType nodeType;
    public NodeState state;
    public List<string> prerequisiteNodeIds; // must be Completed
    public List<string> connectedNodeIds;    // outbound edges
    public string encounterConfigId;         // links to EncounterConfig asset
    public bool isOptional;
}
```

### Availability Logic
```csharp
public bool IsAvailable(MapNode node, Dictionary<string, NodeState> zoneState)
{
    if (node.state == NodeState.Completed) return true;
    return node.prerequisiteNodeIds.All(id => zoneState[id] == NodeState.Completed);
}
```

### Zone Progression
```csharp
[CreateAssetMenu(menuName = "GeneForge/MapZone")]
public class MapZoneConfig : ScriptableObject
{
    public string zoneId;           // e.g., "verdant-basin"
    public string displayName;
    public List<MapNode> nodes;
    public string bossNodeId;
    public Sprite mapBackgroundSprite;
    public CreatureType dominantType;
    public ZoneWeather defaultWeather; // post-MVP
}
```

### Entering a Node
1. Player taps a node with state = Available.
2. Confirm dialog appears for encounter nodes.
3. Scene transition loads the encounter (Combat scene) or activates the station overlay.
4. On return, node.state = Completed; downstream nodes become Available.

## 4. Formulas

**Zone Completion Percentage:**
```
completion% = (completedNonOptionalNodes / totalNonOptionalNodes) * 100
```

**RP Bonus on Full Zone Clear (all nodes including optional):**
```
bonusRP = baseZoneBonus * 1.5
```
Where `baseZoneBonus` is defined per zone in MapZoneConfig.

**Node Unlock Distance** (UI label for how many hops away a locked node is):
```
hops = BFS distance from nearest completed node to target node
```

## 5. Edge Cases

- **All paths blocked:** If every forward edge from the player's current position requires a prerequisite that hasn't been met (e.g., missing a required field ability), display a warning tooltip. At least one path must always be passable per design constraint above.
- **Revisiting completed encounter nodes:** Node appears grayed with a checkmark. Player may enter but encounter does not replay; node shows a summary of rewards earned.
- **Trophy node after zone boss:** Some zones may have post-boss trophy nodes. These unlock when the boss node is completed, regardless of other paths.
- **Zone not yet unlocked:** Node graph is shown with fog of war (silhouette icons only). Node count is visible but types are hidden until the zone is unlocked.
- **Saving mid-zone:** Save occurs after every node completion and on Research Station entry. Crash recovery resumes at the last completed node.
- **Rival encounter as final node:** If the rival is the boss node, defeating or losing still marks the node as completed (story progresses regardless). Losing awards reduced RP.

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Save/Load System | Persists node states, zone progress, current zone index |
| Encounter System | Provides EncounterConfig loaded when entering encounter nodes |
| Research Station | Activated when entering station nodes; uses StationUpgradeConfig |
| Game State Manager | Handles scene transition from map to combat and back |
| Living Ecosystem | Reads zone ecosystem state to modify wild encounter species pools |
| Environmental Puzzle System | Gates DNA Vault nodes; consumes field ability resources |
| Rival Trainer System | Provides rival encounter config for boss nodes |
| UI Shell | Renders the map node graph; handles node selection, path drawing |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `nodesPerZone` | 10–14 | Including optional |
| `mandatoryResearchStations` | 3 | Minimum safe zones per zone |
| `optionalNodeFraction` | 0.2 | ~20% of nodes are optional |
| `baseZoneCompletionBonus` | 300 RP | Awarded for completing all mandatory nodes |
| `fullClearBonusMultiplier` | 1.5x | Applied to base bonus if all nodes cleared |
| `trophyNodeRewardTier` | "rare" | DNA material rarity from trophy |
| `fogOfWarEnabled` | true | Hides future zone node types |
| `backtrackAllowed` | true | Player can revisit completed non-combat nodes |

## 8. Acceptance Criteria

- [ ] Verdant Basin loads with exactly 8–12 nodes arranged in a DAG with no cycles.
- [ ] At least one path from start to boss node requires no special field abilities.
- [ ] Completing a node transitions it to Completed state and unlocks all directly connected forward nodes.
- [ ] Trophy and DNA Vault nodes are always optional (no blocking path runs through them exclusively).
- [ ] Saving and reloading restores exact node states for the current zone.
- [ ] Zone completion percentage displays correctly in the UI after each node.
- [ ] Entering a completed encounter node shows the summary screen without replaying the encounter.
- [ ] Zone boss node completion triggers the next zone unlock animation and saves.
- [ ] Fog of war correctly hides node types for locked zones and reveals them on unlock.
- [ ] MapZoneConfig ScriptableObject validates that bossNodeId exists in the nodes list (editor validation).
