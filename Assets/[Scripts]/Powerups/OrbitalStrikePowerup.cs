using UnityEngine;
using System.Collections.Generic;
using Planetarium;

namespace Planetarium
{
    public class OrbitalStrikePowerup : PowerupBase
    {
        [Header("Strike Settings")]
        [SerializeField] private OrbitalStrikeProjectile asteroidPrefab;
        [SerializeField] private float strikeDelay = 1f;
        [SerializeField] private float targetRadius = 15f;

        private PlanetBase planet;
        private float nextStrikeTime;

        public override void Initialize(PowerupManager powerupManager)
        {
            base.Initialize(powerupManager);
            planet = FindObjectOfType<PlanetBase>();
            if (planet == null)
            {
                Debug.LogError("No planet found in scene!");
            }
        }

        protected override void OnPowerupActivate()
        {
            nextStrikeTime = Time.time;
        }

        protected override void OnPowerupUpdate()
        {
            if (Time.time >= nextStrikeTime)
            {
                LaunchStrike();
                nextStrikeTime = Time.time + strikeDelay;
            }
        }

        private void LaunchStrike()
        {
            // Find a random position on the planet surface
            float randomAngle = Random.Range(0f, 360f);
            Vector3 randomDirection = Quaternion.Euler(0, 0, randomAngle) * Vector3.right;
            Vector3 targetPosition = planet.transform.position + (randomDirection * planet.GetPlanetRadius());

            // Try to find enemies near the random position
            Collider[] colliders = Physics.OverlapSphere(targetPosition, targetRadius);
            bool enemyFound = false;

            foreach (var collider in colliders)
            {
                if (collider.GetComponent<EnemyBase>() != null)
                {
                    targetPosition = collider.transform.position;
                    enemyFound = true;
                    break;
                }
            }

            // Spawn the asteroid
            var asteroid = Instantiate(asteroidPrefab);
            asteroid.Initialize(planet, targetPosition);
        }

        protected override void OnPowerupDeactivate()
        {
            // Nothing special needed for deactivation
        }
    }
}
