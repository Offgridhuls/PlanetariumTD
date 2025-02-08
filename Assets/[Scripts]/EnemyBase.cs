using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using Planetarium;
using Unity.Collections;
using Planetarium.AI;

public class EnemyBase : MonoBehaviour, IDamageable
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

    [Header("State Machine")]
    [SerializeField, ReadOnly] protected string currentStateName;

    [Header("Attack Settings")]
    [SerializeField] protected ProjectileBase projectilePrefab;
    [SerializeField] protected Transform projectileSpawnPoint;

    public Rigidbody rb { get; private set; }
    protected Dictionary<Type, EnemyStateBase> states = new Dictionary<Type, EnemyStateBase>();
    protected EnemyStateBase currentState;
    protected GeneratorBase currentTarget;
    
    public UnityEvent<float> onHealthChanged = new UnityEvent<float>();
    public UnityEvent onDeath = new UnityEvent();
    public UnityEvent<int> onScoreGained;
    public UnityEvent<int> onResourceGained;

    public GeneratorBase CurrentTarget => currentTarget;
    public string CurrentStateName => currentStateName;
    public bool IsAlive => currentHealth > 0;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public PlanetBase CurrentPlanet => currentPlanet;

    #region Unity Lifecycle

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        InitializeStates();
    }

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        currentPlanet = FindFirstObjectByType<PlanetBase>();
        
        if (currentPlanet == null)
        {
            Debug.LogError("No planet found in scene!");
            return;
        }
        
        if (healthBarPrefab != null)
        {
            healthBarInstance = Instantiate(healthBarPrefab, transform);
        }

        // Start with initial state if one is registered
        if (states.Count > 0 && currentState == null)
        {
            TransitionToInitialState();
        }
    }

    protected virtual void Update()
    {
        if (!IsAlive) return;
        currentState?.Update();
    }

    protected virtual void FixedUpdate()
    {
        if (!IsAlive) return;
        currentState?.FixedUpdate();
    }

    #endregion

    #region State Machine

    protected virtual void InitializeStates()
    {
        // Override this in derived classes to register states
        // Example:
        // RegisterState<MoveState>();
        // RegisterState<AttackState>();
    }

    protected void RegisterState<T>() where T : EnemyStateBase, new()
    {
        var state = new T();
        state.Initialize(this);
        states[typeof(T)] = state;
    }

    protected virtual void TransitionToInitialState()
    {
        // Override this in derived classes to set the initial state
    }

    public void TransitionToState<T>() where T : EnemyStateBase
    {
        var type = typeof(T);
        if (!states.ContainsKey(type))
        {
            Debug.LogError($"State {type.Name} not registered!");
            return;
        }

        currentState?.Exit();
        currentState = states[type];
        currentStateName = type.Name;
        currentState.Enter();
    }

    #endregion

    #region Target Management

    public void SetCurrentTarget(GeneratorBase target)
    {
        currentTarget = target;
    }

    public GeneratorBase FindNearestGenerator()
    {
        GeneratorBase[] generators = FindObjectsOfType<GeneratorBase>();
        GeneratorBase nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var generator in generators)
        {
            if (generator.IsDestroyed) continue;

            float distance = Vector3.Distance(transform.position, generator.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = generator;
            }
        }

        currentTarget = nearest;
        return nearest;
    }

    #endregion

    #region Combat

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

    public virtual ProjectileBase ShootProjectile(Vector3 targetPosition)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("No projectile prefab assigned to enemy!");
            return null;
        }

        Vector3 spawnPosition = projectileSpawnPoint != null 
            ? projectileSpawnPoint.position 
            : transform.position;

        Vector3 direction = (targetPosition - spawnPosition).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction, transform.up);
        
        ProjectileBase projectile = Instantiate(projectilePrefab, spawnPosition, rotation);
        return projectile;
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        if (possibleResources != null && possibleResources.Length > 0)
        {
            int resourceCount = UnityEngine.Random.Range((int)resourceDropRange.x, (int)resourceDropRange.y + 1);
            ResourceManager resourceManager = FindFirstObjectByType<ResourceManager>();

            if (resourceManager != null)
            {
                for (int i = 0; i < resourceCount; i++)
                {
                    ResourceType selectedResource = possibleResources[UnityEngine.Random.Range(0, possibleResources.Length)];
                    Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * 1f;
                    resourceManager.SpawnResource(selectedResource, transform.position + randomOffset);
                }
            }
        }

        onDeath.Invoke();
        onScoreGained?.Invoke(scoreValue);
        Destroy(gameObject);
    }

    #endregion

    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    public EnemySpawnData GetStats()
    {
        return enemyStats;
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

    protected void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        /*var damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            DamageableType targetType = damageable.GetDamageableType();
            if (targetType != DamageableType.Enemy) // Don't damage other enemies
            {
                damageable.TakeDamage(enemyStats.damage);
                Die();
            }
        }*/
    }
}
