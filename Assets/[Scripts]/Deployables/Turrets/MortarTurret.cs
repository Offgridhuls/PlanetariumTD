using Planetarium;
using UnityEngine;

public class MortarTurret : DeployableBase
{
    [Header("Mortar Settings")]
    [SerializeField] private Transform mortarBarrel;
    [SerializeField] private float barrelRotationSpeed = 45f;
    [SerializeField] private float minFiringAngle = 45f;
    [SerializeField] private float maxFiringAngle = 75f;
    [SerializeField] private Transform firePoint;

    private float currentBarrelAngle;
    private PlanetBase planet;

    protected override void Start()
    {
        base.Start();
        planet = FindFirstObjectByType<PlanetBase>();

        // Initialize barrel angle
        currentBarrelAngle = minFiringAngle;
    }

    protected override void Update()
    {
        base.Update();
        
        if (ClosestTarget == null || !ClosestTarget.IsAlive)
        {
            ClosestTarget = null;
        }

        FireTimer += Time.deltaTime;
        if (FireTimer >= M_TurretStats.GetFireInterval())
        {
            FireTimer = 0f;
            FireTurret();
        }
    }

    
    protected override void RotateTowardsTarget(Vector3 target)
    {
        if (TurretPivot == null || planet == null) 
        {
            return;
        }
        Debug.Log("Rotating");
        // Get direction to target in world space
        Vector3 toTarget = target - transform.position;
        Vector3 upDirection = (transform.position - planet.transform.position).normalized;

        // Project target direction onto the plane perpendicular to up direction
        Vector3 projectedDirection = Vector3.ProjectOnPlane(toTarget, upDirection).normalized;

        // Calculate yaw angle for base rotation (around Y axis)
        float targetYaw = Mathf.Atan2(projectedDirection.x, projectedDirection.z) * Mathf.Rad2Deg;

        // Create base rotation (Y-axis only)
        Quaternion baseRotation = Quaternion.Euler(0, targetYaw, 0);

        // Apply base rotation
        TurretPivot.localRotation = Quaternion.RotateTowards(
            TurretPivot.localRotation,
            baseRotation,
            M_TurretStats.GetRotationSpeed() * Time.deltaTime
        );

        // Update barrel rotation (X-axis only)
        if (mortarBarrel != null)
        {
            // Oscillate barrel angle
            currentBarrelAngle = Mathf.PingPong(Time.time * barrelRotationSpeed, maxFiringAngle - minFiringAngle) + minFiringAngle;
            
            // Apply barrel rotation (X-axis only)
            mortarBarrel.localRotation = Quaternion.Euler(currentBarrelAngle, 0, 0);
        }
    }
    protected override void FireTurret()
    {
        if (M_Projectile != null && ClosestTarget != null && planet != null)
        {
            // Calculate spawn position
            Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
            
            // Calculate initial rotation based on current barrel angle
            Vector3 upDirection = (transform.position - planet.transform.position).normalized;
            Quaternion spawnRotation = Quaternion.AngleAxis(currentBarrelAngle, transform.right) * transform.rotation;

            // Spawn and initialize projectile
            ProjectileBase projectile = Instantiate(M_Projectile, spawnPosition, spawnRotation);
            projectile.Initialize((int)M_TurretStats.GetDamage(), ClosestTarget.transform.position, M_TurretStats.GetProjectileSpeed());
            projectile.ShootProjectile(ClosestTarget.transform.position, ClosestTarget.gameObject);

            // Add initial velocity in the direction of the barrel
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = spawnRotation * Vector3.forward * 20f;
            }
        }
    }

   

  

    private void OnDrawGizmosSelected()
    {
        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(firePoint.position, 0.2f);
        }
    }
}
