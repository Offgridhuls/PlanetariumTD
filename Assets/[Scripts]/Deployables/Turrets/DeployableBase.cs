using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeployableBase : MonoBehaviour
{
    [Header("Turret Components")]
    [SerializeField]
    private Transform Muzzle;
    [SerializeField]
    private Transform TurretPivot;

    [Header("Projectile")]
    [SerializeField]
    private ProjectileBase M_Projectile;

    private UpgradeComponent M_UpgradeComponent;

    [SerializeField]
    private TurretStats M_TurretStats;

    private bool IsDestroyed = false;
    private EnemyBase ClosestTarget;

    private float FireTimer = 0;

    private Vector3 targetPosition;
    private Vector3 previousPosition;
    private Vector3 enemyVelocity;

    void Update()
    {
        if (ClosestTarget != null)
        {
            targetPosition = PredictFuturePosition(ClosestTarget, M_Projectile.GetProjectileSpeed());
            previousPosition = ClosestTarget.gameObject.transform.position;
        }

        if (!IsDestroyed)
        {
            FireTimer += Time.deltaTime;
            if (FireTimer >= M_TurretStats.GetFireInterval())
            {
                if (M_Projectile != null && ClosestTarget != null)
                {
                    FireTurret();
                }
                FireTimer = 0;
            }
            CheckClosestTarget();
            if (ClosestTarget != null)
            {
                RotateTowardsTarget(targetPosition);
            }
        }
    }

    public int GetTurretDamage()
    {
        return M_TurretStats.GetDamage();
    }

    protected virtual void FireTurret()
    {
        var Projectile = Instantiate(M_Projectile, Muzzle.position, Quaternion.identity);
        Projectile.ShootProjectile(targetPosition);
    }

    void CheckClosestTarget()
    {
        Collider[] hitTargets = Physics.OverlapSphere(transform.position, M_TurretStats.GetAgroRadius());
        EnemyBase closestEnemy = null;

        float closestDistance = M_TurretStats.GetAgroRadius();

        foreach (Collider hitCollider in hitTargets)
        {
            if (hitCollider.GetComponent<EnemyBase>() != null)
            {
                var Enemy = hitCollider.GetComponent<EnemyBase>();
                float distance = Vector3.Distance(transform.position, Enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = Enemy;
                }
            }
        }

        ClosestTarget = closestEnemy;
    }

    private void RotateTowardsTarget(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        TurretPivot.transform.rotation = Quaternion.Slerp(TurretPivot.transform.rotation, targetRotation, Time.deltaTime * 5f);
    }

    Vector3 PredictFuturePosition(EnemyBase target, float bulletSpeed)
    {
        enemyVelocity = (target.gameObject.transform.position - previousPosition) / Time.deltaTime;

        if (enemyVelocity.magnitude > 0)
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
            float travelTime = distanceToTarget / bulletSpeed;
            return target.transform.position + enemyVelocity * travelTime;
        }
        else
        {
            return target.transform.position;
        }
    }
}