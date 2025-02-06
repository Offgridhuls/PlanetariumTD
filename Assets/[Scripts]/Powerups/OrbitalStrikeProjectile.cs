using UnityEngine;
using Planetarium;

namespace Planetarium
{
    public class OrbitalStrikeProjectile : CoreBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float impactSpeed = 20f;
        [SerializeField] private float rotationSpeed = 360f;
        
        [Header("Impact Settings")]
        [SerializeField] private float impactRadius = 3f;
        [SerializeField] private float impactDamage = 50f;
        [SerializeField] private GameObject explosionPrefab;

        private Vector3 targetPosition;
        private bool isActive = false;
        private PlanetBase planet;
        private Rigidbody rb;

        private void OnEnable()
        {
            // Ensure we're on the correct layer for collisions
            gameObject.layer = LayerMask.NameToLayer("Projectile");
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
        }

        public void Initialize(PlanetBase planet, Vector3 targetPos)
        {
            this.planet = planet;
            this.targetPosition = targetPos;
            isActive = true;

            // Position the asteroid high above the target
            Vector3 spawnOffset = targetPos.normalized * (planet.GetPlanetRadius() + 20f);
            transform.position = targetPos + spawnOffset;
            
            // Look at target
            transform.LookAt(targetPosition);

            // Add force to move towards target
            rb.linearVelocity = (targetPosition - transform.position).normalized * impactSpeed;
        }

        private void Update()
        {
            if (!isActive) return;

            // Rotate for visual effect
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);

            // Check for impact with planet surface
            float distanceToCenter = Vector3.Distance(transform.position, planet.transform.position);
            if (distanceToCenter <= planet.GetPlanetRadius())
            {
                Impact();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Handle collision with planet or other objects
            Impact();
        }

        private void Impact()
        {
            if (!isActive) return;
            isActive = false;

            // Create explosion effect
            if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            }

            // Deal damage to enemies in radius
            var colliders = Physics.OverlapSphere(transform.position, impactRadius);
            foreach (var collider in colliders)
            {
                var enemy = collider.GetComponent<IDamageable>();
                if (enemy != null)
                {
                    enemy.TakeDamage(impactDamage);
                }
            }

            // Destroy the asteroid
            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, impactRadius);
        }
    }
}
