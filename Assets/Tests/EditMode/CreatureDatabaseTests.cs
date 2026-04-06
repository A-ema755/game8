using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using GeneForge.Core;
using GeneForge.Creatures;

namespace GeneForge.Tests
{
    [TestFixture]
    public class CreatureDatabaseTests
    {
        static readonly BindingFlags InstancePrivate =
            BindingFlags.Instance | BindingFlags.NonPublic;

        static Dictionary<string, CreatureConfig> _creatures;
        static Dictionary<string, MoveConfig> _moves;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _creatures = new Dictionary<string, CreatureConfig>();
            var creatureAssets = Resources.LoadAll<CreatureConfig>("Data/Creatures");
            foreach (var c in creatureAssets)
                _creatures[c.Id] = c;

            _moves = new Dictionary<string, MoveConfig>();
            var moveAssets = Resources.LoadAll<MoveConfig>("Data/Moves");
            foreach (var m in moveAssets)
                _moves[m.Id] = m;
        }

        // ─── Archetype → Expected Slots (from GDD 3.2) ─────────────

        static readonly Dictionary<BodyArchetype, HashSet<BodySlot>> ExpectedSlots = new()
        {
            { BodyArchetype.Bipedal, new HashSet<BodySlot>
                { BodySlot.Head, BodySlot.Back, BodySlot.LeftArm, BodySlot.RightArm, BodySlot.Tail, BodySlot.Legs } },
            { BodyArchetype.Quadruped, new HashSet<BodySlot>
                { BodySlot.Head, BodySlot.Back, BodySlot.Tail, BodySlot.Legs, BodySlot.Hide } },
            { BodyArchetype.Serpentine, new HashSet<BodySlot>
                { BodySlot.Head, BodySlot.BodyUpper, BodySlot.BodyLower, BodySlot.Tail } },
            { BodyArchetype.Avian, new HashSet<BodySlot>
                { BodySlot.Head, BodySlot.Wings, BodySlot.Tail, BodySlot.Talons } },
            { BodyArchetype.Amorphous, new HashSet<BodySlot>
                { BodySlot.CoreA, BodySlot.CoreB, BodySlot.CoreC, BodySlot.Appendage } },
        };

        // ─── 1. All assets load ─────────────────────────────────────

        [Test]
        public void Test_CreatureDB_AllAssetsLoad_CountAtLeast14()
        {
            Assert.GreaterOrEqual(_creatures.Count, 14,
                $"Expected >= 14 creatures, got {_creatures.Count}");
        }

        // ─── 2. Each creature has level-1 moves ────────────────────

        [Test]
        public void Test_CreatureDB_EachCreature_HasLevelOneMoves()
        {
            foreach (var kvp in _creatures)
            {
                bool hasLevelOne = false;
                foreach (var entry in kvp.Value.MovePool)
                {
                    if (entry.Level == 1) { hasLevelOne = true; break; }
                }
                Assert.IsTrue(hasLevelOne,
                    $"Creature '{kvp.Key}' has no level-1 move in its move pool");
            }
        }

        // ─── 3. Move pool IDs exist in move registry ───────────────

        [Test]
        public void Test_CreatureDB_MovePoolIds_ExistInMoveRegistry()
        {
            var broken = CreatureDatabase.ValidateMoveReferences(_creatures, _moves);
            Assert.IsEmpty(broken,
                $"Broken move references: {string.Join(", ", broken)}");
        }

        // ─── 4. No duplicate IDs ───────────────────────────────────

        [Test]
        public void Test_CreatureDB_NoDuplicateIds()
        {
            var seen = new HashSet<string>();
            var assets = Resources.LoadAll<CreatureConfig>("Data/Creatures");
            foreach (var c in assets)
            {
                Assert.IsTrue(seen.Add(c.Id),
                    $"Duplicate creature ID: '{c.Id}'");
            }
        }

        // ─── 5. IsDualType true for dual-type ──────────────────────

