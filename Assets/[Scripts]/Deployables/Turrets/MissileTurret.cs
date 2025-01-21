using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileTurret : DeployableBase
{
    [Header("Missile Settings")]
    [SerializeField] private Transform firePoint;

    protected override void FireTurret()
    {
        if (M_Projectile != null && ClosestTarget != null)
        {
            Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
            ProjectileBase projectile = Instantiate(M_Projectile, spawnPosition, transform.rotation);
            projectile.Initialize((int)M_TurretStats.GetDamage(), ClosestTarget.transform.position);
            projectile.ShootProjectile(ClosestTarget.transform.position, ClosestTarget.gameObject);
        }
    }
}
