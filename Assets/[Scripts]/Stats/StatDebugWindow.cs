using UnityEngine;
using System.Text;
using UnityEditor;

namespace Planetarium.Stats
{
#if UNITY_EDITOR
    public class StatDebugWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private StatManager statManager;
        private StringBuilder stringBuilder = new StringBuilder();

        [MenuItem("Planetarium/Stats/Debug Window")]
        private static void ShowWindow()
        {
            var window = GetWindow<StatDebugWindow>();
            window.titleContent = new GUIContent("Stats Debug");
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        private void OnGUI()
        {
            if (statManager == null)
            {
                statManager = StatManager.Instance;
                if (statManager == null)
                {
                    EditorGUILayout.HelpBox("StatManager not found in scene!", MessageType.Warning);
                    return;
                }
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            stringBuilder.Clear();

            // Wave Stats
            var waveStat = statManager.GetOrCreateStat<WaveStat>("WaveStat");
            if (waveStat != null)
            {
                var waveData = statManager.GetValue<WaveStat.WaveData>("WaveStat");
                stringBuilder.AppendLine("=== Wave Stats ===");
                stringBuilder.AppendLine($"Current Wave: {waveData.currentWave}");
                stringBuilder.AppendLine($"Enemies Remaining: {waveData.enemiesRemaining}");
                stringBuilder.AppendLine($"Time Until Next: {waveData.timeUntilNextWave:F1}s");
                stringBuilder.AppendLine($"Final Wave: {waveData.isFinalWave}");
                stringBuilder.AppendLine();
            }

            // Resource Stats
            var resourceStat = statManager.GetOrCreateStat<ResourceStat>("ResourceStats");
            if (resourceStat != null)
            {
                var resourceData = statManager.GetValue<ResourceStat.ResourceData>("ResourceStats");
                stringBuilder.AppendLine("=== Resource Stats ===");
                stringBuilder.AppendLine($"Current Resources: {resourceData.currentResources}");
                stringBuilder.AppendLine($"Total Collected: {resourceData.totalResourcesCollected}");
                stringBuilder.AppendLine($"Total Spent: {resourceData.totalResourcesSpent}");
                stringBuilder.AppendLine($"Multiplier: {resourceData.resourceMultiplier:F2}x");
                stringBuilder.AppendLine();
            }

            // Turret Stats
            var turretStats = statManager.GetOrCreateStat<TurretTypeStats>("TurretTypeStats");
            if (turretStats != null)
            {
                var turretData = statManager.GetValue<TurretTypeStats.TurretTypeData>("TurretTypeStats");
                stringBuilder.AppendLine("=== Turret Stats ===");
                foreach (var kvp in turretData.turretStats)
                {
                    stringBuilder.AppendLine($"Turret: {kvp.Key}");
                    stringBuilder.AppendLine($"  Built: {kvp.Value.totalBuilt}");
                    stringBuilder.AppendLine($"  Kills: {kvp.Value.totalKills}");
                    stringBuilder.AppendLine($"  Damage: {kvp.Value.totalDamageDealt:F0}");
                    stringBuilder.AppendLine($"  Accuracy: {kvp.Value.accuracy:P0}");
                    stringBuilder.AppendLine();
                }
            }

            // Enemy Stats
            var enemyStats = statManager.GetOrCreateStat<EnemyTypeStats>("EnemyTypeStats");
            if (enemyStats != null)
            {
                var enemyData = statManager.GetValue<EnemyTypeStats.EnemyTypeData>("EnemyTypeStats");
                stringBuilder.AppendLine("=== Enemy Stats ===");
                foreach (var kvp in enemyData.enemyStats)
                {
                    stringBuilder.AppendLine($"Enemy: {kvp.Key}");
                    stringBuilder.AppendLine($"  Spawned: {kvp.Value.totalSpawned}");
                    stringBuilder.AppendLine($"  Killed: {kvp.Value.totalKilled}");
                    stringBuilder.AppendLine($"  Damage Dealt: {kvp.Value.totalDamageDealt:F0}");
                    stringBuilder.AppendLine($"  Damage Taken: {kvp.Value.totalDamageTaken:F0}");
                    stringBuilder.AppendLine($"  Avg Lifetime: {kvp.Value.averageLifetime:F1}s");
                    stringBuilder.AppendLine();
                }
            }

            EditorGUILayout.TextArea(stringBuilder.ToString());
            EditorGUILayout.EndScrollView();
        }
    }
#endif
}
