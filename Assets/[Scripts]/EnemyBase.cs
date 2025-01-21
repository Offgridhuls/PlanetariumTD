using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FSMC.Runtime;
using UnityEngine.Events;

public class EnemyBase : FSMC_Executer, IDamageable
{
    [Header("Components")]
    protected PlanetBase currentPlanet;
    [SerializeField] protected EnemyStats enemyStats;

    [Header("Health Settings")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float currentHealth;
    [SerializeField] protected float integrity = 1f;

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
        if (!IsAlive) return;

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

    public EnemyStats GetStats()
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
                planetDamageable.TakeDamage(enemyStats.GetDamage());
            }
            else
            {
                Debug.LogWarning($"Planet {planet.name} is not damageable.");
            }
            Die();
        }
    }
}
