using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FSMC.Runtime;
using Planetarium;
using UnityEngine.Events;

public class EnemyBase : FSMC_Executer, IDamageable
{
    [Header("Components")]
    protected PlanetBase currentPlanet;
    [SerializeField] public EnemySpawnData enemyStats;

    [Header("Enemy Settings")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float currentHealth;
    [SerializeField] protected float integrity = 0.5f;
    [SerializeField] protected bool isDead = false;

    [Header("Effects")]
    [SerializeField] protected GameObject deathEffect;
    [SerializeField] protected GameObject damageEffect;
    [SerializeField] protected GameObject healthBarPrefab;
    protected GameObject healthBarInstance;

    [Header("Rewards")]
    [SerializeField] protected int scoreValue = 10;
    [SerializeField] protected ResourceType[] possibleResources;
    [SerializeField] protected Vector2 resourceDropRange = new Vector2(1, 3);

    
    public Rigidbody rb;
    
    public UnityEvent<float> onHealthChanged = new UnityEvent<float>();
    public UnityEvent onDeath = new UnityEvent();
    public UnityEvent<int> onScoreGained;
    public UnityEvent<int> onResourceGained;

    public virtual DamageableType GetDamageableType()
    {
        return DamageableType.Enemy;
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
        damage *= (1f - integrity);
        currentHealth = Mathf.Max(0f, currentHealth - damage);

        if (damageEffect != null)
        {
            Instantiate(damageEffect, transform.position, Quaternion.identity);
        }

        onHealthChanged?.Invoke(GetHealthPercentage());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public bool IsAlive => currentHealth > 0;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public PlanetBase CurrentPlanet => currentPlanet;

    protected override void Start()
    {
        currentHealth = maxHealth;
        currentPlanet = Object.FindFirstObjectByType<PlanetBase>();
        if (currentPlanet == null)
        {
            Debug.LogError("No planet found in scene!");
            return;
        }
        
        rb = GetComponent<Rigidbody>();

        if (healthBarPrefab != null)
        {
            healthBarInstance = Instantiate(healthBarPrefab, transform);
        }

        base.Start();
    }

    protected override void Awake()
    {
        base.Awake();
    }
    public void ProcessSpawnData(EnemySpawnData spawnData, float speedMult, bool elite)
    {
        enemyStats = spawnData;
        maxHealth = spawnData.maxHealth * (elite ? 2f : 1f);
        currentHealth = maxHealth;
        integrity = spawnData.integrity;
        onHealthChanged?.Invoke(GetHealthPercentage());
    }

    public void SetIntegrity(float value)
    {
        integrity = value;
        Debug.Log("Integrity set to: " + integrity);
    }

    public void SetEffects(GameObject death, GameObject damage, GameObject healthBar)
    {
        deathEffect = death;
        damageEffect = damage;
        healthBarPrefab = healthBar;

        if (healthBarPrefab != null && healthBarInstance == null)
        {
            healthBarInstance = Instantiate(healthBarPrefab, transform);
        }
    }

    public void SetRewards(int score, int resources)
    {
        scoreValue = score;
        //resourceValue = resources;
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        // Spawn death effect if assigned
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        // Drop resources
        if (possibleResources != null && possibleResources.Length > 0)
        {
            int resourceCount = Random.Range((int)resourceDropRange.x, (int)resourceDropRange.y + 1);
            ResourceManager resourceManager = FindFirstObjectByType<ResourceManager>();

            if (resourceManager != null)
            {
                for (int i = 0; i < resourceCount; i++)
                {
                    ResourceType selectedResource = possibleResources[Random.Range(0, possibleResources.Length)];
                    Vector3 randomOffset = Random.insideUnitSphere * 1f;
                    resourceManager.SpawnResource(selectedResource, transform.position + randomOffset);
                }
            }
        }

        onDeath.Invoke();
        onScoreGained?.Invoke(scoreValue);

        // Destroy the enemy object
        Destroy(gameObject);
    }

    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    protected override void Update()
    {
        if (!IsAlive) return;
        base.Update();
    }

    public EnemySpawnData GetStats()
    {
        return enemyStats;
    }
    protected void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        var damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            DamageableType targetType = damageable.GetDamageableType();
            if (targetType != DamageableType.Enemy) // Don't damage other enemies
            {
                damageable.TakeDamage(enemyStats.damage);
                Die();
            }
        }
    }
}
