using System.Collections;
using System.Collections.Generic;
using Planetarium.Deployables;
using UnityEngine;

public class MachineGun : DeployableBase
{
    
    [Header("Machine Gun Settings")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private float spreadAngle = 5f;  // Maximum spread angle in degrees
    [SerializeField] private bool useSpread = true;

    protected Vector3 PredictTargetPosition(GameObject target, float projectileSpeed)
    {
        if (target == null) return Vector3.zero;

        Vector3 targetPos = target.transform.position;
        
        // Get target's velocity from Rigidbody if available
        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        if (targetRb != null)
        {
            Vector3 targetVelocity = targetRb.linearVelocity;
            
            // Calculate time for projectile to reach the target
            float distance = Vector3.Distance(firePoint.position, targetPos);
            float timeToTarget = distance / projectileSpeed;
            
            // Predict future position based on current velocity
            targetPos += targetVelocity * timeToTarget;
            
            // Optional: Add a second iteration for more accuracy
            distance = Vector3.Distance(firePoint.position, targetPos);
            timeToTarget = distance / projectileSpeed;
            targetPos = target.transform.position + targetVelocity * timeToTarget;
        }
        
        return targetPos;
    }

    protected override void FireTurret()
    {
        if (ClosestTarget == null) return;

        // Calculate predicted position if target is moving
        Vector3 targetPos = PredictTargetPosition(ClosestTarget.gameObject, M_TurretStats.GetProjectileSpeed());
        
        // Calculate direction to target with optional spread
        Vector3 directionToTarget = (targetPos - firePoint.position).normalized;
        
        if (useSpread)
        {
            // Add random spread
            float randomSpreadX = Random.Range(-spreadAngle, spreadAngle);
            float randomSpreadY = Random.Range(-spreadAngle, spreadAngle);
            Quaternion spreadRotation = Quaternion.Euler(randomSpreadX, randomSpreadY, 0);
            directionToTarget = spreadRotation * directionToTarget;
            
            // Update target position with spread
            targetPos = firePoint.position + directionToTarget * Vector3.Distance(firePoint.position, targetPos);
        }
        
        // Spawn and initialize projectile
        ProjectileBase projectile = Instantiate(M_Projectile, TurretMuzzle.position, transform.rotation);
        projectile.Initialize(M_TurretStats.GetDamage(), targetPos, M_TurretStats.GetProjectileSpeed());
        projectile.ShootProjectile(targetPos, ClosestTarget.gameObject);
    }
 
    protected override void Update()
    {
        base.Update();
        
        FireTimer += Time.deltaTime;
        if (FireTimer >= M_TurretStats.GetFireInterval())
        {
            FireTimer = 0f;
            FireTurret();
           
        }
    }
}
