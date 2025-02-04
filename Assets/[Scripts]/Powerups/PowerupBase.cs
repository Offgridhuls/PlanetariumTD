using UnityEngine;
using Planetarium;

namespace Planetarium
{
    public abstract class PowerupBase : CoreBehaviour
    {
        [Header("Powerup Settings")]
        [SerializeField] protected bool isActiveByDefault = false;
        
        protected bool isActive = false;
        protected bool isInitialized = false;
        protected PowerupManager manager;

        public virtual void Initialize(PowerupManager powerupManager)
        {
            if (isInitialized) return;
            
            manager = powerupManager;
            isInitialized = true;
            
            if (isActiveByDefault)
            {
                Activate();
            }
        }

        protected virtual void Update()
        {
            if (!isInitialized || !isActive) return;
            OnPowerupUpdate();
        }

        /// <summary>
        /// Activates the powerup effect
        /// </summary>
        public virtual void Activate()
        {
            if (!isInitialized || isActive) return;
            
            isActive = true;
            OnPowerupActivate();
        }

        /// <summary>
        /// Deactivates the powerup effect
        /// </summary>
        public virtual void Deactivate()
        {
            if (!isInitialized || !isActive) return;
            
            isActive = false;
            OnPowerupDeactivate();
        }

        /// <summary>
        /// Called every frame while the powerup is active
        /// </summary>
        protected abstract void OnPowerupUpdate();

        /// <summary>
        /// Called when the powerup is activated
        /// </summary>
        protected abstract void OnPowerupActivate();

        /// <summary>
        /// Called when the powerup is deactivated
        /// </summary>
        protected abstract void OnPowerupDeactivate();

        /// <summary>
        /// Gets whether the powerup is currently active
        /// </summary>
        public bool IsActive() => isActive;
    }
}
