using UnityEngine;
using System.Collections.Generic;

namespace Planetarium.Spawning
{
    public class SpawnPortal : MonoBehaviour
    {
        [Header("Portal Settings")]
        [SerializeField] private string portalId;
        [SerializeField] private ParticleSystem portalEffect;
        [SerializeField] private List<EnemySpawnData> allowedEnemyTypes = new List<EnemySpawnData>();
        
        [Header("Spawn Settings")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float activationDelay = 1f;
        [SerializeField] private float deactivationDelay = 1f;

        [Header("Visualization")]
        [SerializeField] private MeshRenderer portalRenderer;
        [SerializeField] private Color baseColor = Color.blue;
        [SerializeField] private Color activeColor = Color.cyan;
        [SerializeField] private Color spawnColor = Color.white;
        [SerializeField] private float spawnFlashDuration = 0.2f;
        [SerializeField] private float usageIndicatorSize = 1f;
        [SerializeField] private int maxUsageIndicators = 5;
        
        [Header("Distribution Pattern")]
        [SerializeField] private Vector2 spawnSpread = new Vector2(1f, 1f);
        [SerializeField] private AnimationCurve spawnDistribution = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField] private float rotationSpeed = 30f;
        
        private bool isActive;
        private float currentDelay;
        private float lastSpawnTime;
        private int spawnCount;
        private List<Transform> usageIndicators = new List<Transform>();
        private Material portalMaterial;
        private Color currentColor;
        private float colorLerpTime;

        public string PortalId => portalId;
        public bool IsActive => isActive;
        public int SpawnCount => spawnCount;
        public bool CanSpawnType(EnemySpawnData enemyType) => allowedEnemyTypes.Count == 0 || allowedEnemyTypes.Contains(enemyType);

        private void Awake()
        {
            if (spawnPoint == null) spawnPoint = transform;
            if (string.IsNullOrEmpty(portalId)) portalId = System.Guid.NewGuid().ToString();
            
            if (portalRenderer != null)
            {
                // Create instance material for color changes
                portalMaterial = new Material(portalRenderer.material);
                portalRenderer.material = portalMaterial;
                currentColor = baseColor;
                UpdatePortalColor();
            }

            CreateUsageIndicators();
        }

        private void Update()
        {
            // Update portal color
            if (colorLerpTime > 0)
            {
                colorLerpTime -= Time.deltaTime;
                float t = 1 - (colorLerpTime / spawnFlashDuration);
                portalMaterial.color = Color.Lerp(spawnColor, currentColor, t);
            }

            // Rotate usage indicators
            if (isActive && usageIndicators.Count > 0)
            {
                foreach (var indicator in usageIndicators)
                {
                    indicator.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
                }
            }
        }

        private void CreateUsageIndicators()
        {
            // Create small spheres around portal to show usage
            float angleStep = 360f / maxUsageIndicators;
            for (int i = 0; i < maxUsageIndicators; i++)
            {
                GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                indicator.transform.SetParent(transform);
                
                float angle = i * angleStep;
                float radius = usageIndicatorSize;
                Vector3 position = transform.position + new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                    0.5f,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * radius
                );
                
                indicator.transform.position = position;
                indicator.transform.localScale = Vector3.one * 0.2f;
                indicator.SetActive(false);
                
                // Remove collider, we don't need physics
                Destroy(indicator.GetComponent<Collider>());
                
                usageIndicators.Add(indicator.transform);
            }
        }

        public void UpdateUsageVisualization(float usageRatio)
        {
            int activeCount = Mathf.RoundToInt(usageRatio * maxUsageIndicators);
            for (int i = 0; i < usageIndicators.Count; i++)
            {
                usageIndicators[i].gameObject.SetActive(i < activeCount);
            }
        }

        public void Activate()
        {
            if (isActive) return;
            isActive = true;
            currentDelay = activationDelay;
            
            if (portalEffect != null)
            {
                portalEffect.Play();
            }

            // Update color
            currentColor = activeColor;
            UpdatePortalColor();
        }

        public void Deactivate()
        {
            if (!isActive) return;
            isActive = false;
            currentDelay = deactivationDelay;
            
            if (portalEffect != null)
            {
                portalEffect.Stop();
            }

            // Update color and hide indicators
            currentColor = baseColor;
            UpdatePortalColor();
            //UpdateUsageVisualization(0);
        }

        public Vector3 GetSpawnPosition(float normalizedIndex = 0f)
        {
            // Get base spawn position
            Vector3 basePosition = spawnPoint != null ? spawnPoint.position : transform.position;
            
            // Apply distribution pattern
            float t = spawnDistribution.Evaluate(normalizedIndex);
            Vector2 offset = new Vector2(
                Mathf.Cos(t * Mathf.PI * 2) * spawnSpread.x,
                Mathf.Sin(t * Mathf.PI * 2) * spawnSpread.y
            );
            
            return basePosition + new Vector3(offset.x, 0, offset.y);
        }

        public Quaternion GetSpawnRotation()
        {
            return spawnPoint != null ? spawnPoint.rotation : transform.rotation;
        }

        public void OnSpawn()
        {
            spawnCount++;
            lastSpawnTime = Time.time;
            
            // Flash portal
            colorLerpTime = spawnFlashDuration;
            if (portalMaterial != null)
            {
                portalMaterial.color = spawnColor;
            }
        }

        private void UpdatePortalColor()
        {
            if (portalMaterial != null)
            {
                portalMaterial.color = currentColor;
            }
        }

        private void OnDestroy()
        {
            if (portalMaterial != null)
            {
                Destroy(portalMaterial);
            }
        }
    }
}
