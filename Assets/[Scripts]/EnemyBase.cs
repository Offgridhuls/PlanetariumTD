using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FSMC.Runtime;
using UnityEngine.Events;

public abstract class EnemyBase : FSMC_Executer, IDamageable
{
    [Header("Components")]
    protected PlanetBase currentPlanet;
    [SerializeField] protected EnemySpawnData enemyStats;

    [Header("Enemy Settings")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float currentHealth;
    [SerializeField] protected float integrity = 1f;
    [SerializeField] protected bool isDead = false;

    [Header("Effects")]
    [SerializeField] protected GameObject deathEffect;
    [SerializeField] protected GameObject damageEffect;
    [SerializeField] protected GameObject healthBarPrefab;
    protected GameObject healthBarInstance;

    [Header("Rewards")]
    [SerializeField] protected int scoreValue = 10;
    [SerializeField] protected int resourceValue = 5;

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

    protected override void Start()
    {
        currentHealth = maxHealth;
        currentPlanet = Object.FindFirstObjectByType<PlanetBase>();
        if (currentPlanet == null)
        {
            Debug.LogError("No planet found in scene!");
            return;
        }

        if (healthBarPrefab != null)
        {
            healthBarInstance = Instantiate(healthBarPrefab, transform);
        }

        base.Start();
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
        resourceValue = resources;
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        onDeath?.Invoke();
        onScoreGained?.Invoke(scoreValue);
        onResourceGained?.Invoke(resourceValue);
        
        //GameManager.Instance?.AddScore(scoreValue);
        //ResourceManager.Instance?.AddResources(resourceValue);

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

    public PlanetBase GetCurrentPlanet()
    {
        return currentPlanet;
    }

    public EnemySpawnData GetStats()
    {
        return enemyStats;
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        // Check for collision with planet
        PlanetBase planet = other.GetComponent<PlanetBase>();
        if (planet != null)
        {
            var planetDamageable = planet.GetComponent<IDamageable>();
            if (planetDamageable != null)
            {
                planetDamageable.TakeDamage(enemyStats.damage);
            }
            else
            {
                Debug.LogWarning($"Planet {planet.name} is not damageable.");
            }
            Die();
        }
    }
}
