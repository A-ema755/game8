using System.Collections.Generic;
using GeneForge.Creatures;
using UnityEngine;

namespace GeneForge.Core
{
    /// <summary>
    /// Singleton loader for all ScriptableObject config data.
    /// Must be initialized before any system queries creature, move, or part data.
    /// Singleton pattern approved under ADR-003.
    /// </summary>
    public class ConfigLoader : MonoBehaviour
    {
        const string CreaturesPath = "Data/Creatures";
        const string MovesPath = "Data/Moves";
        const string BodyPartsPath = "Data/BodyParts";
        const string StatusEffectsPath = "Data/StatusEffects";
        const string EncountersPath = "Data/Encounters";
        const string AIPersonalitiesPath = "Data/AIPersonalities";

        public static ConfigLoader Instance { get; private set; }

        /// <summary>Singleton settings — not a collection, loaded separately.</summary>
        public static GameSettings Settings { get; private set; }

        public static IReadOnlyDictionary<string, CreatureConfig> Creatures => _creatures;
        public static IReadOnlyDictionary<string, MoveConfig> Moves => _moves;
        public static IReadOnlyDictionary<string, BodyPartConfig> BodyParts => _bodyParts;
        public static IReadOnlyDictionary<string, StatusEffectConfig> StatusEffects => _statusEffects;
        public static IReadOnlyDictionary<string, EncounterConfig> Encounters => _encounters;
        public static IReadOnlyDictionary<string, AIPersonalityConfig> AIPersonalities => _aiPersonalities;

        static readonly Dictionary<string, CreatureConfig> _creatures = new();
        static readonly Dictionary<string, MoveConfig> _moves = new();
        static readonly Dictionary<string, BodyPartConfig> _bodyParts = new();
        static readonly Dictionary<string, StatusEffectConfig> _statusEffects = new();
        static readonly Dictionary<string, EncounterConfig> _encounters = new();
        static readonly Dictionary<string, AIPersonalityConfig> _aiPersonalities = new();

        static bool _initialized;

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        /// <summary>
        /// Loads all config assets from Resources/Data/ into typed dictionaries.
        /// Idempotent — calling multiple times has no effect after first successful load.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            LoadAll<CreatureConfig>(CreaturesPath, _creatures);
            LoadAll<MoveConfig>(MovesPath, _moves);
            LoadAll<BodyPartConfig>(BodyPartsPath, _bodyParts);
            LoadAll<StatusEffectConfig>(StatusEffectsPath, _statusEffects);
            LoadAll<EncounterConfig>(EncountersPath, _encounters);
            LoadAll<AIPersonalityConfig>(AIPersonalitiesPath, _aiPersonalities);

            Settings = Resources.Load<GameSettings>("Data/GameSettings");
            if (Settings == null)
                Debug.LogError("[ConfigLoader] GameSettings.asset not found at Data/GameSettings.");

            InstabilityThresholds.CacheFromSettings(Settings);

            _initialized = true;
        }

        static void LoadAll<T>(string resourcePath, Dictionary<string, T> registry)
            where T : ConfigBase
        {
            var assets = Resources.LoadAll<T>(resourcePath);
            foreach (var asset in assets)
            {
                if (string.IsNullOrEmpty(asset.Id))
                {
                    Debug.LogError($"[ConfigLoader] {typeof(T).Name} at {resourcePath} has empty ID.");
                    continue;
                }
                if (!registry.TryAdd(asset.Id, asset))
                    Debug.LogWarning($"[ConfigLoader] Duplicate ID '{asset.Id}' in {typeof(T).Name}.");
            }
#if UNITY_EDITOR
            Debug.Log($"[ConfigLoader] Loaded {registry.Count} {typeof(T).Name} assets.");
#endif
        }

        static T Get<T>(IReadOnlyDictionary<string, T> registry, string id)
            where T : ConfigBase
        {
            if (registry.TryGetValue(id, out var config)) return config;
            Debug.LogError($"[ConfigLoader] Config not found: '{id}' in {typeof(T).Name} registry.");
            return null;
        }

        /// <summary>Look up a creature config by kebab-case ID.</summary>
        public static CreatureConfig GetCreature(string id) => Get(_creatures, id);

        /// <summary>Look up a move config by kebab-case ID.</summary>
        public static MoveConfig GetMove(string id) => Get(_moves, id);

        /// <summary>Look up a body part config by kebab-case ID.</summary>
        public static BodyPartConfig GetBodyPart(string id) => Get(_bodyParts, id);

        /// <summary>Look up a status effect config by kebab-case ID.</summary>
        public static StatusEffectConfig GetStatusEffect(string id) => Get(_statusEffects, id);

        /// <summary>Look up an encounter config by kebab-case ID.</summary>
        public static EncounterConfig GetEncounter(string id) => Get(_encounters, id);

        /// <summary>Look up an AI personality config by kebab-case ID.</summary>
        public static AIPersonalityConfig GetAIPersonality(string id) => Get(_aiPersonalities, id);

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only: clears all registries and reloads from disk.
        /// Call from a custom editor menu or Inspector button.
        /// </summary>
        public static void Reinitialize()
        {
            _creatures.Clear();
            _moves.Clear();
            _bodyParts.Clear();
            _statusEffects.Clear();
            _encounters.Clear();
            _aiPersonalities.Clear();
            _initialized = false;
            Initialize();
            Debug.Log("[ConfigLoader] Reinitialized all config registries.");
        }
#endif
    }
}
