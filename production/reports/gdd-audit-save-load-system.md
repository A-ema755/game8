# GDD Audit: Save / Load System

**File**: `design/gdd/save-load-system.md`
**Audited**: 2026-04-05 (post-design-review fixes)
**Auditor**: gdd-audit pipeline (automated)

---

## Section Presence & Content Check

| # | Section | Present | Non-Empty | Notes |
|---|---------|---------|-----------|-------|
| 1 | Overview | Yes | Yes | Includes pillar alignment statement |
| 2 | Player Fantasy | Yes | Yes | Focused, evocative |
| 3 | Detailed Rules | Yes | Yes | 5 subsections with data structures and code |
| 4 | Formulas | Yes | Yes | Explicitly states N/A for I/O system — valid |
| 5 | Edge Cases | Yes | Yes | 14 cases with explicit behavior |
| 6 | Dependencies | Yes | Yes | 15 dependencies with filenames |
| 7 | Tuning Knobs | Yes | Yes | 8 parameters |
| 8 | Acceptance Criteria | Yes | Yes | 17 testable criteria |

All 8 required sections present and non-empty. No placeholder text detected.

---

## Cross-Reference Validation

| Referenced File | Exists? |
|----------------|---------|
| `game-state-manager.md` | Yes |
| `creature-instance.md` | Yes |
| `data-configuration-pipeline.md` | Yes |
| `pokedex-system.md` | Yes |
| `campaign-map.md` | Yes |
| `dna-alteration-system.md` | Yes |
| `body-part-system.md` | Yes |
| `battle-scar-system.md` | Yes |
| `living-ecosystem.md` | Yes |
| `party-system.md` | Yes |
| `settings-system.md` | Yes |
| `institute-rank-system.md` | Yes |
| `creature-arena.md` | Yes |

All cross-references point to existing files.

---

## Hardcoded Values Outside Tuning Knobs

| Section | Value | In Tuning Knobs? | Issue |
|---------|-------|-------------------|-------|
| §3.2 `RunState` — `saveVersion = "1.0"` | Yes | `SaveVersion` | OK |
| §3.2 `MetaState` — `saveVersion = "1.0"` | Yes | `SaveVersion` | OK |
| §3.2 `MetaState` — `instituteRank` 0-4 | No | **Missing** | Range not in tuning knobs; belongs in Institute Rank System but should be cross-referenced |
| §3.2 `MetaState` — `stationUpgradeLevel` 1-5 | No | **Missing** | Range not in tuning knobs; belongs in Station Upgrade System but should be cross-referenced |
| §3.2 `Settings` — `masterVolume = 1.0f` | Yes (cross-ref) | `Settings defaults` | OK via cross-ref |
| §3.2 `Settings` — `combatSpeed = 1` valid values 1,2,4 | No | **Missing** | Valid combat speed values not documented in tuning knobs |
| §3.2 `CreatureSaveData` — `instability` 0-100 | No | **Partially** | Range mentioned in edge cases but not in tuning knobs; belongs in DNA Alteration System |
| §3.2 `EcosystemSaveData` — `conservationScore` 0-100 | No | **Missing** | Range not in tuning knobs; belongs in Living Ecosystem |
| §3.2 `ZoneEcosystemData` — `predatorPreyBalance` -1.0 to 1.0 | No | **Missing** | Range not in tuning knobs |
| §3.3 `MaxPartySize` const = 6 | Yes | `Max party size` | OK |

---

## Pillar Alignment

Pillar alignment is explicitly stated in Overview. Primary: **Living World**. Secondary: **Genetic Architect**, **Discovery Through Play**. Well-justified.

---

## Findings

| Section | Issue | Fix |
|---------|-------|-----|
| §3.2 `Settings` | `combatSpeed` valid values (1, 2, 4) are hardcoded in a comment but not formalized in Tuning Knobs or as a const. | Add `ValidCombatSpeeds` to Tuning Knobs (1, 2, 4) with cross-reference to Settings System GDD. |
| §3.2 Range values | Several fields have ranges documented in comments (instability 0-100, conservationScore 0-100, instituteRank 0-4, stationUpgradeLevel 1-5, predatorPreyBalance -1.0 to 1.0) that are owned by other systems but have no cross-reference note. | Add a design note after §3.2 clarifying that field ranges are owned by their respective system GDDs and validated on load. |
| §3.3 `StartNewRun` | New `CampaignSaveData` does not initialize `rivalStates` list (added in Phase 2 fixes), risking null reference. | Add `rivalStates = new List<RivalTrainerSaveData>()` to `StartNewRun`. |
| §3.3 `StartNewRun` | New `EcosystemSaveData` does not initialize `zoneStates` list (added in Phase 2 fixes). | Add `zoneStates = new List<ZoneEcosystemData>()` to `StartNewRun`. |
| §6 Dependencies | Missing dependency on Rival Trainer System (`rival-trainer-system.md`) despite `RivalTrainerSaveData` being defined. | Add Rival Trainer System as inbound dependency. |

---

## Verdict

**PASS** — All 8 sections present and non-empty, no placeholder text, all cross-references valid, pillar alignment stated. Five minor issues remain: combat speed values, range cross-references, two uninitialized lists in `StartNewRun`, and a missing dependency.

---

## Fixes Applied

1. **Combat speed tuning knob** — Added `ValidCombatSpeeds` (1, 2, 4) to Tuning Knobs with cross-reference to Settings System GDD.
2. **Field range ownership note** — Added design note after §3.2 clarifying that field ranges (instability, conservationScore, instituteRank, stationUpgradeLevel, predatorPreyBalance) are owned by their respective system GDDs and validated/clamped on load.
3. **StartNewRun rivalStates init** — Added `rivalStates = new List<RivalTrainerSaveData>()` to `CampaignSaveData` initialization in `StartNewRun()`.
4. **StartNewRun zoneStates init** — Added `zoneStates = new List<ZoneEcosystemData>()` to `EcosystemSaveData` initialization in `StartNewRun()`.
5. **Rival Trainer dependency** — Added Rival Trainer System (`rival-trainer-system.md`) as inbound dependency.
