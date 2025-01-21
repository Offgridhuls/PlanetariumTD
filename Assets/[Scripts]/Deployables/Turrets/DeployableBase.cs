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

    [Header("Health Settings")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float currentHealth;
    [SerializeField] protected float integrity = 1f;

    [Header("Effects")]
    [SerializeField] protected GameObject deathEffect;
    [SerializeField] protected GameObject damageEffect;
    [SerializeField] protected GameObject healthBarPrefab;
    protected GameObject healthBarInstance;

    private bool IsDestroyed = false;
    protected EnemyBase ClosestTarget;
    protected Vector3 targetPosition;
    private Vector3 previousPosition;
    private Vector3 enemyVelocity;
    private float velocityUpdateTimer = 0f;
    private const float VELOCITY_UPDATE_INTERVAL = 0.1f;
    protected float FireTimer = 0;

    public UnityEvent<float> onHealthChanged = new UnityEvent<float>();
    public UnityEvent onDeath = new UnityEvent();

    #region IDamageable Implementation
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;

    public virtual void TakeDamage(float damage)
    {
        if (!IsAlive) return;

        damage *= (1f - integrity);
        currentHealth = Mathf.Max(0f, currentHealth - damage);

        if (damageEffect != null)
        {
            Instantiate(damageEffect, transform.position, Quaternion.identity);
        }

        onHealthChanged?.Invoke(GetHealthPercentage());

        if (!IsAlive)
        {
            Die();
        }
    }
    #endregion

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        if (healthBarPrefab != null)
        {
            healthBarInstance = Instantiate(healthBarPrefab, transform);
        }
    }

    public void SetMaxHealth(float health)
    {
        maxHealth = health;
        currentHealth = health;
        onHealthChanged?.Invoke(GetHealthPercentage());
    }

    public void SetIntegrity(float value)
    {
        integrity = value;
    }

    protected virtual void Die()
    {
        if (!IsAlive) return;
        IsDestroyed = true;

        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        onDeath?.Invoke();
        Destroy(gameObject);
    }

    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    protected virtual void Update()
    {
        if (ClosestTarget != null)
        {
            targetPosition = PredictFuturePosition(ClosestTarget, M_Projectile.GetProjectileSpeed());
            previousPosition = ClosestTarget.gameObject.transform.position;

            velocityUpdateTimer += Time.deltaTime;
            if (velocityUpdateTimer >= VELOCITY_UPDATE_INTERVAL)
            {
                enemyVelocity = (ClosestTarget.transform.position - previousPosition) / VELOCITY_UPDATE_INTERVAL;
                velocityUpdateTimer = 0f;
            }
        }
        else
        {
            ClosestTarget = null;
        }

        if (!IsDestroyed && IsAlive)
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

            FindClosestTarget();
            if (ClosestTarget != null)
            {
                RotateTowardsTarget(targetPosition);
            }
        }
    }

    private void FindClosestTarget()
    {
        Collider[] hitTargets = Physics.OverlapSphere(transform.position, M_TurretStats.GetAgroRadius());
        EnemyBase closestEnemy = null;

        float closestDistance = M_TurretStats.GetAgroRadius();

        foreach (Collider hitCollider in hitTargets)
        {
            EnemyBase enemy = hitCollider.GetComponent<EnemyBase>();
            if (enemy != null && enemy.IsAlive)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy;
                }
            }
        }

        ClosestTarget = closestEnemy;
    }

    protected virtual void RotateTowardsTarget(Vector3 target)
    {
        Vector3 targetDirection = target - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, M_TurretStats.GetRotationSpeed() * Time.deltaTime);
    }

    protected virtual void FireTurret()
    {
        if (M_Projectile != null)
        {
            ProjectileBase projectile = Instantiate(M_Projectile, transform.position, transform.rotation);
            projectile.Initialize(M_TurretStats.GetDamage(), targetPosition);
        }
    }

    protected virtual Vector3 PredictFuturePosition(EnemyBase target, float projectileSpeed)
    {
        Vector3 targetPos = target.transform.position;
        Vector3 targetVelocity = enemyVelocity;
        Vector3 relativePosition = targetPos - transform.position;
        
        float timeToTarget = relativePosition.magnitude / projectileSpeed;
        Vector3 predictedPosition = targetPos + targetVelocity * timeToTarget;
        
        return predictedPosition;
    }
}