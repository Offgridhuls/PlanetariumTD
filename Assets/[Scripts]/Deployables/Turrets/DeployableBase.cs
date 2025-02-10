using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using Planetarium.Stats;
using UnityEditor.iOS;

public class DeployableBase : MonoBehaviour, IDamageable
{
    [Header("Turret Components")]
    [SerializeField] protected ProjectileBase M_Projectile;
    [SerializeField] public TurretStats M_TurretStats;
    [SerializeField] protected Transform TurretMuzzle;
    [SerializeField] protected Transform TurretPivot;
    [SerializeField] protected bool requiresLineOfSight = true;
    [SerializeField] protected LayerMask lineOfSightMask;

    [Header("Health Settings")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float currentHealth;
    [SerializeField] protected float integrity = 1f;

    [Header("Effects")]
    [SerializeField] protected GameObject deathEffect;
    [SerializeField] protected GameObject damageEffect;
    [SerializeField] protected GameObject healthBarPrefab;
    protected GameObject healthBarInstance;

    [Header("Sound Effects")]
    [SerializeField] protected AudioClip fireSound;
    [SerializeField] protected AudioClip deathSound;
    [SerializeField] protected float soundVolume = 0.5f;

    [Header("Behaviour")] [SerializeField] protected bool doesRotate = true;
    protected bool isDead = false;
    protected EnemyBase ClosestTarget;
    protected Vector3 targetPosition;
    private Vector3 previousPosition;
    private Vector3 enemyVelocity;
    private float velocityUpdateTimer = 0f;
    private const float VELOCITY_UPDATE_INTERVAL = 0.1f;
    protected float FireTimer = 0;

    
    public UnityEvent onDeath = new UnityEvent();
    public UnityEvent<float> onHealthChanged = new UnityEvent<float>();
    public UnityEvent<DeployableBase> OnDeployableDeath = new UnityEvent<DeployableBase>();

    protected AudioSource audioSource;

    protected virtual void Awake()
    {
        // Set up audio source if we have sound effects
        if (fireSound != null || deathSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.maxDistance = 30f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.volume = soundVolume;
        }
    }

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        if (healthBarPrefab != null)
        {
            healthBarInstance = Instantiate(healthBarPrefab, transform);
        }

        // Register turret construction in stats
        GameStatsHelper.OnTurretBuilt(M_TurretStats.GetName(), M_TurretStats.GetCoinCost());
    }

    public virtual DamageableType GetDamageableType()
    {
        return DamageableType.Turret;
    }

    public virtual void ProcessDamage(DamageData data)
    {
        if (isDead) return;
        float damage = data.Damage * (1f / integrity);
        TakeDamage(damage, data.Source);
    }

