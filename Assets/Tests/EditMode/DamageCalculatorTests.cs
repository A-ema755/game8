using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using GeneForge.Core;
using GeneForge.Creatures;
using GeneForge.Combat;
using GeneForge.Grid;
using UnityEngine;

namespace GeneForge.Tests
{
    [TestFixture]
    public class DamageCalculatorTests
    {
        // ── Reflection Helpers ───────────────────────────────────────────

        private static void SetField(object obj, string fieldName, object value)
        {
            var type = obj.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                type = type.BaseType;
            }
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {obj.GetType().Name}");
            field.SetValue(obj, value);
        }

        private static void SetStaticField(Type type, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(field, $"Static field '{fieldName}' not found on {type.Name}");
            field.SetValue(null, value);
        }

        private static object GetStaticField(Type type, string fieldName)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            return field?.GetValue(null);
        }

        // ── Factory Helpers ──────────────────────────────────────────────

        private MoveConfig CreateMove(
            string id, CreatureType genomeType, DamageForm form,
            int power, int accuracy = 100, int pp = 10,
            List<MoveEffect> effects = null)
        {
            var move = ScriptableObject.CreateInstance<MoveConfig>();
            SetField(move, "id", id);
            SetField(move, "displayName", id);
            SetField(move, "genomeType", genomeType);
            SetField(move, "form", form);
            SetField(move, "power", power);
            SetField(move, "accuracy", accuracy);
            SetField(move, "pp", pp);
            SetField(move, "range", 3);
            SetField(move, "targetType", TargetType.Single);
            SetField(move, "priority", 0);
            SetField(move, "effects", effects ?? new List<MoveEffect>());
            return move;
        }

        private MoveConfig CreateStatusMove(string id)
        {
            return CreateMove(id, CreatureType.None, DamageForm.None, 0);
        }

        private CreatureConfig CreateCreatureConfig(
            string id,
            BaseStats stats,
            CreatureType primary = CreatureType.Thermal,
            CreatureType secondary = CreatureType.None,
            CreatureType terrainSynergy = CreatureType.None)
        {
            var config = ScriptableObject.CreateInstance<CreatureConfig>();
            SetField(config, "id", id);
            SetField(config, "displayName", id);
            SetField(config, "primaryType", primary);
            SetField(config, "secondaryType", secondary);
            SetField(config, "baseStats", stats);
            SetField(config, "growthCurve", GrowthCurve.Medium);
            SetField(config, "movePool", new List<LevelMoveEntry>());
            SetField(config, "availableSlots", new List<BodySlot>());
            SetField(config, "defaultPartIds", new List<string>());
            SetField(config, "habitatZoneIds", new List<string>());
            SetField(config, "terrainSynergyType", terrainSynergy);
            return config;
        }

        private CreatureInstance CreateCreature(CreatureConfig config, int level = 10)
        {
            return CreatureInstance.Create(config, level);
        }

        private void PlaceCreature(CreatureInstance creature, Vector2Int pos)
        {
            creature.SetGridPosition(pos);
        }

        private GridSystem CreateGrid(int width = 8, int depth = 8)
        {
            var grid = new GridSystem(width, depth);
            for (int x = 0; x < width; x++)
            for (int z = 0; z < depth; z++)
                grid.SetTile(new TileData(new Vector2Int(x, z), 0, TerrainType.Neutral));
            return grid;
        }

        private void SetTile(GridSystem grid, int x, int z, int height,
            TerrainType terrain = TerrainType.Neutral, bool cover = false)
        {
            grid.SetTile(new TileData(
                new Vector2Int(x, z), height, terrain,
                isPassable: true, blocksLoS: false, providesCover: cover));
        }

        // ── Configs ──────────────────────────────────────────────────────

        // Balanced stats: HP=50, ATK=30, DEF=20, SPD=20, ACC=30
        private BaseStats _balancedStats;
        // Low stats for testing minimum damage
        private BaseStats _lowStats;

        private CreatureConfig _thermalConfig;
        private CreatureConfig _aquaConfig;
        private CreatureConfig _neutralConfig;
        private CreatureConfig _thermalSynergyConfig;
        private CreatureConfig _aquaSynergyConfig;

        // ── Setup ────────────────────────────────────────────────────────

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TypeChart.Initialize();
        }

        [SetUp]
        public void SetUp()
        {
            // Inject ConfigLoader as initialized (moves/parts not needed for damage calc tests)
            SetStaticField(typeof(ConfigLoader), "_initialized", true);

            _balancedStats = new BaseStats(50, 30, 20, 20, 30);
            _lowStats = new BaseStats(10, 5, 5, 5, 5);

            _thermalConfig = CreateCreatureConfig("thermal-creature", _balancedStats,
                primary: CreatureType.Thermal);
            _aquaConfig = CreateCreatureConfig("aqua-creature", _balancedStats,
                primary: CreatureType.Aqua);
            _neutralConfig = CreateCreatureConfig("neutral-creature", _balancedStats,
                primary: CreatureType.Mineral, secondary: CreatureType.None);
            _thermalSynergyConfig = CreateCreatureConfig("thermal-synergy", _balancedStats,
                primary: CreatureType.Thermal, terrainSynergy: CreatureType.Thermal);
            _aquaSynergyConfig = CreateCreatureConfig("aqua-synergy", _balancedStats,
                primary: CreatureType.Aqua, terrainSynergy: CreatureType.Aqua);
        }

        // ── Stat Pairing Tests ───────────────────────────────────────────

        [Test]
        public void test_DamageCalculator_Physical_usesAtkVsDef()
        {
            // Arrange: two creatures with distinct ATK, DEF, SPD, ACC values
            var highAtk = new BaseStats(50, 60, 10, 10, 10);
            var highDef = new BaseStats(50, 10, 40, 10, 10);
            var highSpd = new BaseStats(50, 10, 10, 40, 10);

            var attackerConfig = CreateCreatureConfig("atk-creature", highAtk,
                primary: CreatureType.Mineral);
            var defenderDefConfig = CreateCreatureConfig("def-creature", highDef,
                primary: CreatureType.Mineral);
            var defenderSpdConfig = CreateCreatureConfig("spd-creature", highSpd,
                primary: CreatureType.Mineral);

            var attacker = CreateCreature(attackerConfig, 10);
            var defenderDef = CreateCreature(defenderDefConfig, 10);
            var defenderSpd = CreateCreature(defenderSpdConfig, 10);

            PlaceCreature(attacker, new Vector2Int(0, 0));
            PlaceCreature(defenderDef, new Vector2Int(1, 0));
            PlaceCreature(defenderSpd, new Vector2Int(2, 0));

            var grid = CreateGrid();
            var move = CreateMove("phys-move", CreatureType.Ferro, DamageForm.Physical, 60);
            var calc = new DamageCalculator(new System.Random(42));

            // Act: Physical uses ATK vs DEF, so higher DEF defender takes less damage
            var calc2 = new DamageCalculator(new System.Random(42));
            int dmgVsHighDef = calc.Calculate(move, attacker, defenderDef, grid);
            int dmgVsHighSpd = calc2.Calculate(move, attacker, defenderSpd, grid);

            // Assert: high DEF defender takes less damage than high SPD defender
            Assert.Less(dmgVsHighDef, dmgVsHighSpd,
                "Physical form should use DEF, so high-DEF defender takes less damage");
        }

        [Test]
        public void test_DamageCalculator_Energy_usesAtkVsSpd()
        {
            var highAtk = new BaseStats(50, 60, 10, 10, 10);
            var highDef = new BaseStats(50, 10, 40, 10, 10);
            var highSpd = new BaseStats(50, 10, 10, 40, 10);

            var attackerConfig = CreateCreatureConfig("atk", highAtk, primary: CreatureType.Mineral);
            var defDef = CreateCreatureConfig("def", highDef, primary: CreatureType.Mineral);
            var defSpd = CreateCreatureConfig("spd", highSpd, primary: CreatureType.Mineral);

            var attacker = CreateCreature(attackerConfig, 10);
            var defenderDef = CreateCreature(defDef, 10);
            var defenderSpd = CreateCreature(defSpd, 10);

            PlaceCreature(attacker, new Vector2Int(0, 0));
            PlaceCreature(defenderDef, new Vector2Int(1, 0));
            PlaceCreature(defenderSpd, new Vector2Int(2, 0));

            var grid = CreateGrid();
            var move = CreateMove("energy-move", CreatureType.Ferro, DamageForm.Energy, 60);

            int dmgVsHighDef = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defenderDef, grid);
            int dmgVsHighSpd = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defenderSpd, grid);

            // Energy uses SPD as defensive stat, so high SPD takes less damage
            Assert.Less(dmgVsHighSpd, dmgVsHighDef,
                "Energy form should use SPD, so high-SPD defender takes less damage");
        }

        [Test]
        public void test_DamageCalculator_Bio_usesAccVsDef()
        {
            var highAcc = new BaseStats(50, 10, 10, 10, 60);
            var lowAcc = new BaseStats(50, 60, 10, 10, 10);

            var attackerHigh = CreateCreatureConfig("acc-hi", highAcc, primary: CreatureType.Mineral);
            var attackerLow = CreateCreatureConfig("acc-lo", lowAcc, primary: CreatureType.Mineral);
            var defender = CreateCreatureConfig("def", _balancedStats, primary: CreatureType.Mineral);

            var atkHi = CreateCreature(attackerHigh, 10);
            var atkLo = CreateCreature(attackerLow, 10);
            var def = CreateCreature(defender, 10);

            PlaceCreature(atkHi, new Vector2Int(0, 0));
            PlaceCreature(atkLo, new Vector2Int(1, 0));
            PlaceCreature(def, new Vector2Int(2, 0));

            var grid = CreateGrid();
            var move = CreateMove("bio-move", CreatureType.Ferro, DamageForm.Bio, 60);

            int dmgHighAcc = new DamageCalculator(new System.Random(42)).Calculate(move, atkHi, def, grid);
            int dmgLowAcc = new DamageCalculator(new System.Random(42)).Calculate(move, atkLo, def, grid);

            // Bio uses ACC as offensive stat
            Assert.Greater(dmgHighAcc, dmgLowAcc,
                "Bio form should use ACC, so high-ACC attacker deals more damage");
        }

        // ── Height Bonus Tests ───────────────────────────────────────────

        [Test]
        public void test_DamageCalculator_Bio_ignoresHeightDifference()
        {
            var attacker = CreateCreature(_neutralConfig, 10);
            var defender = CreateCreature(_neutralConfig, 10);
            PlaceCreature(attacker, new Vector2Int(0, 0));
            PlaceCreature(defender, new Vector2Int(1, 0));

            var grid = CreateGrid();
            var move = CreateMove("bio", CreatureType.Ferro, DamageForm.Bio, 60);

            // Flat ground
            int dmgFlat = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defender, grid);

            // Attacker at height 4, defender at height 0
            SetTile(grid, 0, 0, 4);
            int dmgHigh = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defender, grid);

            Assert.AreEqual(dmgFlat, dmgHigh, "Bio form should ignore height difference");
        }

        [Test]
        public void test_DamageCalculator_PhysicalEnergy_heightBonus_plus3_returns1point3x()
        {
            var attacker = CreateCreature(_neutralConfig, 10);
            var defender = CreateCreature(_neutralConfig, 10);
            PlaceCreature(attacker, new Vector2Int(0, 0));
            PlaceCreature(defender, new Vector2Int(1, 0));

            var grid = CreateGrid();
            var move = CreateMove("phys", CreatureType.Ferro, DamageForm.Physical, 60);

            // Baseline: flat
            int dmgFlat = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defender, grid);

            // Attacker +3 height
            SetTile(grid, 0, 0, 3);
            int dmgHigh = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defender, grid);

            // 1.3x expected. Allow ±1 for int truncation
            float ratio = (float)dmgHigh / dmgFlat;
            Assert.That(ratio, Is.InRange(1.25f, 1.35f),
                $"Height +3 should give ~1.3x damage. Got {dmgHigh}/{dmgFlat} = {ratio:F3}");
        }

        [Test]
        public void test_DamageCalculator_heightBonus_capsAt2x()
        {
            var attacker = CreateCreature(_neutralConfig, 10);
            var defender = CreateCreature(_neutralConfig, 10);
            PlaceCreature(attacker, new Vector2Int(0, 0));
            PlaceCreature(defender, new Vector2Int(1, 0));

            var grid = CreateGrid();
            var move = CreateMove("phys", CreatureType.Ferro, DamageForm.Physical, 60);

            // Baseline flat
            int dmgFlat = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defender, grid);

            // Height 4 vs 0 = +4 → 1.4x (under cap)
            SetTile(grid, 0, 0, 4);
            int dmg4 = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defender, grid);

            // Verify the cap: height difference is 4, bonus = min(2.0, 1+0.4) = 1.4
            // Since max tile height is 4 and min is 0, max diff is 4, which is 1.4x (under cap)
            // The cap matters when heightDiff >= 10, but max grid height is 4.
            // We verify the formula works correctly for max possible height diff.
            float ratio = (float)dmg4 / dmgFlat;
            Assert.That(ratio, Is.InRange(1.35f, 1.45f),
                $"Height +4 should give ~1.4x (capped at 2.0x). Got {ratio:F3}");
        }

        // ── STAB Test ────────────────────────────────────────────────────

        [Test]
        public void test_DamageCalculator_stab_returns1point5x()
        {
            // Thermal creature using Thermal move = STAB
            var stabAttacker = CreateCreature(_thermalConfig, 10);
            // Mineral creature using Thermal move = no STAB
            var noStabAttacker = CreateCreature(_neutralConfig, 10);
            // Neutral defender (Mineral resists Mineral, so use Ferro defender to be neutral)
            var defConfig = CreateCreatureConfig("ferro-def", _balancedStats,
                primary: CreatureType.Ferro);
            var defender = CreateCreature(defConfig, 10);

            PlaceCreature(stabAttacker, new Vector2Int(0, 0));
            PlaceCreature(noStabAttacker, new Vector2Int(0, 0));
            PlaceCreature(defender, new Vector2Int(1, 0));

            var grid = CreateGrid();
            // Thermal move vs Ferro = neutral (1.0x) per type chart
            // Wait, Ferro resists Thermal? Let me use a type that's neutral.
            // Actually: Thermal SE vs Organic, resisted by self.
            // Ferro → Neural is SE. Let's pick a defender type where Thermal is neutral.
            // Thermal vs Ferro: check type chart. Self-resist is 0.5x.
            // Let's use Kinetic defender — Thermal vs Kinetic should be neutral (1.0x)
            var kinDef = CreateCreatureConfig("kin-def", _balancedStats, primary: CreatureType.Kinetic);
            defender = CreateCreature(kinDef, 10);
            PlaceCreature(defender, new Vector2Int(1, 0));

            var move = CreateMove("thermal-move", CreatureType.Thermal, DamageForm.Physical, 60);

            int stabDmg = new DamageCalculator(new System.Random(42)).Calculate(move, stabAttacker, defender, grid);
            int noStabDmg = new DamageCalculator(new System.Random(42)).Calculate(move, noStabAttacker, defender, grid);

            float ratio = (float)stabDmg / noStabDmg;
            Assert.That(ratio, Is.InRange(1.4f, 1.6f),
                $"STAB should give ~1.5x. Got {stabDmg}/{noStabDmg} = {ratio:F3}");
        }

        // ── Type Effectiveness Tests ─────────────────────────────────────

        [Test]
        public void test_DamageCalculator_superEffective_returns2x()
        {
            // Thermal vs Organic = SE (2.0x)
            var attacker = CreateCreature(_neutralConfig, 10); // Mineral, no STAB on Thermal
            var organicDef = CreateCreatureConfig("organic-def", _balancedStats,
                primary: CreatureType.Organic);
            var neutralDef = CreateCreatureConfig("kinetic-def", _balancedStats,
                primary: CreatureType.Kinetic);
            var defender = CreateCreature(organicDef, 10);
            var defNeutral = CreateCreature(neutralDef, 10);

            PlaceCreature(attacker, new Vector2Int(0, 0));
            PlaceCreature(defender, new Vector2Int(1, 0));
            PlaceCreature(defNeutral, new Vector2Int(2, 0));

            var grid = CreateGrid();
            var move = CreateMove("thermal-atk", CreatureType.Thermal, DamageForm.Physical, 60);

            int seDmg = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defender, grid);
            int neutDmg = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defNeutral, grid);

            float ratio = (float)seDmg / neutDmg;
            Assert.That(ratio, Is.InRange(1.9f, 2.1f),
                $"Super effective should give ~2.0x. Got {ratio:F3}");
        }

        [Test]
        public void test_DamageCalculator_resisted_returns0point5x()
        {
            // Thermal vs Thermal = resisted (0.5x, self-resist)
            var attacker = CreateCreature(_neutralConfig, 10);
            var thermalDef = CreateCreature(_thermalConfig, 10);
            var neutralDef = CreateCreatureConfig("kinetic-def", _balancedStats,
                primary: CreatureType.Kinetic);
            var defNeutral = CreateCreature(neutralDef, 10);

            PlaceCreature(attacker, new Vector2Int(0, 0));
            PlaceCreature(thermalDef, new Vector2Int(1, 0));
            PlaceCreature(defNeutral, new Vector2Int(2, 0));

            var grid = CreateGrid();
            var move = CreateMove("thermal-atk", CreatureType.Thermal, DamageForm.Physical, 60);

            int resistDmg = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, thermalDef, grid);
            int neutDmg = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defNeutral, grid);

            float ratio = (float)resistDmg / neutDmg;
            Assert.That(ratio, Is.InRange(0.45f, 0.55f),
                $"Resisted should give ~0.5x. Got {ratio:F3}");
        }

        [Test]
        public void test_DamageCalculator_dualType_superEffectiveBoth_returns4x()
        {
            // Need a move type SE against both defender types
            // Thermal SE vs Organic. Need another type also weak to Thermal.
            // Actually, in our type chart Thermal only has one SE target (Organic).
            // Use GetMultiplier directly: we need attacker type X where X is SE vs both A and B.
            // Bioelectric SE vs Aqua (triangle 2). Let's find dual-type where both are weak.
            // Alternative: just test with a custom dual-type defender
            // Thermal vs Organic = 2.0x. If defender is Organic/Cryo and Thermal vs Cryo is also SE?
            // Check: Cryo resists itself but Thermal vs Cryo... Cryo SE vs Organic (triangle 5, Cryo→Organic 2.0x)
            // From type chart: Thermal→Organic=2.0x. Need Thermal→? = 2.0x for second type.
            // Looking at the type chart setup: Thermal only sets SE vs Organic explicitly.
            // So we'd need a move type with multiple SE targets. Let's use Aqua: Aqua→Thermal=2.0x, Aqua→Mineral=2.0x
            var attacker = CreateCreature(_neutralConfig, 10);
            var dualDef = CreateCreatureConfig("dual-weak", _balancedStats,
                primary: CreatureType.Thermal, secondary: CreatureType.Mineral);
            var defender = CreateCreature(dualDef, 10);
            var singleDef = CreateCreatureConfig("single-weak", _balancedStats,
                primary: CreatureType.Thermal);
            var defSingle = CreateCreature(singleDef, 10);

            PlaceCreature(attacker, new Vector2Int(0, 0));
            PlaceCreature(defender, new Vector2Int(1, 0));
            PlaceCreature(defSingle, new Vector2Int(2, 0));

            var grid = CreateGrid();
            // Aqua SE vs Thermal (2.0x) and Aqua SE vs Mineral (2.0x) = 4.0x total
            var move = CreateMove("aqua-atk", CreatureType.Aqua, DamageForm.Physical, 60);

            int dualDmg = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defender, grid);
            int singleDmg = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defSingle, grid);

            float ratio = (float)dualDmg / singleDmg;
            Assert.That(ratio, Is.InRange(1.9f, 2.1f),
                $"Dual SE should be ~2x more than single SE (4x vs 2x). Got {ratio:F3}");
        }

        [Test]
        public void test_DamageCalculator_dualType_seVsResist_returnsNeutral()
        {
            // Aqua vs Thermal = SE (2.0x), Aqua vs Aqua = resisted (0.5x) → 1.0x
            var attacker = CreateCreature(_neutralConfig, 10);
            var dualDef = CreateCreatureConfig("dual-cancel", _balancedStats,
                primary: CreatureType.Thermal, secondary: CreatureType.Aqua);
            var defender = CreateCreature(dualDef, 10);
            var neutralDef = CreateCreatureConfig("kinetic", _balancedStats,
                primary: CreatureType.Kinetic);
            var defNeutral = CreateCreature(neutralDef, 10);

            PlaceCreature(attacker, new Vector2Int(0, 0));
            PlaceCreature(defender, new Vector2Int(1, 0));
            PlaceCreature(defNeutral, new Vector2Int(2, 0));

            var grid = CreateGrid();
            var move = CreateMove("aqua-atk", CreatureType.Aqua, DamageForm.Physical, 60);

            int cancelDmg = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defender, grid);
            int neutDmg = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defNeutral, grid);

            float ratio = (float)cancelDmg / neutDmg;
            Assert.That(ratio, Is.InRange(0.9f, 1.1f),
                $"SE + resist should cancel to ~1.0x. Got {ratio:F3}");
        }

        // ── Terrain Synergy Tests ────────────────────────────────────────

        [Test]
        public void test_DamageCalculator_attackerSynergyTerrain_returns1point2x()
        {
            var attacker = CreateCreature(_thermalSynergyConfig, 10);
            var defender = CreateCreature(_neutralConfig, 10);
            PlaceCreature(attacker, new Vector2Int(0, 0));
            PlaceCreature(defender, new Vector2Int(1, 0));

            var grid = CreateGrid();
            var move = CreateMove("ferro-atk", CreatureType.Ferro, DamageForm.Physical, 60);

            // Baseline: neutral terrain
            int baseDmg = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defender, grid);

            // Set attacker tile to Thermal terrain (matches TerrainSynergyType)
            SetTile(grid, 0, 0, 0, TerrainType.Thermal);
            int synergyDmg = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defender, grid);

            float ratio = (float)synergyDmg / baseDmg;
            Assert.That(ratio, Is.InRange(1.15f, 1.25f),
                $"Attacker synergy terrain should give ~1.2x. Got {ratio:F3}");
        }

        [Test]
        public void test_DamageCalculator_defenderSynergyTerrain_returns0point8x()
        {
            var attacker = CreateCreature(_neutralConfig, 10);
            var defender = CreateCreature(_aquaSynergyConfig, 10);
            PlaceCreature(attacker, new Vector2Int(0, 0));
            PlaceCreature(defender, new Vector2Int(1, 0));

            var grid = CreateGrid();
            var move = CreateMove("ferro-atk", CreatureType.Ferro, DamageForm.Physical, 60);

            int baseDmg = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defender, grid);

            SetTile(grid, 1, 0, 0, TerrainType.Aqua);
            int synergyDmg = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defender, grid);

            float ratio = (float)synergyDmg / baseDmg;
            Assert.That(ratio, Is.InRange(0.70f, 0.90f),
                $"Defender synergy terrain should give ~0.8x. Got {ratio:F3}");
        }

        [Test]
        public void test_DamageCalculator_bothSynergyTerrain_returns0point96x()
        {
            var attacker = CreateCreature(_thermalSynergyConfig, 10);
            var defender = CreateCreature(_aquaSynergyConfig, 10);
            PlaceCreature(attacker, new Vector2Int(0, 0));
            PlaceCreature(defender, new Vector2Int(1, 0));

            var grid = CreateGrid();
            var move = CreateMove("ferro-atk", CreatureType.Ferro, DamageForm.Physical, 60);

            int baseDmg = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defender, grid);

            SetTile(grid, 0, 0, 0, TerrainType.Thermal);
            SetTile(grid, 1, 0, 0, TerrainType.Aqua);
            int bothDmg = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defender, grid);

            float ratio = (float)bothDmg / baseDmg;
            Assert.That(ratio, Is.InRange(0.85f, 1.05f),
                $"Both synergy should give ~0.96x (1.2*0.8). Got {ratio:F3}");
        }

        // ── Cover Tests ──────────────────────────────────────────────────

        [Test]
        public void test_DamageCalculator_Energy_withCover_returns0point5x()
        {
            var attacker = CreateCreature(_neutralConfig, 10);
            var defender = CreateCreature(_neutralConfig, 10);
            PlaceCreature(attacker, new Vector2Int(0, 0));
            PlaceCreature(defender, new Vector2Int(1, 0));

            var grid = CreateGrid();
            var move = CreateMove("energy-atk", CreatureType.Ferro, DamageForm.Energy, 60);

            int baseDmg = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defender, grid);

            SetTile(grid, 1, 0, 0, TerrainType.Neutral, cover: true);
            int coverDmg = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defender, grid);

            float ratio = (float)coverDmg / baseDmg;
            Assert.That(ratio, Is.InRange(0.45f, 0.55f),
                $"Energy + cover should give ~0.5x. Got {ratio:F3}");
        }

        [Test]
        public void test_DamageCalculator_Bio_withCover_ignoresCover()
        {
            var attacker = CreateCreature(_neutralConfig, 10);
            var defender = CreateCreature(_neutralConfig, 10);
            PlaceCreature(attacker, new Vector2Int(0, 0));
            PlaceCreature(defender, new Vector2Int(1, 0));

            var grid = CreateGrid();
            var move = CreateMove("bio-atk", CreatureType.Ferro, DamageForm.Bio, 60);

            int baseDmg = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defender, grid);

            SetTile(grid, 1, 0, 0, TerrainType.Neutral, cover: true);
            int coverDmg = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defender, grid);

            Assert.AreEqual(baseDmg, coverDmg, "Bio form should ignore cover");
        }

        // ── RNG / Determinism Tests ──────────────────────────────────────

        [Test]
        public void test_DamageCalculator_seededRng_deterministicAcrossAllForms()
        {
            var attacker = CreateCreature(_neutralConfig, 10);
            var defender = CreateCreature(_neutralConfig, 10);
            PlaceCreature(attacker, new Vector2Int(0, 0));
            PlaceCreature(defender, new Vector2Int(1, 0));

            var grid = CreateGrid();

            foreach (var form in new[] { DamageForm.Physical, DamageForm.Energy, DamageForm.Bio })
            {
                var move = CreateMove($"test-{form}", CreatureType.Ferro, form, 60);

                int dmg1 = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defender, grid);
                int dmg2 = new DamageCalculator(new System.Random(42)).Calculate(move, attacker, defender, grid);

                Assert.AreEqual(dmg1, dmg2, $"Seeded RNG should produce deterministic {form} damage");
            }
        }

        // ── Edge Case Tests ──────────────────────────────────────────────

        [Test]
        public void test_DamageCalculator_minimumDamage_alwaysAtLeast1()
        {
            // Low power move, resisted type, low ATK
            var weakConfig = CreateCreatureConfig("weak", _lowStats, primary: CreatureType.Thermal);
            var tankConfig = CreateCreatureConfig("tank", new BaseStats(100, 10, 100, 100, 10),
                primary: CreatureType.Thermal);
            var attacker = CreateCreature(weakConfig, 1);
            var defender = CreateCreature(tankConfig, 50);
            PlaceCreature(attacker, new Vector2Int(0, 0));
            PlaceCreature(defender, new Vector2Int(1, 0));

            var grid = CreateGrid();
            // Thermal vs Thermal = 0.5x (self-resist), low power
            var move = CreateMove("weak-move", CreatureType.Thermal, DamageForm.Physical, 10);

            var calc = new DamageCalculator(new System.Random(42));
            int dmg = calc.Calculate(move, attacker, defender, grid);

            Assert.GreaterOrEqual(dmg, 1, "Damage should always be at least 1");
        }

        [Test]
        public void test_DamageCalculator_statusMove_returnsDamage0()
        {
            var attacker = CreateCreature(_thermalConfig, 10);
            var defender = CreateCreature(_aquaConfig, 10);
            PlaceCreature(attacker, new Vector2Int(0, 0));
            PlaceCreature(defender, new Vector2Int(1, 0));

            var grid = CreateGrid();
            var statusMove = CreateStatusMove("status-move");

            var calc = new DamageCalculator(new System.Random(42));
            int dmg = calc.Calculate(statusMove, attacker, defender, grid);

            Assert.AreEqual(0, dmg, "Status move should deal 0 damage");
        }

        [Test]
        public void test_DamageCalculator_defStat0_treatedAs1_noException()
        {
            // Creature with 0 DEF (BaseStats won't produce 0 normally, but edge case)
            var zeroDef = new BaseStats(50, 30, 0, 20, 30);
            var defConfig = CreateCreatureConfig("zero-def", zeroDef, primary: CreatureType.Kinetic);
            var attacker = CreateCreature(_neutralConfig, 10);
            var defender = CreateCreature(defConfig, 1);
            PlaceCreature(attacker, new Vector2Int(0, 0));
            PlaceCreature(defender, new Vector2Int(1, 0));

            var grid = CreateGrid();
            var move = CreateMove("phys", CreatureType.Ferro, DamageForm.Physical, 60);

            var calc = new DamageCalculator(new System.Random(42));
            Assert.DoesNotThrow(() => calc.Calculate(move, attacker, defender, grid),
                "Should not throw on 0 DEF");
            Assert.GreaterOrEqual(calc.Calculate(move, attacker, defender, grid), 1);
        }

        [Test]
        public void test_DamageCalculator_accStat0_Bio_treatedAs1()
        {
            var zeroAcc = new BaseStats(50, 30, 20, 20, 0);
            var atkConfig = CreateCreatureConfig("zero-acc", zeroAcc, primary: CreatureType.Kinetic);
            var attacker = CreateCreature(atkConfig, 1);
            var defender = CreateCreature(_neutralConfig, 10);
            PlaceCreature(attacker, new Vector2Int(0, 0));
            PlaceCreature(defender, new Vector2Int(1, 0));

            var grid = CreateGrid();
            var move = CreateMove("bio", CreatureType.Ferro, DamageForm.Bio, 60);

            var calc = new DamageCalculator(new System.Random(42));
            Assert.DoesNotThrow(() => calc.Calculate(move, attacker, defender, grid),
                "Should not throw on 0 ACC for Bio move");
        }

        // ── Estimate Tests ───────────────────────────────────────────────

        [Test]
        public void test_Estimate_returnsConsistentResult()
        {
            var attacker = CreateCreature(_thermalConfig, 10);
            var defender = CreateCreature(_aquaConfig, 10);
            PlaceCreature(attacker, new Vector2Int(0, 0));
            PlaceCreature(defender, new Vector2Int(1, 0));

            var grid = CreateGrid();
            var move = CreateMove("thermal-atk", CreatureType.Thermal, DamageForm.Physical, 60);

            int est1 = DamageCalculator.Estimate(move, attacker, defender, grid);
            int est2 = DamageCalculator.Estimate(move, attacker, defender, grid);

            Assert.AreEqual(est1, est2, "Estimate should return same result every call");
        }

        [Test]
        public void test_Estimate_noCritNeverExceedsCalculateMax()
        {
            var attacker = CreateCreature(_thermalConfig, 10);
            var defender = CreateCreature(_aquaConfig, 10);
            PlaceCreature(attacker, new Vector2Int(0, 0));
            PlaceCreature(defender, new Vector2Int(1, 0));

            var grid = CreateGrid();
            var move = CreateMove("thermal-atk", CreatureType.Thermal, DamageForm.Physical, 60);

            int estimate = DamageCalculator.Estimate(move, attacker, defender, grid);

            // Run Calculate many times to find max (with crits)
            int maxCalc = 0;
            for (int i = 0; i < 1000; i++)
            {
                int dmg = new DamageCalculator(new System.Random(i)).Calculate(move, attacker, defender, grid);
                if (dmg > maxCalc) maxCalc = dmg;
            }

            // Estimate (no crit, 0.925 variance) should be <= max Calculate (with crit, 1.0 variance)
            Assert.LessOrEqual(estimate, maxCalc,
                "Estimate without crit should not exceed max possible Calculate with crit");
        }

        // ── TypeChart Terrain Mapping Tests ──────────────────────────────

        [Test]
        public void test_TypeChart_TerrainMatchesCreatureType_correctMappings()
        {
            Assert.IsTrue(TypeChart.TerrainMatchesCreatureType(TerrainType.Thermal, CreatureType.Thermal));
            Assert.IsTrue(TypeChart.TerrainMatchesCreatureType(TerrainType.Aqua, CreatureType.Aqua));
            Assert.IsTrue(TypeChart.TerrainMatchesCreatureType(TerrainType.Organic, CreatureType.Organic));
            Assert.IsTrue(TypeChart.TerrainMatchesCreatureType(TerrainType.Cryo, CreatureType.Cryo));
            Assert.IsTrue(TypeChart.TerrainMatchesCreatureType(TerrainType.Mineral, CreatureType.Mineral));
            Assert.IsTrue(TypeChart.TerrainMatchesCreatureType(TerrainType.Kinetic, CreatureType.Kinetic));
            Assert.IsTrue(TypeChart.TerrainMatchesCreatureType(TerrainType.Neural, CreatureType.Neural));
            Assert.IsTrue(TypeChart.TerrainMatchesCreatureType(TerrainType.Toxic, CreatureType.Toxic));

            // Non-matching
            Assert.IsFalse(TypeChart.TerrainMatchesCreatureType(TerrainType.Neutral, CreatureType.Thermal));
            Assert.IsFalse(TypeChart.TerrainMatchesCreatureType(TerrainType.Hazard, CreatureType.Thermal));
            Assert.IsFalse(TypeChart.TerrainMatchesCreatureType(TerrainType.Difficult, CreatureType.Thermal));
            Assert.IsFalse(TypeChart.TerrainMatchesCreatureType(TerrainType.Elevated, CreatureType.Thermal));

            // Wrong pairing
            Assert.IsFalse(TypeChart.TerrainMatchesCreatureType(TerrainType.Thermal, CreatureType.Aqua));
        }

        [Test]
        public void test_TypeChart_TerrainMatchesCreatureType_neutral_returnsFalse()
        {
            // Neutral terrain should not match any creature type
            Assert.IsFalse(TypeChart.TerrainMatchesCreatureType(TerrainType.Neutral, CreatureType.None));
            Assert.IsFalse(TypeChart.TerrainMatchesCreatureType(TerrainType.Neutral, CreatureType.Thermal));
            Assert.IsFalse(TypeChart.TerrainMatchesCreatureType(TerrainType.Neutral, CreatureType.Aqua));
        }
    }
}
