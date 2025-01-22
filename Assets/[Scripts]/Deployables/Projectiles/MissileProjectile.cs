using UnityEngine;

public class MissileProjectile : ProjectileBase
{
    [Header("Missile Settings")]
    [SerializeField] private float turnSpeed = 180f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float explosionForce = 10f;
    [SerializeField] private LayerMask explosionLayers;
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private TrailRenderer missileTrail;

    private Vector3 currentVelocity;
    private bool hasExploded = false;

    public override void ShootProjectile(Vector3 target, GameObject enemy)
    {
        targetPosition = target;
        targetEnemy = enemy;
        currentVelocity = transform.forward * projectileSpeed;
        isInitialized = true;
    }

    protected override void Update()
    {
        if (!isInitialized || hasExploded) return;

        // Update target position if enemy is still alive
        if (targetEnemy != null)
        {
            targetPosition = targetEnemy.transform.position;
        }

        // Calculate direction to target
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        
        // Smoothly rotate towards target
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

        // Update velocity based on rotation
        currentVelocity = Vector3.Lerp(currentVelocity, transform.forward * projectileSpeed, Time.deltaTime * 5f);
        
        // Move missile
        transform.position += currentVelocity * Time.deltaTime;

        // Check for hits
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
        if (missileTrail != null)
        {
            missileTrail.enabled = false;
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
        // Cleanup or additional effects when missile hits
        if (missileTrail != null)
        {
            missileTrail.enabled = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw explosion radius in editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
