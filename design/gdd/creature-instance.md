# Creature Instance

## 1. Overview

The Creature Instance represents the runtime state of a single creature during play: its level, current HP, learned moves, equipped body parts, DNA modifications, personality, and transient battle status. Each `CreatureInstance` is created from a `CreatureConfig` blueprint and persists across scenes in the party or storage. The instance owns all mutable data; the config is immutable. Stat computation accounts for base stats, growth curves, level, DNA modifiers, personality traits, and body part synergies. The factory method `Create()` initializes a new creature at level 1 with sensible defaults.

## 2. Player Fantasy

Your creatures are uniquely yours. A captured creature isn't just a species; it's an individual with a nickname, a history of battles, DNA mods applied, and acquired scars. As you level them up, their stats grow in predictable ways. As you splice DNA and equip parts, their capabilities shift in visible ways — stats change, new moves unlock, visual silhouette mutates. The instance is the living embodiment of your research; the config is just the template.

## 3. Detailed Rules

### 3.1 CreatureInstance Class Structure

```csharp
namespace GeneForge.Creatures
{
    /// <summary>
    /// Runtime state of a single creature instance.
    /// Mutable; persisted in SaveData.
    /// Created from immutable CreatureConfig blueprint.
    /// </summary>
    [System.Serializable]
    public class CreatureInstance
    {
        // ── Blueprint Reference ──────────────────────────────────────────
        public CreatureConfig Config { get; private set; }
        public string Id => Config.id;
        public string DisplayName => Config.displayName;

        // ── Identity ─────────────────────────────────────────────────────
        [SerializeField] private string _nickname;
        public string Nickname => _nickname ?? DisplayName;

        // ── Progression ──────────────────────────────────────────────────
        [SerializeField] private int _level = 1;
        public int Level => _level;

        [SerializeField] private int _currentXP = 0;
        public int CurrentXP => _currentXP;

        private int _xpNextLevel = 100; // Computed; scales with level

        // ── Health ───────────────────────────────────────────────────────
        [SerializeField] private int _currentHP;
        public int CurrentHP => _currentHP;

        [SerializeField] private int _maxHP;
        public int MaxHP => _maxHP;

        // ── Stats (Computed) ─────────────────────────────────────────────
        /// <summary>Stats after applying level, base, DNA mods, and personality.</summary>
        [SerializeField] private StatsBlock _computedStats;
        public StatsBlock ComputedStats => _computedStats;

        [System.Serializable]
        public class StatsBlock
        {
            public int HP;
            public int ATK;
            public int DEF;
            public int SPD;
            public int ACC;
        }

        // ── Moves ────────────────────────────────────────────────────────
        [SerializeField] private List<string> _learnedMoveIds = new();
        public IReadOnlyList<string> LearnedMoveIds => _learnedMoveIds;

        [SerializeField] private List<int> _learnedMovePP = new();
        public IReadOnlyList<int> LearnedMovePP => _learnedMovePP;

        // ── Body Parts ───────────────────────────────────────────────────
        [SerializeField] private Dictionary<BodySlot, string> _equippedPartIds = new();
        public IReadOnlyDictionary<BodySlot, string> EquippedPartIds => _equippedPartIds;

        // ── DNA Modifications ────────────────────────────────────────────
        [SerializeField] private List<string> _appliedDNAModIds = new();
        public IReadOnlyList<string> AppliedDNAMods => _appliedDNAModIds;

        [SerializeField] private int _instability = 0;
        public int Instability => _instability;

        // ── Personality ──────────────────────────────────────────────────
        [SerializeField] private PersonalityTrait _personality = PersonalityTrait.None;
        public PersonalityTrait Personality => _personality;

        // ── Affinity ─────────────────────────────────────────────────────
        [SerializeField] private Dictionary<string, int> _affinityLevels = new();
        public IReadOnlyDictionary<string, int> AffinityLevels => _affinityLevels;

        // ── Battle State ─────────────────────────────────────────────────
        [SerializeField] private Vector2Int _gridPosition;
        public Vector2Int GridPosition => _gridPosition;

        [SerializeField] private Facing _facing = Facing.Right;
        public Facing Facing => _facing;

        [SerializeField] private List<StatusEffect> _activeStatusEffects = new();
        public IReadOnlyList<StatusEffect> ActiveStatusEffects => _activeStatusEffects;

        [SerializeField] private bool _isFainted = false;
        public bool IsFainted => _isFainted;

        [SerializeField] private bool _hasMoved = false;
        public bool HasMoved => _hasMoved;

        [SerializeField] private bool _hasActed = false;
        public bool HasActed => _hasActed;

        // ── Scars ────────────────────────────────────────────────────────
        [SerializeField] private List<BattleScar> _scars = new();
        public IReadOnlyList<BattleScar> Scars => _scars;

        [System.Serializable]
        public class BattleScar
        {
            public string damageType;      // Fire, Physical, etc.
            public Vector2 position;       // Where on model
            public string loreEntry;       // What battle caused it
        }

        // ── Factory Method ───────────────────────────────────────────────

        /// <summary>Create a new creature instance from config at specified level.</summary>
        public static CreatureInstance Create(CreatureConfig config, int level = 1, string nickname = null)
        {
            var instance = new CreatureInstance
            {
                Config = config,
                _nickname = nickname ?? config.displayName,
                _level = Mathf.Max(1, level),
                _currentXP = 0,
            };

            instance.RecalculateStats();
            instance._currentHP = instance._maxHP;
            instance.LearnStartingMoves();

            return instance;
        }

        // ── Stat Computation ─────────────────────────────────────────────

        /// <summary>Recompute all stats from base, level, DNA mods, and personality.</summary>
        public void RecalculateStats()
        {
            var baseStats = Config.BaseStats;
            float levelMult = GetGrowthMultiplier(Config.GrowthCurve);

            // HP: (baseHP × levelMult × level / 50) + baseHP
            _maxHP = (int)((baseStats.HP * levelMult * _level / 50f) + baseStats.HP);
            _currentHP = Mathf.Min(_currentHP, _maxHP); // Cap current HP

            // ATK: (baseATK × levelMult × level / 50) + baseATK + DNA bonus
            int dnaBonusATK = GetDNAStatBonus(StatType.ATK);
            int baseATK = (int)((baseStats.ATK * levelMult * _level / 50f) + baseStats.ATK);
            float personalityModATK = GetPersonalityStatModifier(StatType.ATK);
            int finalATK = (int)(Mathf.Max(1, baseATK + dnaBonusATK) * personalityModATK);

            // DEF: (baseDEF × levelMult × level / 50) + baseDEF + DNA bonus
            int dnaBonusDEF = GetDNAStatBonus(StatType.DEF);
            int baseDEF = (int)((baseStats.DEF * levelMult * _level / 50f) + baseStats.DEF);
            float personalityModDEF = GetPersonalityStatModifier(StatType.DEF);
            int finalDEF = (int)(Mathf.Max(1, baseDEF + dnaBonusDEF) * personalityModDEF);

            // SPD: (baseSPD × levelMult × level / 50) + baseSPD + DNA bonus
            int dnaBonusSPD = GetDNAStatBonus(StatType.SPD);
            int baseSPD = (int)((baseStats.SPD * levelMult * _level / 50f) + baseStats.SPD);
            float personalityModSPD = GetPersonalityStatModifier(StatType.SPD);
            int finalSPD = (int)(Mathf.Max(1, baseSPD + dnaBonusSPD) * personalityModSPD);

            // ACC: typically fixed at base value
            int finalACC = baseStats.ACC;

            _computedStats = new StatsBlock
            {
                HP = _maxHP,
                ATK = finalATK,
                DEF = finalDEF,
                SPD = finalSPD,
                ACC = finalACC
            };

            // Update XP threshold for next level
            _xpNextLevel = ComputeXPThreshold(_level + 1);
        }

        /// <summary>Get growth curve multiplier for stat scaling.</summary>
        private float GetGrowthMultiplier(GrowthCurve curve)
        {
            return curve switch
            {
                GrowthCurve.Slow => 0.8f,
                GrowthCurve.Fast => 1.2f,
                _ => 1.0f, // Medium
            };
        }

        /// <summary>Get total DNA stat bonus from all applied mods (cumulatively).</summary>
        private int GetDNAStatBonus(StatType stat)
        {
            // Simplified: each DNA mod in _appliedDNAModIds adds a flat bonus
            // Post-MVP: look up DNAModConfig to get precise bonuses
            int total = 0;
            foreach (var modId in _appliedDNAModIds)
            {
                // For MVP, assume +2 per stat boost mod
                total += 2;
            }
            return total;
        }

        /// <summary>Get personality multiplier for a specific stat.</summary>
        private float GetPersonalityStatModifier(StatType stat)
        {
            return _personality switch
            {
                PersonalityTrait.Aggressive when stat == StatType.ATK => 1.1f,
                PersonalityTrait.Cautious when stat == StatType.DEF => 1.1f,
                PersonalityTrait.Cautious when stat == StatType.SPD => 0.95f,
                PersonalityTrait.Loyal => 1.0f,
                PersonalityTrait.Feral when stat == StatType.ATK => 1.2f,
                _ => 1.0f,
            };
        }

        // ── Move Learning ────────────────────────────────────────────────

        /// <summary>Learn the first 4 moves from config move pool at creature's current level.</summary>
        private void LearnStartingMoves()
        {
            _learnedMoveIds.Clear();
            _learnedMovePP.Clear();

            foreach (var entry in Config.MovePool)
            {
                if (entry.Level <= _level && _learnedMoveIds.Count < 4)
                {
                    _learnedMoveIds.Add(entry.MoveId);
                    // PP value looked up from MoveConfig at load time
                    var moveConfig = ConfigLoader.GetMove(entry.MoveId);
                    _learnedMovePP.Add(moveConfig.PP);
                }
            }
        }

        /// <summary>Learn a new move, replacing slot if full (or post-MVP: prompt user).</summary>
        public void LearnMove(string moveId, int slot = -1)
        {
            if (_learnedMoveIds.Contains(moveId)) return; // Already knows it

            var moveConfig = ConfigLoader.GetMove(moveId);
            if (moveConfig == null) return; // Invalid move

            if (slot < 0 || slot >= _learnedMoveIds.Count)
                slot = _learnedMoveIds.Count; // Append

            if (slot >= 4)
                slot = 3; // Cap at 4 moves (MVP: auto-replace slot 4)

            if (_learnedMoveIds.Count <= slot)
            {
                _learnedMoveIds.Add(moveId);
                _learnedMovePP.Add(moveConfig.PP);
            }
            else
            {
                _learnedMoveIds[slot] = moveId;
                _learnedMovePP[slot] = moveConfig.PP;
            }
        }

        /// <summary>Deduct PP from a move slot. Called by TurnManager after move execution.</summary>
        public void DeductPP(int moveSlot)
        {
            if (moveSlot < 0 || moveSlot >= _learnedMovePP.Count) return;
            _learnedMovePP[moveSlot] = Mathf.Max(0, _learnedMovePP[moveSlot] - 1);
        }

        // ── Body Part Management ────────────────────────────────────────

        /// <summary>Equip a body part to a slot. Validates archetype compatibility.</summary>
        public bool EquipPart(BodySlot slot, string partId)
        {
            if (!Config.AvailableSlots.Contains(slot))
                return false; // Invalid slot for this archetype

            var partConfig = ConfigLoader.GetBodyPart(partId);
            if (partConfig == null) return false;

            _equippedPartIds[slot] = partId;
            RecalculateStats(); // Parts may affect stats
            return true;
        }

        /// <summary>Unequip a body part from a slot.</summary>
        public void UnequipPart(BodySlot slot)
        {
            if (_equippedPartIds.ContainsKey(slot))
            {
                _equippedPartIds.Remove(slot);
                RecalculateStats();
            }
        }

        // ── DNA Modification ────────────────────────────────────────────

        /// <summary>Apply a DNA modification. Increases instability; updates stats.</summary>
        public void ApplyDNAMod(string modId, int instabilityIncrease = 5)
        {
            if (_appliedDNAModIds.Contains(modId)) return; // Already applied

            _appliedDNAModIds.Add(modId);
            _instability = Mathf.Min(100, _instability + instabilityIncrease);
            RecalculateStats();
        }

        // ── Battle State Management ──────────────────────────────────────

        /// <summary>Move creature to a tile on the grid.</summary>
        public void MoveTo(Vector2Int position, GridSystem grid)
        {
            _gridPosition = position;
            var tile = grid.GetTile(position);
            if (tile != null) tile.Occupant = this;
        }

        /// <summary>Apply a status effect to this creature.</summary>
        public void ApplyStatus(StatusEffect status, int durationRounds = -1)
        {
            // MVP: simplified; post-MVP: track duration per effect
            _activeStatusEffects.Add(status);
        }

        /// <summary>Remove a status effect.</summary>
        public void RemoveStatus(StatusEffect status)
        {
            _activeStatusEffects.Remove(status);
        }

        /// <summary>Take damage. Called by TurnManager.</summary>
        public void TakeDamage(int damage)
        {
            _currentHP = Mathf.Max(0, _currentHP - damage);
            if (_currentHP == 0) Faint();
        }

        /// <summary>Heal HP. Called by moves with Drain or by recovery items.</summary>
        public void Heal(int amount)
        {
            _currentHP = Mathf.Min(_maxHP, _currentHP + amount);
        }

        /// <summary>Restore all HP (research station).</summary>
        public void HealFull()
        {
            _currentHP = _maxHP;
        }

        /// <summary>Mark creature as fainted.</summary>
        public void Faint()
        {
            _currentHP = 0;
            _isFainted = true;
        }

        /// <summary>Revive creature at specified HP.</summary>
        public void Revive(int hp = 1)
        {
            _isFainted = false;
            _currentHP = Mathf.Min(hp, _maxHP);
        }

        /// <summary>Set moved flag for turn tracking.</summary>
        public void SetMoved(bool moved) => _hasMoved = moved;

        /// <summary>Set acted flag for turn tracking.</summary>
        public void SetActed(bool acted) => _hasActed = acted;

        // ── Affinity System ──────────────────────────────────────────────

        /// <summary>Increase affinity with another creature ID by 1.</summary>
        public void IncreaseAffinity(string targetCreatureId, int amount = 1)
        {
            if (!_affinityLevels.ContainsKey(targetCreatureId))
                _affinityLevels[targetCreatureId] = 0;
            _affinityLevels[targetCreatureId] = Mathf.Min(10, _affinityLevels[targetCreatureId] + amount);
        }

        /// <summary>Get affinity level with another creature (0–10).</summary>
        public int GetAffinity(string targetCreatureId)
        {
            return _affinityLevels.ContainsKey(targetCreatureId) ? _affinityLevels[targetCreatureId] : 0;
        }

        // ── Leveling & XP ───────────────────────────────────────────────

        /// <summary>Award XP and check for level up.</summary>
        public void AwardXP(int xpAmount)
        {
            _currentXP += xpAmount;
            while (_currentXP >= _xpNextLevel && _level < 50)
            {
                _currentXP -= _xpNextLevel;
                _level++;
                RecalculateStats();
                _currentHP = _maxHP; // Full heal on level up
                _xpNextLevel = ComputeXPThreshold(_level + 1);
            }
        }

        /// <summary>Compute XP required to reach target level. Uses cubic formula from leveling-xp-system.md.</summary>
        private int ComputeXPThreshold(int level)
        {
            return Mathf.FloorToInt(0.8f * level * level * level + 10 * level + 50);
        }

        // ── Personality Change ──────────────────────────────────────────

        /// <summary>Change creature personality. Requires personality item/DNA mod (post-MVP).</summary>
        public void SetPersonality(PersonalityTrait trait)
        {
            _personality = trait;
            RecalculateStats();
        }
    }

    // PersonalityTrait enum is defined in data-configuration-pipeline.md (canonical source)
    // None=0, Aggressive, Cautious, Loyal, Feral, Curious, Territorial

    public enum StatType
    {
        HP, ATK, DEF, SPD, ACC
    }

    public enum Facing
    {
        N, NE, E, SE, S, SW, W, NW  // 8-directional, matches grid-tile-system.md flanking
    }
}
```

