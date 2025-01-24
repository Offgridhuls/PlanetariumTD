using UnityEngine;

public class RicochetTurret : DeployableBase
{
    [Header("Ricochet Settings")]
    [SerializeField] private ParticleSystem muzzleFlash;

    protected override void Update()
    {
        base.Update();
        if (isDead) return;

        // Handle firing
        if (ClosestTarget != null && HasLineOfSight(ClosestTarget.transform))
        {
            FireTimer += Time.deltaTime;
            if (FireTimer >= M_TurretStats.GetFireInterval())
            {
                FireTimer = 0f;
                FireTurret();
                
            }
        }
    }

    protected override void FireTurret()
    {
        if (ClosestTarget == null || !HasLineOfSight(ClosestTarget.transform)) return;
        // Calculate predicted position
        Vector3 targetPos = PredictTargetPosition(ClosestTarget, M_TurretStats.GetProjectileSpeed());

        // Only fire if target is far enough away
        if (Vector3.Distance(transform.position, ClosestTarget.transform.position) <= M_TurretStats.GetAgroRadius())
        {
            // Play effects
            PlayFireSound();
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }

            // Spawn and initialize projectile
            ProjectileBase projectile = Instantiate(M_Projectile, TurretMuzzle.position, Quaternion.identity);
            projectile.ShootProjectile(targetPos, ClosestTarget.gameObject);
        }
    }

    protected override void RotateTowardsTarget(Vector3 target)
    {
       // if (TurretMuzzle != null)
       // {
       //     // Rotate only the muzzle, not the entire turret
       //     Vector3 targetDirection = target - transform.position;
       //     Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
       //     transform.rotation = Quaternion.RotateTowards(
       //         transform.rotation,
       //         targetRotation,
       //         M_TurretStats.GetRotationSpeed() * Time.deltaTime
       //     );
       // }
       // else
       // {
       //     base.RotateTowardsTarget(target);
       // }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Draw the minimum target distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, M_TurretStats.GetAgroRadius());

        // Draw the maximum range
        if (M_TurretStats != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, M_TurretStats.GetAgroRadius());
        }
    }
#endif
}
