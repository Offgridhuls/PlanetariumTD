using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RicochetProjectile : ProjectileBase
{
    [Header("Ricochet Settings")]
    [SerializeField] private int maxRicochets = 3;
    [SerializeField] private float ricochetRange = 10f;
    [SerializeField] private float damageReductionPerBounce = 0.2f; // Each bounce does 20% less damage
    [SerializeField] private float speedReductionAfterFirstBounce = 0.5f; // Reduces speed by 50% after first bounce
    [SerializeField] private TrailRenderer bulletTrail;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private float minBounceAngle = 20f; // Minimum angle change on bounce

    [Header("Arc Settings")]
    [SerializeField] private float arcHeight = 5f;
    [SerializeField] private float gravityScale = 5f;
    private bool isArcing = false;
    private float arcStartTime;
    private Vector3 arcStartPos;
    private float arcDuration;
    private Vector3 arcUpDirection; // Direction for the arc's "up"

    private Vector3 direction;
    private int ricochetsRemaining;
    private HashSet<GameObject> hitTargets = new HashSet<GameObject>();
    private float currentDamage;
    private float currentSpeed;
    private Dictionary<EnemyBase, Vector3> previousPositions = new Dictionary<EnemyBase, Vector3>();
    private EnemyBase currentTarget;
    private Vector3 targetPredictedPos;

    public override void Initialize(int damage, Vector3 target, float speed)
    {
        base.Initialize(damage, target, speed);
        currentDamage = damage;
    }

    public override void ShootProjectile(Vector3 target, GameObject enemy)
    {
        targetPosition = target;
        targetEnemy = enemy;
        isInitialized = true;
        ricochetsRemaining = maxRicochets;
        currentSpeed = projectileSpeed;
        
        direction = (target - transform.position).normalized;
        var rb = GetComponent<Rigidbody>();
        rb.linearVelocity = direction * currentSpeed;
    }

    protected override void Update()
    {
        base.Update();
        if (!isInitialized) return;

        var rb = GetComponent<Rigidbody>();
        
        if (isArcing)
        {
            float timeSinceArc = Time.time - arcStartTime;
            if (timeSinceArc < arcDuration)
            {
                // Calculate arc trajectory
                float progress = timeSinceArc / arcDuration;
                float heightOffset = Mathf.Sin(progress * Mathf.PI) * arcHeight;
                
                // Update position towards target with arc
                Vector3 directPath = targetPredictedPos - arcStartPos;
                Vector3 currentTargetPos = arcStartPos + (directPath * progress);
                
                // Apply height offset in the random arc direction
                currentTargetPos += arcUpDirection * heightOffset;
                
                // Calculate velocity to reach next position
                Vector3 newVelocity = (currentTargetPos - transform.position) / Time.deltaTime;
                rb.linearVelocity = Vector3.ClampMagnitude(newVelocity, currentSpeed);
                
                // Update direction for visuals
                direction = rb.linearVelocity.normalized;
                transform.rotation = Quaternion.LookRotation(direction);
            }
            else
            {
                isArcing = false;
            }
        }
        else if (currentTarget != null)
        {
            // Normal target tracking when not arcing
            targetPredictedPos = PredictEnemyPosition(currentTarget, Vector3.Distance(transform.position, currentTarget.transform.position) / currentSpeed);
            direction = (targetPredictedPos - transform.position).normalized;
            rb.linearVelocity = direction * currentSpeed;
            transform.rotation = Quaternion.LookRotation(direction);
        }
        
        // Check for hits
        CheckForHits();
    }

    protected void CheckForHits()
    {
        // Check if we're close enough to current target for direct hit
        if (currentTarget != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
            if (distanceToTarget < 1f && !hitTargets.Contains(currentTarget.gameObject))
            {
                HandleHit(currentTarget.gameObject);
                return;
            }
        }

        // Backup raycast check
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

        // Clear current target since we hit it
        if (currentTarget != null && currentTarget.gameObject == hitObject)
        {
            currentTarget = null;
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

        // Reduce speed after first bounce
        if (ricochetsRemaining == maxRicochets - 1)
        {
            currentSpeed *= speedReductionAfterFirstBounce;
        }

        // Set up arc trajectory
        currentTarget = nearestTarget.Enemy;
        targetPredictedPos = PredictEnemyPosition(currentTarget, nearestTarget.Distance / currentSpeed);
        
        // Initialize arc parameters
        isArcing = true;
        arcStartTime = Time.time;
        arcStartPos = hitPosition;
        arcDuration = nearestTarget.Distance / currentSpeed;

        // Calculate a random arc direction perpendicular to the path
        Vector3 directPath = (targetPredictedPos - hitPosition).normalized;
        Vector3 randomPerp = Vector3.Cross(directPath, Random.onUnitSphere).normalized;
        arcUpDirection = randomPerp;
        
        // Initial velocity for arc
        direction = (targetPredictedPos - hitPosition).normalized;
        rb.linearVelocity = direction * currentSpeed;
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
        var currentPlanet = enemy.CurrentPlanet;
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
