using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletProjectile : ProjectileBase
{
    [Header("Bullet Settings")]
    [SerializeField] private float penetrationDepth = 0.1f;
    [SerializeField] private TrailRenderer bulletTrail;
    [SerializeField] private GameObject hitEffect;

    private Vector3 direction;

    public override void ShootProjectile(Vector3 target, GameObject enemy)
    {
        targetPosition = target;
        targetEnemy = enemy;
        isInitialized = true;
        
        Vector3 targetDirection = (target - transform.position).normalized;
        GetComponent<Rigidbody>().AddForce(targetDirection * projectileSpeed, ForceMode.Impulse);
    }

    protected override void Update()
    {
        base.Update();
        if (!isInitialized) return;
        
        // Move bullet in straight line
        direction = (targetPosition - transform.position).normalized;
       // transform.position += direction * projectileSpeed * Time.deltaTime;
        
        // Check for hits
        CheckForHits();
    }

    protected void CheckForHits()
    {
        RaycastHit[] hits = Physics.RaycastAll(transform.position, direction, 1.5f, targetLayers);
        if (hits.Length > 0)
        {
            // Sort hits by distance
            System.Array.Sort(hits, (a, b) => 
                (a.point - transform.position).sqrMagnitude.CompareTo((b.point - transform.position).sqrMagnitude));
            
            // Handle closest hit
            HandleHit(hits[0].collider.gameObject);
        }
    }

    protected override void HandleHit(GameObject hitObject)
    {
        // Create hit effect
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(effect, effectLifetime);
        }

        // Disable trail before destroying
        if (bulletTrail != null)
        {
            bulletTrail.enabled = false;
        }

        // Deal damage
        var damageable = hitObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            DamageData damageData = new DamageData
            {
                Damage = damage,
                Source = gameObject
            };
            damageable.ProcessDamage(damageData);
        }

        OnProjectileHit();
        Destroy(gameObject);
    }

    public override void OnProjectileHit()
    {
        // Cleanup effects
        if (bulletTrail != null)
        {
            bulletTrail.enabled = false;
        }
    }
}
