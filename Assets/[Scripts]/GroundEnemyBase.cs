using UnityEngine;
using Planetarium.AI;

namespace Planetarium
{
    [RequireComponent(typeof(Rigidbody))]
    public class GroundEnemyBase : EnemyBase
    {
        [Header("Ground Movement")]
        [SerializeField] protected float groundCheckDistance = 1f;
        [SerializeField] protected float groundStickForce = 10f;
        
        [Header("Ground Attack")]
        [SerializeField] protected ParticleSystem chargeEffect;
        [SerializeField] protected ParticleSystem attackEffect;
        [SerializeField] protected float chargeTime = 0.5f;
        [SerializeField] protected int burstCount = 3;
        [SerializeField] protected float burstDelay = 0.2f;
        
        protected bool isCharging;
        protected float chargeStartTime;
        protected int currentBurst;
        protected float nextBurstTime;

        protected override void Awake()
        {
            base.Awake();
            
            
        }

        protected override void Start()
        {
            base.Start();

            // Configure rigidbody for ground movement
            if (rb != null)
            {
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                rb.linearDamping = 1f;
            }
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            // Apply ground stick force
            Vector3 surfacePoint, surfaceNormal;
            if (GetPlanetSurfacePoint(out surfacePoint, out surfaceNormal))
            {
                float distanceToGround = Vector3.Distance(transform.position, surfacePoint);
                if (distanceToGround > groundCheckDistance)
                {
                    Vector3 stickForce = -surfaceNormal * groundStickForce;
                    rb.AddForce(stickForce, ForceMode.Force);
                }
            }
        }

        public override void Attack(Vector3 targetPosition)
        {
            if (!isCharging)
            {
                // Start charge
                StartCharge();
            }
            else if (Time.time - chargeStartTime >= chargeTime)
            {
                // Check if it's time for next burst
                if (Time.time >= nextBurstTime && currentBurst < burstCount)
                {
                    FireBurst(targetPosition);
                    currentBurst++;
                    nextBurstTime = Time.time + burstDelay;
                }
                else if (currentBurst >= burstCount)
                {
                    // Reset charge state
                    isCharging = false;
                    currentBurst = 0;
                }
            }
        }

        protected virtual void StartCharge()
        {
            isCharging = true;
            chargeStartTime = Time.time;
            currentBurst = 0;
            nextBurstTime = chargeStartTime + chargeTime;

            if (chargeEffect != null)
            {
                chargeEffect.Play();
            }
        }

        protected virtual void FireBurst(Vector3 targetPosition)
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning("Attack: No projectile prefab assigned to enemy!");
                return;
            }

            // Calculate spread pattern for burst
            Vector3 spawnPosition = projectileSpawnPoint != null 
                ? projectileSpawnPoint.position 
                : transform.position;

            Vector3 direction = (targetPosition - spawnPosition).normalized;
            Quaternion baseRotation = Quaternion.LookRotation(direction, transform.up);

            // Create projectile with slight spread
            float spreadAngle = 15f * (currentBurst - (burstCount / 2f)) / burstCount;
            Quaternion spreadRotation = baseRotation * Quaternion.Euler(0, spreadAngle, 0);
            
            ProjectileBase projectile = Instantiate(projectilePrefab, spawnPosition, spreadRotation);
            projectile.Initialize(
                GetStats().attackDamage,
                targetPosition,
                GetStats().ProjectileSpeed
            );
            projectile.ShootProjectile(targetPosition, CurrentTarget.gameObject);

            if (attackEffect != null)
            {
                attackEffect.transform.rotation = spreadRotation;
                attackEffect.Play();
            }
        }
    }
}
