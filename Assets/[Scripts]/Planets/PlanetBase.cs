using UnityEngine;
using System;

namespace Planetarium
{
    public class PlanetBase : SceneService
    {
        public event Action<float> OnHealthChanged;
        public event Action<float> OnShieldChanged;
        public event Action OnDestroyed;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public float CurrentShield { get; private set; }
        public bool IsDestroyed { get; private set; }

        [Header("Planet Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxShield = 50f;
        [SerializeField] private float shieldRegenRate = 5f;
        [SerializeField] private float shieldRegenDelay = 3f;

        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem damageEffect;
        [SerializeField] private ParticleSystem shieldHitEffect;
        [SerializeField] private ParticleSystem destructionEffect;

        private float lastDamageTime;
        private GameStateManager gameState;

        // Cache the collider for performance
        private SphereCollider planetCollider;
        private Vector3 lastScale;
        private float cachedRadius;

        protected override void OnInitialize()
        {
            gameState = Context.GameState;
            CurrentHealth = maxHealth;
            CurrentShield = maxShield;
            IsDestroyed = false;

            // Cache collider and initial scale
            planetCollider = GetComponent<SphereCollider>();
            if (planetCollider != null)
            {
                lastScale = transform.lossyScale;
                UpdateCachedRadius();
            }
        }

        private void UpdateCachedRadius()
        {
            // Get the largest scale component for safety
            float maxScale = Mathf.Max(lastScale.x, Mathf.Max(lastScale.y, lastScale.z));
            cachedRadius = planetCollider.radius * maxScale;
        }

        /// <summary>
        /// Efficiently finds the closest point on the planet's surface to a given position.
        /// This method accounts for non-uniform scaling and updates the cached radius if the planet's scale has changed.
        /// </summary>
        /// <param name="position">World position to check against</param>
        /// <returns>The closest point on the planet's surface in world space</returns>
        public Vector3 GetClosestSurfacePoint(Vector3 position)
        {
            if (planetCollider == null) return transform.position;

            // Check if scale changed
            if (transform.lossyScale != lastScale)
            {
                lastScale = transform.lossyScale;
                UpdateCachedRadius();
            }

            Vector3 directionToPosition = (position - transform.position).normalized;
            return transform.position + directionToPosition * cachedRadius;
        }

        /// <summary>
        /// Gets the current distance from a point to the planet's surface.
        /// Positive values indicate distance above surface, negative values indicate penetration.
        /// </summary>
        /// <param name="position">World position to check</param>
        /// <returns>Distance from the surface (positive if outside, negative if inside)</returns>
        public float GetDistanceFromSurface(Vector3 position)
        {
            if (planetCollider == null) return float.MaxValue;
            
            Vector3 closestPoint = GetClosestSurfacePoint(position);
            return Vector3.Distance(position, closestPoint);
        }

        protected override void OnDeinitialize()
        {
            OnHealthChanged = null;
            OnShieldChanged = null;
            OnDestroyed = null;
        }

        protected override void OnTick()
        {
            if (IsDestroyed) return;

            // Regenerate shield
            if (Time.time - lastDamageTime >= shieldRegenDelay && CurrentShield < maxShield)
            {
                float newShield = Mathf.Min(maxShield, CurrentShield + shieldRegenRate * Time.deltaTime);
                if (newShield != CurrentShield)
                {
                    CurrentShield = newShield;
                    OnShieldChanged?.Invoke(CurrentShield);
                }
            }
        }

        public void TakeDamage(float damage, Vector3 hitPoint)
        {
            if (IsDestroyed) return;

            lastDamageTime = Time.time;
            float remainingDamage = damage;

            // Apply damage to shield first
            if (CurrentShield > 0)
            {
                float shieldDamage = Mathf.Min(CurrentShield, remainingDamage);
                CurrentShield -= shieldDamage;
                remainingDamage -= shieldDamage;
                OnShieldChanged?.Invoke(CurrentShield);

                if (shieldHitEffect != null)
                {
                    shieldHitEffect.transform.position = hitPoint;
                    shieldHitEffect.Play();
                }
            }

            // Apply remaining damage to health
            if (remainingDamage > 0)
            {
                CurrentHealth = Mathf.Max(0, CurrentHealth - remainingDamage);
                OnHealthChanged?.Invoke(CurrentHealth);

                if (damageEffect != null)
                {
                    damageEffect.transform.position = hitPoint;
                    damageEffect.Play();
                }

                // Notify game state manager
                gameState?.TakeDamage(remainingDamage);

                // Check for destruction
                if (CurrentHealth <= 0 && !IsDestroyed)
                {
                    IsDestroyed = true;
                    if (destructionEffect != null)
                    {
                        destructionEffect.Play();
                    }
                    OnDestroyed?.Invoke();
                }
            }
        }

        public void Heal(float amount)
        {
            if (IsDestroyed || CurrentHealth >= maxHealth) return;

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth);
        }

        public void RepairShield(float amount)
        {
            if (IsDestroyed || CurrentShield >= maxShield) return;

            CurrentShield = Mathf.Min(maxShield, CurrentShield + amount);
            OnShieldChanged?.Invoke(CurrentShield);
        }

        public float GetPlanetRadius()
        {
            return transform.localScale.x / 2;
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            maxShield = Mathf.Max(0f, maxShield);
            shieldRegenRate = Mathf.Max(0f, shieldRegenRate);
            shieldRegenDelay = Mathf.Max(0f, shieldRegenDelay);
        }
    }
}
