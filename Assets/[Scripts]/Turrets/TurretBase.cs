using UnityEngine;

public abstract class TurretBase : MonoBehaviour
{
    [Header("Turret Properties")]
    [SerializeField] protected float range = 10f;
    [SerializeField] protected float fireRate = 1f;
    [SerializeField] protected float rotationSpeed = 5f;
    [SerializeField] protected Transform turretHead;
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected GameObject bulletPrefab;

    protected float nextFireTime;
    protected Transform currentTarget;

    protected virtual void Update()
    {
        if (currentTarget == null)
        {
            FindTarget();
            return;
        }

        if (!IsTargetInRange())
        {
            currentTarget = null;
            return;
        }

        AimAtTarget();
        TryShoot();
    }

    protected virtual void FindTarget()
    {
        // Override in specific turret types to implement targeting logic
    }

    protected virtual bool IsTargetInRange()
    {
        if (currentTarget == null) return false;
        return Vector3.Distance(transform.position, currentTarget.position) <= range;
    }

    protected virtual void AimAtTarget()
    {
        if (turretHead == null || currentTarget == null) return;

        Vector3 targetDirection = currentTarget.position - turretHead.position;
        targetDirection.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        turretHead.rotation = Quaternion.Slerp(turretHead.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }

    protected virtual void TryShoot()
    {
        if (Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + 1f / fireRate;
        }
    }

    protected virtual void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Bullet bulletComponent = bullet.GetComponent<Bullet>();
        if (bulletComponent != null)
        {
            bulletComponent.Initialize(currentTarget);
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
