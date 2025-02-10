using UnityEngine;
using UnityEditor;
using Planetarium.Stats;
using System.Collections.Generic;

namespace Planetarium.Editor
{
    public static class GameplayTagSetup
    {
        [MenuItem("PlanetariumTD/Setup/Update Gameplay Tags")]
        public static void UpdateGameplayTags()
        {
            var config = GameplayTagConfig.Instance;
            if (config == null)
            {
                // Create config if it doesn't exist
                config = ScriptableObject.CreateInstance<GameplayTagConfig>();
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                AssetDatabase.CreateAsset(config, "Assets/Resources/GameplayTagConfig.asset");
            }

            var tags = new List<GameplayTagDefinition>
            {
                // Deployable tags
                new GameplayTagDefinition { Tag = "Deployable.Base", DevComment = "Base tag for all deployable objects" },
                new GameplayTagDefinition { Tag = "Deployable.Active", DevComment = "Indicates if a deployable is currently active" },
                new GameplayTagDefinition { Tag = "Deployable.Inactive", DevComment = "Indicates if a deployable is currently inactive" },
                new GameplayTagDefinition { Tag = "Deployable.Turret", DevComment = "Base tag for turret type deployables" },
                new GameplayTagDefinition { Tag = "Deployable.Structure", DevComment = "Base tag for structure type deployables" },

                // Enemy tags
                new GameplayTagDefinition { Tag = "Enemy.Base", DevComment = "Base tag for all enemy objects" },
                new GameplayTagDefinition { Tag = "Enemy.Flying", DevComment = "Flying type enemy" },
                new GameplayTagDefinition { Tag = "Enemy.Ground", DevComment = "Ground type enemy" },
                new GameplayTagDefinition { Tag = "Enemy.Boss", DevComment = "Boss type enemy" },

                // Stats - Wave
                new GameplayTagDefinition { Tag = "Stats.Wave.Current", DevComment = "Current wave number" },
                new GameplayTagDefinition { Tag = "Stats.Wave.Total", DevComment = "Total number of waves" },
                new GameplayTagDefinition { Tag = "Stats.Wave.TimeUntilNext", DevComment = "Time until next wave" },
                new GameplayTagDefinition { Tag = "Stats.Wave.EnemiesRemaining", DevComment = "Number of enemies remaining in wave" },
                new GameplayTagDefinition { Tag = "Stats.Wave.IsFinal", DevComment = "Indicates if this is the final wave" },

                // Stats - Enemy
                new GameplayTagDefinition { Tag = "Stats.Enemy.TotalSpawned", DevComment = "Total number of enemies spawned" },
                new GameplayTagDefinition { Tag = "Stats.Enemy.CurrentAlive", DevComment = "Current number of enemies alive" },
                new GameplayTagDefinition { Tag = "Stats.Enemy.TotalKilled", DevComment = "Total number of enemies killed" },
                new GameplayTagDefinition { Tag = "Stats.Enemy.TotalReachedEnd", DevComment = "Total number of enemies that reached the end" },
                new GameplayTagDefinition { Tag = "Stats.Enemy.TotalDamageTaken", DevComment = "Total damage taken by enemies" },
                new GameplayTagDefinition { Tag = "Stats.Enemy.TotalDamageDealt", DevComment = "Total damage dealt by enemies" },

                // Stats - Turret
                new GameplayTagDefinition { Tag = "Stats.Turret.TotalPlaced", DevComment = "Total number of turrets placed" },
                new GameplayTagDefinition { Tag = "Stats.Turret.CurrentActive", DevComment = "Current number of active turrets" },
                new GameplayTagDefinition { Tag = "Stats.Turret.TotalRemoved", DevComment = "Total number of turrets removed" },
                new GameplayTagDefinition { Tag = "Stats.Turret.TotalDamageDealt", DevComment = "Total damage dealt by turrets" },
                new GameplayTagDefinition { Tag = "Stats.Turret.TotalKills", DevComment = "Total number of kills by turrets" },

                // Stats - Resources
                new GameplayTagDefinition { Tag = "Stats.Resources.Current", DevComment = "Current amount of resources" },
                new GameplayTagDefinition { Tag = "Stats.Resources.TotalGained", DevComment = "Total amount of resources gained" },
                new GameplayTagDefinition { Tag = "Stats.Resources.TotalSpent", DevComment = "Total amount of resources spent" },

                // Stats - Player
                new GameplayTagDefinition { Tag = "Stats.Player.Health", DevComment = "Current player health" },
                new GameplayTagDefinition { Tag = "Stats.Player.MaxHealth", DevComment = "Maximum player health" }
            };

            // Clear existing tags and add new ones
            var tagListField = typeof(GameplayTagConfig).GetField("gameplayTagList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var tagList = new List<GameplayTagDefinition>();
            tagList.AddRange(tags);
            tagListField.SetValue(config, tagList);

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("Updated gameplay tags in config");
        }
    }
}
