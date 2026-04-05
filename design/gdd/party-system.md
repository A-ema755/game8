# Party System

## 1. Overview

The Party System manages the player's active team of 4–6 creatures and their unlimited creature storage. Party order determines which creature enters combat first. Players can swap creatures between the active party and storage at research stations, or between encounters on the campaign map. Each creature type provides a distinct field ability usable on the campaign map to solve environmental obstacles and unlock hidden content. The `PartyState` class persists both the active party and storage through the save system.

## 2. Player Fantasy

Your party is a deliberate curation, not an accident. Every slot has a cost — the creature you leave in storage can't solve a puzzle or contribute to a battle today. Choosing which six to carry into a new habitat zone, knowing the terrain ahead favors Water types and that your Fire creature opens shortcuts, makes roster management feel like expedition planning. Swapping at a research station between fights has the same satisfying weight as making a substitution at a critical moment.

## 3. Detailed Rules

### 3.1 Active Party

- The active party holds between 1 and `MaxPartySize` creatures (default 6, minimum enforced is 1 — the player can never have zero party creatures).
- Party slots are ordered (slot 0 through slot N-1). Slot 0 is the "lead" creature and enters combat first.
- If the lead creature is fainted, slot 1 becomes the new lead on the next battle's entry.
- Party order is drag-reordered in the Party Management UI.
- Difficulty setting can reduce `MaxPartySize` to 4 for a harder experience.

### 3.2 Storage

- Storage is unlimited. No cap on stored creatures.
- Storage is only accessible at research stations, not during encounters or in the field.
- Creatures in storage do not gain XP, do not participate in expeditions unless explicitly dispatched, and cannot use field abilities.
- Depositing to storage: always allowed as long as at least 1 creature remains in the active party.
- Withdrawing from storage: always allowed as long as the active party has at least one open slot.

### 3.3 Swapping Rules

**At a research station:**
- Player can freely swap any creature between active party and storage.
- Swaps are unlimited per station visit.
- No cost, no cooldown.

**Between encounters (campaign map, no station present):**
- Player can reorder the active party (drag slots) but cannot pull from or deposit to storage.
- This lets players adjust lead order before entering a known battle without full access to the bench.

**During battle:**
- Switching the active creature costs that creature's turn.
- Only party creatures can be switched in — storage is inaccessible in battle.
- A fainted creature cannot be switched in.

### 3.4 Field Abilities

Each creature type has one field ability. Field abilities are usable on the campaign map when the creature is in the active party (not stored). The player taps/clicks on a compatible obstacle node on the map and selects the creature to use.

| Creature Type | Field Ability | Map Effect |
|--------------|---------------|------------|
| Fire | Burn Through | Clears forest blockades, ice walls, and thorny barriers |
| Water | Ford | Allows the full party to cross water terrain nodes |
| Electric | Power Up | Activates dead generators, opens electric-sealed doors |
| Flying | Scout | Reveals adjacent hidden encounter nodes (1 node radius) |
| Psychic | Sense | Highlights rare creature spawn nodes within 2 nodes |
| Grass | Overgrow | Creates vine bridges across chasms |
| Rock | Excavate | Clears rockslide blockades, opens dig-site nodes |
| Ice | Freeze | Freezes waterfall nodes to create ice bridges |

Only one use of a field ability is needed per obstacle — the ability does not consume PP or have a cooldown. The creature must be conscious (not fainted) to use its field ability.

If a creature has a secondary type via DNA infusion, it does not gain the secondary type's field ability — field abilities are keyed to the creature's primary native type from `CreatureConfig`.

### 3.5 PartyState Class

```csharp
[System.Serializable]
public class PartyState
{
    public List<CreatureInstance> activeParty;   // Ordered; max MaxPartySize
    public List<CreatureInstance> storage;       // Unordered; unlimited

    public int ActiveCount => activeParty.Count;
    public int StorageCount => storage.Count;

    /// <summary>Returns the lead creature (slot 0), or null if party is empty.</summary>
    public CreatureInstance Lead => activeParty.Count > 0 ? activeParty[0] : null;

    /// <summary>Returns true if the active party has at least one conscious creature.</summary>
    public bool HasConscious => activeParty.Any(c => !c.isFainted);

    /// <summary>Advances to next conscious creature if lead is fainted.</summary>
    public void PromoteNextConscious()
    {
        while (activeParty.Count > 0 && activeParty[0].isFainted)
        {
            activeParty.Add(activeParty[0]);
            activeParty.RemoveAt(0);
        }
    }
}
```

### 3.6 Party Wipe

If all active party creatures are fainted (`HasConscious == false`), the encounter ends in a loss. Consequences:
- Player is returned to the last visited research station.
- All creatures are revived to 1 HP (no permanent faint in normal mode).
- A small RP penalty is applied (default: 20% of RP earned since last station, minimum 0).
- In Permadeath Mode: fainted = permanently dead; party wipe ends the run.