## 4. Formulas

| Formula | Expression | Notes |
|---------|-----------|-------|
| Max HP | `(baseHP × growthMult × level / 50) + baseHP` | Level-scaled HP cap |
| Other stats | `(baseStat × growthMult × level / 50) + baseStat + DNABonus` | Applied before personality |
| Personality modifier | `1.0–1.2×` per trait per stat | Multiplicative after all additions |
| XP threshold | `level² × 10` | XP needed to reach next level |
| DNA instability cap | 100 | Ranges 0–100 |
| Affinity cap | 10 | Ranges 0–10 |
| Max moves (active) | 4 | Hard limit on learned move slots |
| Max body parts equipped | Varies by archetype | 2–6 slots per creature |

## 5. Edge Cases

| Situation | Behavior |
|-----------|----------|
| `Create()` called with level 0 | Level set to 1 minimum |
| `Create()` called with level 100+ | Level accepted; XP system caps at 50 on level up |
| Creature gains stat increase that would exceed int.MaxValue | Capped to int.MaxValue (no crash) |
| `RecalculateStats()` called while health > maxHP | CurrentHP capped to new MaxHP |
| `LearnMove()` called with full move slots (4) | Post-MVP: prompt user; MVP: replaces slot 4 silently |
| `DeductPP()` called on empty move slot | No-op; does not crash |
| `EquipPart()` called with invalid slot for archetype | Returns false; part not equipped |
| `ApplyDNAMod()` called with already-applied mod | No-op; doesn't double-apply |
| Personality set to invalid enum | Treated as None |
| `TakeDamage(0)` | CurrentHP unchanged; no faint |
| `TakeDamage()` reducing HP below 0 | HP set to 0; faint triggered |
| `Heal()` exceeding MaxHP | CurrentHP capped to MaxHP |
| Affinity increased beyond 10 | Capped to 10 |
| Affinity queried for non-existent creature ID | Returns 0 (no affinity) |
| `MoveTo()` called with invalid grid position | Position set anyway; grid tile lookup may return null (validate in GridSystem) |

