using System;
using System.Collections.Generic;
using System.Linq;
using GeneForge.Core;
using UnityEngine;

namespace GeneForge.Creatures
{
    /// <summary>
    /// Runtime state of a single creature instance.
    /// Mutable; persisted in SaveData via JsonUtility.
    /// Created from immutable CreatureConfig blueprint.
    /// </summary>
    [System.Serializable]
    public class CreatureInstance
    {
        // ── Events ───────────────────────────────────────────────────────
        /// <summary>Fired after each level-up during XP award.</summary>
        [field: NonSerialized] public event Action<CreatureInstance> LeveledUp;

        /// <summary>Fired when HP reaches 0.</summary>
        [field: NonSerialized] public event Action<CreatureInstance> Fainted;

        /// <summary>Fired after any stat recomputation.</summary>
        [field: NonSerialized] public event Action<CreatureInstance> StatsChanged;

        // ── Settings shorthand ───────────────────────────────────────────
        private static GameSettings S => ConfigLoader.Settings;

        // ── Defaults (used when GameSettings is null, e.g. EditMode tests) ──
        const float DefaultGrowthFast = 1.2f;
        const float DefaultGrowthMedium = 1.0f;
        const float DefaultGrowthSlow = 0.8f;
        const float DefaultGrowthErratic = 1.1f;
        const float DefaultPersonalityBoost = 1.1f;
        const float DefaultPersonalityPenalty = 0.95f;
        const int DefaultDnaBonusPerMod = 2;
        const int DefaultBlightThreshold = 80;
        const int DefaultXpMultiplier = 10;

        // ── Blueprint Reference ──────────────────────────────────────────
        [SerializeField] private CreatureConfig _config;

        /// <summary>Immutable species blueprint this instance was created from.</summary>
        public CreatureConfig Config => _config;

        /// <summary>Config ID (kebab-case).</summary>
        public string Id => _config.Id;

        /// <summary>Species display name from config.</summary>
        public string DisplayName => _config.DisplayName;

        // ── Identity ─────────────────────────────────────────────────────
        [SerializeField] private string _nickname;

        /// <summary>Player-assigned nickname; falls back to DisplayName if null.</summary>
        public string Nickname => _nickname ?? DisplayName;

        // ── Progression ──────────────────────────────────────────────────
        [SerializeField] private int _level = 1;

        /// <summary>Current level (1–50).</summary>
        public int Level => _level;

        [SerializeField] private int _currentXP;

        /// <summary>XP accumulated toward next level.</summary>
        public int CurrentXP => _currentXP;

        [SerializeField] private int _xpNextLevel = 100;

        /// <summary>XP threshold required to reach next level.</summary>
        public int XPNextLevel => _xpNextLevel;

        // ── Health ───────────────────────────────────────────────────────
        [SerializeField] private int _currentHP;

        /// <summary>Current hit points.</summary>
        public int CurrentHP => _currentHP;

        [SerializeField] private int _maxHP;

        /// <summary>Maximum hit points (computed from stats).</summary>
        public int MaxHP => _maxHP;

        // ── Stats (Computed) ─────────────────────────────────────────────
        [SerializeField] private StatsBlock _computedStats;

        /// <summary>Stats after applying level, base, DNA mods, and personality.</summary>
        public StatsBlock ComputedStats => _computedStats;

        /// <summary>Computed stat container. Struct per ADR-009.</summary>
        [Serializable]
        public struct StatsBlock
        {
            [SerializeField] private int _hp;
            [SerializeField] private int _atk;
            [SerializeField] private int _def;
            [SerializeField] private int _spd;
            [SerializeField] private int _acc;

            public int HP => _hp;
            public int ATK => _atk;
            public int DEF => _def;
            public int SPD => _spd;
            public int ACC => _acc;

            public StatsBlock(int hp, int atk, int def, int spd, int acc)
            {
                _hp = hp; _atk = atk; _def = def; _spd = spd; _acc = acc;
            }
        }

