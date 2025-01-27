using UnityEngine;
using System;

namespace Planetarium
{
    public class GeneratorBase : MonoBehaviour
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
        public bool IsDestroyed { get; private set; }
        public event Action<float> OnHealthChanged;
        public event Action OnDestroyed;

        private float lastRepairTime;

        protected virtual void Awake()
        {
            CurrentHealth = maxHealth;
        }

        protected virtual void Update()
        {
            if (!IsDestroyed && CurrentHealth < maxHealth)
            {
                if (Time.time - lastRepairTime >= repairInterval)
                {
                    Repair();
                    lastRepairTime = Time.time;
                }
            }
        }

        public virtual void TakeDamage(float damage)
        {
            if (IsDestroyed) return;

            CurrentHealth -= damage;
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

        protected virtual void Repair()
        {
            if (IsDestroyed) return;

            float previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Min(CurrentHealth + repairRate, maxHealth);

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

            // Optional: You might want to disable the generator's mesh or other components here
            // but keep the GameObject alive for the particle effects to finish
            Destroy(gameObject, 2f); // Destroy after effects finish
        }

        public float GetHealthPercentage()
        {
            return CurrentHealth / maxHealth;
        }
    }
}