## 6. Dependencies

| Dependency | Direction | Notes |
|------------|-----------|-------|
| `CreatureConfig` | Inbound | Blueprint reference; base stats read |
| `ConfigLoader` | Outbound | LoadMove, GetBodyPart calls on learn/equip |
| `GridSystem` | Inbound | Grid position tracking, tile occupancy |
| `StatusEffect` enum | Inbound | Status effects listed |
| `DamageCalculator` | Outbound | Reads stats for damage estimation |
| `TurnManager` | Outbound | Used as actor; TakeDamage/Heal/Faint called |
| `PersonalityTrait` enum | Inbound | Personality modifiers |
| `BodySlot` enum | Inbound | Part equipping |

## 7. Tuning Knobs

| Parameter | Location | Default | Notes |
|-----------|----------|---------|-------|
| HP level multiplier (Slow growth) | `GetGrowthMultiplier` | 0.8× | Slow creatures have less HP scaling |
| HP level multiplier (Fast growth) | `GetGrowthMultiplier` | 1.2× | Fast creatures have more HP scaling |
| DNA stat bonus per mod | `GetDNAStatBonus` | +2 | Each mod adds flat bonus |
| Personality ATK modifier (Aggressive) | `GetPersonalityStatModifier` | 1.1× | 10% boost |
| Personality ATK modifier (Feral) | `GetPersonalityStatModifier` | 1.2× | 20% boost |
| Personality DEF modifier (Cautious) | `GetPersonalityStatModifier` | 1.1× | 10% boost |
| Personality SPD penalty (Cautious) | `GetPersonalityStatModifier` | 0.95× | Slightly slower |
| Instability increase per DNA mod | `ApplyDNAMod` | +5 | Configurable per mod (post-MVP) |
| Affinity gain per battle | Design | +1 | Doubles with high synergy (post-MVP) |
| Max level | Design | 50 | Level cap |
| XP threshold formula | `ComputeXPThreshold` | `level² × 10` | Quadratic scaling |
| Max active moves | Design | 4 | Hard cap |
| Max instability | Design | 100 | Full destabilization at 100 |