        [Test]
        public void Test_CreatureDB_IsDualType_TrueForDualType()
        {
            Assert.IsTrue(_creatures.ContainsKey("thorn-slug"), "thorn-slug not found");
            Assert.IsTrue(_creatures["thorn-slug"].IsDualType);
        }

        // ─── 6. IsDualType false for single-type ───────────────────

        [Test]
        public void Test_CreatureDB_IsDualType_FalseForSingleType()
        {
            Assert.IsTrue(_creatures.ContainsKey("emberfox"), "emberfox not found");
            Assert.IsFalse(_creatures["emberfox"].IsDualType);
        }

        // ─── 7. AvailableSlots non-empty ───────────────────────────

        [Test]
        public void Test_CreatureDB_AvailableSlots_NonEmptyForAllCreatures()
        {
            foreach (var kvp in _creatures)
            {
                Assert.Greater(kvp.Value.AvailableSlots.Count, 0,
                    $"Creature '{kvp.Key}' has empty availableSlots");
            }
        }

        // ─── 8. CatchRate within bounds ────────────────────────────

        [Test]
        public void Test_CreatureDB_CatchRate_WithinBounds()
        {
            foreach (var kvp in _creatures)
            {
                Assert.GreaterOrEqual(kvp.Value.CatchRate, 0,
                    $"Creature '{kvp.Key}' catchRate < 0");
                Assert.LessOrEqual(kvp.Value.CatchRate, 255,
                    $"Creature '{kvp.Key}' catchRate > 255");
            }
        }

        // ─── 9. DefaultPartIds non-empty ───────────────────────────

        [Test]
        public void Test_CreatureDB_DefaultPartIds_NonEmpty()
        {
            foreach (var kvp in _creatures)
            {
                Assert.GreaterOrEqual(kvp.Value.DefaultPartIds.Count, 1,
                    $"Creature '{kvp.Key}' has no default part IDs");
            }
        }

        // ─── 10. Emberfox primary type ─────────────────────────────

        [Test]
        public void Test_CreatureDB_Emberfox_PrimaryType_IsThermal()
        {
            Assert.AreEqual(CreatureType.Thermal, _creatures["emberfox"].PrimaryType);
        }

        // ─── 11. Thornslug IsDualType ──────────────────────────────

        [Test]
        public void Test_CreatureDB_Thornslug_IsDualType_True()
        {
            Assert.IsTrue(_creatures["thorn-slug"].IsDualType);
        }

        // ─── 12. Mosshell move pool contains boulder-slam at 19 ────

        [Test]
        public void Test_CreatureDB_Mosshell_MovePool_ContainsBoulderSlamAt19()
        {
            var mosshell = _creatures["mosshell"];
            bool found = false;
            foreach (var entry in mosshell.MovePool)
            {
                if (entry.MoveId == "boulder-slam" && entry.Level == 19)
                { found = true; break; }
            }
            Assert.IsTrue(found, "Mosshell missing boulder-slam at level 19");
        }

        // ─── 13. Type coverage — at least 9 primary types ─────────

        [Test]
        public void Test_CreatureDB_TypeCoverage_AtLeastNineTypes()
        {
            var types = new HashSet<CreatureType>();
            foreach (var kvp in _creatures)
                types.Add(kvp.Value.PrimaryType);

            Assert.GreaterOrEqual(types.Count, 9,
                $"Only {types.Count} distinct primary types represented");
        }

        // ─── 14. Archetype slots match GDD 3.2 ────────────────────

        [Test]
        public void Test_CreatureDB_AllCreatures_MatchArchetypeSlots()
        {
            foreach (var kvp in _creatures)
            {
                var creature = kvp.Value;
                var expected = ExpectedSlots[creature.BodyArchetype];
                var actual = new HashSet<BodySlot>();
                foreach (var slot in creature.AvailableSlots)
                    actual.Add(slot);

                Assert.IsTrue(expected.SetEquals(actual),
                    $"Creature '{kvp.Key}' ({creature.BodyArchetype}) slots mismatch. " +
                    $"Expected: {string.Join(",", expected)}, Got: {string.Join(",", actual)}");
            }
        }
    }
}