        // ── Moves ────────────────────────────────────────────────────────
        [SerializeField] private List<string> _learnedMoveIds = new();

        /// <summary>IDs of currently learned moves (max 4).</summary>
        public IReadOnlyList<string> LearnedMoveIds => _learnedMoveIds;

        [SerializeField] private List<int> _learnedMovePP = new();

        /// <summary>Current PP for each learned move slot.</summary>
        public IReadOnlyList<int> LearnedMovePP => _learnedMovePP;

        // ── Body Parts (parallel lists for JsonUtility serialization) ────
        [SerializeField] private List<int> _equippedPartSlots = new();
        [SerializeField] private List<string> _equippedPartIds = new();

        [NonSerialized] private Dictionary<BodySlot, string> _partsCache;
        [NonSerialized] private bool _partsCacheDirty = true;

        /// <summary>Currently equipped body parts by slot.</summary>
        public IReadOnlyDictionary<BodySlot, string> EquippedPartIds
        {
            get
            {
                if (_partsCacheDirty || _partsCache == null)
                    RebuildPartsCache();
                return _partsCache;
            }
        }

        private void RebuildPartsCache()
        {
            _partsCache ??= new Dictionary<BodySlot, string>();
            _partsCache.Clear();
            for (int i = 0; i < _equippedPartSlots.Count; i++)
                _partsCache[(BodySlot)_equippedPartSlots[i]] = _equippedPartIds[i];
            _partsCacheDirty = false;
        }

        // ── DNA Modifications ────────────────────────────────────────────
        [SerializeField] private List<string> _appliedDNAModIds = new();

        /// <summary>IDs of applied DNA modifications.</summary>
        public IReadOnlyList<string> AppliedDNAMods => _appliedDNAModIds;

        [SerializeField] private int _instability;

        /// <summary>Current instability value (0–100).</summary>
        public int Instability => _instability;

        /// <summary>
        /// Active secondary genome type. Normally from Config.SecondaryType.
        /// Overridden to Blight when instability >= blight threshold (default 80).
        /// </summary>
        public CreatureType ActiveSecondaryType
        {
            get
            {
                int threshold = S != null ? S.BlightInstabilityThreshold : DefaultBlightThreshold;
                return _instability >= threshold ? CreatureType.Blight : _config.SecondaryType;
            }
        }

        /// <summary>True if creature has two active genome types (including Blight from instability).</summary>
        public bool IsDualType => ActiveSecondaryType != CreatureType.None;

        // ── Form Access (cached) ─────────────────────────────────────────
        [NonSerialized] private HashSet<DamageForm> _formsCache;
        [NonSerialized] private bool _formsCacheDirty = true;

        /// <summary>
        /// Returns the set of damage forms this creature can use,
        /// derived from currently equipped body parts.
        /// </summary>
        public HashSet<DamageForm> AvailableForms
        {
            get
            {
                if (_formsCacheDirty || _formsCache == null)
                    RebuildFormsCache();
                return _formsCache;
            }
        }

        private void RebuildFormsCache()
        {
            _formsCache ??= new HashSet<DamageForm>();
            _formsCache.Clear();
            for (int i = 0; i < _equippedPartIds.Count; i++)
            {
                var partConfig = ConfigLoader.GetBodyPart(_equippedPartIds[i]);
                if (partConfig != null && partConfig.FormAccess != DamageForm.None)
                    _formsCache.Add(partConfig.FormAccess);
            }
            _formsCacheDirty = false;
        }

        // ── Personality ──────────────────────────────────────────────────
        [SerializeField] private PersonalityTrait _personality = PersonalityTrait.None;

        /// <summary>Current personality trait.</summary>
        public PersonalityTrait Personality => _personality;

        // ── Affinity (parallel lists for JsonUtility serialization) ───────
        [SerializeField] private List<string> _affinityCreatureIds = new();
        [SerializeField] private List<int> _affinityValues = new();

