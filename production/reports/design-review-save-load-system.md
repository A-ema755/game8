# Design Review: Save / Load System

**File**: `design/gdd/save-load-system.md`
**Reviewed**: 2026-04-05
**Reviewer**: design-review pipeline (automated)

---

## Section Completeness Check

| # | Required Section | Present? | Notes |
|---|-----------------|----------|-------|
| 1 | Overview | Yes | Complete one-paragraph summary |
| 2 | Player Fantasy | Yes | Clear, focused on invisible reliability |
| 3 | Detailed Rules | Yes | 5 subsections with code and tables |
| 4 | Formulas | Yes | Explicitly states "no mathematical formulas" — valid for I/O system |
| 5 | Edge Cases | Yes | 11 cases with explicit behavior |
| 6 | Dependencies | Yes | 8 entries with direction |
| 7 | Tuning Knobs | Yes | 6 parameters |
| 8 | Acceptance Criteria | Yes | 12 testable criteria |

All 8 required sections present.

---

## Findings

### CRITICAL

[CRITICAL] Detailed Rules §3.3 > `SaveLoadManager` is a MonoBehaviour singleton using `Instance` pattern, but ADR-003 in technical-preferences.md does not list `SaveLoadManager` as an approved singleton. ADR-003 approves: GameStateManager, ConfigLoader, AudioManager, SettingsManager. > Either add `SaveLoadManager` to the approved singleton list in ADR-003 (with justification), or redesign as a non-singleton service resolved via DI/serialized reference. This is a coding standards violation.

### MAJOR

[MAJOR] Overview > No explicit pillar alignment statement. The save/load system is infrastructure that serves all four pillars (Living World most directly — ecosystem state persistence), but this is not stated. > Add pillar alignment note identifying which pillars this system supports and why.

[MAJOR] Detailed Rules §3.2 > `CreatureSaveData` stores `personalityTrait` as `PersonalityTrait` enum directly, but `SlotPartPair` stores its enum as `int`. Inconsistent serialization strategy — `JsonUtility` handles enums as ints, but the declared type difference is confusing for implementers. > Standardize: either all enums stored as `int` with cast comments, or all stored as typed enums with a note that `JsonUtility` serializes enums as underlying int. Add a design note clarifying the chosen approach.

[MAJOR] Detailed Rules §3.2 > `EcosystemSaveData` only stores `populations` and `conservationScore`. The Living Ecosystem system (per game-pillars.md) also tracks migration cycles, predator/prey dynamics, and weather state per zone. These are missing from the save data. > Add fields for migration phase, zone weather state, and predator/prey balance data to `EcosystemSaveData`, or document that these are derived from `populations` on load. If derived, explain the derivation.

[MAJOR] Detailed Rules §3.2 > `MetaState` stores `instituteRank` (0-4) and `stationUpgradeLevel` (1-5) but no field for black market usage tracking. The Institute Rank System and game pillars reference ethical consequences from black market DNA usage, which must persist across sessions. > Add `blackMarketTransactionCount` or equivalent field to `MetaState`, or document where this is tracked if elsewhere.

[MAJOR] Detailed Rules §3.3 > `OnApplicationQuit` is mentioned in §3.4 auto-save triggers table but is not implemented in the code. The `SaveLoadManager` code only implements `OnExit` via `IStateHandler`. > Add `OnApplicationQuit()` method to the code example, or clarify that GameStateManager handles quit by firing state exit events.

[MAJOR] Detailed Rules §3.5 > Version migration is shown but `MigrateRunState` is never called in the `LoadJson` or `LoadAll` flow. The code loads data and returns it without migration. > Show where migration is called in the load pipeline (after `JsonUtility.FromJson` in `LoadAll`).

[MAJOR] Dependencies > Missing dependencies for systems whose data is serialized: Body Part System (SlotPartPair), Battle Scar System (ScarSaveData), Living Ecosystem (EcosystemSaveData), Party System (party size validation), Settings System (Settings object), Institute Rank System (rank in MetaState), Creature Arena (arenaHighFloor in MetaState). > Add these as inbound dependencies since they read/write data through SaveLoadManager.

[MAJOR] Tuning Knobs > `Max party size` is listed as a tuning knob but is referenced as coming from `GameSettings` SO. However, the `RunState` code uses `List<CreatureSaveData> party` with a comment `(max 6)` — the max is not enforced in any shown code. > Clarify that party size validation occurs on load (clamp to `GameSettings.MaxPartySize`) and add this to edge cases.

### MINOR

[MINOR] Detailed Rules §3.1 > No mention of file size budget. The Living World pillar section of game-pillars.md notes "serializable within save file size budgets" — but no budget is defined. > Add a tuning knob for expected max save file size, or note that this is a post-MVP concern.

