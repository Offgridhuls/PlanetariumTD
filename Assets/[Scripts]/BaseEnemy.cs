using UnityEngine;
using System.Collections.Generic;

public class BaseEnemy : EnemyBase
{
    private Vector3 orbitAxis;
    private float orbitRadius;
    private float orbitAngle;
    private float orbitHeight;
    private bool isOrbiting = false;
    private bool isDiving = false;
    private float orbitDirection;
    private float nextDiveCheckTime;
    private float diveCheckInterval = 3f;
    private float diveSpeed = 40f;
    private Transform currentTarget;
    private Vector3 diveTarget;
    private float detectionRange = 30f;

    protected override void Start()
    {
        base.Start();
        if (currentPlanet != null)
        {
            // Calculate initial orbit parameters
            orbitRadius = Vector3.Distance(transform.position, currentPlanet.transform.position);
            orbitHeight = transform.position.y - currentPlanet.transform.position.y;
            
            // Calculate random orbit axis (perpendicular to initial direction)
            Vector3 toEnemy = transform.position - currentPlanet.transform.position;
            orbitAxis = Vector3.Cross(toEnemy, Random.onUnitSphere).normalized;
            
            // Random starting angle and direction
            orbitAngle = Random.Range(0f, 360f);
            orbitDirection = Random.value > 0.5f ? 1f : -1f;
            
            isOrbiting = true;
            nextDiveCheckTime = Time.time + Random.Range(0f, diveCheckInterval);
        }
    }

    protected override void Update()
    {
        if (isDead || currentPlanet == null) return;
        
        if (isDiving)
        {
            HandleDiving();
        }
        else if (isOrbiting)
        {
            HandleOrbiting();
            CheckForDiveTarget();
        }
    }

    private void HandleOrbiting()
    {
        // Update orbit angle based on speed and direction
        orbitAngle += enemyStats.MoveSpeed * orbitDirection * Time.deltaTime;
        
        // Calculate new position
        Vector3 orbitCenter = currentPlanet.transform.position;
        Quaternion rotation = Quaternion.AngleAxis(orbitAngle, orbitAxis);
        Vector3 orbitPosition = orbitCenter + rotation * (Vector3.forward * orbitRadius);
        orbitPosition.y = orbitCenter.y + orbitHeight;
        
        // Move to new position
        transform.position = orbitPosition;
        
        // Face movement direction
        Vector3 targetDirection = (orbitPosition - transform.position).normalized;
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, enemyStats.RotSpeed * Time.deltaTime);
        }
    }

    private void CheckForDiveTarget()
    {
        if (Time.time < nextDiveCheckTime) return;
        
        nextDiveCheckTime = Time.time + diveCheckInterval;
        
        // Random chance to initiate dive
        if (Random.value < 0.3f)
        {
            // Find potential targets (IDamageable objects)
            Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRange);
            List<Transform> potentialTargets = new List<Transform>();
        
            foreach (Collider col in colliders)
            {
                var damageable = col.GetComponent<IDamageable>();
                if (damageable != null && col.gameObject != gameObject)
                {
                    // Only target planets, turrets, and structures
                    DamageableType targetType = damageable.GetDamageableType();
                    if (targetType == DamageableType.Planet || 
                        targetType == DamageableType.Turret || 
                        targetType == DamageableType.Structure)
                    {
                        potentialTargets.Add(col.transform);
                    }
                }
            }
        
            // If we found any targets, randomly select one and start diving
            if (potentialTargets.Count > 0)
            {
                currentTarget = potentialTargets[Random.Range(0, potentialTargets.Count)];
                StartDive();
            }
        }
    }

    private void StartDive()
    {
        isDiving = true;
        isOrbiting = false;
        diveTarget = currentTarget.position;
    }

    private void HandleDiving()
    {
        if (currentTarget == null)
        {
            ReturnToOrbit();
            return;
        }
        
        // Update dive target position
        diveTarget = currentTarget.position;
        
        // Calculate direction to target
        Vector3 directionToTarget = (diveTarget - transform.position).normalized;
        
        // Move towards target
        transform.position += directionToTarget * diveSpeed * Time.deltaTime;
        
        // Face dive direction
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, enemyStats.RotSpeed * Time.deltaTime);
        
        // Check if we've reached the target
        if (Vector3.Distance(transform.position, diveTarget) < 1f)
        {
            // Try to damage the target
            var damageable = currentTarget.GetComponent<IDamageable>();
            if (damageable != null)
            {
                DamageableType targetType = damageable.GetDamageableType();
                if (targetType != DamageableType.Enemy) // Double check we're not hitting another enemy
                {
                    damageable.TakeDamage(enemyStats.damage);
                }
            }
            ReturnToOrbit();
        }
    }

    private void ReturnToOrbit()
    {
        isDiving = false;
        isOrbiting = true;
        currentTarget = null;
        nextDiveCheckTime = Time.time + diveCheckInterval;
    }

    public override DamageableType GetDamageableType()
    {
        return DamageableType.Enemy;
    }

    protected override void Die()
    {
        if (isDead) return;
        
        // Spawn death effect if available
        if (enemyStats != null && enemyStats.deathEffect != null)
        {
            Instantiate(enemyStats.deathEffect, transform.position, Quaternion.identity);
        }
        
        base.Die();
    }
}
