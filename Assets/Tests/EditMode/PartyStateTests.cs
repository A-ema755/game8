using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using GeneForge.Core;
using GeneForge.Creatures;
using GeneForge.Gameplay;
using UnityEngine;

namespace GeneForge.Tests
{
    [TestFixture]
    public class PartyStateTests
    {
        // ── Test Configs ─────────────────────────────────────────────────
        private CreatureConfig _testConfig;
        private MoveConfig _moveA;

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

        private MoveConfig CreateMoveConfig(string id, string name, int pp)
        {
            var move = ScriptableObject.CreateInstance<MoveConfig>();
            SetField(move, "id", id);
            SetField(move, "displayName", name);
            SetField(move, "pp", pp);
            return move;
        }

        private CreatureConfig CreateCreatureConfig(string id, string name)
        {
            var config = ScriptableObject.CreateInstance<CreatureConfig>();
            SetField(config, "id", id);
            SetField(config, "displayName", name);
            SetField(config, "primaryType", CreatureType.Thermal);
            SetField(config, "secondaryType", CreatureType.None);
            SetField(config, "baseStats", new BaseStats(50, 40, 30, 35, 100));
            SetField(config, "growthCurve", GrowthCurve.Medium);
            SetField(config, "movePool", new List<LevelMoveEntry> { new LevelMoveEntry(1, "move-a") });
            SetField(config, "availableSlots", new List<BodySlot> { BodySlot.Head });
            SetField(config, "defaultPartIds", new List<string>());
            SetField(config, "habitatZoneIds", new List<string>());
            return config;
        }

        private void InjectIntoConfigLoader()
        {
            var movesDict = (Dictionary<string, MoveConfig>)GetStaticField(typeof(ConfigLoader), "_moves");
            movesDict.Clear();
            movesDict["move-a"] = _moveA;
            SetStaticField(typeof(ConfigLoader), "_initialized", true);
        }

        private CreatureInstance MakeCreature(string nickname = null)
            => CreatureInstance.Create(_testConfig, 1, nickname);

        private CreatureInstance MakeFainted(string nickname = null)
        {
            var c = MakeCreature(nickname);
            c.TakeDamage(c.MaxHP);
            return c;
        }

        // ── Setup / Teardown ─────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _moveA = CreateMoveConfig("move-a", "Flame Claw", 10);
            _testConfig = CreateCreatureConfig("emberfox", "Emberfox");
            InjectIntoConfigLoader();
        }

        [TearDown]
        public void TearDown()
        {
            var movesDict = (Dictionary<string, MoveConfig>)GetStaticField(typeof(ConfigLoader), "_moves");
            movesDict?.Clear();
            SetStaticField(typeof(ConfigLoader), "_initialized", false);
            ScriptableObject.DestroyImmediate(_testConfig);
            ScriptableObject.DestroyImmediate(_moveA);
        }

        // ================================================================
        // GDD Acceptance Criteria — Party System
        // ================================================================

        [Test]
        public void test_PartyState_AddToParty_UpToMaxPartySize_ReturnsTrue()
        {
            // Arrange
            var party = new PartyState();

            // Act / Assert — 6 slots fill up, 7th is rejected
            for (int i = 0; i < 6; i++)
                Assert.IsTrue(party.AddToParty(MakeCreature()), $"Slot {i} should succeed");

            Assert.IsFalse(party.AddToParty(MakeCreature()), "7th add should return false");
            Assert.AreEqual(6, party.ActiveCount);
        }

        [Test]
        public void test_PartyState_AddToStorage_HasNoCap()
        {
            // Arrange
            var party = new PartyState();

            // Act
            for (int i = 0; i < 55; i++)
                party.AddToStorage(MakeCreature());

            // Assert
            Assert.AreEqual(55, party.StorageCount);
        }

        [Test]
        public void test_PartyState_Lead_ReturnsSlot0Creature()
        {
            // Arrange
            var party = new PartyState();
            var first = MakeCreature("First");
            party.AddToParty(first);
            party.AddToParty(MakeCreature("Second"));

            // Assert
            Assert.AreSame(first, party.Lead);
        }

        [Test]
        public void test_PartyState_Lead_ReturnsNullWhenPartyEmpty()
        {
            // Arrange / Assert
            var party = new PartyState();
            Assert.IsNull(party.Lead);
        }

        [Test]
        public void test_PartyState_HasConscious_TrueWhenAtLeastOneNotFainted()
        {
            // Arrange
            var party = new PartyState();
            party.AddToParty(MakeFainted());
            party.AddToParty(MakeCreature());

            // Assert
            Assert.IsTrue(party.HasConscious);
        }

        [Test]
        public void test_PartyState_HasConscious_FalseWhenAllFainted()
        {
            // Arrange
            var party = new PartyState();
            party.AddToParty(MakeFainted());
            party.AddToParty(MakeFainted());

            // Assert
            Assert.IsFalse(party.HasConscious);
        }

        [Test]
        public void test_PartyState_PromoteNextConscious_RotatesFaintedToBack()
        {
            // Arrange
            var party = new PartyState();
            var fainted = MakeFainted("Fainted");
            var conscious = MakeCreature("Conscious");
            party.AddToParty(fainted);
            party.AddToParty(conscious);

            // Act
            party.PromoteNextConscious();

            // Assert
            Assert.AreSame(conscious, party.Lead);
            Assert.AreSame(fainted, party.GetPartyMember(1));
        }

