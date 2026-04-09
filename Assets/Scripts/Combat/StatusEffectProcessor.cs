using System.Collections.Generic;
using GeneForge.Core;
using GeneForge.Creatures;
using UnityEngine;

namespace GeneForge.Combat
{
    /// <summary>
    /// Processes status effect ticks at round boundaries.
    /// Pure C# class — no MonoBehaviour (ADR-002).
    /// Implements GDD turn-manager.md §3.7.
    /// </summary>
    public class StatusEffectProcessor : IStatusEffectProcessor
    {
        private readonly CombatSettings _settings;

        /// <summary>
        /// Create a StatusEffectProcessor reading tuning knobs from CombatSettings.
        /// </summary>
        public StatusEffectProcessor(CombatSettings settings)
        {
            _settings = settings;
        }

        /// <inheritdoc/>
        public bool ApplyStartOfRound(
            CreatureInstance creature,
            List<StatusEffectEntry> statusEntries,
            double rngRoll)
        {
            bool suppressed = false;

            // Iterate backwards — entries may be removed during iteration.
            for (int i = statusEntries.Count - 1; i >= 0; i--)
            {
                var entry = statusEntries[i];

                switch (entry.Effect)
                {
                    // ── DoT (indefinite, no suppression) ─────────────────
                    case StatusEffect.Burn:
                        int burnDmg = Mathf.Max(1, creature.MaxHP / _settings.BurnDotDivisor);
                        creature.TakeDamage(burnDmg);
                        break;

                    case StatusEffect.Poison:
                        int poisonDmg = Mathf.Max(1, creature.MaxHP / _settings.PoisonDotDivisor);
                        creature.TakeDamage(poisonDmg);
                        break;

                    // ── Suppression (probabilistic) ──────────────────────
                    case StatusEffect.Paralysis:
                        if (rngRoll < _settings.ParalysisSuppressionChance)
                            suppressed = true;
                        break;

                    // ── Suppression (unconditional + duration) ────────────
                    case StatusEffect.Sleep:
                        suppressed = true;
                        entry.RemainingRounds--;
                        statusEntries[i] = entry;
                        if (entry.RemainingRounds <= 0)
                        {
                            statusEntries.RemoveAt(i);
                            creature.RemoveStatusEffect(StatusEffect.Sleep);
                        }
                        break;

                    case StatusEffect.Freeze:
                        suppressed = true;
                        entry.RemainingRounds--;
                        statusEntries[i] = entry;
                        if (entry.RemainingRounds <= 0)
                        {
                            statusEntries.RemoveAt(i);
                            creature.RemoveStatusEffect(StatusEffect.Freeze);
                        }
                        break;

                    // Confusion handled at action time in TurnManager §3.8, not here.
                    // Taunt, Stealth: no start-of-round effect.
                }
            }

            return suppressed;
        }

        /// <inheritdoc/>
        public void DecrementDurations(
            CreatureInstance creature,
            List<StatusEffectEntry> statusEntries)
        {
            for (int i = statusEntries.Count - 1; i >= 0; i--)
            {
                var entry = statusEntries[i];

                // Indefinite effects (Burn, Poison, Paralysis) have RemainingRounds == -1.
                if (entry.IsIndefinite) continue;

                // Sleep/Freeze already decremented during ApplyStartOfRound — skip to avoid
                // double-decrement that would halve their effective duration.
                if (entry.Effect == StatusEffect.Sleep || entry.Effect == StatusEffect.Freeze)
                    continue;

                // Confusion, Taunt: decrement here.
                entry.RemainingRounds--;
                if (entry.RemainingRounds <= 0)
                {
                    statusEntries.RemoveAt(i);
                    creature.RemoveStatusEffect(entry.Effect);
                }
                else
                {
                    statusEntries[i] = entry;
                }
            }
        }
    }
}