        // ── Accuracy / Evasion Stage Multipliers (Stub) ─────────────────────
        // Stubs for the post-MVP stat stage system.
        // Full implementation: maintain a stage int (-3 to +3) and map via the
        // stage table in GDD Turn Manager §3.9. TurnManager.MoveHitCheck reads these.

        /// <summary>
        /// Accuracy stage multiplier used in hit-chance calculation.
        /// Stub — always returns 1.0f until the stat stage system is implemented (post-MVP).
        /// Full stage table: -3=0.50, -2=0.67, -1=0.75, 0=1.00, +1=1.33, +2=1.50, +3=2.00.
        /// </summary>
        public float AccuracyStageMultiplier => 1.0f;

        /// <summary>
        /// Evasion stage multiplier used in hit-chance calculation.
        /// Stub — always returns 1.0f until the stat stage system is implemented (post-MVP).
        /// Full stage table: -3=0.50, -2=0.67, -1=0.75, 0=1.00, +1=1.33, +2=1.50, +3=2.00.
        /// </summary>
        public float EvasionStageMultiplier => 1.0f;

        // ── Battle State ─────────────────────────────────────────────────
        [SerializeField] private Vector2Int _gridPosition;

        /// <summary>Current tile position on combat grid.</summary>
        public Vector2Int GridPosition => _gridPosition;

        [SerializeField] private Facing _facing = Facing.E;

        /// <summary>Current facing direction (8-directional).</summary>
        public Facing Facing => _facing;

        [SerializeField] private List<StatusEffect> _activeStatusEffects = new();

        /// <summary>Currently active status effects.</summary>
        public IReadOnlyList<StatusEffect> ActiveStatusEffects => _activeStatusEffects;

        [SerializeField] private bool _isFainted;

        /// <summary>True if creature has been knocked out.</summary>
        public bool IsFainted => _isFainted;

        [SerializeField] private bool _hasMoved;

        /// <summary>True if creature has moved this turn.</summary>
        public bool HasMoved => _hasMoved;

        [SerializeField] private bool _hasActed;

        /// <summary>True if creature has acted this turn.</summary>
        public bool HasActed => _hasActed;

        // ── Scars ────────────────────────────────────────────────────────
        [SerializeField] private List<BattleScar> _scars = new();

        /// <summary>Persistent battle scars from combat history.</summary>
        public IReadOnlyList<BattleScar> Scars => _scars;

        /// <summary>Visual scar from battle damage.</summary>
        [Serializable]
        public class BattleScar
        {
            [SerializeField] private string _damageType;
            [SerializeField] private Vector2 _position;
            [SerializeField] private string _loreEntry;

            public string DamageType => _damageType;
            public Vector2 Position => _position;
            public string LoreEntry => _loreEntry;