        [Test]
        public void test_PartyState_PromoteNextConscious_AllFainted_DoesNotInfiniteLoop()
        {
            // Arrange
            var party = new PartyState();
            for (int i = 0; i < 3; i++)
                party.AddToParty(MakeFainted());

            // Act — must complete without hanging
            Assert.DoesNotThrow(() => party.PromoteNextConscious());
            Assert.AreEqual(3, party.ActiveCount);
        }

        [Test]
        public void test_PartyState_DepositToStorage_MovesCreatureToStorage()
        {
            // Arrange
            var party = new PartyState();
            var a = MakeCreature("A");
            var b = MakeCreature("B");
            party.AddToParty(a);
            party.AddToParty(b);

            // Act
            bool result = party.DepositToStorage(0);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, party.ActiveCount);
            Assert.AreEqual(1, party.StorageCount);
            Assert.AreSame(b, party.Lead);
            Assert.AreSame(a, party.Storage[0]);
        }

        [Test]
        public void test_PartyState_DepositToStorage_BlockedWhenLastConscious()
        {
            // Arrange
            var party = new PartyState();
            var conscious = MakeCreature("Conscious");
            var fainted = MakeFainted("Fainted");
            party.AddToParty(conscious);
            party.AddToParty(fainted);

            // Act — try to deposit the only conscious creature
            bool result = party.DepositToStorage(0);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(2, party.ActiveCount);
            Assert.AreEqual(0, party.StorageCount);
        }

        [Test]
        public void test_PartyState_DepositToStorage_AllowedForFaintedWhenOthersConscious()
        {
            // Arrange
            var party = new PartyState();
            var conscious = MakeCreature("Conscious");
            var fainted = MakeFainted("Fainted");
            party.AddToParty(conscious);
            party.AddToParty(fainted);

            // Act — deposit the fainted creature (slot 1)
            bool result = party.DepositToStorage(1);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, party.ActiveCount);
            Assert.AreEqual(1, party.StorageCount);
        }

        [Test]
        public void test_PartyState_WithdrawFromStorage_MovesCreatureToParty()
        {
            // Arrange
            var party = new PartyState();
            var stored = MakeCreature("Stored");
            party.AddToStorage(stored);

            // Act
            bool result = party.WithdrawFromStorage(0);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, party.ActiveCount);
            Assert.AreEqual(0, party.StorageCount);
            Assert.AreSame(stored, party.Lead);
        }

        [Test]
        public void test_PartyState_WithdrawFromStorage_ReturnsFalseWhenPartyFull()
        {
            // Arrange
            var party = new PartyState();
            for (int i = 0; i < 6; i++)
                party.AddToParty(MakeCreature());
            party.AddToStorage(MakeCreature("Overflow"));

            // Act
            bool result = party.WithdrawFromStorage(0);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(6, party.ActiveCount);
            Assert.AreEqual(1, party.StorageCount);
        }

        [Test]
        public void test_PartyState_SwapPartySlots_ReordersCorrectly()
        {
            // Arrange
            var party = new PartyState();
            var a = MakeCreature("A");
            var b = MakeCreature("B");
            var c = MakeCreature("C");
            party.AddToParty(a);
            party.AddToParty(b);
            party.AddToParty(c);

            // Act
            party.SwapPartySlots(0, 2);

            // Assert
            Assert.AreSame(c, party.GetPartyMember(0));
            Assert.AreSame(b, party.GetPartyMember(1));
            Assert.AreSame(a, party.GetPartyMember(2));
        }

        [Test]
        public void test_PartyState_PartyChanged_EventFiresOnMutations()
        {
            // Arrange
            var party = new PartyState();
            int changeCount = 0;
            party.PartyChanged += () => changeCount++;
            var a = MakeCreature("A");
            var b = MakeCreature("B");

            // Act
            party.AddToParty(a);                // +1
            party.AddToParty(b);                // +1
            party.SwapPartySlots(0, 1);         // +1
            party.AddToStorage(MakeCreature()); // +1

            // Assert
            Assert.AreEqual(4, changeCount);
        }

        [Test]
        public void test_PartyState_ReviveAll_SetsAllFaintedTo1HP()
        {
            // Arrange
            var party = new PartyState();
            var a = MakeFainted("A");
            var b = MakeFainted("B");
            party.AddToParty(a);
            party.AddToParty(b);

            // Act
            party.ReviveAll();

            // Assert
            Assert.IsFalse(a.IsFainted);
            Assert.IsFalse(b.IsFainted);
            Assert.AreEqual(1, a.CurrentHP);
            Assert.AreEqual(1, b.CurrentHP);
        }

        [Test]
        public void test_PartyState_PartyWiped_EventFiresWhenAllFainted()
        {
            // Arrange
            var party = new PartyState();
            bool wiped = false;
            party.PartyWiped += () => wiped = true;
            var a = MakeFainted("A");
            var b = MakeFainted("B");

            // Act — adding all-fainted creatures triggers wipe event
            party.AddToParty(a);
            party.AddToParty(b);

            // Assert
            Assert.IsTrue(wiped);
        }
    }
}
