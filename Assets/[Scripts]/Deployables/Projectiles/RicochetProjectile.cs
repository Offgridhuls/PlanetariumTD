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
    private EnemyBase currentTarget;
    private Vector3 targetPredictedPos;

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

        if (currentTarget != null)
        {
            // Update predicted position and adjust course
            targetPredictedPos = PredictEnemyPosition(currentTarget, Vector3.Distance(transform.position, currentTarget.transform.position) / projectileSpeed);
            direction = (targetPredictedPos - transform.position).normalized;
            GetComponent<Rigidbody>().linearVelocity = direction * projectileSpeed;
            transform.rotation = Quaternion.LookRotation(direction);
        }
        
        // Check for hits
        CheckForHits();
    }

    protected void CheckForHits()
    {
        // Use velocity-based distance for raycast to ensure we don't miss fast-moving targets
        float checkDistance = Mathf.Max(GetComponent<Rigidbody>().linearVelocity.magnitude * Time.deltaTime * 1f, 1f);
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
        
        if (colliders.Length == 0) return false;

        // Get current velocity
        var rb = GetComponent<Rigidbody>();
        float currentSpeed = projectileSpeed;
        
        // Find nearest valid target
        var nearestTarget = colliders
            .Select(c => {
                var enemy = c.GetComponent<EnemyBase>();
                if (enemy == null || hitTargets.Contains(c.gameObject)) return null;
                
                Vector3 enemyPos = enemy.transform.position;
                float distanceToTarget = Vector3.Distance(hitPosition, enemyPos);
                
                return new { 
                    Enemy = enemy,
                    Distance = distanceToTarget
                };
            })
            .Where(x => x != null)
            .OrderBy(x => x.Distance)
            .FirstOrDefault();

        if (nearestTarget == null) return false;

        // Set the new target and initial direction
        currentTarget = nearestTarget.Enemy;
        targetPredictedPos = PredictEnemyPosition(currentTarget, nearestTarget.Distance / projectileSpeed);
        direction = (targetPredictedPos - hitPosition).normalized;
        rb.linearVelocity = direction * projectileSpeed;
        transform.rotation = Quaternion.LookRotation(direction);

        return true;
    }

    private Vector3 PredictEnemyPosition(EnemyBase enemy, float predictionTime)
    {
        if (!previousPositions.TryGetValue(enemy, out Vector3 prevPos))
        {
            prevPos = enemy.transform.position;
            previousPositions[enemy] = prevPos;
        }

        Vector3 currentPos = enemy.transform.position;
        
        // Calculate velocity based on actual movement
        Vector3 velocity = (currentPos - prevPos) / Time.deltaTime;
        previousPositions[enemy] = currentPos;

        // Get the current planet and orbital information
        var currentPlanet = enemy.GetCurrentPlanet();
        if (currentPlanet != null)
        {
            Vector3 planetPos = currentPlanet.transform.position;
            Vector3 toEnemy = currentPos - planetPos;
            float orbitRadius = toEnemy.magnitude;
            
            // Calculate orbital velocity
            Quaternion currentRotation = Quaternion.LookRotation(toEnemy);
            Vector3 predictedPos = planetPos + (Quaternion.AngleAxis(velocity.magnitude * Mathf.Rad2Deg * predictionTime, Vector3.up) * toEnemy);
            
            // Ensure we maintain the correct orbit radius
            predictedPos = planetPos + (predictedPos - planetPos).normalized * orbitRadius;
            return predictedPos;
        }
        
        // For non-orbiting enemies, use linear prediction
        return currentPos + velocity * predictionTime;
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
