using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GeneForge.Core;
using GeneForge.Creatures;

namespace GeneForge.Tests
{
    [TestFixture]
    public class MoveDatabaseTests
    {
        static Dictionary<string, MoveConfig> _moves;
        static Dictionary<string, CreatureConfig> _creatures;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _moves = new Dictionary<string, MoveConfig>();
            var moveAssets = Resources.LoadAll<MoveConfig>("Data/Moves");
            foreach (var m in moveAssets)
                _moves[m.Id] = m;

            _creatures = new Dictionary<string, CreatureConfig>();
            var creatureAssets = Resources.LoadAll<CreatureConfig>("Data/Creatures");
            foreach (var c in creatureAssets)
                _creatures[c.Id] = c;
        }

        // ─── Helper ────────────────────────────────────────────────

        MoveConfig Get(string id)
        {
            Assert.IsTrue(_moves.ContainsKey(id), $"Move '{id}' not found");
            return _moves[id];
        }

        bool HasEffectType(MoveConfig move, MoveEffectType type)
        {
            foreach (var e in move.Effects)
                if (e.EffectType == type) return true;
            return false;
        }

        // ─── 1. All assets load ────────────────────────────────────

        [Test]
        public void Test_MoveDB_AllAssetsLoad_CountAtLeast45()
        {
            Assert.GreaterOrEqual(_moves.Count, 45,
                $"Expected >= 45 moves, got {_moves.Count}");
        }

        // ─── 2. No duplicate IDs ──────────────────────────────────

        [Test]
        public void Test_MoveDB_NoDuplicateIds()
        {
            var seen = new HashSet<string>();
            var assets = Resources.LoadAll<MoveConfig>("Data/Moves");
            foreach (var m in assets)
            {
                Assert.IsTrue(seen.Add(m.Id),
                    $"Duplicate move ID: '{m.Id}'");
            }
        }

        // ─── 3. Damaging moves have type and form ──────────────────

        [Test]
        public void Test_MoveDB_DamagingMoves_HaveTypeAndForm()
        {
            foreach (var kvp in _moves)
            {
                var move = kvp.Value;
                if (move.IsDamaging)
                {
                    Assert.AreNotEqual(DamageForm.None, move.Form,
                        $"Damaging move '{kvp.Key}' has Form=None");
                }
            }
        }

        // ─── 4. Status moves have zero power and None form ─────────

        [Test]
        public void Test_MoveDB_StatusMoves_HaveZeroPowerAndNoneForm()
        {
            foreach (var kvp in _moves)
            {
                var move = kvp.Value;
                if (move.Power == 0 && move.Form == DamageForm.None)
                {
                    Assert.IsTrue(
                        move.TargetType == TargetType.Self || move.Effects.Count > 0,
                        $"Status move '{kvp.Key}' has no effects and is not Self-target");
                }
            }
        }

        // ─── 5. Feint Attack always hits ───────────────────────────

        [Test]
        public void Test_MoveDB_FeintAttack_AlwaysHits()
        {
            var move = Get("feint-attack");
            Assert.AreEqual(0, move.Accuracy);
            Assert.IsTrue(move.AlwaysHits);
        }

        // ─── 6. Ice Shard has priority 1 ──────────────────────────

        [Test]
        public void Test_MoveDB_IceShard_HasPriorityOne()
        {
            var move = Get("ice-shard");
            Assert.AreEqual(1, move.Priority);
        }

        // ─── 7. Root Bind has Paralysis and TerrainCreate ──────────

        [Test]
        public void Test_MoveDB_RootBind_HasParalysisAndTerrainCreate()
        {
            var move = Get("root-bind");
            bool hasParalysis = false;
            bool hasTerrain = false;
            foreach (var e in move.Effects)
            {
                if (e.EffectType == MoveEffectType.ApplyStatus &&
                    e.StatusToApply == StatusEffect.Paralysis)
                    hasParalysis = true;
                if (e.EffectType == MoveEffectType.TerrainCreate)
                    hasTerrain = true;
            }
            Assert.IsTrue(hasParalysis, "root-bind missing ApplyStatus(Paralysis)");
            Assert.IsTrue(hasTerrain, "root-bind missing TerrainCreate");
        }

        // ─── 8. Harden targets self, affectsSelf ──────────────────

