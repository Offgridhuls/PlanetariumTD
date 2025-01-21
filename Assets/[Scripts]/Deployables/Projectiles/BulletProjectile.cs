using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletProjectile : ProjectileBase
{
    [Header("Bullet Settings")]
    [SerializeField] private float penetrationDepth = 0.1f;
    [SerializeField] private TrailRenderer trail;

    public override void ShootProjectile(Vector3 target, GameObject enemy)
    {
        targetPosition = target;
        targetEnemy = enemy;
        isInitialized = true;
        
        // Point bullet in the right direction immediately
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    protected override void MoveProjectile()
    {
        if (!isInitialized) return;

        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position += direction * projectileSpeed * Time.deltaTime;
    }

    protected override void HandleHit(RaycastHit hit)
    {
        // Create hit effect
        if (hitEffect != null)
        {
            Instantiate(hitEffect, hit.point, Quaternion.LookRotation(hit.normal));
        }

        // Deal damage
        DealDamage(hit.collider.gameObject);

        // Disable trail if it exists
        if (trail != null)
        {
            trail.transform.SetParent(null);
            Destroy(trail.gameObject, trail.time);
        }

        OnProjectileHit();
        Destroy(gameObject);
    }

    public override void OnProjectileHit()
    {
        // Additional effects or cleanup can be added here
    }
}
