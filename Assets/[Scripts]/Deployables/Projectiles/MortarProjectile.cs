using UnityEngine;

public class MortarProjectile : ProjectileBase
{
    [Header("Mortar Settings")]
    [SerializeField] private float arcHeight = 5f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float minDistance = 5f; // Minimum distance to prevent self-collision
    [SerializeField] private float maxDistance = 15f; // Maximum distance for better control

    private PlanetBase planet;
    private Vector3 startPoint;
    private Vector3 endPoint;
    private float journeyLength;
    private float journeyTime;

    protected override void Awake()
    {
        base.Awake();
        planet = Object.FindFirstObjectByType<PlanetBase>();
        if (planet == null)
        {
            Debug.LogError("No planet found in scene!");
        }
    }

    public override void ShootProjectile(Vector3 target, GameObject enemy)
    {
        if (planet == null) return;

        startPoint = transform.position;
        endPoint = GetRandomTargetPoint();
        if (endPoint == Vector3.zero)
        {
            Debug.LogError("Failed to find valid target point for mortar");
            Destroy(gameObject);
            return;
        }

        journeyLength = Vector3.Distance(startPoint, endPoint);
        journeyTime = 0f;
        isInitialized = true;

        // Disable collider initially to prevent self-collision
        var collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
            Invoke(nameof(EnableCollider), 0.2f); // Enable after a short delay
        }
    }

    private void EnableCollider()
    {
        var collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = true;
        }
    }

    protected override void MoveProjectile()
    {
        if (!isInitialized || planet == null) return;

        if (journeyTime <= 1f)
        {
            journeyTime += Time.deltaTime * projectileSpeed / journeyLength;

            // Clamp to prevent NaN
            float clampedJourneyTime = Mathf.Clamp01(journeyTime);

            // Interpolate position along the sphere's surface
            Vector3 interpolatedPoint = Vector3.Slerp(startPoint, endPoint, clampedJourneyTime);

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

            if (clampedJourneyTime >= 1f)
            {
                Explode();
            }
        }
    }

    private Vector3 GetRandomTargetPoint()
    {
        for (int i = 0; i < 10; i++) // Try 10 times to find a valid point
        {
            float distance = Random.Range(minDistance, maxDistance);
            Vector3 randomPoint = GetRandomPointOnSphere(distance);
            
            // Validate the point
            if (Vector3.Distance(randomPoint, transform.position) >= minDistance)
            {
                return randomPoint;
            }
        }
        return Vector3.zero; // Return zero if no valid point found
    }

    private Vector3 GetRandomPointOnSphere(float radius)
    {
        float theta = Random.Range(0f, Mathf.PI * 2);
        float phi = Mathf.Acos(Random.Range(-1f, 1f));

        float x = radius * Mathf.Sin(phi) * Mathf.Cos(theta);
        float y = radius * Mathf.Sin(phi) * Mathf.Sin(theta);
        float z = radius * Mathf.Cos(phi);

        return new Vector3(x, y, z) + planet.transform.position;
    }

    protected override void HandleHit(RaycastHit hit)
    {
        Explode();
    }

    private void Explode()
    {
        // Create explosion effect
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        // Deal area damage
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius, enemyLayer);
        foreach (var hitCollider in hitColliders)
        {
            DealDamage(hitCollider.gameObject);
        }

        OnProjectileHit();
        Destroy(gameObject);
    }

    public override void OnProjectileHit()
    {
        // Additional effects or cleanup can be added here
    }

    private void OnDrawGizmosSelected()
    {
        // Draw explosion radius in editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}