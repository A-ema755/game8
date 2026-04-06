using UnityEditor;
using UnityEngine;
using GeneForge.Core;

namespace GeneForge.Editor
{
    /// <summary>
    /// Auto-creates GameSettings.asset at Resources/Data/ if missing.
    /// Runs once on editor load via InitializeOnLoadMethod.
    /// </summary>
    public static class GameSettingsBootstrap
    {
        const string AssetPath = "Assets/Resources/Data/GameSettings.asset";

        [InitializeOnLoadMethod]
        static void EnsureGameSettingsExists()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameSettings>(AssetPath);
            if (existing != null) return;

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Data"))
                AssetDatabase.CreateFolder("Assets/Resources", "Data");

            var settings = ScriptableObject.CreateInstance<GameSettings>();
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GameSettingsBootstrap] Created {AssetPath} with default values.");
        }
    }
}
