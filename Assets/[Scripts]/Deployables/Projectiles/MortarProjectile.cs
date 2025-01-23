using Planetarium;
using UnityEngine;

public class MortarProjectile : ProjectileBase
{
    [Header("Mortar Settings")]
    [SerializeField] private float arcHeight = 5f;
    [SerializeField] private float explosionRadius = 8f;
    [SerializeField] private float explosionForce = 15f;
    [SerializeField] private LayerMask explosionLayers;
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private TrailRenderer mortarTrail;

    
    private Vector3 startPoint;
    private Vector3 endPoint;
    
    private Vector3 startPosition;
    private float journeyLength;
    private float journeyTime;
    private bool hasExploded = false;
    PlanetBase planet;
    public override void ShootProjectile(Vector3 target, GameObject enemy)
    {
        targetPosition = target;
        targetEnemy = enemy;
        startPosition = transform.position;
        isInitialized = true;
        
        planet = FindFirstObjectByType<PlanetBase>();
        Vector3 randomPoint = GetRandomPointOnSphere(planet.GetPlanetRadius());
        startPoint = transform.position;
        endPoint = randomPoint;
        journeyLength = Vector3.Distance(startPosition, targetPosition);
        journeyTime = 0f;
    }

    Vector3 GetRandomPointOnSphere(float radius)
    {
        // Generate random spherical coordinates
        float theta = Random.Range(0f, Mathf.PI * 2); // Angle around the equator
        float phi = Mathf.Acos(Random.Range(-1f, 1f)); // Angle from the pole

        // Convert spherical coordinates to Cartesian coordinates
        float x = radius * Mathf.Sin(phi) * Mathf.Cos(theta);
        float y = radius * Mathf.Sin(phi) * Mathf.Sin(theta);
        float z = radius * Mathf.Cos(phi);

        return new Vector3(x, y, z);
    }
    
    protected override void Update()
    {
       if (!isInitialized || planet == null) return;

        if (journeyTime <= 1f)
        {
            journeyTime += Time.deltaTime * projectileSpeed / journeyLength;

            // Clamp to prevent NaN
            float clampedJourneyTime = Mathf.Clamp01(journeyTime);

            // Interpolate position along the sphere's surface
            Vector3 interpolatedPoint = Vector3.Slerp(startPoint, endPoint, clampedJourneyTime);
            CheckForHits();
            // Add arc height using sine curve
            float height = Mathf.Sin(clampedJourneyTime * Mathf.PI) * arcHeight;
            Vector3 direction = (interpolatedPoint - planet.transform.position).normalized;
            Vector3 newPosition = interpolatedPoint + direction * height;

            // Validate position before assigning
            if (!float.IsNaN(newPosition.x) && !float.IsNaN(newPosition.y) && !float.IsNaN(newPosition.z))
            {
                transform.position = newPosition;

                // Calculate rotation to face movement direction
                if (clampedJourneyTime < 1f)
                {
                    Vector3 nextInterpolatedPoint = Vector3.Slerp(startPoint, endPoint, Mathf.Clamp01(clampedJourneyTime + 0.01f));
                    float nextHeight = Mathf.Sin(Mathf.Clamp01(clampedJourneyTime + 0.01f) * Mathf.PI) * arcHeight;
                    Vector3 nextDirection = (nextInterpolatedPoint - planet.transform.position).normalized;
                    Vector3 nextPosition = nextInterpolatedPoint + nextDirection * nextHeight;

                    if (!float.IsNaN(nextPosition.x) && !float.IsNaN(nextPosition.y) && !float.IsNaN(nextPosition.z))
                    {
                        Vector3 forwardDirection = (nextPosition - transform.position).normalized;
                        transform.rotation = Quaternion.LookRotation(forwardDirection, direction);
                    }
                }
            }
            else
            {
                Debug.LogWarning("Invalid position calculated for mortar projectile");
                Destroy(gameObject);
                return;
            }

            if (clampedJourneyTime >= maxLifetime)
            {
                Explode();
            }
        }
    }
    
    private void Explode()
    {
        // Create explosion effect
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }

        // Deal area damage
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius, explosionLayers);
        foreach (var hitCollider in hitColliders)
        {
            HandleHit(hitCollider.gameObject);
        }

        OnProjectileHit();
        Destroy(gameObject);
    }

    protected void CheckForHits()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 0.5f, targetLayers);
        foreach (Collider col in colliders)
        {
            if (col.gameObject != gameObject)
            {
                HandleHit(col.gameObject);
                break;
            }
        }
    }

    protected override void HandleHit(GameObject hitObject)
    {
        if (hasExploded) return;
        hasExploded = true;

        // Create explosion effect
        if (explosionEffect != null)
        {
            GameObject effect = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            Destroy(effect, effectLifetime);
        }

        // Disable trail
        if (mortarTrail != null)
        {
            mortarTrail.enabled = false;
        }

        // Apply explosion force and damage to nearby objects
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, explosionLayers);
        foreach (Collider col in colliders)
        {
            // Apply explosion force to rigidbodies
            Rigidbody rb = col.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, 1f, ForceMode.Impulse);
            }

            // Deal damage with falloff based on distance
            float distance = Vector3.Distance(transform.position, col.transform.position);
            float damageMultiplier = 1f - (distance / explosionRadius);
            if (damageMultiplier > 0)
            {
                var damageable = col.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    int finalDamage = Mathf.RoundToInt(damage * damageMultiplier);
                    if (finalDamage > 0)
                    {
                        DamageData damageData = new DamageData
                        {
                            Damage = finalDamage,
                            Source = gameObject
                        };
                        damageable.ProcessDamage(damageData);
                    }
                }
            }
        }

        OnProjectileHit();
        Destroy(gameObject);
    }

    public override void OnProjectileHit()
    {
        // Cleanup or additional effects when mortar hits
        if (mortarTrail != null)
        {
            mortarTrail.enabled = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw explosion radius in editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}