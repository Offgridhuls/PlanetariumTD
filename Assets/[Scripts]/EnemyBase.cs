using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using Planetarium;
using Unity.Collections;
using Planetarium.AI;
using Planetarium.Stats;

/// <summary>
/// Base class for all enemies in the game
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(TaggedComponent))]
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
    //[SerializeField] protected ResourceKind[] possibleResources;
    [SerializeField] protected Vector2 CoinResourceRange = new Vector2(5, 10);
    [SerializeField] protected Vector2 GemResourceRange = new Vector2(5, 10);
    [SerializeField] protected float coinDropRate = 0.5f;
    [SerializeField] protected float gemDropRate = 0.25f;
    [SerializeField] protected float resourceSpawnRadius = 0.5f;

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

    // Cached tags for better performance
    protected static class CachedTags
    {
        public static readonly GameplayTag EnemyBase = new GameplayTag("Enemy.Base");
        public static readonly GameplayTag EnemyFlying = new GameplayTag("Enemy.Flying");
        public static readonly GameplayTag EnemyGround = new GameplayTag("Enemy.Ground");
    }

    public TaggedComponent taggedComponent;

    #region Unity Lifecycle

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        InitializeStates();
        
        // Ensure we have a TaggedComponent
        taggedComponent = GetComponent<TaggedComponent>();
        if (taggedComponent == null)
        {
            taggedComponent = gameObject.AddComponent<TaggedComponent>();
        }
        
        taggedComponent.AddTag(CachedTags.EnemyBase);
        
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
        //TaggedStatsHelper.OnEnemySpawned(enemyStats.name);

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
        TaggedStatsHelper.OnEnemyDamageTaken(damage);
        
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
            // Track damage dealt
            //TaggedStatsHelper.OnEnemyDamageDealt(damage);

            // Track damage based on target type
            var damageableType = target.GetDamageableType();
            switch (damageableType)
            {
                case DamageableType.Structure:
                    TaggedStatsHelper.OnEnemyDamageTaken(damage);
                    break;
            }

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
        projectile.SetSource(ProjectileSource.Enemy, enemyStats.name);
        projectile.ShootProjectile(CurrentTarget.transform.position, CurrentTarget.gameObject);
    }

    #endregion

    protected virtual void DropResources()
    {
        ResourceManager resourceManager = FindFirstObjectByType<ResourceManager>();
        /*if (resourceManager == null || possibleResources == null) return;

        // Coins drop
        if (possibleResources.Length > 0 && UnityEngine.Random.value <= coinDropRate)
        {
            var resourceType = resourceManager.availableResources.FirstOrDefault(r => r.resourceName == "Coins");
            if (resourceType != null)
            {
                int amount = UnityEngine.Random.Range((int)CoinResourceRange.x, (int)CoinResourceRange.y + 1);
                Vector3 spawnPos = transform.position + UnityEngine.Random.insideUnitSphere * resourceSpawnRadius;
                resourceManager.SpawnResource(resourceType, spawnPos, amount);
                TaggedStatsHelper.OnResourceEarned("Coins", amount);
                onResourceGained?.Invoke(amount);
            }
        }

        // Gems drop
        if (possibleResources.Length > 1 && UnityEngine.Random.value <= gemDropRate)
        {
            var resourceType = resourceManager.availableResources.FirstOrDefault(r => r.resourceName == "Gems");
            if (resourceType != null)
            {
                int amount = UnityEngine.Random.Range((int)GemResourceRange.x, (int)GemResourceRange.y + 1);
                Vector3 spawnPos = transform.position + UnityEngine.Random.insideUnitSphere * resourceSpawnRadius;
                resourceManager.SpawnResource(resourceType, spawnPos, amount);
                TaggedStatsHelper.OnResourceEarned("Gems", amount);
                onResourceGained?.Invoke(amount);
            }
        }*/
    }

    protected virtual void Die(GameObject source = null)
    {
        if (isDead) return;
        isDead = true;

        float lifetime = Time.time - spawnTime;
       // TaggedStatsHelper.OnEnemyKilled(enemyStats.name, maxHealth, lifetime, pathProgress);
        
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
         //possibleResources = new ResourceKind[] { ResourceKind.Coins, ResourceKind.Gems };
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
