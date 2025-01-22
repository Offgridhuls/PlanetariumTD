using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MachineGun : DeployableBase
{
    
    [Header("Machine Gun Settings")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private float spreadAngle = 5f;  // Maximum spread angle in degrees
    [SerializeField] private bool useSpread = true;

    protected override void FireTurret()
    {
        if (ClosestTarget == null || !HasLineOfSight(ClosestTarget.transform)) return;

        // Calculate predicted position if target is moving
        Vector3 targetPos = PredictTargetPosition();
        
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
        ProjectileBase projectile = Instantiate(M_Projectile, firePoint.position, Quaternion.LookRotation(directionToTarget));
        projectile.Initialize(M_TurretStats.GetDamage(), targetPos, M_TurretStats.GetProjectileSpeed());
        projectile.ShootProjectile(targetPos, ClosestTarget.gameObject);
    }
 
    protected override void Update()
    {
        base.Update();
    }
}