            public BattleScar(string damageType, Vector2 position, string loreEntry)
            {
                _damageType = damageType;
                _position = position;
                _loreEntry = loreEntry;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // Factory Method
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Create a new creature instance from config at specified level.</summary>
        public static CreatureInstance Create(CreatureConfig config, int level = 1, string nickname = null)
        {
            var instance = new CreatureInstance
            {
                _config = config,
                _nickname = nickname,
                _level = Mathf.Max(1, level),
                _currentXP = 0,
            };

            instance.RecalculateStats();
            instance._currentHP = instance._maxHP;
            instance.LearnStartingMoves();

            return instance;
        }

        // ══════════════════════════════════════════════════════════════════
        // Stat Computation
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Recompute all stats from base, level, DNA mods, and personality.</summary>
        public void RecalculateStats()
        {
            var baseStats = _config.BaseStats;
            float growthMult = GetGrowthMultiplier(_config.GrowthCurve);
            int dnaBonusPerMod = S != null ? S.DnaStatBonusPerMod : DefaultDnaBonusPerMod;
            int dnaBonus = _appliedDNAModIds.Count * dnaBonusPerMod;
            float boost = S != null ? S.PersonalityBoostMultiplier : DefaultPersonalityBoost;
            float penalty = S != null ? S.PersonalityPenaltyMultiplier : DefaultPersonalityPenalty;

            _maxHP = (int)((baseStats.HP * growthMult * _level / 50f) + baseStats.HP);
            _currentHP = Mathf.Min(_currentHP, _maxHP);

            int rawATK = (int)((baseStats.ATK * growthMult * _level / 50f) + baseStats.ATK);
            int rawDEF = (int)((baseStats.DEF * growthMult * _level / 50f) + baseStats.DEF);
            int rawSPD = (int)((baseStats.SPD * growthMult * _level / 50f) + baseStats.SPD);

            int finalATK = (int)(Mathf.Max(1, rawATK + dnaBonus) * GetPersonalityMod(StatType.ATK, boost, penalty));
            int finalDEF = (int)(Mathf.Max(1, rawDEF + dnaBonus) * GetPersonalityMod(StatType.DEF, boost, penalty));
            int finalSPD = (int)(Mathf.Max(1, rawSPD + dnaBonus) * GetPersonalityMod(StatType.SPD, boost, penalty));

            _computedStats = new StatsBlock(_maxHP, finalATK, finalDEF, finalSPD, baseStats.ACC);
            _xpNextLevel = ComputeXPThreshold(_level + 1);

            StatsChanged?.Invoke(this);
        }

        /// <summary>Get growth curve multiplier from GameSettings (with fallback defaults).</summary>
        private float GetGrowthMultiplier(GrowthCurve curve)
        {
            return curve switch
            {
                GrowthCurve.Fast => S != null ? S.GrowthMultiplierFast : DefaultGrowthFast,
                GrowthCurve.Slow => S != null ? S.GrowthMultiplierSlow : DefaultGrowthSlow,
                GrowthCurve.Erratic => S != null ? S.GrowthMultiplierErratic : DefaultGrowthErratic,
                _ => S != null ? S.GrowthMultiplierMedium : DefaultGrowthMedium,
            };
        }

        /// <summary>Get personality multiplier for a specific stat.</summary>
        private float GetPersonalityMod(StatType stat, float boost, float penalty)
        {
            return _personality switch
            {
                PersonalityTrait.Aggressive when stat == StatType.ATK => boost,
                PersonalityTrait.Aggressive when stat == StatType.DEF => penalty,
                PersonalityTrait.Cautious when stat == StatType.DEF => boost,
                PersonalityTrait.Cautious when stat == StatType.SPD => penalty,
                PersonalityTrait.Feral when stat == StatType.ATK => boost,
                PersonalityTrait.Feral when stat == StatType.DEF => penalty,
                PersonalityTrait.Curious when stat == StatType.SPD => boost,
                PersonalityTrait.Curious when stat == StatType.ATK => penalty,
                PersonalityTrait.Territorial when stat == StatType.DEF => boost,
                PersonalityTrait.Territorial when stat == StatType.SPD => penalty,
                _ => 1.0f,
            };
        }

        // ══════════════════════════════════════════════════════════════════
        // Move Management
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Learn the first 4 moves from config move pool at creature's current level.</summary>
        private void LearnStartingMoves()
        {
            _learnedMoveIds.Clear();
            _learnedMovePP.Clear();

            foreach (var entry in _config.MovePool)
            {
                if (entry.Level <= _level && _learnedMoveIds.Count < 4)
                {
                    _learnedMoveIds.Add(entry.MoveId);
                    var moveConfig = ConfigLoader.GetMove(entry.MoveId);
                    _learnedMovePP.Add(moveConfig != null ? moveConfig.PP : 5);
                }
            }
        }

        /// <summary>Learn a new move, replacing slot if full (MVP: auto-replace last slot at cap).</summary>
        public void LearnMove(string moveId, int slot = -1)
        {
            if (_learnedMoveIds.Contains(moveId)) return;

            var moveConfig = ConfigLoader.GetMove(moveId);
            if (moveConfig == null) return;

            if (slot < 0 || slot >= _learnedMoveIds.Count)
                slot = _learnedMoveIds.Count;

            if (slot >= 4)
                slot = 3;

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

        /// <summary>Remove a move from the specified slot index.</summary>
        public void ForgetMove(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _learnedMoveIds.Count) return;
            _learnedMoveIds.RemoveAt(slotIndex);
            _learnedMovePP.RemoveAt(slotIndex);
        }

        /// <summary>Deduct 1 PP from a move slot. Called by TurnManager after move execution.</summary>
        public void DeductPP(int moveSlot)
        {
            if (moveSlot < 0 || moveSlot >= _learnedMovePP.Count) return;
            _learnedMovePP[moveSlot] = Mathf.Max(0, _learnedMovePP[moveSlot] - 1);
        }

        /// <summary>Reset all move PP to their max values from MoveConfig.</summary>
        public void RestoreAllPP()
        {
            for (int i = 0; i < _learnedMoveIds.Count; i++)
            {
                var moveConfig = ConfigLoader.GetMove(_learnedMoveIds[i]);
                if (moveConfig != null)
                    _learnedMovePP[i] = moveConfig.PP;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // Body Part Management
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Equip a body part to a slot. Validates archetype compatibility.</summary>
        public bool EquipPart(BodySlot slot, string partId)
        {
            if (!Enumerable.Contains(_config.AvailableSlots, slot))
                return false;

            var partConfig = ConfigLoader.GetBodyPart(partId);
            if (partConfig == null) return false;

            int idx = _equippedPartSlots.IndexOf((int)slot);
            if (idx >= 0)
            {
                _equippedPartIds[idx] = partId;
            }
            else
            {
                _equippedPartSlots.Add((int)slot);
                _equippedPartIds.Add(partId);
            }
            _partsCacheDirty = true;
            _formsCacheDirty = true;
            RecalculateStats();
            return true;
        }

        /// <summary>Unequip a body part from a slot.</summary>
        public void UnequipPart(BodySlot slot)
        {
            int idx = _equippedPartSlots.IndexOf((int)slot);
            if (idx >= 0)
            {
                _equippedPartSlots.RemoveAt(idx);
                _equippedPartIds.RemoveAt(idx);
                _partsCacheDirty = true;
                _formsCacheDirty = true;
                RecalculateStats();
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // DNA Modification
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Apply a DNA modification. Increases instability; updates stats.
        /// If instability reaches blight threshold, ActiveSecondaryType becomes Blight.
        /// </summary>
        public void ApplyDNAMod(string modId, int instabilityIncrease = 5)
        {
            if (_appliedDNAModIds.Contains(modId)) return;

            _appliedDNAModIds.Add(modId);
            _instability = Mathf.Min(100, _instability + instabilityIncrease);
            RecalculateStats();
        }

        /// <summary>Remove a DNA modification. Decreases instability; updates stats.</summary>
        public void RemoveDNAMod(string modId, int instabilityDecrease = 5)
        {
            if (!_appliedDNAModIds.Remove(modId)) return;

            _instability = Mathf.Max(0, _instability - instabilityDecrease);
            RecalculateStats();
        }

        // ══════════════════════════════════════════════════════════════════
        // Battle State Management
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Set creature's grid position.</summary>
        public void SetGridPosition(Vector2Int position)
        {
            _gridPosition = position;
        }

        /// <summary>Set creature's facing direction.</summary>
        public void SetFacing(Facing facing)
        {
            _facing = facing;
        }

        /// <summary>Apply a status effect to this creature.</summary>
        public void ApplyStatusEffect(StatusEffect status)
        {
            if (!_activeStatusEffects.Contains(status))
                _activeStatusEffects.Add(status);
        }

        /// <summary>Remove a status effect.</summary>
        public void RemoveStatusEffect(StatusEffect status)
        {
            _activeStatusEffects.Remove(status);
        }

        /// <summary>Take damage. Triggers Fainted event when HP reaches 0.</summary>
        public void TakeDamage(int damage)
        {
            if (damage <= 0) return;
            _currentHP = Mathf.Max(0, _currentHP - damage);
            if (_currentHP == 0)
            {
                _isFainted = true;
                Fainted?.Invoke(this);
            }
        }

        /// <summary>Heal HP. Cannot heal fainted creature.</summary>
        public void Heal(int amount)
        {
            if (_isFainted || amount <= 0) return;
            _currentHP = Mathf.Min(_maxHP, _currentHP + amount);
        }

        /// <summary>Restore all HP (research station).</summary>
        public void HealFull()
        {
            if (_isFainted) return;
            _currentHP = _maxHP;
        }

        /// <summary>Revive creature at specified HP.</summary>
        public void Revive(int hp = 1)
        {
            _isFainted = false;
            _currentHP = Mathf.Clamp(hp, 1, _maxHP);
        }

        /// <summary>Set moved flag for turn tracking.</summary>
        public void SetMoved(bool moved) => _hasMoved = moved;

        /// <summary>Set acted flag for turn tracking.</summary>
        public void SetActed(bool acted) => _hasActed = acted;

        // ══════════════════════════════════════════════════════════════════
        // Affinity System
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Increase affinity with another creature ID.</summary>
        public void IncreaseAffinity(string targetCreatureId, int amount = 1)
        {
            int idx = _affinityCreatureIds.IndexOf(targetCreatureId);
            if (idx < 0)
            {
                _affinityCreatureIds.Add(targetCreatureId);
                _affinityValues.Add(Mathf.Min(10, amount));
            }
            else
            {
                _affinityValues[idx] = Mathf.Min(10, _affinityValues[idx] + amount);
            }
        }

        /// <summary>Get affinity level with another creature (0–10).</summary>
        public int GetAffinity(string targetCreatureId)
        {
            int idx = _affinityCreatureIds.IndexOf(targetCreatureId);
            return idx >= 0 ? _affinityValues[idx] : 0;
        }

        // ══════════════════════════════════════════════════════════════════
        // Leveling & XP
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Award XP and handle level-ups. Full heals on each level-up.</summary>
        public void AwardXP(int xpAmount)
        {
            if (xpAmount <= 0) return;
            int maxLvl = S != null ? S.MaxLevel : 50;

            _currentXP += xpAmount;
            while (_currentXP >= _xpNextLevel && _level < maxLvl)
            {
                _currentXP -= _xpNextLevel;
                _level++;
                RecalculateStats();
                _currentHP = _maxHP;
                _xpNextLevel = ComputeXPThreshold(_level + 1);
                CheckMovePoolForNewMoves();
                LeveledUp?.Invoke(this);
            }
        }

        /// <summary>Compute XP required to reach target level.</summary>
        private int ComputeXPThreshold(int level)
        {
            int mult = S != null ? S.XpBaseMultiplier : DefaultXpMultiplier;
            return level * level * mult;
        }

        /// <summary>Check move pool for moves learnable at current level.</summary>
        private void CheckMovePoolForNewMoves()
        {
            foreach (var entry in _config.MovePool)
            {
                if (entry.Level == _level && !_learnedMoveIds.Contains(entry.MoveId))
                    LearnMove(entry.MoveId);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // Personality
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Change creature personality. Recalculates stats.</summary>
        public void SetPersonality(PersonalityTrait trait)
        {
            _personality = trait;
            RecalculateStats();
        }
    }

    /// <summary>Stat identifier for modifier lookups.</summary>
    public enum StatType
    {
        HP, ATK, DEF, SPD, ACC
    }

    /// <summary>8-directional facing for grid flanking.</summary>
    public enum Facing
    {
        N, NE, E, SE, S, SW, W, NW
    }
}
