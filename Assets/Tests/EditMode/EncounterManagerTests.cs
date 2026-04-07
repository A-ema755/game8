using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using GeneForge.Combat;
using GeneForge.Core;
using GeneForge.Creatures;
using GeneForge.Gameplay;
using GeneForge.Grid;
using UnityEngine;

namespace GeneForge.Tests
{
    /// <summary>
    /// EditMode unit tests for EncounterManager and EncounterConfig.
    /// Implements GDD: design/gdd/encounter-system.md §8 Acceptance Criteria (MVP scope).
    /// </summary>
    [TestFixture]
    public class EncounterManagerTests
    {
        // ── Test Fixtures ─────────────────────────────────────────────────

        private EncounterManager _manager;
        private CreatureConfig _speciesA;
        private CreatureConfig _speciesB;
        private readonly List<ScriptableObject> _createdAssets = new();
        private readonly Dictionary<string, CreatureConfig> _creatureRegistry = new();

        // ── Reflection Helpers ────────────────────────────────────────────

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

        // ── Factory Helpers ───────────────────────────────────────────────

        private CreatureConfig CreateCreatureConfig(string id)
        {
            var config = ScriptableObject.CreateInstance<CreatureConfig>();
            _createdAssets.Add(config);
            SetField(config, "id", id);
            SetField(config, "displayName", id);
            SetField(config, "primaryType", CreatureType.Thermal);
            SetField(config, "secondaryType", CreatureType.None);
            SetField(config, "baseStats", new BaseStats(50, 40, 30, 35, 100));
            SetField(config, "growthCurve", GrowthCurve.Medium);
            SetField(config, "movePool", new List<LevelMoveEntry>());
            SetField(config, "availableSlots", new List<BodySlot>());
            SetField(config, "defaultPartIds", new List<string>());
            SetField(config, "habitatZoneIds", new List<string>());
            SetField(config, "rarity", Rarity.Common);
            SetField(config, "bodyArchetype", BodyArchetype.Bipedal);
            return config;
        }

        private EncounterConfig CreateEncounterConfig(
            string id = "test-encounter",
            EncounterType type = EncounterType.Wild,
            int width = 8, int depth = 6,
            bool captureAllowed = true, bool retreatAllowed = true)
        {
            var config = ScriptableObject.CreateInstance<EncounterConfig>();
            _createdAssets.Add(config);
            SetField(config, "id", id);
            SetField(config, "displayName", id);
            SetField(config, "encounterType", type);
            SetField(config, "gridDimensions", new Vector2Int(width, depth));

            int size = width * depth;
            SetField(config, "heightMapFlat", new int[size]);
            SetField(config, "tileLayoutFlat", new TerrainType[size]);
            SetField(config, "playerStartTiles", new Vector2Int[] { new(0, 0), new(1, 0) });
            SetField(config, "enemies", new List<EncounterCreatureEntry>());
            SetField(config, "captureAllowed", captureAllowed);
            SetField(config, "retreatAllowed", retreatAllowed);
            SetField(config, "rpBase", 100);
            SetField(config, "totalWaves", 1);
            SetField(config, "waves", new List<WaveConfig>());
            SetField(config, "preEncounterDialogueId", "");
            SetField(config, "postEncounterDialogueId", "");

            return config;
        }

        private EncounterCreatureEntry CreateEnemyEntry(
            string speciesId, int level, Vector2Int spawnTile, bool isBoss = false)
        {
            var entry = new EncounterCreatureEntry();
            SetField(entry, "speciesId", speciesId);
            SetField(entry, "level", level);
            SetField(entry, "personalityConfigId", "");
            SetField(entry, "overrideMoves", new List<string>());
            SetField(entry, "spawnTile", spawnTile);
            SetField(entry, "isBoss", isBoss);
            return entry;
        }

        private void AddEnemy(EncounterConfig config, string speciesId, int level, Vector2Int spawnTile, bool isBoss = false)
        {
            var enemies = (List<EncounterCreatureEntry>)typeof(EncounterConfig)
                .GetField("enemies", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(config);
            enemies.Add(CreateEnemyEntry(speciesId, level, spawnTile, isBoss));
        }

        private PartyState CreatePartyWithCreatures(params CreatureConfig[] configs)
        {
            var party = new PartyState();
            foreach (var c in configs)
            {
                var creature = CreatureInstance.Create(c, 5);
                party.AddToParty(creature);
            }
            return party;
        }

        // ── Setup / Teardown ──────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _speciesA = CreateCreatureConfig("species-a");
            _speciesB = CreateCreatureConfig("species-b");

            _creatureRegistry.Clear();
            _creatureRegistry["species-a"] = _speciesA;
            _creatureRegistry["species-b"] = _speciesB;

            _manager = new EncounterManager(id =>
                _creatureRegistry.TryGetValue(id, out var c) ? c : null);
        }