## 8. Acceptance Criteria

- [ ] `Create(config, 1)` returns creature with HP = MaxHP, level 1, 4 learned moves from pool
- [ ] `RecalculateStats()` produces correct stats for level 1, 25, 50 per growth curve
- [ ] Personality multipliers correctly apply to stats (1.1× for Aggressive ATK)
- [ ] `LearnMove()` replaces slot 4 when 4 moves already learned
- [ ] `DeductPP()` reduces PP by 1, does not go below 0
- [ ] `EquipPart()` rejects parts for invalid body slot
- [ ] `ApplyDNAMod()` increases instability and recalculates stats
- [ ] `TakeDamage()` faints creature when HP reaches 0
- [ ] `AwardXP()` triggers level up when threshold met
- [ ] `AwardXP()` full-heals creature on level up
- [ ] Level capped at 50; XP excess stored for next level
- [ ] Affinity capped at 10; does not exceed
- [ ] Personality change recalculates stats correctly
- [ ] `MoveTo()` updates grid position and tile occupancy
- [ ] ApplyStatus/RemoveStatus correctly manage active status list
- [ ] Scars list persists across saves
- [ ] EditMode test: stat computation for Emberfox at level 50 matches expected values
- [ ] PlayMode test: damage dealt in combat scales with level-up stat increases
