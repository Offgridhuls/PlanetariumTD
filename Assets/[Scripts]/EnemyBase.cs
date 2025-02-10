using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using Planetarium;
using Unity.Collections;
using Planetarium.AI;
using Planetarium.Stats;

public class EnemyBase : CoreBehaviour, IDamageable
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
    [SerializeField] protected Planetarium.Stats.ResourceType[] possibleResources;
    [SerializeField] protected Vector2 CoinResourceRange = new Vector2(5, 10);
    [SerializeField] protected Vector2 GemResourceRange = new Vector2(5, 10);

    [Header("State Machine")]
    [SerializeField] protected EnemyStateConfig stateConfig;
    [SerializeField, ReadOnly] protected string currentStateName;

    [Header("Attack Settings")]
    [SerializeField] protected ProjectileBase projectilePrefab;
    [SerializeField] protected Transform projectileSpawnPoint;

    [Header("Planet Interaction")]
    [SerializeField] protected LayerMask planetLayerMask;

    /// <summary>
    /// Raycast towards the planet and get the hit point and normal
    /// </summary>
    public bool GetPlanetSurfacePoint(out Vector3 hitPoint, out Vector3 hitNormal)
    {
        hitPoint = Vector3.zero;
        hitNormal = Vector3.up;

        if (currentPlanet == null) return false;

        Vector3 toPlanet = currentPlanet.transform.position - transform.position;
        RaycastHit hit;
        
        if (Physics.Raycast(transform.position, toPlanet.normalized, out hit, float.MaxValue, planetLayerMask))
        {
            hitPoint = hit.point;
            hitNormal = hit.normal;
            return true;
        }

        return false;
    }

    [Header("Debug")]
    [SerializeField] private bool enableStateLogging = true;

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

    private float spawnTime;
    private float pathProgress;

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
        
        spawnTime = Time.time;
        
        // Register enemy spawn in stats
        GameStatsHelper.OnEnemySpawned(enemyStats);

        if (healthBarPrefab != null)
        {
            healthBarInstance = Instantiate(healthBarPrefab, transform);
        }

        pathProgress = 0f;

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
        if (stateConfig == null)
        {
            Debug.LogError($"[{gameObject.name}] No state config assigned to enemy!", this);
            return;
        }

        foreach (var stateEntry in stateConfig.States)
        {
            Type stateType = stateEntry.GetStateType();
            if (stateType == null || !typeof(EnemyStateBase).IsAssignableFrom(stateType))
            {
                Debug.LogError($"[{gameObject.name}] Invalid state type in config: {stateEntry.StateName}", this);
                continue;
            }

            var state = (EnemyStateBase)Activator.CreateInstance(stateType);
            state.Initialize(this);
            states[stateType] = state;
            
            if (enableStateLogging)
            {
                Debug.Log($"[{gameObject.name}] Registered state: {stateEntry.StateName}", this);
            }
        }
    }

    protected virtual void TransitionToInitialState()
    {
        if (stateConfig == null) return;

        // Find the default state from config
        var defaultStateEntry = System.Array.Find(stateConfig.States, s => s.IsDefaultState);
        if (defaultStateEntry != null)
        {
            Type defaultStateType = defaultStateEntry.GetStateType();
            if (defaultStateType != null && states.ContainsKey(defaultStateType))
            {
                currentState = states[defaultStateType];
                currentStateName = defaultStateEntry.StateName;
                currentState.Enter();
            }
        }
        else if (states.Count > 0)
        {
            // Fallback to first state if no default specified
            var firstState = states.Values.GetEnumerator();
            firstState.MoveNext();
            currentState = firstState.Current;
            currentStateName = currentState.GetType().Name;
            currentState.Enter();
        }
    }

    public void TransitionToState<T>() where T : EnemyStateBase
    {
        var type = typeof(T);
        if (!states.ContainsKey(type))
        {
            Debug.LogError($"[{gameObject.name}] State {type.Name} not registered!", this);
            return;
        }

        string previousState = currentState?.GetType().Name ?? "null";
        currentState?.Exit();
        currentState = states[type];
        currentStateName = type.Name;
        LogStateTransition(previousState, currentStateName);
        currentState.Enter();
    }

    private void LogStateTransition(string from, string to)
    {
        if (!enableStateLogging) return;
        Debug.Log($"[{gameObject.name}] State Transition: {from ?? "null"} -> {to}", this);
    }

    #endregion

    #region Target Management

    public void SetCurrentTarget(GeneratorBase target)
    {
        currentTarget = target;
    }

    public GeneratorBase FindNearestGenerator()
    {
        GeneratorBase[] generators = FindObjectsByType<GeneratorBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    
        bool anyGeneratorAlive = false;
        GeneratorBase nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var generator in generators)
        {
            if (!generator.IsDestroyed)
            {
                anyGeneratorAlive = true;
                float distance = Vector3.Distance(transform.position, generator.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = generator;
                }
            }
        }

        if (!anyGeneratorAlive)
        {
            Die();
            return null;
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
        TakeDamage(damage, data.Source);
    }

    public virtual void TakeDamage(float damage, GameObject source = null)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        GameStatsHelper.OnEnemyDamageTaken(enemyStats.name, damage);
        
        onHealthChanged?.Invoke(GetHealthPercentage());

        if (damageEffect != null)
        {
            Instantiate(damageEffect, transform.position, Quaternion.identity);
        }

        if (currentHealth <= 0)
        {
            Die(source);
        }
    }

    public virtual void DealDamage(float damage, IDamageable target)
    {
        if (target != null)
        {
            GameStatsHelper.OnEnemyDamageDealt(enemyStats.name, damage);
            target.ProcessDamage(new DamageData(damage, gameObject));
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
    
    public virtual void Attack(Vector3 targetPosition)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("Attack : No projectile prefab assigned to enemy!");
            return;
        }
        
        Vector3 spawnPosition = projectileSpawnPoint != null 
            ? projectileSpawnPoint.position 
            : transform.position;

        Vector3 direction = (targetPosition - spawnPosition).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction, transform.up);
        
        ProjectileBase projectile = Instantiate(projectilePrefab, spawnPosition, rotation);
        projectile.Initialize(
            enemyStats.attackDamage,
            CurrentTarget.transform.position,
            enemyStats.ProjectileSpeed
        );
        projectile.ShootProjectile(CurrentTarget.transform.position, CurrentTarget.gameObject);
    }

    #endregion

    protected virtual void Die(GameObject source = null)
    {
        if (isDead) return;
        isDead = true;

        float lifetime = Time.time - spawnTime;
        GameStatsHelper.OnEnemyKilled(enemyStats.name, maxHealth, lifetime, pathProgress);
        
        // Handle resource drops
        DropResources();

        // Handle score
        if (onScoreGained != null)
            onScoreGained.Invoke(scoreValue);

        // Handle effects
        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        onDeath?.Invoke();
        Destroy(gameObject);
    }

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

    protected void DropResources()
    {
        if (possibleResources == null || possibleResources.Length == 0) return;

        foreach (var resourceType in possibleResources)
        {
            int amount = 0;
            switch (resourceType)
            {
                case Planetarium.Stats.ResourceType.Coins:
                    amount = Mathf.RoundToInt(UnityEngine.Random.Range(CoinResourceRange.x, CoinResourceRange.y));
                    break;
                case Planetarium.Stats.ResourceType.Gems:
                    amount = Mathf.RoundToInt(UnityEngine.Random.Range(GemResourceRange.x, GemResourceRange.y));
                    break;
            }

            if (amount > 0)
            {
                GameStatsHelper.OnEnemyDroppedResources(enemyStats.name, amount);
                onResourceGained?.Invoke(amount);
            }
        }
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