        [TearDown]
        public void TearDown()
        {
            _creatureRegistry.Clear();

            foreach (var asset in _createdAssets)
                ScriptableObject.DestroyImmediate(asset);
            _createdAssets.Clear();
        }

        // ================================================================
        // ValidateConfig Tests
        // ================================================================

        [Test]
        public void test_ValidateConfig_ValidConfig_ReturnsNoErrors()
        {
            // Arrange
            var config = CreateEncounterConfig();
            AddEnemy(config, "species-a", 3, new Vector2Int(7, 5));

            // Act
            var errors = _manager.ValidateConfig(config);

            // Assert
            Assert.IsEmpty(errors);
        }

        [Test]
        public void test_ValidateConfig_NullConfig_ReturnsError()
        {
            // Act
            var errors = _manager.ValidateConfig(null);

            // Assert
            Assert.IsNotEmpty(errors);
            Assert.IsTrue(errors[0].Contains("null"));
        }

        [Test]
        public void test_ValidateConfig_GridWidthTooSmall_ReturnsError()
        {
            // Arrange — width 4 is below MinGridWidth (6)
            var config = CreateEncounterConfig(width: 4, depth: 6);
            AddEnemy(config, "species-a", 3, new Vector2Int(0, 0));

            // Act
            var errors = _manager.ValidateConfig(config);

            // Assert
            Assert.IsTrue(errors.Exists(e => e.Contains("width")), "Expected width error");
        }

        [Test]
        public void test_ValidateConfig_GridWidthTooLarge_ReturnsError()
        {
            // Arrange — width 20 is above MaxGridWidth (16)
            var config = CreateEncounterConfig(width: 20, depth: 6);
            AddEnemy(config, "species-a", 3, new Vector2Int(0, 0));

            // Act
            var errors = _manager.ValidateConfig(config);

            // Assert
            Assert.IsTrue(errors.Exists(e => e.Contains("width")), "Expected width error");
        }

        [Test]
        public void test_ValidateConfig_HeightMapWrongLength_ReturnsError()
        {
            // Arrange
            var config = CreateEncounterConfig(width: 8, depth: 6);
            SetField(config, "heightMapFlat", new int[10]); // wrong: should be 48
            AddEnemy(config, "species-a", 3, new Vector2Int(0, 0));

            // Act
            var errors = _manager.ValidateConfig(config);

            // Assert
            Assert.IsTrue(errors.Exists(e => e.Contains("HeightMapFlat")), "Expected height map length error");
        }

        [Test]
        public void test_ValidateConfig_TileLayoutWrongLength_ReturnsError()
        {
            // Arrange
            var config = CreateEncounterConfig(width: 8, depth: 6);
            SetField(config, "tileLayoutFlat", new TerrainType[10]); // wrong: should be 48
            AddEnemy(config, "species-a", 3, new Vector2Int(0, 0));

            // Act
            var errors = _manager.ValidateConfig(config);

            // Assert
            Assert.IsTrue(errors.Exists(e => e.Contains("TileLayoutFlat")), "Expected tile layout length error");
        }

        [Test]
        public void test_ValidateConfig_PlayerStartTileOutOfBounds_ReturnsError()
        {
            // Arrange
            var config = CreateEncounterConfig();
            SetField(config, "playerStartTiles", new Vector2Int[] { new(99, 99) });
            AddEnemy(config, "species-a", 3, new Vector2Int(0, 0));

            // Act
            var errors = _manager.ValidateConfig(config);

            // Assert
            Assert.IsTrue(errors.Exists(e => e.Contains("PlayerStartTile")), "Expected start tile error");
        }

        [Test]
        public void test_ValidateConfig_EnemySpawnTileOutOfBounds_ReturnsError()
        {
            // Arrange
            var config = CreateEncounterConfig();
            AddEnemy(config, "species-a", 3, new Vector2Int(99, 99));

            // Act
            var errors = _manager.ValidateConfig(config);

            // Assert
            Assert.IsTrue(errors.Exists(e => e.Contains("spawnTile")), "Expected spawn tile error");
        }

        [Test]
        public void test_ValidateConfig_EmptyEnemiesList_ReturnsError()
        {
            // Arrange — no enemies added
            var config = CreateEncounterConfig();

            // Act
            var errors = _manager.ValidateConfig(config);

            // Assert
            Assert.IsTrue(errors.Exists(e => e.Contains("empty")), "Expected empty enemies error");
        }