        [Test]
        public void Test_MoveDB_Harden_TargetSelf_AffectsSelf()
        {
            var move = Get("harden");
            Assert.AreEqual(TargetType.Self, move.TargetType);
            Assert.Greater(move.Effects.Count, 0, "harden has no effects");
            Assert.IsTrue(move.Effects[0].AffectsSelf, "harden effect[0].AffectsSelf is false");
        }

        // ─── 9. Discharge targets Adjacent ─────────────────────────

        [Test]
        public void Test_MoveDB_Discharge_TargetAdjacent()
        {
            var move = Get("discharge");
            Assert.AreEqual(TargetType.Adjacent, move.TargetType);
        }

        // ─── 10. Flame Claw — Thermal Physical ────────────────────

        [Test]
        public void Test_MoveDB_FlameClaw_ThermalPhysical()
        {
            var move = Get("flame-claw");
            Assert.AreEqual(CreatureType.Thermal, move.GenomeType);
            Assert.AreEqual(DamageForm.Physical, move.Form);
        }

        // ─── 11. Ember — Thermal Energy ───────────────────────────

        [Test]
        public void Test_MoveDB_Ember_ThermalEnergy()
        {
            var move = Get("ember");
            Assert.AreEqual(CreatureType.Thermal, move.GenomeType);
            Assert.AreEqual(DamageForm.Energy, move.Form);
        }

        // ─── 12. Toxic Spore — Toxic Bio ──────────────────────────

        [Test]
        public void Test_MoveDB_ToxicSpore_ToxicBio()
        {
            var move = Get("toxic-spore");
            Assert.AreEqual(CreatureType.Toxic, move.GenomeType);
            Assert.AreEqual(DamageForm.Bio, move.Form);
        }

        // ─── 13. Leech Sting — Toxic Bio with Drain ───────────────

        [Test]
        public void Test_MoveDB_LeechSting_ToxicBioWithDrain()
        {
            var move = Get("leech-sting");
            Assert.AreEqual(CreatureType.Toxic, move.GenomeType);
            Assert.AreEqual(DamageForm.Bio, move.Form);

            bool hasDrain = false;
            foreach (var e in move.Effects)
            {
                if (e.EffectType == MoveEffectType.Drain && e.Magnitude == 50)
                { hasDrain = true; break; }
            }
            Assert.IsTrue(hasDrain, "leech-sting missing Drain(50)");
        }

        // ─── 14. Boulder Slam — Power 100, Physical ───────────────

        [Test]
        public void Test_MoveDB_BoulderSlam_Power100_PhysicalForm()
        {
            var move = Get("boulder-slam");
            Assert.AreEqual(100, move.Power);
            Assert.AreEqual(DamageForm.Physical, move.Form);
        }

        // ─── 15. Spore Cloud — AoE Bio ────────────────────────────

        [Test]
        public void Test_MoveDB_SporeCloud_AoE_Bio()
        {
            var move = Get("spore-cloud");
            Assert.AreEqual(TargetType.AoE, move.TargetType);
            Assert.AreEqual(DamageForm.Bio, move.Form);
        }

        // ─── 16. All creature move IDs exist in move DB ───────────

        [Test]
        public void Test_MoveDB_AllCreatureMoveIds_ExistInMoveDB()
        {
            var broken = CreatureDatabase.ValidateMoveReferences(_creatures, _moves);
            Assert.IsEmpty(broken,
                $"Creature move pool references missing moves: {string.Join(", ", broken)}");
        }

        // ─── 17. Status moves have Power 0 ────────────────────────

        [Test]
        public void Test_MoveDB_StatusMoves_PowerZero(
            [Values("root-bind", "taunt", "harden", "agility", "purify", "genetic-lock", "screech")]
            string moveId)
        {
            var move = Get(moveId);
            Assert.AreEqual(0, move.Power, $"Status move '{moveId}' has Power != 0");
        }

        // ─── 18. Self-target moves always hit ─────────────────────

        [Test]
        public void Test_MoveDB_SelfTargetMoves_AlwaysHit(
            [Values("harden", "agility", "purify")] string moveId)
        {
            var move = Get(moveId);
            Assert.IsTrue(move.AlwaysHits,
                $"Self-target move '{moveId}' should always hit (accuracy=0)");
        }
    }
}
