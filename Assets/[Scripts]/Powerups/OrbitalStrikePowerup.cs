using UnityEngine;
using System.Collections.Generic;
using Planetarium;

namespace Planetarium
{
    public class OrbitalStrikePowerup : PowerupBase
    {
        [Header("Orbital Settings")]
        [SerializeField] private OrbitalProjectile projectilePrefab;
        [SerializeField] private int projectileCount = 3;
        [SerializeField] private float spawnDelay = 0.2f;
        
        [Header("Orbit Configuration")]
        [SerializeField] private float orbitAngleMin = -30f;
        [SerializeField] private float orbitAngleMax = 30f;

        private PlanetBase planet;
        private List<OrbitalProjectile> activeProjectiles = new List<OrbitalProjectile>();
        private float nextSpawnTime;
        private int projectilesSpawned;

        public override void Initialize(PowerupManager powerupManager)
        {
            base.Initialize(powerupManager);
            // Find the planet in the scene
            planet = FindObjectOfType<PlanetBase>();
            if (planet == null)
            {
                Debug.LogError("No planet found in scene!");
            }
        }

        protected override void OnPowerupActivate()
        {
            projectilesSpawned = 0;
            nextSpawnTime = Time.time;
        }

        protected override void OnPowerupUpdate()
        {
            // Spawn projectiles over time
            if (projectilesSpawned < projectileCount && Time.time >= nextSpawnTime)
            {
                SpawnProjectile();
                nextSpawnTime = Time.time + spawnDelay;
            }
        }

        protected override void OnPowerupDeactivate()
        {
            // Clean up all projectiles
            foreach (var projectile in activeProjectiles)
            {
                if (projectile != null)
                {
                    Destroy(projectile.gameObject);
                }
            }
            activeProjectiles.Clear();
            projectilesSpawned = 0;
        }

        private void SpawnProjectile()
        {
            if (planet == null || projectilePrefab == null) return;

            // Create projectile
            OrbitalProjectile projectile = Instantiate(projectilePrefab, transform);
            
            // Calculate random orbit angle
            float orbitAngle = Random.Range(orbitAngleMin, orbitAngleMax);
            Vector3 orbitAxis = Quaternion.Euler(orbitAngle, Random.Range(0f, 360f), 0f) * Vector3.up;
            
            // Calculate start position (random around planet)
            Vector3 startRotation = new Vector3(
                Random.Range(0f, 360f),
                Random.Range(0f, 360f),
                Random.Range(0f, 360f)
            );

            // Initialize the projectile
            projectile.Initialize(planet, startRotation, orbitAxis);
            
            // Track the projectile
            activeProjectiles.Add(projectile);
            projectilesSpawned++;
        }
    }
}
