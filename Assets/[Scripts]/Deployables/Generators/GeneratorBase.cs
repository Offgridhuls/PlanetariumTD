using UnityEngine;
using System;
using UnityEngine.Events;
using Planetarium;

namespace Planetarium
{
    public class GeneratorBase : CoreBehaviour, IDamageable
    {
        [Header("Generator Settings")]
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected float repairRate = 5f;
        [SerializeField] protected float repairInterval = 1f;
        
        [Header("Visual Feedback")]
        [SerializeField] protected ParticleSystem damageEffect;
        [SerializeField] protected ParticleSystem repairEffect;
        [SerializeField] protected ParticleSystem destructionEffect;
        [SerializeField] protected AudioSource generatorAudioSource;
        [SerializeField] protected AudioClip damageSound;
        [SerializeField] protected AudioClip repairSound;
        [SerializeField] protected AudioClip destructionSound;

        public float CurrentHealth { get; private set; }
        public float MaxHealth => maxHealth;
        public bool IsDestroyed { get; private set; }
        public bool IsAlive => !IsDestroyed;
        
        public event Action<float> OnHealthChanged;
        public event Action OnDestroyed;

        private float lastRepairTime;
        private GameStateManager gameStateManager;

        protected virtual void Awake()
        {
            CurrentHealth = maxHealth;
            IsDestroyed = false;
            gameStateManager = FindFirstObjectByType<GameStateManager>();
        }

        protected virtual void Start()
        {
            // Register this generator with GameStateManager
            if (gameStateManager != null)
            {
                gameStateManager.RegisterGenerator(this);
            }
        }

        protected virtual void OnDestroy()
        {
            // Unregister from GameStateManager when destroyed
            if (gameStateManager != null)
            {
                gameStateManager.UnregisterGenerator(this);
            }
        }

        public DamageableType GetDamageableType()
        {
            return DamageableType.Structure;
        }

        public void ProcessDamage(DamageData data)
        {
            if (IsDestroyed) return;
            TakeDamage(data.Damage);
        }

        public void TakeDamage(float damage, GameObject source = null)
        {
            if (IsDestroyed) return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
            OnHealthChanged?.Invoke(CurrentHealth / maxHealth);

            if (damageEffect != null)
            {
                damageEffect.Play();
            }

            if (generatorAudioSource != null && damageSound != null)
            {
                generatorAudioSource.PlayOneShot(damageSound);
            }

            if (CurrentHealth <= 0)
            {
                Die();
            }
        }

        public void Repair(float amount)
        {
            if (IsDestroyed) return;

            float previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);

            if (CurrentHealth > previousHealth)
            {
                OnHealthChanged?.Invoke(CurrentHealth / maxHealth);

                if (repairEffect != null)
                {
                    repairEffect.Play();
                }

                if (generatorAudioSource != null && repairSound != null)
                {
                    generatorAudioSource.PlayOneShot(repairSound);
                }
            }
        }

        protected virtual void Die()
        {
            if (IsDestroyed) return;

            IsDestroyed = true;
            OnDestroyed?.Invoke();

            if (destructionEffect != null)
            {
                destructionEffect.Play();
            }

            if (generatorAudioSource != null && destructionSound != null)
            {
                generatorAudioSource.PlayOneShot(destructionSound);
            }

            // Optionally destroy the GameObject
            // Destroy(gameObject);
        }

        protected virtual void Update()
        {
            if (IsDestroyed) return;

            // Auto-repair logic
            if (Time.time - lastRepairTime >= repairInterval)
            {
                lastRepairTime = Time.time;
                Repair(repairRate * repairInterval);
            }
        }

        public float GetHealthPercentage()
        {
            return CurrentHealth / maxHealth;
        }

        /// <summary>
        /// Resets all generator flags and states to their initial values.
        /// Called when restarting the game or reinitializing the generator.
        /// </summary>
        public virtual void ResetFlags()
        {
            try
            {
                // Reset health
                CurrentHealth = maxHealth;

                // Reset operational state
                IsDestroyed = false;

                // Reset visual states
                if (damageEffect != null)
                {
                    damageEffect.Stop();
                }
                if (repairEffect != null)
                {
                    repairEffect.Stop();
                }
                if (destructionEffect != null)
                {
                    destructionEffect.Stop();
                }

                // Reset any active effects or modifiers
                ClearAllModifiers();

               
                /*if (showDebug)
                {
                    Debug.Log($"Generator {gameObject.name}: Flags reset");
                }*/
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error resetting generator flags: {e.Message}");
            }
        }

        /// <summary>
        /// Clears all active modifiers and effects on the generator.
        /// </summary>
        protected virtual void ClearAllModifiers()
        {
            // Override in derived classes to clear specific modifiers
            // Example: damage multipliers, speed buffs, etc.
        }
    }
}
