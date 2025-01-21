using UnityEngine;

public class MissileProjectile : ProjectileBase
{
    [Header("Missile Settings")]
    [SerializeField] private float turnSpeed = 180f;
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private LayerMask explosionLayers;
    [SerializeField] private float proximityThreshold = 0.5f;

    private bool isLaunched = false;

    protected override void Awake()
    {
        base.Awake();
    }

    public override void ShootProjectile(Vector3 target, GameObject enemy)
    {
        targetPosition = target;
        targetEnemy = enemy;
        isLaunched = true;
        isInitialized = true;

        // Initial orientation
        Vector3 direction = (target - transform.position).normalized;
        transform.LookAt(target);
    }

    protected override void MoveProjectile()
    {
        if (!isLaunched || targetEnemy == null) return;

        // Update target position to follow enemy
        targetPosition = targetEnemy.transform.position;
        Vector3 direction = (targetPosition - transform.position).normalized;
        
        // Smoothly rotate towards target
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

        // Apply movement using rigidbody
        if (rigidBody != null)
        {
            rigidBody.linearVelocity = transform.forward * projectileSpeed;
        }
        else
        {
            transform.position += transform.forward * projectileSpeed * Time.deltaTime;
        }

        // Check if we're close enough to explode
        if (Vector3.Distance(transform.position, targetPosition) < proximityThreshold)
        {
            Explode();
        }
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

        // Deal damage to all objects in radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius, explosionLayers);
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
