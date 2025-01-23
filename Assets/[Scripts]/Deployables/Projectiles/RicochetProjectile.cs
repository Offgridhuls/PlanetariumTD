using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RicochetProjectile : ProjectileBase
{
    [Header("Ricochet Settings")]
    [SerializeField] private int maxRicochets = 3;
    [SerializeField] private float ricochetRange = 10f;
    [SerializeField] private float damageReductionPerBounce = 0.2f; // Each bounce does 20% less damage
    [SerializeField] private TrailRenderer bulletTrail;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private float minBounceAngle = 20f; // Minimum angle change on bounce

    private Vector3 direction;
    private int ricochetsRemaining;
    private HashSet<GameObject> hitTargets = new HashSet<GameObject>();
    private float currentDamage;
    private Dictionary<EnemyBase, Vector3> previousPositions = new Dictionary<EnemyBase, Vector3>();

    public override void ShootProjectile(Vector3 target, GameObject enemy)
    {
        targetPosition = target;
        targetEnemy = enemy;
        isInitialized = true;
        ricochetsRemaining = maxRicochets;
        currentDamage = damage;
        
        direction = (target - transform.position).normalized;
        var rb = GetComponent<Rigidbody>();
        rb.linearVelocity = direction * projectileSpeed;
    }

    protected override void Update()
    {
        base.Update();
        if (!isInitialized) return;
        
        // Update direction for raycast hit detection
        direction = GetComponent<Rigidbody>().linearVelocity.normalized;
        
        // Check for hits
        CheckForHits();
    }

    protected void CheckForHits()
    {
        // Use velocity-based distance for raycast to ensure we don't miss fast-moving targets
        float checkDistance = Mathf.Max(GetComponent<Rigidbody>().linearVelocity.magnitude * Time.deltaTime * 2f, 2f);
        RaycastHit[] hits = Physics.RaycastAll(transform.position, direction, checkDistance, targetLayers);
        
        if (hits.Length > 0)
        {
            // Sort hits by distance
            System.Array.Sort(hits, (a, b) => 
                (a.point - transform.position).sqrMagnitude.CompareTo((b.point - transform.position).sqrMagnitude));
            
            // Find the first valid hit (one we haven't hit before)
            foreach (var hit in hits)
            {
                if (!hitTargets.Contains(hit.collider.gameObject))
                {
                    HandleHit(hit.collider.gameObject);
                    break;
                }
            }
        }
    }

    protected override void HandleHit(GameObject hitObject)
    {
        // Don't hit the same target twice
        if (hitTargets.Contains(hitObject)) return;
        hitTargets.Add(hitObject);

        // Create hit effect
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(effect, effectLifetime);
        }

        // Deal damage
        var damageable = hitObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            DamageData damageData = new DamageData
            {
                Damage = currentDamage,
                Source = gameObject
            };
            damageable.ProcessDamage(damageData);
        }

        // If we have ricochets remaining, find next target
        if (ricochetsRemaining > 0)
        {
            if (FindAndBounceToNextTarget(hitObject.transform.position))
            {
                ricochetsRemaining--;
                currentDamage *= (1f - damageReductionPerBounce);
            }
            else
            {
                // No valid targets found, destroy projectile
                DestroyProjectile();
            }
        }
        else
        {
            DestroyProjectile();
        }
    }

    private bool FindAndBounceToNextTarget(Vector3 hitPosition)
    {
        // Find all potential targets in range
        Collider[] colliders = Physics.OverlapSphere(hitPosition, ricochetRange, targetLayers);
        
        // Filter out already hit targets and sort by distance
        var validTargets = colliders
            .Select(c => {
                var enemy = c.GetComponent<EnemyBase>();
                return new { GameObject = c.gameObject, Enemy = enemy };
            })
            .Where(obj => !hitTargets.Contains(obj.GameObject) && obj.Enemy != null)
            .OrderBy(obj => 
            {
                // Calculate predicted position for this enemy
                Vector3 predictedPos = PredictEnemyPosition(obj.Enemy);
                Vector3 toTarget = predictedPos - hitPosition;
                float angle = Vector3.Angle(direction, toTarget);
                // Prefer targets that cause larger direction changes (more interesting bounces)
                return angle < minBounceAngle ? float.MaxValue : Vector3.Distance(hitPosition, predictedPos);
            })
            .ToList();

        if (validTargets.Count == 0) return false;

        // Get the best target and update projectile direction
        var nextTarget = validTargets[0];
        Vector3 predictedPosition = PredictEnemyPosition(nextTarget.Enemy);
        Vector3 newDirection = (predictedPosition - hitPosition).normalized;
        
        // Update projectile
        direction = newDirection;
        var rb = GetComponent<Rigidbody>();
        rb.linearVelocity = direction * projectileSpeed;
        transform.rotation = Quaternion.LookRotation(direction);

        return true;
    }

    private Vector3 PredictEnemyPosition(EnemyBase enemy)
    {
        if (enemy == null) return enemy.transform.position;

        // Calculate time to reach target based on current distance and projectile speed
        float distanceToTarget = Vector3.Distance(transform.position, enemy.transform.position);
        float timeToTarget = distanceToTarget / projectileSpeed;

        // Get enemy velocity using position difference
        Vector3 enemyVelocity = Vector3.zero;
        if (!previousPositions.ContainsKey(enemy))
        {
            previousPositions[enemy] = enemy.transform.position;
        }
        else
        {
            enemyVelocity = (enemy.transform.position - previousPositions[enemy]) / Time.deltaTime;
            previousPositions[enemy] = enemy.transform.position;
        }
        
        // Predict future position
        return enemy.transform.position + enemyVelocity * timeToTarget;
    }

    private void DestroyProjectile()
    {
        if (bulletTrail != null)
        {
            bulletTrail.enabled = false;
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

    private void OnDrawGizmosSelected()
    {
        // Draw the ricochet range in the editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, ricochetRange);
    }
}