        [Test]
        public void test_ValidateConfig_EmptySpeciesId_ReturnsError()
        {
            // Arrange
            var config = CreateEncounterConfig();
            AddEnemy(config, "", 3, new Vector2Int(0, 0));

            // Act
            var errors = _manager.ValidateConfig(config);

            // Assert
            Assert.IsTrue(errors.Exists(e => e.Contains("speciesId")), "Expected empty speciesId error");
        }

        // ================================================================
        // InitializeEncounter Tests
        // ================================================================

        [Test]
        public void test_InitializeEncounter_GridDimensionsMatchConfig()
        {
            // Arrange
            var config = CreateEncounterConfig(width: 10, depth: 8);
            AddEnemy(config, "species-a", 5, new Vector2Int(9, 7));
            var party = CreatePartyWithCreatures(_speciesA);

            // Act
            var ctx = _manager.InitializeEncounter(config, party);

            // Assert
            Assert.AreEqual(10, ctx.Grid.Width);
            Assert.AreEqual(8, ctx.Grid.Depth);
        }

        [Test]
        public void test_InitializeEncounter_PlayerCreaturesAtCorrectTiles()
        {
            // Arrange
            var config = CreateEncounterConfig();
            SetField(config, "playerStartTiles", new Vector2Int[] { new(2, 1), new(3, 1) });
            AddEnemy(config, "species-a", 3, new Vector2Int(7, 5));
            var party = CreatePartyWithCreatures(_speciesA, _speciesB);

            // Act
            var ctx = _manager.InitializeEncounter(config, party);

            // Assert
            Assert.AreEqual(2, ctx.PlayerCreatures.Count);
            Assert.AreEqual(new Vector2Int(2, 1), ctx.PlayerCreatures[0].GridPosition);
            Assert.AreEqual(new Vector2Int(3, 1), ctx.PlayerCreatures[1].GridPosition);
        }

        [Test]
        public void test_InitializeEncounter_FaintedCreaturesSkipped()
        {
            // Arrange
            var config = CreateEncounterConfig();
            AddEnemy(config, "species-a", 3, new Vector2Int(7, 5));

            var party = new PartyState();
            var healthy = CreatureInstance.Create(_speciesA, 5);
            var fainted = CreatureInstance.Create(_speciesB, 5);
            fainted.TakeDamage(fainted.MaxHP); // faint the creature
            party.AddToParty(healthy);
            party.AddToParty(fainted);

            // Act
            var ctx = _manager.InitializeEncounter(config, party);

            // Assert — only healthy creature placed
            Assert.AreEqual(1, ctx.PlayerCreatures.Count);
            Assert.IsFalse(ctx.PlayerCreatures[0].IsFainted);
        }

        [Test]
        public void test_InitializeEncounter_EnemiesSpawnAtCorrectTilesAndLevels()
        {
            // Arrange
            var config = CreateEncounterConfig();
            AddEnemy(config, "species-a", 3, new Vector2Int(6, 4));
            AddEnemy(config, "species-b", 7, new Vector2Int(7, 5));
            var party = CreatePartyWithCreatures(_speciesA);

            // Act
            var ctx = _manager.InitializeEncounter(config, party);

            // Assert
            Assert.AreEqual(2, ctx.EnemyCreatures.Count);
            Assert.AreEqual(new Vector2Int(6, 4), ctx.EnemyCreatures[0].GridPosition);
            Assert.AreEqual(3, ctx.EnemyCreatures[0].Level);
            Assert.AreEqual(new Vector2Int(7, 5), ctx.EnemyCreatures[1].GridPosition);
            Assert.AreEqual(7, ctx.EnemyCreatures[1].Level);
        }

        [Test]
        public void test_InitializeEncounter_CaptureAllowedPropagated()
        {
            // Arrange
            var config = CreateEncounterConfig(captureAllowed: false);
            AddEnemy(config, "species-a", 3, new Vector2Int(7, 5));
            var party = CreatePartyWithCreatures(_speciesA);

            // Act
            var ctx = _manager.InitializeEncounter(config, party);

            // Assert
            Assert.IsFalse(ctx.CaptureAllowed);
        }

        [Test]
        public void test_InitializeEncounter_RetreatAllowedPropagated()
        {
            // Arrange
            var config = CreateEncounterConfig(retreatAllowed: false);
            AddEnemy(config, "species-a", 3, new Vector2Int(7, 5));
            var party = CreatePartyWithCreatures(_speciesA);

            // Act
            var ctx = _manager.InitializeEncounter(config, party);

            // Assert
            Assert.IsFalse(ctx.RetreatAllowed);
        }

