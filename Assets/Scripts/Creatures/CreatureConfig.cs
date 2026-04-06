using System;
using System.Collections.Generic;
using GeneForge.Core;
using UnityEngine;

namespace GeneForge.Creatures
{
    /// <summary>Base stat block for a species. Lv1 base values. Struct per ADR-009.</summary>
    [Serializable]
    public struct BaseStats
    {
        [SerializeField] int hp;
        [SerializeField] int atk;
        [SerializeField] int def;
        [SerializeField] int spd;
        [SerializeField] int acc;

        /// <summary>Base max HP.</summary>
        public int HP => hp;

        /// <summary>Physical attack.</summary>
        public int ATK => atk;

        /// <summary>Physical defense.</summary>
        public int DEF => def;

        /// <summary>Speed (initiative, move priority).</summary>
        public int SPD => spd;

        /// <summary>Accuracy modifier (base 100).</summary>
        public int ACC => acc;

        /// <summary>Creates a BaseStats with the given values.</summary>
        public BaseStats(int hp, int atk, int def, int spd, int acc)
        {
            this.hp = hp;
            this.atk = atk;
            this.def = def;
            this.spd = spd;
            this.acc = acc;
        }
    }

    /// <summary>A move learnable at a specific level threshold. Struct per ADR-009.</summary>
    [Serializable]
    public struct LevelMoveEntry
    {
        [SerializeField] int level;
        [SerializeField] string moveId;

        /// <summary>Level at which this move is learned.</summary>
        public int Level => level;

        /// <summary>Config ID of the move (kebab-case).</summary>
        public string MoveId => moveId;

        /// <summary>Creates a LevelMoveEntry with the given values.</summary>
        public LevelMoveEntry(int level, string moveId)
        {
            this.level = level;
            this.moveId = moveId;
        }
    }

    /// <summary>
    /// Immutable species blueprint. One asset per species in Resources/Data/Creatures/.
    /// Do not modify at runtime — all runtime state lives in CreatureInstance.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCreature", menuName = "GeneForge/Creature Config")]
    public class CreatureConfig : ConfigBase
    {
        [Header("Identity")]
        [SerializeField] CreatureType primaryType;
        [SerializeField] CreatureType secondaryType;
        [SerializeField] Rarity rarity;
        [SerializeField] BodyArchetype bodyArchetype;

        [Header("Base Stats")]
        [SerializeField] BaseStats baseStats;

        [Header("Move Pool")]
        [SerializeField] List<LevelMoveEntry> movePool;

        [Header("Body")]
        [SerializeField] List<BodySlot> availableSlots;
        [SerializeField] List<string> defaultPartIds;
        [SerializeField] string signaturePartId;

        [Header("Progression")]
        [SerializeField] GrowthCurve growthCurve;
        [SerializeField] int baseXpYield;
        [SerializeField] int catchRate;

        [Header("World")]
        [SerializeField] List<string> habitatZoneIds;
        [SerializeField] CreatureType terrainSynergyType;

        /// <summary>Primary genome type.</summary>
        public CreatureType PrimaryType => primaryType;

        /// <summary>Secondary genome type. None if single-type.</summary>
        public CreatureType SecondaryType => secondaryType;

        /// <summary>Encounter rarity tier.</summary>
        public Rarity Rarity => rarity;

        /// <summary>Broad body shape archetype — determines available BodySlots.</summary>
        public BodyArchetype BodyArchetype => bodyArchetype;

        /// <summary>Lv1 base stat block.</summary>
        public BaseStats BaseStats => baseStats;

        /// <summary>Moves learnable by level-up.</summary>
        public IReadOnlyList<LevelMoveEntry> MovePool => movePool;

        /// <summary>Body slots available for part equipping (determined by archetype).</summary>
        public IReadOnlyList<BodySlot> AvailableSlots => availableSlots;

        /// <summary>Body parts equipped at capture/creation.</summary>
        public IReadOnlyList<string> DefaultPartIds => defaultPartIds;

        /// <summary>Config ID of this species' signature body part.</summary>
        public string SignaturePartId => signaturePartId;

        /// <summary>XP growth rate curve.</summary>
        public GrowthCurve GrowthCurve => growthCurve;

        /// <summary>XP awarded to opponent on defeat.</summary>
        public int BaseXpYield => baseXpYield;

        /// <summary>Catch rate 0-255. Higher = easier. 0 = uncatchable.</summary>
        public int CatchRate => catchRate;

        /// <summary>Zone IDs where this species spawns naturally.</summary>
        public IReadOnlyList<string> HabitatZoneIds => habitatZoneIds;

        /// <summary>Type of terrain tile that grants synergy bonus.</summary>
        public CreatureType TerrainSynergyType => terrainSynergyType;

        /// <summary>True if this creature has a secondary type.</summary>
        public bool IsDualType => secondaryType != CreatureType.None;
    }
}
