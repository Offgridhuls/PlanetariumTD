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

    private Vector3 startPosition;
    private float journeyLength;
    private float journeyTime;
    private bool hasExploded = false;

    public override void ShootProjectile(Vector3 target, GameObject enemy)
    {
        targetPosition = target;
        targetEnemy = enemy;
        startPosition = transform.position;
        journeyLength = Vector3.Distance(startPosition, targetPosition);
        journeyTime = journeyLength / projectileSpeed;
        isInitialized = true;
    }

    protected override void Update()
    {
        if (!isInitialized || hasExploded) return;

        // Calculate journey progress
        aliveTime += Time.deltaTime;
        float progress = aliveTime / journeyTime;

        if (progress >= 1f)
        {
            // Reached target, explode
            transform.position = targetPosition;
            HandleHit(gameObject);
            return;
        }

        // Calculate arc motion
        Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, progress);
        float heightOffset = arcHeight * Mathf.Sin(progress * Mathf.PI);
        currentPos.y += heightOffset;
        
        // Update position
        transform.position = currentPos;
        
        // Update rotation to face movement direction
        if (progress < 1f)
        {
            Vector3 nextPos = Vector3.Lerp(startPosition, targetPosition, Mathf.Min(1f, progress + 0.1f));
            nextPos.y += arcHeight * Mathf.Sin(Mathf.Min(1f, progress + 0.1f) * Mathf.PI);
            transform.rotation = Quaternion.LookRotation((nextPos - currentPos).normalized);
        }

        // Check for early hits
        CheckForHits();
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