    public virtual void TakeDamage(float damage, GameObject source = null)
    {
        if (isDead) return;
        
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        onHealthChanged?.Invoke(GetHealthPercentage());

        if (damageEffect != null)
        {
            Instantiate(damageEffect, transform.position, Quaternion.identity);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        // Play death sound if we have one
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        onDeath?.Invoke();
        OnDeployableDeath?.Invoke(this);

        // Track turret destruction in stats
        GameStatsHelper.OnTurretDestroyed(M_TurretStats.GetName());

        Destroy(gameObject);
    }

    protected virtual void OnDestroy()
    {
        if (isDead)
        {
            GameStatsHelper.OnTurretDestroyed(M_TurretStats.GetName());
        }
    }

    public bool IsAlive => !isDead;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float GetHealthPercentage() => currentHealth / maxHealth;

    protected virtual void Update()
    {
        if (isDead) return;

        if (ClosestTarget != null)
        {
            targetPosition = PredictTargetPosition(ClosestTarget, M_TurretStats.GetProjectileSpeed());
            previousPosition = ClosestTarget.gameObject.transform.position;
        }

        
           
        CheckClosestTarget();
        if (ClosestTarget != null && doesRotate)
        { 
            RotateTowardsTarget(targetPosition);
        }
        
       
        
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
                //TODO: Implement LOS Check
                var Enemy = hitCollider.GetComponent<EnemyBase>();
                float distance = Vector3.Distance(transform.position, Enemy.transform.position);
                if (distance < closestDistance )
                {
                    if (!requiresLineOfSight || HasLineOfSight(Enemy.transform))
                    {
                        closestDistance = distance;
                        closestEnemy = Enemy; 
                    }
                    
                }
                
                
            }
        }

        
      
       
        ClosestTarget = closestEnemy;
    }

    protected bool HasLineOfSight(Transform target)
    {
        if (!requiresLineOfSight) return true;

        // Use turret pivot or muzzle as starting point, falling back to transform position if neither exists
        Vector3 startPosition = TurretMuzzle.position;
        
        Vector3 directionToTarget = (target.position - startPosition).normalized;
        float distanceToTarget = Vector3.Distance(startPosition, target.position);

        // Raycast to check for obstacles
        RaycastHit hit;
        bool didHit = Physics.Raycast(startPosition, directionToTarget, out hit, distanceToTarget, lineOfSightMask);
        
        // Draw debug ray in scene view
        #if UNITY_EDITOR
        Debug.DrawLine(startPosition, startPosition + directionToTarget * distanceToTarget, 
            didHit && hit.transform != target ? Color.red : Color.green, 0.1f);
        #endif

        // If we hit something and it's not the target, we don't have line of sight
        if (didHit && hit.transform != target)
        {
            return false;
        }

        return true;
    }

    protected virtual void FireTurret()
    {
        if (ClosestTarget == null || !HasLineOfSight(ClosestTarget.transform)) return;

        // Play fire sound
        PlayFireSound();
        
        // Calculate predicted position if target is moving
        Vector3 targetPos = PredictTargetPosition(ClosestTarget, M_TurretStats.GetProjectileSpeed());
        
        // Spawn and initialize projectile
        ProjectileBase projectile = Instantiate(M_Projectile, TurretMuzzle.position, Quaternion.identity);
        projectile.Initialize(M_TurretStats.GetDamage(), targetPos, M_TurretStats.GetProjectileSpeed());
        projectile.SetSource(ProjectileSource.Turret, M_TurretStats.GetName());
        projectile.ShootProjectile(targetPos, ClosestTarget.gameObject);

        // Track shot fired in stats
        GameStatsHelper.OnTurretShotFired(M_TurretStats.GetName());
    }

    protected virtual void FireProjectile(Vector3 targetPos, EnemyBase target)
    {
        if (M_Projectile != null && TurretMuzzle != null)
        {
            var projectile = Instantiate(M_Projectile, TurretMuzzle.position, TurretMuzzle.rotation);
            projectile.Initialize(M_TurretStats.GetDamage(), ClosestTarget.transform.position, M_TurretStats.GetProjectileSpeed());
            projectile.SetSource(ProjectileSource.Turret, M_TurretStats.GetName());
            projectile.ShootProjectile(targetPos, target.gameObject);
            
            // Play fire sound if we have one
            if (audioSource != null && fireSound != null)
            {
                audioSource.PlayOneShot(fireSound);
            }

            // Track shot fired in stats
            GameStatsHelper.OnTurretShotFired(M_TurretStats.GetName());
        }
    }

    protected virtual void PlayFireSound()
    {
        if (audioSource != null && fireSound != null)
        {
            audioSource.PlayOneShot(fireSound, soundVolume);
        }
    }

    protected virtual void PlayDeathSound()
    {
        if (audioSource != null && deathSound != null)
        {
            // Create a temporary audio source for the death sound
            GameObject audioObj = new GameObject("DeathSound");
            audioObj.transform.position = transform.position;
            AudioSource tempAudio = audioObj.AddComponent<AudioSource>();
            tempAudio.clip = deathSound;
            tempAudio.spatialBlend = 1f;
            tempAudio.maxDistance = 30f;
            tempAudio.rolloffMode = AudioRolloffMode.Linear;
            tempAudio.volume = soundVolume;
            tempAudio.Play();

            // Destroy the audio object after the sound finishes
            Destroy(audioObj, deathSound.length + 0.1f);
        }
    }

    public Vector3 PredictTargetPosition(EnemyBase target, float bulletSpeed)
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

    protected virtual void RotateTowardsTarget(Vector3 target)
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

    protected virtual void OnProjectileHit(EnemyBase target, float damageDealt)
    {
        if (target != null)
        {
            if (target.CurrentHealth <= 0)
            {
                GameStatsHelper.OnTurretKill(M_TurretStats.GetName());
            }
        }
    }
}