[MINOR] Detailed Rules §3.2 > `CampaignSaveData` does not store rival trainer state (adaptation data, encounter history). The Rival Trainer System is core to the Living World pillar — rivals must adapt across sessions. > Add `rivalTrainerStates` field to `CampaignSaveData` or `RunState`, or document where rival state is persisted.

[MINOR] Detailed Rules §3.2 > `CreatureSaveData` has no `capturedAtUnixMs` or `capturedInZone` field. For Pokedex lore and creature lineage display, knowing when/where a creature was captured is valuable. > Consider adding `capturedAtUnixMs` and `capturedInZone` fields. Mark as `<!-- TODO: Evaluate in balance pass -->` if not MVP-critical.

[MINOR] Detailed Rules §3.4 > Auto-save triggers table lists `Application quit` writing both `run.json` and `meta.json`, but the code only shows `OnExit` which writes `run.json`. No auto-save for `meta.json` on quit. > Clarify that `OnApplicationQuit` should save both run and meta state.

[MINOR] Edge Cases > No edge case for save file exceeding platform storage limits on specific platforms. > Add edge case: "Platform storage quota exceeded → log error, notify player via UI."

[MINOR] Edge Cases > No edge case for concurrent write attempts (e.g., auto-save and quit-save firing simultaneously). > Add edge case: "Concurrent save requests → queue writes; only most recent state is written."

[MINOR] Acceptance Criteria > No acceptance criterion for version migration round-trip. > Add: "SaveLoadManager correctly migrates a v0.9 format save to v1.0 on load."

[MINOR] Acceptance Criteria > No acceptance criterion for `OnApplicationQuit` behavior. > Add: "`OnApplicationQuit` saves both `run.json` and `meta.json`."

[MINOR] Tuning Knobs > Default volume values (`masterVolume = 1.0f`, `musicVolume = 0.8f`, `sfxVolume = 1.0f`, `combatSpeed = 1`, `targetFrameRate = 60`, `qualityLevel = 2`) are hardcoded in the `Settings` class but not listed in Tuning Knobs. > These likely belong in the Settings System GDD, but add a cross-reference note in Tuning Knobs.

---

## Summary

- **CRITICAL**: 1 (singleton pattern not approved in ADR-003)
- **MAJOR**: 7
- **MINOR**: 9

The document is well-structured with thorough data structures and good edge case coverage. The primary gaps are: (1) unapproved singleton usage, (2) missing pillar alignment, (3) incomplete ecosystem/rival save data, (4) missing migration call in load flow, and (5) several undocumented dependencies.

---

## Fixes Applied

1. **[CRITICAL] Singleton justification** — Added XML doc remarks to `SaveLoadManager` class explaining singleton necessity and a TODO to update ADR-003.
2. **[MAJOR] Pillar alignment** — Added pillar alignment statement to Overview (Living World primary, Genetic Architect and Discovery Through Play secondary).
3. **[MAJOR] Enum serialization** — Added design note before §3.2 code block clarifying JsonUtility enum-as-int serialization strategy.
4. **[MAJOR] Ecosystem save data** — Added `ZoneEcosystemData` class with `migrationPhase`, `weatherState`, and `predatorPreyBalance` fields. Added `zoneStates` list to `EcosystemSaveData`.
5. **[MAJOR] Black market tracking** — Added `blackMarketTransactionCount` field to `MetaState`.
6. **[MAJOR] OnApplicationQuit** — Added `OnApplicationQuit()` method calling `SaveRun()` and `SaveMeta()`.
7. **[MAJOR] Migration in load flow** — Added `MigrateRunState()` call in `LoadAll()` after loading `RunState`.
8. **[MAJOR] Missing dependencies** — Added 7 inbound dependencies: Body Part System, Battle Scar System, Living Ecosystem, Party System, Settings System, Institute Rank System, Creature Arena. Normalized all dependency filenames.
9. **[MAJOR] Party size validation** — Added party size clamp in `LoadAll()` with `MaxPartySize` const and warning log.
10. **[MINOR] Save file budget** — Added `Max save file size` (1 MB) to Tuning Knobs with balance-pass TODO.
11. **[MINOR] Rival trainer state** — Added `RivalTrainerSaveData` class and `rivalStates` list to `CampaignSaveData`.
12. **[MINOR] Creature capture metadata** — Added `capturedAtUnixMs` and `capturedInZone` fields to `CreatureSaveData`.
13. **[MINOR] OnApplicationQuit meta save** — Covered by fix #6.
14. **[MINOR] Storage quota edge case** — Added to edge cases table.
15. **[MINOR] Concurrent save edge case** — Added to edge cases table.
16. **[MINOR] Party size edge case** — Added to edge cases table.
17. **[MINOR] Migration acceptance criterion** — Added.
18. **[MINOR] OnApplicationQuit acceptance criterion** — Added.
19. **[MINOR] Settings defaults cross-reference** — Added tuning knob entry referencing Settings System GDD.
