using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

public class DeployableBase : MonoBehaviour, IDamageable
{
    [Header("Turret Components")]
    [SerializeField] protected ProjectileBase M_Projectile;
    [SerializeField] protected TurretStats M_TurretStats;
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

    private bool isDead = false;
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
    }

    public virtual DamageableType GetDamageableType()
    {
        return DamageableType.Turret;
    }

    public virtual void ProcessDamage(DamageData data)
    {
        if (isDead) return;
        float damage = data.Damage * (1f / integrity);
        TakeDamage(damage);
    }

    public virtual void TakeDamage(float damage)
    {
        if (isDead) return;
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        onHealthChanged?.Invoke(GetHealthPercentage());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        if (!isDead)
        {
            isDead = true;
            PlayDeathSound();
            
            // Notify any listeners about the death
            OnDeployableDeath?.Invoke(this);
            
            // Destroy the deployable
            Destroy(gameObject);
        }
    }

    public bool IsAlive => !isDead;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float GetHealthPercentage() => currentHealth / maxHealth;

    protected virtual void Update()
    {
        if (isDead) return;

        // Update closest target
        float closestDistance = float.MaxValue;
        Collider[] colliders = Physics.OverlapSphere(transform.position, M_TurretStats.GetAgroRadius());

        foreach (Collider col in colliders)
        {
            EnemyBase enemy = col.GetComponent<EnemyBase>();
            if (enemy != null && enemy.IsAlive)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    // Check line of sight if required
                    if (!requiresLineOfSight || HasLineOfSight(enemy.transform))
                    {
                        closestDistance = distance;
                        ClosestTarget = enemy;
                        targetPosition = enemy.transform.position;
                    }
                }
            }
        }

        if (ClosestTarget == null || !ClosestTarget.IsAlive)
        {
            ClosestTarget = null;
        }

        if (!isDead && ClosestTarget != null)
        {
            RotateTowardsTarget(ClosestTarget.transform.position);
            FireTimer += Time.deltaTime;
            if (FireTimer >= M_TurretStats.GetFireInterval())
            {
                FireTimer = 0f;
                FireTurret();
            }
        }
    }

    protected bool HasLineOfSight(Transform target)
    {
        if (!requiresLineOfSight) return true;

        Vector3 directionToTarget = (target.position - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // Raycast to check for obstacles
        RaycastHit hit;
        if (Physics.Raycast(transform.position, directionToTarget, out hit, distanceToTarget, lineOfSightMask))
        {
            // If we hit something that's not the target, we don't have line of sight
            if (hit.transform != target)
            {
                return false;
            }
        }

        return true;
    }

    protected virtual void FireTurret()
    {
        if (ClosestTarget == null || !HasLineOfSight(ClosestTarget.transform)) return;

        // Play fire sound
        PlayFireSound();
        
        // Calculate predicted position if target is moving
        Vector3 targetPos = PredictTargetPosition();
        
        // Spawn and initialize projectile
        ProjectileBase projectile = Instantiate(M_Projectile, transform.position, Quaternion.identity);
        projectile.Initialize(M_TurretStats.GetDamage(), targetPos, M_TurretStats.GetProjectileSpeed());
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

    protected Vector3 PredictTargetPosition()
    {
        if (ClosestTarget == null) return Vector3.zero;

        // Get target's rigidbody and current position
        Rigidbody targetRb = ClosestTarget.GetComponent<Rigidbody>();
        Vector3 targetPos = ClosestTarget.transform.position;
        
        if (targetRb == null) return targetPos;

        // Get projectile spawn position (either firePoint or turret position)
        Vector3 firePosition = transform.position;
        if (GetComponentInChildren<Transform>().Find("FirePoint") != null)
        {
            firePosition = GetComponentInChildren<Transform>().Find("FirePoint").position;
        }

        // Calculate initial distance and time
        float distanceToTarget = Vector3.Distance(firePosition, targetPos);
        float projectileSpeed = M_TurretStats.GetProjectileSpeed();
        
        if (projectileSpeed <= 0) return targetPos;

        // Calculate time for projectile to reach target's current position
        float timeToTarget = distanceToTarget / projectileSpeed;

        // Get target velocity and adjust for gravity if applicable
        Vector3 targetVelocity = targetRb.linearVelocity;
        bool isTargetAffectedByGravity = targetRb.useGravity;
        
        // Predict position using iterative approach for better accuracy
        const int maxIterations = 3;
        for (int i = 0; i < maxIterations; i++)
        {
            // Calculate predicted position based on velocity and time
            Vector3 predictedPos = targetPos + (targetVelocity * timeToTarget);
            
            // Add gravity influence if applicable
            if (isTargetAffectedByGravity)
            {
                predictedPos += 0.5f * Physics.gravity * timeToTarget * timeToTarget;
            }
            
            // Recalculate distance and time based on predicted position
            float newDistance = Vector3.Distance(firePosition, predictedPos);
            float newTimeToTarget = newDistance / projectileSpeed;
            
            // Update time if it changed significantly
            if (Mathf.Abs(newTimeToTarget - timeToTarget) < 0.1f)
                break;
                
            timeToTarget = newTimeToTarget;
        }

        // Calculate final predicted position
        Vector3 finalPredictedPos = targetPos + (targetVelocity * timeToTarget);
        if (isTargetAffectedByGravity)
        {
            finalPredictedPos += 0.5f * Physics.gravity * timeToTarget * timeToTarget;
        }

        return finalPredictedPos;
    }

    protected virtual void RotateTowardsTarget(Vector3 target)
    {
        Vector3 targetDirection = target - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, M_TurretStats.GetRotationSpeed() * Time.deltaTime);
    }
}