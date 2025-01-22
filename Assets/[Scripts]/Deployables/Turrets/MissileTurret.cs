using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileTurret : DeployableBase
{
    [Header("Missile Settings")]
    [SerializeField] private Transform firePoint;

  
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
