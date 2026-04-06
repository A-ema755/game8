using System;
using System.Collections.Generic;
using GeneForge.Core;
using UnityEngine;

namespace GeneForge.Creatures
{
    /// <summary>Effect types that can be attached to moves.</summary>
    public enum MoveEffectType
    {
        ApplyStatus = 0,
        StatStage = 1,
        Recoil = 2,
        Drain = 3,
        ForcedMove = 4,
        IgnoreDefense = 5,
        MultiHit = 6,
        HighCrit = 7,
        Flinch = 8,
        TerrainCreate = 9,
        PriorityNext = 10
    }

    /// <summary>
    /// A single on-hit or on-use effect attached to a move.
    /// Class (not struct) — optional fields benefit from reference semantics in Inspector.
    /// </summary>
    [Serializable]
    public class MoveEffect
    {
        [SerializeField] MoveEffectType effectType;
        [SerializeField] float chance;
        [SerializeField] int magnitude;
        [SerializeField] StatusEffect statusToApply;
        [SerializeField] bool affectsSelf;

        // statTarget encodes which stat is affected by StatStage effects.
        // 0 = ATK, 1 = DEF, 2 = SPD
        [SerializeField] int statTarget;

        /// <summary>The type of effect.</summary>
        public MoveEffectType EffectType => effectType;

        /// <summary>Probability 0.0–1.0. 1.0 = guaranteed.</summary>
        public float Chance => chance;

        /// <summary>Effect-specific value (stat stages, damage %, tile count).</summary>
        public int Magnitude => magnitude;

        /// <summary>Status condition to inflict (for ApplyStatus effects).</summary>
        public StatusEffect StatusToApply => statusToApply;

        /// <summary>True if effect targets the user, not the target.</summary>
        public bool AffectsSelf => affectsSelf;

        /// <summary>Stat targeted by StatStage effects. 0=ATK, 1=DEF, 2=SPD.</summary>
        public int StatTarget => statTarget;
    }

    /// <summary>
    /// Immutable move blueprint. One asset per move in Resources/Data/Moves/.
    /// Do not modify at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMove", menuName = "GeneForge/Move Config")]
    public class MoveConfig : ConfigBase
    {
        [Header("Genome Type & Damage Form")]
        [SerializeField] CreatureType genomeType;
        [SerializeField] DamageForm form;

        [Header("Stats")]
        [SerializeField] int power;
        [SerializeField] int accuracy;
        [SerializeField] int pp;
        [SerializeField] int priority;

        [Header("Targeting")]
        [SerializeField] TargetType targetType;
        [SerializeField] int range;

        [Header("Effects")]
        [SerializeField] List<MoveEffect> effects;

        /// <summary>Genome type for type effectiveness and STAB.</summary>
        public CreatureType GenomeType => genomeType;

        /// <summary>Damage form: Physical, Energy, Bio, or None (status).</summary>
        public DamageForm Form => form;

        /// <summary>Base power. 0 for status moves.</summary>
        public int Power => power;

        /// <summary>Accuracy 0-100. 0 = always hits (sentinel).</summary>
        public int Accuracy => accuracy;

        /// <summary>Power Points — uses per battle.</summary>
        public int PP => pp;

        /// <summary>Priority bracket. Higher goes first regardless of speed.</summary>
        public int Priority => priority;

        /// <summary>Targeting pattern.</summary>
        public TargetType TargetType => targetType;

        /// <summary>Chebyshev distance range.</summary>
        public int Range => range;

        /// <summary>On-hit and on-use effects.</summary>
        public IReadOnlyList<MoveEffect> Effects => effects;

        /// <summary>True if this move deals damage (has a form and power > 0).</summary>
        public bool IsDamaging => form != DamageForm.None && power > 0;

        /// <summary>True if this move always hits (accuracy sentinel = 0).</summary>
        public bool AlwaysHits => accuracy == 0;
    }
}
