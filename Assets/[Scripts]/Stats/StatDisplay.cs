using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Planetarium.Stats
{
    [RequireComponent(typeof(ContentSizeFitter))]
    [RequireComponent(typeof(VerticalLayoutGroup))]
    public class StatDisplay : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private VerticalLayoutGroup contentLayout;
        [SerializeField] private TextMeshProUGUI valuePrefab;
        [SerializeField] private RectTransform backgroundImage;
        
        [Header("Layout Settings")]
        [SerializeField] private float padding = 10f;
        [SerializeField] private float spacing = 5f;
        [SerializeField] private bool expandWidth = true;
        
        private StatBase stat;
        private ContentSizeFitter contentSizeFitter;
        private VerticalLayoutGroup rootLayout;

        private void Awake()
        {
            // Get or add required components
            contentSizeFitter = GetComponent<ContentSizeFitter>();
            rootLayout = GetComponent<VerticalLayoutGroup>();

            // Configure root layout
            if (rootLayout != null)
            {
                rootLayout.childControlHeight = true;
                rootLayout.childControlWidth = true;
                rootLayout.childForceExpandHeight = false;
                rootLayout.childForceExpandWidth = expandWidth;
                rootLayout.spacing = spacing;
                rootLayout.padding.left = rootLayout.padding.right = (int)padding;
                rootLayout.padding.top = rootLayout.padding.bottom = (int)padding;
            }

            // Configure content size fitter
            if (contentSizeFitter != null)
            {
                contentSizeFitter.horizontalFit = expandWidth ? ContentSizeFitter.FitMode.Unconstrained : ContentSizeFitter.FitMode.PreferredSize;
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // Configure content layout
            if (contentLayout != null)
            {
                contentLayout.childControlHeight = true;
                contentLayout.childControlWidth = true;
                contentLayout.childForceExpandHeight = false;
                contentLayout.childForceExpandWidth = expandWidth;
                contentLayout.spacing = spacing;
            }

            // Configure background
            if (backgroundImage != null)
            {
                backgroundImage.anchorMin = Vector2.zero;
                backgroundImage.anchorMax = Vector2.one;
                backgroundImage.sizeDelta = Vector2.zero;
            }

            // Configure title text if needed
            if (titleText != null)
            {
                var titleRT = titleText.GetComponent<RectTransform>();
                if (titleRT != null)
                {
                    titleRT.anchorMin = new Vector2(0, 1);
                    titleRT.anchorMax = new Vector2(1, 1);
                }
            }
        }

        public void Initialize(StatBase stat)
        {
            if (stat == null)
            {
                Debug.LogError("StatDisplay: Cannot initialize with null stat!");
                return;
            }

            Debug.Log($"Initializing StatDisplay for {stat.name}");
            
            this.stat = stat;
            
            if (titleText != null)
            {
                titleText.text = stat.name;
                Debug.Log($"Set title text to: {stat.name}");
            }
            else
            {
                Debug.LogError("StatDisplay: Title text component is missing!");
            }

            // Register for updates and show initial value
            if (StatManager.Instance != null)
            {
                StatManager.Instance.RegisterCallback<object>(stat.name, OnStatUpdated);
                Debug.Log($"Registered callback for {stat.name}");
                UpdateDisplay();
            }
            else
            {
                Debug.LogError("StatDisplay: StatManager.Instance is null!");
            }
        }

        private void OnStatUpdated(object newValue)
        {
            Debug.Log($"Stat updated for {stat?.name}: {newValue}");
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (stat == null)
            {
                Debug.LogError("StatDisplay: stat is null during UpdateDisplay!");
                return;
            }

            Debug.Log($"Updating display for {stat.name}");

            // Clear existing value displays
            if (contentLayout != null)
            {
                foreach (Transform child in contentLayout.transform)
                {
                    Destroy(child.gameObject);
                }
            }
            else
            {
                Debug.LogError("StatDisplay: Content layout is missing!");
                return;
            }

            if (valuePrefab == null)
            {
                Debug.LogError("StatDisplay: Value prefab is missing!");
                return;
            }

            var value = StatManager.Instance.GetValue<object>(stat.name);
            if (value == null)
            {
                Debug.LogWarning($"No value found for stat: {stat.name}");
                return;
            }

            Debug.Log($"Got value for {stat.name}: {value}");
            
            // Handle different stat types
            if (stat is EnemyTypeStats)
            {
                var data = value as EnemyTypeStats.EnemyTypeData;
                if (data != null)
                {
                    foreach (var kvp in data.enemyStats)
                    {
                        AddValueText($"{kvp.Key}:");
                        AddValueText($"  Spawned: {kvp.Value.totalSpawned}");
                        AddValueText($"  Killed: {kvp.Value.totalKilled}");
                        AddValueText($"  Damage Dealt: {kvp.Value.totalDamageDealt:F0}");
                        AddValueText($"  Damage Taken: {kvp.Value.totalDamageTaken:F0}");
                    }
                }
            }
            else if (stat is TurretTypeStats)
            {
                var data = value as TurretTypeStats.TurretTypeData;
                if (data != null)
                {
                    foreach (var kvp in data.turretStats)
                    {
                        AddValueText($"{kvp.Key}:");
                        AddValueText($"  Built: {kvp.Value.totalBuilt}");
                        AddValueText($"  Kills: {kvp.Value.totalKills}");
                        AddValueText($"  Damage: {kvp.Value.totalDamageDealt:F0}");
                        AddValueText($"  Accuracy: {kvp.Value.accuracy:P0}");
                    }
                }
            }
            else if (stat is WaveStat)
            {
                try
                {
                    var data = (WaveStat.WaveData)value;
                    AddValueText($"Wave: {data.currentWave}");
                    AddValueText($"Enemies: {data.enemiesRemaining}");
                    if (!data.isFinalWave)
                        AddValueText($"Next Wave: {data.timeUntilNextWave:F1}s");
                    else
                        AddValueText("Final Wave!");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error displaying wave stats: {e.Message}");
                }
            }
            else if (stat is ResourceStat)
            {
                try
                {
                    var data = (ResourceStat.ResourceData)value;
                    AddValueText($"Current: {data.currentResources}");
                    AddValueText($"Total Collected: {data.totalResourcesCollected}");
                    AddValueText($"Total Spent: {data.totalResourcesSpent}");
                    AddValueText($"Multiplier: {data.resourceMultiplier:F2}x");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error displaying resource stats: {e.Message}");
                }
            }

            // Force layout update
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
        }

        private void AddValueText(string text)
        {
            if (contentLayout != null && valuePrefab != null)
            {
                var valueDisplay = Instantiate(valuePrefab, contentLayout.transform);
                valueDisplay.text = text;
            }
            else
            {
                Debug.LogError($"StatDisplay: Cannot add value text - missing {(contentLayout == null ? "contentLayout" : "valuePrefab")}");
            }
        }

        private void OnDestroy()
        {
            if (stat != null)
            {
                //StatManager.Instance?.UnregisterCallback<object>(stat.name, OnStatUpdated);
            }
        }
    }
}