        [Test]
        public void test_InitializeEncounter_EncounterTypePropagated()
        {
            // Arrange
            var config = CreateEncounterConfig(type: EncounterType.Trainer);
            AddEnemy(config, "species-a", 5, new Vector2Int(7, 5));
            var party = CreatePartyWithCreatures(_speciesA);

            // Act
            var ctx = _manager.InitializeEncounter(config, party);

            // Assert
            Assert.AreEqual(EncounterType.Trainer, ctx.EncounterType);
        }

        [Test]
        public void test_InitializeEncounter_InvalidSpeciesSkipped()
        {
            // Arrange — "bad-species" not in ConfigLoader
            var config = CreateEncounterConfig();
            AddEnemy(config, "bad-species", 3, new Vector2Int(6, 4));
            AddEnemy(config, "species-a", 5, new Vector2Int(7, 5));
            var party = CreatePartyWithCreatures(_speciesA);

            // Act
            var ctx = _manager.InitializeEncounter(config, party);

            // Assert — only species-a spawned, bad-species skipped
            Assert.AreEqual(1, ctx.EnemyCreatures.Count);
        }

        [Test]
        public void test_InitializeEncounter_OutOfBoundsStartTileFallsBackToCenter()
        {
            // Arrange
            var config = CreateEncounterConfig(width: 8, depth: 6);
            SetField(config, "playerStartTiles", new Vector2Int[] { new(99, 99) });
            AddEnemy(config, "species-a", 3, new Vector2Int(7, 5));
            var party = CreatePartyWithCreatures(_speciesA);

            // Act
            var ctx = _manager.InitializeEncounter(config, party);

            // Assert — should be at grid center (4, 3)
            Assert.AreEqual(1, ctx.PlayerCreatures.Count);
            Assert.AreEqual(new Vector2Int(4, 3), ctx.PlayerCreatures[0].GridPosition);
        }

        [Test]
        public void test_InitializeEncounter_BossFlag_Propagated()
        {
            // Arrange
            var config = CreateEncounterConfig(type: EncounterType.Trophy);
            AddEnemy(config, "species-a", 15, new Vector2Int(5, 3), isBoss: true);
            var party = CreatePartyWithCreatures(_speciesA);

            // Act
            var ctx = _manager.InitializeEncounter(config, party);

            // Assert — enemy spawned, context has correct type
            Assert.AreEqual(1, ctx.EnemyCreatures.Count);
            Assert.AreEqual(EncounterType.Trophy, ctx.EncounterType);
        }

        [Test]
        public void test_InitializeEncounter_TileOccupantSetForEnemies()
        {
            // Arrange
            var config = CreateEncounterConfig();
            AddEnemy(config, "species-a", 3, new Vector2Int(6, 4));
            var party = CreatePartyWithCreatures(_speciesA);

            // Act
            var ctx = _manager.InitializeEncounter(config, party);

            // Assert — tile at (6,4) has the enemy as occupant
            var tile = ctx.Grid.GetTile(6, 4);
            Assert.IsNotNull(tile.Occupant);
            Assert.AreEqual(ctx.EnemyCreatures[0], tile.Occupant);
        }

        // ================================================================
        // EncounterConfig Helper Tests
        // ================================================================

        [Test]
        public void test_EncounterConfig_GetHeight_ReturnsCorrectValue()
        {
            // Arrange — 8x6 grid, set height at (3, 2) to 2
            var config = CreateEncounterConfig(width: 8, depth: 6);
            var heights = new int[48];
            heights[2 * 8 + 3] = 2; // index = z * width + x
            SetField(config, "heightMapFlat", heights);

            // Act
            int height = config.GetHeight(3, 2);

            // Assert
            Assert.AreEqual(2, height);
        }

        [Test]
        public void test_EncounterConfig_GetTerrain_ReturnsCorrectValue()
        {
            // Arrange — 8x6 grid, set terrain at (5, 4) to Aqua
            var config = CreateEncounterConfig(width: 8, depth: 6);
            var terrains = new TerrainType[48];
            terrains[4 * 8 + 5] = TerrainType.Aqua; // index = z * width + x
            SetField(config, "tileLayoutFlat", terrains);

            // Act
            var terrain = config.GetTerrain(5, 4);

            // Assert
            Assert.AreEqual(TerrainType.Aqua, terrain);
        }
    }
}
