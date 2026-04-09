using System;
using System.Collections.Generic;
using UnityEngine;

namespace GeneForge.Core
{
    /// <summary>
    /// Singleton loader for all ScriptableObject config data.
    /// Must be initialized before any system queries creature, move, or part data.
    /// Singleton pattern approved under ADR-003.
    /// Uses ConfigBase-typed registries to avoid circular assembly references.
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

        // ── Typed registry accessors (callers cast via Get<T>) ───────────

        /// <summary>All loaded creature configs keyed by ID.</summary>
        public static IReadOnlyDictionary<string, ConfigBase> Creatures => _creatures;

        /// <summary>All loaded move configs keyed by ID.</summary>
        public static IReadOnlyDictionary<string, ConfigBase> Moves => _moves;

        /// <summary>All loaded body part configs keyed by ID.</summary>
        public static IReadOnlyDictionary<string, ConfigBase> BodyParts => _bodyParts;

        /// <summary>All loaded status effect configs keyed by ID.</summary>
        public static IReadOnlyDictionary<string, ConfigBase> StatusEffects => _statusEffects;

        /// <summary>All loaded encounter configs keyed by ID.</summary>
        public static IReadOnlyDictionary<string, ConfigBase> Encounters => _encounters;

        /// <summary>All loaded AI personality configs keyed by ID.</summary>
        public static IReadOnlyDictionary<string, ConfigBase> AIPersonalities => _aiPersonalities;

        static readonly Dictionary<string, ConfigBase> _creatures = new();
        static readonly Dictionary<string, ConfigBase> _moves = new();
        static readonly Dictionary<string, ConfigBase> _bodyParts = new();
        static readonly Dictionary<string, ConfigBase> _statusEffects = new();
        static readonly Dictionary<string, ConfigBase> _encounters = new();
        static readonly Dictionary<string, ConfigBase> _aiPersonalities = new();

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

            LoadAll(CreaturesPath, _creatures);
            LoadAll(MovesPath, _moves);
            LoadAll(BodyPartsPath, _bodyParts);
            LoadAll(StatusEffectsPath, _statusEffects);
            LoadAll(EncountersPath, _encounters);
            LoadAll(AIPersonalitiesPath, _aiPersonalities);

            Settings = Resources.Load<GameSettings>("Data/GameSettings");
            if (Settings == null)
                Debug.LogError("[ConfigLoader] GameSettings.asset not found at Data/GameSettings.");

            InstabilityThresholds.CacheFromSettings(Settings);

            _initialized = true;
        }

        static void LoadAll(string resourcePath, Dictionary<string, ConfigBase> registry)
        {
            var assets = Resources.LoadAll<ConfigBase>(resourcePath);
            foreach (var asset in assets)
            {
                if (string.IsNullOrEmpty(asset.Id))
                {
                    Debug.LogError($"[ConfigLoader] {asset.GetType().Name} at {resourcePath} has empty ID.");
                    continue;
                }
                if (!registry.TryAdd(asset.Id, asset))
                    Debug.LogWarning($"[ConfigLoader] Duplicate ID '{asset.Id}' in {asset.GetType().Name}.");
            }
#if UNITY_EDITOR
            Debug.Log($"[ConfigLoader] Loaded {registry.Count} assets from {resourcePath}.");
#endif
        }

        /// <summary>
        /// Generic config lookup by ID. Returns null if not found.
        /// Callers specify the concrete type: ConfigLoader.Get&lt;CreatureConfig&gt;("emberfox").
        /// </summary>
        public static T Get<T>(string id) where T : ConfigBase
        {
            // Search all registries for the ID
            if (TryGet(_creatures, id, out T result)) return result;
            if (TryGet(_moves, id, out result)) return result;
            if (TryGet(_bodyParts, id, out result)) return result;
            if (TryGet(_statusEffects, id, out result)) return result;
            if (TryGet(_encounters, id, out result)) return result;
            if (TryGet(_aiPersonalities, id, out result)) return result;

            Debug.LogError($"[ConfigLoader] Config not found: '{id}' as {typeof(T).Name}.");
            return null;
        }

        static bool TryGet<T>(Dictionary<string, ConfigBase> registry, string id, out T result) where T : ConfigBase
        {
            if (registry.TryGetValue(id, out var config) && config is T typed)
            {
                result = typed;
                return true;
            }
            result = null;
            return false;
        }

        /// <summary>Look up a config from a specific registry by ID.</summary>
        public static ConfigBase GetFromRegistry(IReadOnlyDictionary<string, ConfigBase> registry, string id)
        {
            if (registry.TryGetValue(id, out var config)) return config;
            Debug.LogError($"[ConfigLoader] Config not found: '{id}'.");
            return null;
        }

        /// <summary>Look up a creature config by kebab-case ID.</summary>
        public static ConfigBase GetCreature(string id) => GetFromRegistry(_creatures, id);

        /// <summary>Look up a move config by kebab-case ID.</summary>
        public static ConfigBase GetMove(string id) => GetFromRegistry(_moves, id);

        /// <summary>Look up a body part config by kebab-case ID.</summary>
        public static ConfigBase GetBodyPart(string id) => GetFromRegistry(_bodyParts, id);

        /// <summary>Look up a status effect config by kebab-case ID.</summary>
        public static ConfigBase GetStatusEffect(string id) => GetFromRegistry(_statusEffects, id);

        /// <summary>Look up an encounter config by kebab-case ID.</summary>
        public static ConfigBase GetEncounter(string id) => GetFromRegistry(_encounters, id);

        /// <summary>Look up an AI personality config by kebab-case ID.</summary>
        public static ConfigBase GetAIPersonality(string id) => GetFromRegistry(_aiPersonalities, id);

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
