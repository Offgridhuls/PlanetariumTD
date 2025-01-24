using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileTurret : DeployableBase
{
    [Header("Missile Settings")]
    [SerializeField] private Transform firePoint;

    protected override void RotateTowardsTarget(Vector3 target)
    {
        if (TurretPivot == null) return;

        // Get direction to target
        Vector3 targetDirection = (target - TurretPivot.position).normalized;
        
        // Project the direction onto the XZ plane for yaw
        Vector3 flatDirection = new Vector3(targetDirection.x, 0, targetDirection.z).normalized;
        float yaw = Mathf.Atan2(flatDirection.x, flatDirection.z) * Mathf.Rad2Deg;
        
        // Calculate pitch based on the angle between flat direction and actual direction
        float pitch = -Vector3.SignedAngle(flatDirection, targetDirection, Vector3.Cross(flatDirection, Vector3.up));

        // Create and apply rotation
        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0);
        TurretPivot.rotation = Quaternion.RotateTowards(
            TurretPivot.rotation,
            targetRotation,
            M_TurretStats.GetRotationSpeed() * Time.deltaTime
        );
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

    protected override void FireTurret()
    {
        if (ClosestTarget == null || !HasLineOfSight(ClosestTarget.transform)) return;

 
        // Calculate direction to target with optional spread
        Vector3 directionToTarget = (ClosestTarget.transform.position - firePoint.position).normalized;
        
        
        // Spawn and initialize projectile
        ProjectileBase projectile = Instantiate(M_Projectile, firePoint.position, Quaternion.LookRotation(directionToTarget));
        projectile.Initialize(M_TurretStats.GetDamage(), ClosestTarget.transform.position, M_TurretStats.GetProjectileSpeed());
        projectile.ShootProjectile(ClosestTarget.transform.position, ClosestTarget.gameObject);
    
    }
}
