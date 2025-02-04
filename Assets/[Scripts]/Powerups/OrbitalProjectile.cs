using UnityEngine;
using Planetarium;

namespace Planetarium
{
    public class OrbitalProjectile : CoreBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float orbitSpeed = 100f;
        [SerializeField] private float orbitHeight = 2f;
        [SerializeField] private float rotationSpeed = 360f;

        [Header("Damage Settings")]
        [SerializeField] private float damage = 10f;
        [SerializeField] private float damageRadius = 1f;
        [SerializeField] private LayerMask targetLayer;

        private PlanetBase planet;
        private Vector3 currentRotation;
        private Vector3 orbitAxis;

        public void Initialize(PlanetBase targetPlanet, Vector3 startRotation, Vector3 axis)
        {
            planet = targetPlanet;
            currentRotation = startRotation;
            orbitAxis = axis.normalized;
            
            // Position the projectile
            UpdatePosition();
        }

        private void Update()
        {
            if (planet == null) return;

            // Rotate around the planet
            currentRotation += orbitAxis * (orbitSpeed * Time.deltaTime);
            
            // Update position
            UpdatePosition();
            
            // Rotate the projectile itself
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime, Space.Self);
            
            // Check for targets
            CheckForTargets();
        }

        private void UpdatePosition()
        {
            // Calculate position based on rotation around planet
            Quaternion rotation = Quaternion.Euler(currentRotation);
            Vector3 orbitPosition = rotation * Vector3.forward * (planet.GetPlanetRadius() + orbitHeight);
            transform.position = planet.transform.position + orbitPosition;
            
            // Orient towards orbit direction
            Vector3 orbitTangent = Vector3.Cross(orbitPosition.normalized, orbitAxis);
            transform.rotation = Quaternion.LookRotation(orbitTangent, orbitPosition.normalized);
        }

        private void CheckForTargets()
        {
            // Check for enemies in range
            Collider[] hits = Physics.OverlapSphere(transform.position, damageRadius, targetLayer);
            foreach (var hit in hits)
            {
                var damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, damageRadius);
        }
    }
}