### 3.7 Party in Combat

- At combat start, the lead creature (slot 0) is placed on the player's starting tile.
- The player may manually switch during their turn, costing that turn.
- Switching in a creature that has not participated yet this battle does not grant XP participation (it must act at least once to count as a participant).
- A creature switched out mid-battle retains all status effects, current HP, and any move PP spent.

## 4. Formulas

### Party Wipe RP Penalty

```
rpPenalty = Floor(rpEarnedSinceLastStation * 0.20)
rpPenalty = Max(rpPenalty, 0)
```

### Field Ability Range (Scout/Sense)

```
scoutRadius = 1 node        // Flying: reveals 1-node radius
senseRadius = 2 nodes       // Psychic: highlights rare nodes within 2
```

Radius is measured in campaign map graph edges, not world distance.

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Player tries to deposit last conscious party creature | Blocked; message: "You must keep at least one conscious creature in your party" |
| Player captures a creature and party is full (6 slots) | Capture succeeds; player prompted to choose one to send to storage before continuing |
| Player captures a creature but storage is somehow empty and party is full | Storage is always available; prompt appears; no restriction on storage capacity |
| Lead creature faints mid-battle | `PromoteNextConscious()` called; next conscious creature in slot order becomes lead |
| All party creatures faint simultaneously (e.g. area attack) | Party wipe triggered immediately; encounter ends |
| Player reorders party between encounters to put a fainted creature in slot 0 | Allowed in UI; combat will automatically promote next conscious creature at battle start |
| Creature uses field ability but is at 1 HP (not fainted) | Allowed; field ability is not blocked by low HP |
| Two different creature types in party can both solve same obstacle | Player chooses which to use; either is valid |
| DNA type infusion gives creature secondary Water type; player expects Ford ability | Ford not granted; field ability is keyed to native primary type only; tooltip explains this |
| MaxPartySize changed by difficulty setting mid-campaign | If current party count exceeds new max, excess creatures are automatically moved to storage (last slots first) with a notification |
| Creature dispatched on an expedition is recalled to active party | Expedition is cancelled; creature returns immediately; any partial rewards are forfeited |

## 6. Dependencies

| System | Dependency Type | Notes |
|--------|----------------|-------|
| Creature Instance | Read/Write | HP, faint status, XP participation flag |
| Encounter System | Read | Battle entry uses party lead; party wipe trigger |
| Leveling / XP System | Read | Participation tracking per battle |
| Campaign Map | Read | Field ability compatibility with obstacle node types |
| Research Station | Read | Enables storage access, swap interface |
| Capture System | Write | Captured creature added to party or prompts storage |
| Save/Load System | Read/Write | Full `PartyState` serialized to save file |
| Party Management UI | Read/Write | Drag-reorder, swap, field ability interface |
| Expedition System | Read | Dispatched creatures removed from storage temporarily |

## 7. Tuning Knobs

| Parameter | Default | Range | Notes |
|-----------|---------|-------|-------|
| `MaxPartySize` | 6 | 4–6 | Difficulty setting can reduce to 4 |
| `MinPartySize` | 1 | 1 | Always enforced; player cannot empty party |
| `PartyWipeRpPenaltyFraction` | 0.20 | 0.0–0.50 | Fraction of recent RP lost on wipe |
| `ScoutRevealRadius` | 1 | 1–3 | Nodes revealed by Flying field ability |
| `SenseHighlightRadius` | 2 | 1–4 | Nodes highlighted by Psychic field ability |
| `SwitchCostsTurn` | true | bool | If false, switching is free (easier difficulty) |

## 8. Acceptance Criteria

- [ ] Active party enforces `MaxPartySize`; depositing the last conscious creature is blocked.
- [ ] Party order is drag-reorderable in the Party Management UI and persists after the screen is closed.
- [ ] Storage has no size cap; 50+ creatures can be stored without errors.
- [ ] Storage is inaccessible during battle and on the campaign map outside of research stations.
- [ ] Switching a creature in battle consumes that creature's turn.
- [ ] `PromoteNextConscious()` correctly advances past all fainted creatures to find the next conscious one.
- [ ] Party wipe triggers when all party creatures are fainted; RP penalty is correctly calculated.
- [ ] Revive-to-1HP on wipe is applied to all fainted party creatures.
- [ ] Field abilities are keyed to native primary type; DNA-infused secondary types do not grant field abilities.
- [ ] Each field ability correctly unlocks or solves its designated obstacle node type on the campaign map.
- [ ] Capturing a creature when party is full triggers the storage prompt before continuing.
- [ ] Full `PartyState` (active party order, storage contents) persists through a save/load cycle.
- [ ] Difficulty-reduced `MaxPartySize` of 4 is respected; excess creatures auto-move to storage with notification.
