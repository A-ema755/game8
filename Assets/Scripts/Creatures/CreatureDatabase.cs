using System.Collections.Generic;
using GeneForge.Core;

namespace GeneForge.Creatures
{
    /// <summary>
    /// Static validation and query helper for the creature registry.
    /// Used by tests and editor scripts. Not a singleton — pure utility.
    /// </summary>
    public static class CreatureDatabase
    {
        /// <summary>
        /// Validates that all move IDs in creature move pools exist in the
        /// provided move registry. Returns list of broken references
        /// formatted as "creatureId:moveId".
        /// </summary>
        public static List<string> ValidateMoveReferences(
            IReadOnlyDictionary<string, CreatureConfig> creatures,
            IReadOnlyDictionary<string, MoveConfig> moves)
        {
            var broken = new List<string>();
            foreach (var kvp in creatures)
            {
                var creature = kvp.Value;
                foreach (var entry in creature.MovePool)
                {
                    if (!moves.ContainsKey(entry.MoveId))
                        broken.Add($"{kvp.Key}:{entry.MoveId}");
                }
            }
            return broken;
        }

        /// <summary>
        /// Returns all creatures that have a given primary type.
        /// </summary>
        public static List<CreatureConfig> GetByPrimaryType(
            IReadOnlyDictionary<string, CreatureConfig> creatures,
            CreatureType type)
        {
            var result = new List<CreatureConfig>();
            foreach (var kvp in creatures)
            {
                if (kvp.Value.PrimaryType == type)
                    result.Add(kvp.Value);
            }
            return result;
        }
    }
}
