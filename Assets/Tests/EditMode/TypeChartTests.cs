using System;
using System.Reflection;
using NUnit.Framework;
using GeneForge.Combat;
using GeneForge.Core;
using UnityEngine.TestTools;

namespace GeneForge.Tests
{
    [TestFixture]
    public class TypeChartTests
    {
        [SetUp]
        public void SetUp()
        {
            TypeChart.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            var type = typeof(TypeChart);
            var flags = BindingFlags.NonPublic | BindingFlags.Static;
            type.GetField("_initialized", flags).SetValue(null, false);
            type.GetField("_matrix", flags).SetValue(null, null);
        }

        // ================================================================
        // Acceptance Criteria (GDD Section 8) — tests #1 through #22
        // ================================================================

        [Test] // AC #1
        public void test_TypeChart_Initialize_CompletesWithoutErrors()
        {
            // Arrange — already initialized in SetUp
            // Assert — no exceptions thrown, re-initialize is idempotent
            TypeChart.Initialize();
        }

        [Test] // AC #2
        public void test_TypeChart_GetMultiplier_ThermalVsOrganic_Returns2()
        {
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Thermal, CreatureType.Organic, CreatureType.None), 0.001f);
        }

        [Test] // AC #3
        public void test_TypeChart_GetMultiplier_ThermalVsAqua_Returns05()
        {
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Thermal, CreatureType.Aqua, CreatureType.None), 0.001f);
        }

        [Test] // AC #4
        public void test_TypeChart_GetMultiplier_AquaVsThermal_Returns2()
        {
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Aqua, CreatureType.Thermal, CreatureType.None), 0.001f);
        }

        [Test] // AC #5
        public void test_TypeChart_GetMultiplier_MineralVsBioelectric_Returns2()
        {
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Mineral, CreatureType.Bioelectric, CreatureType.None), 0.001f);
        }

        [Test] // AC #6
        public void test_TypeChart_GetMultiplier_CryoVsOrganicAero_DualSE_Returns4()
        {
            Assert.AreEqual(4.0f, TypeChart.GetMultiplier(CreatureType.Cryo, CreatureType.Organic, CreatureType.Aero), 0.001f);
        }

        [Test] // AC #7
        public void test_TypeChart_GetMultiplier_OrganicVsFerroToxic_DualResist_Returns025()
        {
            Assert.AreEqual(0.25f, TypeChart.GetMultiplier(CreatureType.Organic, CreatureType.Ferro, CreatureType.Toxic), 0.001f);
        }

        [Test] // AC #8
        public void test_TypeChart_GetMultiplier_ArkVsBlight_Returns2()
        {
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Ark, CreatureType.Blight, CreatureType.None), 0.001f);
        }

        [Test] // AC #9
        public void test_TypeChart_GetMultiplier_ThermalVsArk_Returns2()
        {
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Thermal, CreatureType.Ark, CreatureType.None), 0.001f);
        }

        [Test] // AC #10
        public void test_TypeChart_GetMultiplier_OrganicVsBlight_Returns2()
        {
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Organic, CreatureType.Blight, CreatureType.None), 0.001f);
        }

        [Test] // AC #11
        public void test_TypeChart_GetMultiplier_SonicVsNeural_Returns2()
        {
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Sonic, CreatureType.Neural, CreatureType.None), 0.001f);
        }

        [Test] // AC #12
        public void test_TypeChart_GetStab_ThermalMoveThermalCreature_Returns15()
        {
            Assert.AreEqual(1.5f, TypeChart.GetStab(CreatureType.Thermal, CreatureType.Thermal, CreatureType.None), 0.001f);
        }

        [Test] // AC #13
        public void test_TypeChart_GetStab_ThermalMoveAquaOrganicCreature_Returns1()
        {
            Assert.AreEqual(1.0f, TypeChart.GetStab(CreatureType.Thermal, CreatureType.Aqua, CreatureType.Organic), 0.001f);
        }

        [Test] // AC #14
        public void test_TypeChart_GetStab_ThermalMoveAquaThermalCreature_SecondaryStab_Returns15()
        {
            Assert.AreEqual(1.5f, TypeChart.GetStab(CreatureType.Thermal, CreatureType.Aqua, CreatureType.Thermal), 0.001f);
        }

        [Test] // AC #15
        public void test_TypeChart_GetStab_NoneMoveType_Returns1()
        {
            Assert.AreEqual(1.0f, TypeChart.GetStab(CreatureType.None, CreatureType.Thermal, CreatureType.None), 0.001f);
        }

        [Test] // AC #16
        public void test_TypeChart_GetMultiplier_NoneAttackType_Returns1()
        {
            Assert.AreEqual(1.0f, TypeChart.GetMultiplier(CreatureType.None, CreatureType.Thermal, CreatureType.None), 0.001f);
        }

        [Test] // AC #17
        public void test_TypeChart_GetMultiplier_BeforeInitialize_Returns1AndLogsError()
        {
            // Arrange — tear down first to get uninitialized state
            TearDown();

            // Act
            LogAssert.Expect(UnityEngine.LogType.Error, "[TypeChart] Not initialized. Call TypeChart.Initialize() first.");
            float result = TypeChart.GetMultiplier(CreatureType.Thermal, CreatureType.Organic, CreatureType.None);

            // Assert
            Assert.AreEqual(1.0f, result, 0.001f);

            // Re-initialize for TearDown to work cleanly
            TypeChart.Initialize();
        }

        [Test] // AC #18
        public void test_TypeChart_GetLabel_SuperEffective()
        {
            Assert.AreEqual(EffectivenessLabel.SuperEffective, TypeChart.GetLabel(2.0f));
        }

        [Test] // AC #19
        public void test_TypeChart_GetLabel_Resisted()
        {
            Assert.AreEqual(EffectivenessLabel.Resisted, TypeChart.GetLabel(0.5f));
        }

        [Test] // AC #20
        public void test_TypeChart_GetLabel_Neutral()
        {
            Assert.AreEqual(EffectivenessLabel.Neutral, TypeChart.GetLabel(1.0f));
        }

        [Test] // AC #21
        public void test_TypeChart_EffectivenessLabel_HasNoImmuneValue()
        {
            Assert.IsFalse(Enum.IsDefined(typeof(EffectivenessLabel), "Immune"));
        }

        [Test] // AC #22
        public void test_TypeChart_Matrix_HasNoZeroEntries()
        {
            for (int a = 0; a < 15; a++)
            {
                for (int d = 0; d < 15; d++)
                {
                    float val = TypeChart.GetMultiplier((CreatureType)a, (CreatureType)d, CreatureType.None);
                    Assert.GreaterOrEqual(val, 0.5f,
                        $"Matrix[{(CreatureType)a}, {(CreatureType)d}] = {val}, expected >= 0.5f");
                }
            }
        }

        // ================================================================
        // Full SE Coverage — all 36 super-effective relationships (GDD 3.4)
        // ================================================================

        [Test]
        public void test_TypeChart_AllSuperEffectiveRelationships()
        {
            // Core Triangle 1: Elemental
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Thermal, CreatureType.Organic, CreatureType.None), 0.001f);     // #1
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Organic, CreatureType.Aqua, CreatureType.None), 0.001f);        // #2
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Aqua, CreatureType.Thermal, CreatureType.None), 0.001f);        // #3

            // Core Triangle 2: Physical
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Mineral, CreatureType.Bioelectric, CreatureType.None), 0.001f); // #4
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Bioelectric, CreatureType.Aqua, CreatureType.None), 0.001f);    // #5
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Aqua, CreatureType.Mineral, CreatureType.None), 0.001f);        // #6

            // Core Triangle 3: Mental
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Neural, CreatureType.Kinetic, CreatureType.None), 0.001f);      // #7
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Kinetic, CreatureType.Ferro, CreatureType.None), 0.001f);       // #8
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Ferro, CreatureType.Neural, CreatureType.None), 0.001f);        // #9

            // Core Triangle 4: Atmospheric
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Cryo, CreatureType.Aero, CreatureType.None), 0.001f);           // #10
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Aero, CreatureType.Sonic, CreatureType.None), 0.001f);          // #11
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Sonic, CreatureType.Cryo, CreatureType.None), 0.001f);          // #12

            // Cross-Links
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Thermal, CreatureType.Cryo, CreatureType.None), 0.001f);        // #13
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Thermal, CreatureType.Ferro, CreatureType.None), 0.001f);       // #14
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Cryo, CreatureType.Organic, CreatureType.None), 0.001f);        // #15
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Cryo, CreatureType.Kinetic, CreatureType.None), 0.001f);        // #16
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Mineral, CreatureType.Toxic, CreatureType.None), 0.001f);       // #17
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Mineral, CreatureType.Sonic, CreatureType.None), 0.001f);       // #18
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Mineral, CreatureType.Thermal, CreatureType.None), 0.001f);     // #19
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Toxic, CreatureType.Organic, CreatureType.None), 0.001f);       // #20
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Toxic, CreatureType.Ferro, CreatureType.None), 0.001f);         // #21
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Neural, CreatureType.Toxic, CreatureType.None), 0.001f);        // #22
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Ferro, CreatureType.Cryo, CreatureType.None), 0.001f);          // #23
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Kinetic, CreatureType.Mineral, CreatureType.None), 0.001f);     // #24
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Bioelectric, CreatureType.Aero, CreatureType.None), 0.001f);    // #25
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Aero, CreatureType.Thermal, CreatureType.None), 0.001f);        // #26
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Organic, CreatureType.Mineral, CreatureType.None), 0.001f);     // #27

            // Apex
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Ark, CreatureType.Blight, CreatureType.None), 0.001f);         // #28
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Ark, CreatureType.Toxic, CreatureType.None), 0.001f);           // #29
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Ark, CreatureType.Kinetic, CreatureType.None), 0.001f);         // #30
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Blight, CreatureType.Ark, CreatureType.None), 0.001f);          // #31
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Blight, CreatureType.Bioelectric, CreatureType.None), 0.001f);  // #32
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Blight, CreatureType.Neural, CreatureType.None), 0.001f);       // #33
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Thermal, CreatureType.Ark, CreatureType.None), 0.001f);         // #34
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Organic, CreatureType.Blight, CreatureType.None), 0.001f);      // #35
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Sonic, CreatureType.Neural, CreatureType.None), 0.001f);        // #36
        }

        // ================================================================
        // Full Resistance Coverage (GDD 3.5)
        // ================================================================

        [Test]
        public void test_TypeChart_AllResistanceEntries()
        {
            // Self-resist (14 entries: indices 1-14)
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Thermal, CreatureType.Thermal, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Aqua, CreatureType.Aqua, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Organic, CreatureType.Organic, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Bioelectric, CreatureType.Bioelectric, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Cryo, CreatureType.Cryo, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Mineral, CreatureType.Mineral, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Toxic, CreatureType.Toxic, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Neural, CreatureType.Neural, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Ferro, CreatureType.Ferro, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Kinetic, CreatureType.Kinetic, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Aero, CreatureType.Aero, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Sonic, CreatureType.Sonic, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Ark, CreatureType.Ark, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Blight, CreatureType.Blight, CreatureType.None), 0.001f);

            // Thermal resists: Organic, Cryo, Ferro
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Organic, CreatureType.Thermal, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Cryo, CreatureType.Thermal, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Ferro, CreatureType.Thermal, CreatureType.None), 0.001f);

            // Aqua resists: Thermal, Cryo
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Thermal, CreatureType.Aqua, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Cryo, CreatureType.Aqua, CreatureType.None), 0.001f);

            // Organic resists: Aqua, Bioelectric, Mineral
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Aqua, CreatureType.Organic, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Bioelectric, CreatureType.Organic, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Mineral, CreatureType.Organic, CreatureType.None), 0.001f);

            // Bioelectric resists: Aero, Ferro
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Aero, CreatureType.Bioelectric, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Ferro, CreatureType.Bioelectric, CreatureType.None), 0.001f);

            // Cryo resists: Aqua
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Aqua, CreatureType.Cryo, CreatureType.None), 0.001f);

            // Mineral resists: Thermal, Bioelectric, Toxic, Sonic
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Thermal, CreatureType.Mineral, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Bioelectric, CreatureType.Mineral, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Toxic, CreatureType.Mineral, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Sonic, CreatureType.Mineral, CreatureType.None), 0.001f);

            // Toxic resists: Organic
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Organic, CreatureType.Toxic, CreatureType.None), 0.001f);

            // Neural resists: Toxic
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Toxic, CreatureType.Neural, CreatureType.None), 0.001f);

            // Ferro resists: Cryo, Organic, Neural, Sonic, Aero
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Cryo, CreatureType.Ferro, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Organic, CreatureType.Ferro, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Neural, CreatureType.Ferro, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Sonic, CreatureType.Ferro, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Aero, CreatureType.Ferro, CreatureType.None), 0.001f);

            // Kinetic resists: Sonic
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Sonic, CreatureType.Kinetic, CreatureType.None), 0.001f);

            // Aero resists: Organic, Kinetic
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Organic, CreatureType.Aero, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Kinetic, CreatureType.Aero, CreatureType.None), 0.001f);

            // Sonic resists: Kinetic
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Kinetic, CreatureType.Sonic, CreatureType.None), 0.001f);

            // Ark resists: Toxic, Neural, Cryo, Sonic, Mineral
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Toxic, CreatureType.Ark, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Neural, CreatureType.Ark, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Cryo, CreatureType.Ark, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Sonic, CreatureType.Ark, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Mineral, CreatureType.Ark, CreatureType.None), 0.001f);

            // Blight resists: Thermal, Bioelectric, Toxic, Kinetic
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Thermal, CreatureType.Blight, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Bioelectric, CreatureType.Blight, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Toxic, CreatureType.Blight, CreatureType.None), 0.001f);
            Assert.AreEqual(0.5f, TypeChart.GetMultiplier(CreatureType.Kinetic, CreatureType.Blight, CreatureType.None), 0.001f);
        }

        // ================================================================
        // Edge Cases
        // ================================================================

        [Test]
        public void test_TypeChart_SameTypeInBothSlots_ThermalVsCryoCryo_Returns4()
        {
            Assert.AreEqual(4.0f, TypeChart.GetMultiplier(CreatureType.Thermal, CreatureType.Cryo, CreatureType.Cryo), 0.001f);
        }

        [Test]
        public void test_TypeChart_DualResistMinimum_OrganicVsFerroToxic_Returns025()
        {
            Assert.AreEqual(0.25f, TypeChart.GetMultiplier(CreatureType.Organic, CreatureType.Ferro, CreatureType.Toxic), 0.001f);
        }

        [Test]
        public void test_TypeChart_NoneAttackingType_AlwaysReturns1()
        {
            for (int d = 0; d < 15; d++)
            {
                float result = TypeChart.GetMultiplier(CreatureType.None, (CreatureType)d, CreatureType.None);
                Assert.AreEqual(1.0f, result, 0.001f,
                    $"None vs {(CreatureType)d} expected 1.0f, got {result}");
            }
        }

        [Test]
        public void test_TypeChart_SecondaryTypeNone_AppliesOnlyPrimary()
        {
            // Thermal vs Organic (SE) with no secondary — should be 2.0, not modified
            Assert.AreEqual(2.0f, TypeChart.GetMultiplier(CreatureType.Thermal, CreatureType.Organic, CreatureType.None), 0.001f);
        }

        [Test]
        public void test_TypeChart_Initialize_CalledTwice_IsIdempotent()
        {
            // Arrange — already initialized in SetUp
            float before = TypeChart.GetMultiplier(CreatureType.Thermal, CreatureType.Organic, CreatureType.None);

            // Act
            TypeChart.Initialize();
            float after = TypeChart.GetMultiplier(CreatureType.Thermal, CreatureType.Organic, CreatureType.None);

            // Assert
            Assert.AreEqual(before, after, 0.001f);
        }

        // ================================================================
        // Balance Verification (GDD Section 3.7)
        // ================================================================

        [Test]
        public void test_TypeChart_BalanceSummaryMatchesGDD()
        {
            // GDD Section 3.7 balance table
            // Type: (SEs, Weaknesses, Resists)
            // SEs = how many types this type is super effective against
            // Weaknesses = how many types are super effective against this type
            // Resists = how many types this type resists (including self)

            int[] expectedSEs       = { 0, 4, 2, 3, 2, 3, 4, 2, 2, 2, 2, 2, 2, 3, 3 };
            int[] expectedWeaknesses = { 0, 3, 2, 3, 2, 3, 3, 3, 3, 3, 3, 2, 2, 2, 2 };
            int[] expectedResists   = { 0, 4, 3, 4, 3, 2, 5, 2, 2, 6, 2, 3, 2, 6, 5 };

            for (int typeIdx = 1; typeIdx < 15; typeIdx++)
            {
                int seCount = 0;
                int weakCount = 0;
                int resistCount = 0;

                for (int other = 1; other < 15; other++)
                {
                    float attacking = TypeChart.GetMultiplier((CreatureType)typeIdx, (CreatureType)other, CreatureType.None);
                    if (attacking == 2.0f) seCount++;

                    float defending = TypeChart.GetMultiplier((CreatureType)other, (CreatureType)typeIdx, CreatureType.None);
                    if (defending == 2.0f) weakCount++;
                    if (defending == 0.5f) resistCount++;
                }

                string typeName = ((CreatureType)typeIdx).ToString();
                Assert.AreEqual(expectedSEs[typeIdx], seCount,
                    $"{typeName} SE count: expected {expectedSEs[typeIdx]}, got {seCount}");
                Assert.AreEqual(expectedWeaknesses[typeIdx], weakCount,
                    $"{typeName} weakness count: expected {expectedWeaknesses[typeIdx]}, got {weakCount}");
                Assert.AreEqual(expectedResists[typeIdx], resistCount,
                    $"{typeName} resist count: expected {expectedResists[typeIdx]}, got {resistCount}");
            }
        }
    }
}
