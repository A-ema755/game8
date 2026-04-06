#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using GeneForge.Core;
using GeneForge.Creatures;
using UnityEditor;
using UnityEngine;

namespace GeneForge.Editor
{
    /// <summary>
    /// Editor menu script that generates all MVP creature and move ScriptableObject assets.
    /// Run via GeneForge > Create All MVP Assets.
    /// </summary>
    public static class CreateGeneForgeAssets
    {
        const string CreaturePath = "Assets/Resources/Data/Creatures/";
        const string MovePath = "Assets/Resources/Data/Moves/";

        static readonly BindingFlags InstancePrivate =
            BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("GeneForge/Create All MVP Assets")]
        public static void CreateAll()
        {
            CreateAllMoves();
            CreateAllCreatures();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CreateGeneForgeAssets] Created all MVP assets.");
        }

        // ─── Field Setters ──────────────────────────────────────────────

        static void SetBaseField(ConfigBase config, string id, string displayName)
        {
            var baseType = typeof(ConfigBase);
            baseType.GetField("id", InstancePrivate).SetValue(config, id);
            baseType.GetField("displayName", InstancePrivate).SetValue(config, displayName);
        }

        static void SetField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, InstancePrivate);
            if (field == null)
                Debug.LogError($"[CreateGeneForgeAssets] Field '{fieldName}' not found on {obj.GetType().Name}");
            else
                field.SetValue(obj, value);
        }

        // ─── Creature Helpers ───────────────────────────────────────────

        static readonly Dictionary<BodyArchetype, List<BodySlot>> ArchetypeSlots = new()
        {
            { BodyArchetype.Bipedal, new List<BodySlot>
                { BodySlot.Head, BodySlot.Back, BodySlot.LeftArm, BodySlot.RightArm, BodySlot.Tail, BodySlot.Legs } },
            { BodyArchetype.Quadruped, new List<BodySlot>
                { BodySlot.Head, BodySlot.Back, BodySlot.Tail, BodySlot.Legs, BodySlot.Hide } },
            { BodyArchetype.Serpentine, new List<BodySlot>
                { BodySlot.Head, BodySlot.BodyUpper, BodySlot.BodyLower, BodySlot.Tail } },
            { BodyArchetype.Avian, new List<BodySlot>
                { BodySlot.Head, BodySlot.Wings, BodySlot.Tail, BodySlot.Talons } },
            { BodyArchetype.Amorphous, new List<BodySlot>
                { BodySlot.CoreA, BodySlot.CoreB, BodySlot.CoreC, BodySlot.Appendage } },
        };

        static CreatureConfig CreateCreature(
            string id, string displayName,
            CreatureType primary, CreatureType secondary,
            Rarity rarity, BodyArchetype archetype,
            int hp, int atk, int def, int spd, int acc,
            GrowthCurve growth, int catchRate, int xpYield,
            List<string> defaultParts, string signaturePart,
            string habitat, CreatureType terrainSynergy,
            List<LevelMoveEntry> moves)
        {
            var config = ScriptableObject.CreateInstance<CreatureConfig>();
            SetBaseField(config, id, displayName);

            SetField(config, "primaryType", primary);
            SetField(config, "secondaryType", secondary);
            SetField(config, "rarity", rarity);
            SetField(config, "bodyArchetype", archetype);
            SetField(config, "baseStats", new BaseStats(hp, atk, def, spd, acc));
            SetField(config, "movePool", moves);
            SetField(config, "availableSlots", ArchetypeSlots[archetype]);
            SetField(config, "defaultPartIds", defaultParts);
            SetField(config, "signaturePartId", signaturePart);
            SetField(config, "growthCurve", growth);
            SetField(config, "baseXpYield", xpYield);
            SetField(config, "catchRate", catchRate);
            SetField(config, "habitatZoneIds", new List<string> { habitat });
            SetField(config, "terrainSynergyType", terrainSynergy);

            string assetName = ToPascalCase(id);
            AssetDatabase.CreateAsset(config, $"{CreaturePath}{assetName}.asset");
            return config;
        }

        // ─── Move Helpers ───────────────────────────────────────────────

        static MoveEffect MakeEffect(MoveEffectType type, float chance, int magnitude = 0,
            StatusEffect status = StatusEffect.None, bool self = false, int statTarget = 0)
        {
            var effect = new MoveEffect();
            SetField(effect, "effectType", type);
            SetField(effect, "chance", chance);
            SetField(effect, "magnitude", magnitude);
            SetField(effect, "statusToApply", status);
            SetField(effect, "affectsSelf", self);
            SetField(effect, "statTarget", statTarget);
            return effect;
        }

        static MoveConfig CreateMove(
            string id, string displayName,
            CreatureType genome, DamageForm form,
            int power, int accuracy, int pp, int priority,
            TargetType target, int range,
            List<MoveEffect> effects = null)
        {
            var config = ScriptableObject.CreateInstance<MoveConfig>();
            SetBaseField(config, id, displayName);

            SetField(config, "genomeType", genome);
            SetField(config, "form", form);
            SetField(config, "power", power);
            SetField(config, "accuracy", accuracy);
            SetField(config, "pp", pp);
            SetField(config, "priority", priority);
            SetField(config, "targetType", target);
            SetField(config, "range", range);
            SetField(config, "effects", effects ?? new List<MoveEffect>());

            string assetName = ToPascalCase(id);
            AssetDatabase.CreateAsset(config, $"{MovePath}{assetName}.asset");
            return config;
        }

        // ─── Naming ────────────────────────────────────────────────────

        static string ToPascalCase(string kebab)
        {
            var parts = kebab.Split('-');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
            }
            return string.Join("", parts);
        }

        // ─── Move Pool shorthand ───────────────────────────────────────

        static List<LevelMoveEntry> Pool(params (int lvl, string id)[] entries)
        {
            var list = new List<LevelMoveEntry>();
            foreach (var (lvl, id) in entries)
                list.Add(new LevelMoveEntry(lvl, id));
            return list;
        }

        static List<string> Parts(params string[] ids)
        {
            return new List<string>(ids);
        }

        // ═══════════════════════════════════════════════════════════════
        //  MOVE CREATION — 45 moves per GDD Section 3.7
        // ═══════════════════════════════════════════════════════════════

        static void CreateAllMoves()
        {
            // 1. Scratch
            CreateMove("scratch", "Scratch",
                CreatureType.None, DamageForm.Physical,
                40, 100, 35, 0, TargetType.Single, 1);

            // 2. Tackle
            CreateMove("tackle", "Tackle",
                CreatureType.None, DamageForm.Physical,
                40, 100, 35, 0, TargetType.Single, 1);

            // 3. Ember
            CreateMove("ember", "Ember",
                CreatureType.Thermal, DamageForm.Energy,
                40, 100, 25, 0, TargetType.Single, 3,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.ApplyStatus, 0.1f, status: StatusEffect.Burn)
                });

            // 4. Flame Claw
            CreateMove("flame-claw", "Flame Claw",
                CreatureType.Thermal, DamageForm.Physical,
                65, 95, 15, 0, TargetType.Single, 1,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.ApplyStatus, 0.2f, status: StatusEffect.Burn)
                });

            // 5. Inferno Dash
            CreateMove("inferno-dash", "Inferno Dash",
                CreatureType.Thermal, DamageForm.Physical,
                80, 90, 10, 0, TargetType.Single, 3,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.Recoil, 1.0f, magnitude: 25),
                    MakeEffect(MoveEffectType.ForcedMove, 1.0f, magnitude: 1)
                });

            // 6. Vine Lash
            CreateMove("vine-lash", "Vine Lash",
                CreatureType.Organic, DamageForm.Physical,
                45, 100, 25, 0, TargetType.Single, 2);

            // 7. Root Bind
            CreateMove("root-bind", "Root Bind",
                CreatureType.Organic, DamageForm.None,
                0, 85, 20, 0, TargetType.Single, 2,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.ApplyStatus, 1.0f, status: StatusEffect.Paralysis),
                    MakeEffect(MoveEffectType.TerrainCreate, 1.0f)
                });

            // 8. Spore Cloud
            CreateMove("spore-cloud", "Spore Cloud",
                CreatureType.Organic, DamageForm.Bio,
                0, 90, 15, 0, TargetType.AoE, 2,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.ApplyStatus, 1.0f, status: StatusEffect.Sleep)
                });

            // 9. Toxic Spore
            CreateMove("toxic-spore", "Toxic Spore",
                CreatureType.Toxic, DamageForm.Bio,
                45, 90, 15, 0, TargetType.Adjacent, 2,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.ApplyStatus, 1.0f, status: StatusEffect.Poison)
                });

            // 10. Water Pulse
            CreateMove("water-pulse", "Water Pulse",
                CreatureType.Aqua, DamageForm.Energy,
                60, 100, 20, 0, TargetType.Single, 4,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.ApplyStatus, 0.2f, status: StatusEffect.Confusion)
                });

            // 11. Aqua Bolt
            CreateMove("aqua-bolt", "Aqua Bolt",
                CreatureType.Aqua, DamageForm.Energy,
                80, 90, 10, 0, TargetType.Line, 5);

            // 12. Spark
            CreateMove("spark", "Spark",
                CreatureType.Bioelectric, DamageForm.Physical,
                65, 100, 20, 0, TargetType.Single, 1,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.ApplyStatus, 0.3f, status: StatusEffect.Paralysis)
                });

            // 13. Discharge
            CreateMove("discharge", "Discharge",
                CreatureType.Bioelectric, DamageForm.Energy,
                80, 100, 15, 0, TargetType.Adjacent, 1,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.ApplyStatus, 0.3f, status: StatusEffect.Paralysis)
                });

            // 14. Ice Shard
            CreateMove("ice-shard", "Ice Shard",
                CreatureType.Cryo, DamageForm.Physical,
                40, 100, 30, 1, TargetType.Single, 2);

            // 15. Frost Breath
            CreateMove("frost-breath", "Frost Breath",
                CreatureType.Cryo, DamageForm.Energy,
                60, 90, 15, 0, TargetType.Single, 3,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.ApplyStatus, 0.1f, status: StatusEffect.Freeze)
                });

            // 16. Rock Throw
            CreateMove("rock-throw", "Rock Throw",
                CreatureType.Mineral, DamageForm.Physical,
                50, 90, 15, 0, TargetType.Single, 3);

            // 17. Boulder Slam
            CreateMove("boulder-slam", "Boulder Slam",
                CreatureType.Mineral, DamageForm.Physical,
                100, 75, 10, 0, TargetType.Single, 1,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.ForcedMove, 1.0f, magnitude: 2),
                    MakeEffect(MoveEffectType.Flinch, 0.5f)
                });

            // 18. Neural Claw
            CreateMove("neural-claw", "Neural Claw",
                CreatureType.Neural, DamageForm.Physical,
                70, 100, 15, 0, TargetType.Single, 1,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.HighCrit, 1.0f)
                });

            // 19. Mind Beam
            CreateMove("mind-beam", "Mind Beam",
                CreatureType.Neural, DamageForm.Energy,
                65, 100, 20, 0, TargetType.Single, 4,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.ApplyStatus, 0.3f, status: StatusEffect.Confusion)
                });

            // 20. Taunt
            CreateMove("taunt", "Taunt",
                CreatureType.Neural, DamageForm.None,
                0, 100, 20, 0, TargetType.Single, 3,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.ApplyStatus, 1.0f, status: StatusEffect.Taunt)
                });

            // 21. Feint Attack — accuracy 0 = always hits
            CreateMove("feint-attack", "Feint Attack",
                CreatureType.Kinetic, DamageForm.Physical,
                60, 0, 20, 0, TargetType.Single, 2);

            // 22. Ferro Bite
            CreateMove("ferro-bite", "Ferro Bite",
                CreatureType.Ferro, DamageForm.Physical,
                60, 100, 25, 0, TargetType.Single, 1,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.Flinch, 0.3f)
                });

            // 23. Harden — self-target, always hits (acc 0)
            CreateMove("harden", "Harden",
                CreatureType.None, DamageForm.None,
                0, 0, 30, 0, TargetType.Self, 0,
                new List<MoveEffect> {
                    // StatStage: DEF +1, statTarget 1=DEF
                    MakeEffect(MoveEffectType.StatStage, 1.0f, magnitude: 1, self: true, statTarget: 1)
                });

            // 24. Agility — self-target, always hits (acc 0)
            CreateMove("agility", "Agility",
                CreatureType.None, DamageForm.None,
                0, 0, 30, 0, TargetType.Self, 0,
                new List<MoveEffect> {
                    // StatStage: SPD +2, statTarget 2=SPD
                    MakeEffect(MoveEffectType.StatStage, 1.0f, magnitude: 2, self: true, statTarget: 2)
                });

            // 25. Leech Sting
            CreateMove("leech-sting", "Leech Sting",
                CreatureType.Toxic, DamageForm.Bio,
                50, 95, 20, 0, TargetType.Single, 2,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.Drain, 1.0f, magnitude: 50)
                });

            // 26. Iron Bash
            CreateMove("iron-bash", "Iron Bash",
                CreatureType.Ferro, DamageForm.Physical,
                50, 100, 25, 0, TargetType.Single, 1,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.Flinch, 0.2f)
                });

            // 27. Metal Press
            CreateMove("metal-press", "Metal Press",
                CreatureType.Ferro, DamageForm.Physical,
                80, 85, 10, 0, TargetType.Single, 1,
                new List<MoveEffect> {
                    // StatStage: DEF -1, statTarget 1=DEF
                    MakeEffect(MoveEffectType.StatStage, 0.5f, magnitude: -1, statTarget: 1)
                });

            // 28. Siege Slam
            CreateMove("siege-slam", "Siege Slam",
                CreatureType.Ferro, DamageForm.Physical,
                100, 80, 5, -1, TargetType.Single, 1,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.Recoil, 1.0f, magnitude: 20),
                    MakeEffect(MoveEffectType.ForcedMove, 1.0f, magnitude: 1)
                });

            // 29. Wind Slash
            CreateMove("wind-slash", "Wind Slash",
                CreatureType.Aero, DamageForm.Physical,
                55, 95, 25, 0, TargetType.Single, 2);

            // 30. Sonic Pulse
            CreateMove("sonic-pulse", "Sonic Pulse",
                CreatureType.Sonic, DamageForm.Energy,
                50, 100, 20, 0, TargetType.Single, 3,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.ApplyStatus, 0.1f, status: StatusEffect.Confusion)
                });

            // 31. Gust
            CreateMove("gust", "Gust",
                CreatureType.Aero, DamageForm.Energy,
                40, 100, 25, 0, TargetType.Single, 4,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.ForcedMove, 1.0f, magnitude: 1)
                });

            // 32. Screech
            CreateMove("screech", "Screech",
                CreatureType.Sonic, DamageForm.None,
                0, 85, 20, 0, TargetType.Single, 3,
                new List<MoveEffect> {
                    // StatStage: DEF -2, statTarget 1=DEF
                    MakeEffect(MoveEffectType.StatStage, 1.0f, magnitude: -2, statTarget: 1)
                });

            // 33. Cyclone Strike
            CreateMove("cyclone-strike", "Cyclone Strike",
                CreatureType.Aero, DamageForm.Physical,
                85, 90, 10, 0, TargetType.Adjacent, 1);

            // 34. Power Strike
            CreateMove("power-strike", "Power Strike",
                CreatureType.Kinetic, DamageForm.Physical,
                60, 100, 20, 0, TargetType.Single, 1);

            // 35. Seismic Smash
            CreateMove("seismic-smash", "Seismic Smash",
                CreatureType.Kinetic, DamageForm.Physical,
                90, 85, 10, 0, TargetType.Adjacent, 1,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.ForcedMove, 1.0f, magnitude: 1)
                });

            // 36. Acid Spray
            CreateMove("acid-spray", "Acid Spray",
                CreatureType.Toxic, DamageForm.Bio,
                55, 95, 20, 0, TargetType.Single, 2,
                new List<MoveEffect> {
                    // StatStage: DEF -1, statTarget 1=DEF
                    MakeEffect(MoveEffectType.StatStage, 1.0f, magnitude: -1, statTarget: 1)
                });

            // 37. Corrode
            CreateMove("corrode", "Corrode",
                CreatureType.Toxic, DamageForm.Bio,
                70, 90, 15, 0, TargetType.Single, 2,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.ApplyStatus, 0.5f, status: StatusEffect.Poison),
                    // StatStage: DEF -1, statTarget 1=DEF
                    MakeEffect(MoveEffectType.StatStage, 0.5f, magnitude: -1, statTarget: 1)
                });

            // 38. Rust Lash
            CreateMove("rust-lash", "Rust Lash",
                CreatureType.Ferro, DamageForm.Physical,
                75, 95, 15, 0, TargetType.Single, 1,
                new List<MoveEffect> {
                    // StatStage: DEF -1, statTarget 1=DEF
                    MakeEffect(MoveEffectType.StatStage, 0.5f, magnitude: -1, statTarget: 1)
                });

            // 39. Purify — self-target cleanse.
            // Convention: ApplyStatus with statusToApply=None and affectsSelf=true = full cleanse.
            CreateMove("purify", "Purify",
                CreatureType.Ark, DamageForm.None,
                0, 0, 15, 0, TargetType.Self, 0,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.ApplyStatus, 1.0f, status: StatusEffect.None, self: true)
                });

            // 40. Stasis Field
            CreateMove("stasis-field", "Stasis Field",
                CreatureType.Ark, DamageForm.Energy,
                60, 100, 15, 0, TargetType.Single, 3,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.ApplyStatus, 0.3f, status: StatusEffect.Paralysis)
                });

            // 41. Genetic Lock
            CreateMove("genetic-lock", "Genetic Lock",
                CreatureType.Ark, DamageForm.None,
                0, 90, 10, 0, TargetType.Single, 3,
                new List<MoveEffect> {
                    // StatStage: ATK -2, statTarget 0=ATK
                    MakeEffect(MoveEffectType.StatStage, 1.0f, magnitude: -2, statTarget: 0)
                });

            // 42. Blight Claw
            CreateMove("blight-claw", "Blight Claw",
                CreatureType.Blight, DamageForm.Physical,
                60, 100, 20, 0, TargetType.Single, 1,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.ApplyStatus, 0.3f, status: StatusEffect.Poison)
                });

            // 43. Corrupt
            CreateMove("corrupt", "Corrupt",
                CreatureType.Blight, DamageForm.Bio,
                55, 90, 15, 0, TargetType.Single, 2,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.ApplyStatus, 0.5f, status: StatusEffect.Confusion)
                });

            // 44. Entropic Howl
            CreateMove("entropic-howl", "Entropic Howl",
                CreatureType.Blight, DamageForm.Bio,
                70, 95, 15, 0, TargetType.AoE, 2,
                new List<MoveEffect> {
                    // StatStage: ATK -1, statTarget 0=ATK
                    MakeEffect(MoveEffectType.StatStage, 1.0f, magnitude: -1, statTarget: 0)
                });

            // 45. Genetic Collapse
            CreateMove("genetic-collapse", "Genetic Collapse",
                CreatureType.Blight, DamageForm.Bio,
                90, 85, 5, 0, TargetType.Single, 2,
                new List<MoveEffect> {
                    MakeEffect(MoveEffectType.HighCrit, 1.0f),
                    MakeEffect(MoveEffectType.ApplyStatus, 0.3f, status: StatusEffect.Poison)
                });
        }

        // ═══════════════════════════════════════════════════════════════
        //  CREATURE CREATION — 14 creatures per GDD Section 3.5
        // ═══════════════════════════════════════════════════════════════

        static void CreateAllCreatures()
        {
            // 1. Emberfox
            CreateCreature("emberfox", "Emberfox",
                CreatureType.Thermal, CreatureType.None,
                Rarity.Common, BodyArchetype.Bipedal,
                45, 60, 30, 65, 100,
                GrowthCurve.Fast, 180, 65,
                Parts("claws-rending", "glands-thermal"), "flame-tail",
                "verdant-basin", CreatureType.Thermal,
                Pool((1,"scratch"), (1,"ember"), (8,"flame-claw"), (12,"agility"), (18,"inferno-dash")));

            // 2. Thornslug
            CreateCreature("thorn-slug", "Thornslug",
                CreatureType.Organic, CreatureType.Toxic,
                Rarity.Common, BodyArchetype.Serpentine,
                70, 30, 65, 20, 95,
                GrowthCurve.Slow, 190, 55,
                Parts("stinger-venom", "spore-pods"), "toxic-spine",
                "verdant-basin", CreatureType.Organic,
                Pool((1,"vine-lash"), (1,"toxic-spore"), (5,"root-bind"), (10,"leech-sting"), (20,"spore-cloud")));

            // 3. Voltfin
            CreateCreature("voltfin", "Voltfin",
                CreatureType.Bioelectric, CreatureType.Aqua,
                Rarity.Uncommon, BodyArchetype.Serpentine,
                50, 55, 35, 70, 100,
                GrowthCurve.Medium, 120, 80,
                Parts("fangs-serrated", "core-bioelectric"), "shock-fin",
                "verdant-basin", CreatureType.Aqua,
                Pool((1,"water-pulse"), (1,"spark"), (6,"aqua-bolt"), (12,"feint-attack"), (17,"discharge")));

            // 4. Mosshell
            CreateCreature("mosshell", "Mosshell",
                CreatureType.Organic, CreatureType.Mineral,
                Rarity.Common, BodyArchetype.Quadruped,
                80, 35, 75, 15, 90,
                GrowthCurve.Slow, 170, 60,
                Parts("horns-bone", "spore-pods"), "stone-carapace",
                "verdant-basin", CreatureType.Mineral,
                Pool((1,"tackle"), (1,"vine-lash"), (4,"harden"), (9,"rock-throw"), (14,"spore-cloud"), (19,"boulder-slam")));

            // 5. Glacipede
            CreateCreature("glacipede", "Glacipede",
                CreatureType.Cryo, CreatureType.None,
                Rarity.Uncommon, BodyArchetype.Serpentine,
                55, 50, 45, 50, 100,
                GrowthCurve.Medium, 130, 75,
                Parts("fangs-serrated"), "frost-fangs",
                "verdant-basin", CreatureType.Cryo,
                Pool((1,"ice-shard"), (1,"ferro-bite"), (1,"tackle"), (5,"frost-breath"), (10,"harden")));

            // 6. Shadowmite
            CreateCreature("shadowmite", "Shadowmite",
                CreatureType.Neural, CreatureType.None,
                Rarity.Uncommon, BodyArchetype.Amorphous,
                45, 55, 25, 75, 110,
                GrowthCurve.Fast, 110, 85,
                Parts("fangs-serrated", "tendrils-neural"), "void-aura",
                "verdant-basin", CreatureType.Neural,
                Pool((1,"feint-attack"), (4,"neural-claw"), (9,"taunt"), (14,"mind-beam")));

            // 7. Psysprout
            CreateCreature("psysprout", "Psysprout",
                CreatureType.Neural, CreatureType.Organic,
                Rarity.Rare, BodyArchetype.Bipedal,
                55, 45, 40, 55, 105,
                GrowthCurve.Medium, 75, 95,
                Parts("tendrils-neural", "core-neural"), "psi-bloom",
                "verdant-basin", CreatureType.Organic,
                Pool((1,"mind-beam"), (6,"spore-cloud"), (9,"leech-sting"), (12,"toxic-spore"), (15,"root-bind")));

            // 8. Coalbear
            CreateCreature("coalbear", "Coalbear",
                CreatureType.Thermal, CreatureType.Mineral,
                Rarity.Rare, BodyArchetype.Quadruped,
                75, 70, 55, 30, 95,
                GrowthCurve.Medium, 65, 110,
                Parts("claws-rending", "glands-thermal"), "magma-claws",
                "verdant-basin", CreatureType.Thermal,
                Pool((1,"scratch"), (1,"ember"), (5,"rock-throw"), (10,"flame-claw"), (16,"boulder-slam")));

            // 9. Ferrovex
            CreateCreature("ferrovex", "Ferrovex",
                CreatureType.Ferro, CreatureType.Mineral,
                Rarity.Uncommon, BodyArchetype.Quadruped,
                65, 40, 80, 20, 90,
                GrowthCurve.Slow, 115, 90,
                Parts("plating-ferro", "horns-bone"), "ore-carapace",
                "verdant-basin", CreatureType.Mineral,
                Pool((1,"iron-bash"), (1,"harden"), (6,"rock-throw"), (12,"metal-press"), (18,"siege-slam")));

            // 10. Galewhip
            CreateCreature("galewhip", "Galewhip",
                CreatureType.Aero, CreatureType.Sonic,
                Rarity.Common, BodyArchetype.Serpentine,
                45, 55, 25, 80, 100,
                GrowthCurve.Fast, 160, 70,
                Parts("wings-aero", "emitter-resonance"), "gale-tail",
                "verdant-basin", CreatureType.Aero,
                Pool((1,"wind-slash"), (1,"sonic-pulse"), (5,"gust"), (10,"screech"), (16,"cyclone-strike")));

            // 11. Quarrok
            CreateCreature("quarrok", "Quarrok",
                CreatureType.Kinetic, CreatureType.Mineral,
                Rarity.Common, BodyArchetype.Bipedal,
                60, 70, 50, 35, 90,
                GrowthCurve.Medium, 155, 75,
                Parts("fists-impact", "horns-bone"), "seismic-fists",
                "verdant-basin", CreatureType.Kinetic,
                Pool((1,"tackle"), (1,"rock-throw"), (4,"power-strike"), (9,"boulder-slam"), (15,"seismic-smash")));

            // 12. Corrovex
            CreateCreature("corrovex", "Corrovex",
                CreatureType.Ferro, CreatureType.Toxic,
                Rarity.Uncommon, BodyArchetype.Serpentine,
                55, 65, 45, 55, 100,
                GrowthCurve.Medium, 120, 85,
                Parts("fangs-serrated", "glands-toxic"), "acid-scales",
                "verdant-basin", CreatureType.Toxic,
                Pool((1,"ferro-bite"), (1,"toxic-spore"), (7,"acid-spray"), (13,"corrode"), (19,"rust-lash")));

            // 13. Arkveil
            CreateCreature("arkveil", "Arkveil",
                CreatureType.Ark, CreatureType.Aqua,
                Rarity.Epic, BodyArchetype.Amorphous,
                75, 40, 70, 45, 100,
                GrowthCurve.Slow, 15, 140,
                Parts("crystal-lattice", "tendrils-neural"), "purity-aura",
                "verdant-basin", CreatureType.Ark,
                Pool((1,"purify"), (1,"water-pulse"), (8,"aqua-bolt"), (14,"stasis-field"), (22,"genetic-lock")));

            // 14. Blighthowl
            CreateCreature("blighthowl", "Blighthowl",
                CreatureType.Blight, CreatureType.Organic,
                Rarity.Epic, BodyArchetype.Quadruped,
                55, 70, 30, 65, 95,
                GrowthCurve.Fast, 20, 130,
                Parts("claws-rending", "glands-blight"), "corruption-maw",
                "verdant-basin", CreatureType.Blight,
                Pool((1,"vine-lash"), (1,"blight-claw"), (6,"corrupt"), (12,"entropic-howl"), (20,"genetic-collapse")));
        }
    }
}
#endif
