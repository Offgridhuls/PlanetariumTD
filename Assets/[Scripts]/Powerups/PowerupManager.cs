using UnityEngine;
using System.Collections.Generic;
using Planetarium;

namespace Planetarium
{
    public class PowerupManager : SceneService
    {
        [Header("Powerup Settings")]
        [SerializeField] private GameObject powerupsContainer;
        [SerializeField] private PowerupBase[] powerupPrefabs;
        
        private Dictionary<System.Type, PowerupBase> powerups = new Dictionary<System.Type, PowerupBase>();
        private List<PowerupBase> activePowerups = new List<PowerupBase>();

        protected override void OnInitialize()
        {
            base.OnInitialize();
            InitializePowerups();
        }

        private void InitializePowerups()
        {
            // Create container if needed
            if (powerupsContainer == null)
            {
                powerupsContainer = new GameObject("Powerups");
                powerupsContainer.transform.SetParent(transform);
            }

            // Create and initialize each powerup
            foreach (var prefab in powerupPrefabs)
            {
                if (prefab == null) continue;

                // Instantiate the powerup
                PowerupBase powerup = Instantiate(prefab, powerupsContainer.transform);
                System.Type powerupType = powerup.GetType();

                // Initialize it
                powerup.Initialize(this);
                powerups[powerupType] = powerup;
            }
        }

        public T GetPowerup<T>() where T : PowerupBase
        {
            System.Type type = typeof(T);
            return powerups.ContainsKey(type) ? powerups[type] as T : null;
        }

        public void ActivatePowerup<T>() where T : PowerupBase
        {
            var powerup = GetPowerup<T>();
            if (powerup != null)
            {
                ActivatePowerup(powerup);
            }
        }

        public void DeactivatePowerup<T>() where T : PowerupBase
        {
            var powerup = GetPowerup<T>();
            if (powerup != null)
            {
                DeactivatePowerup(powerup);
            }
        }

        public void TogglePowerup<T>() where T : PowerupBase
        {
            var powerup = GetPowerup<T>();
            if (powerup != null)
            {
                TogglePowerup(powerup);
            }
        }

        public bool IsPowerupActive<T>() where T : PowerupBase
        {
            var powerup = GetPowerup<T>();
            return powerup != null && powerup.IsActive();
        }

        private void ActivatePowerup(PowerupBase powerup)
        {
            if (powerup == null || activePowerups.Contains(powerup)) return;

            powerup.Activate();
            activePowerups.Add(powerup);
        }

        private void DeactivatePowerup(PowerupBase powerup)
        {
            if (powerup == null || !activePowerups.Contains(powerup)) return;

            powerup.Deactivate();
            activePowerups.Remove(powerup);
        }

        private void TogglePowerup(PowerupBase powerup)
        {
            if (powerup == null) return;

            if (powerup.IsActive())
            {
                DeactivatePowerup(powerup);
            }
            else
            {
                ActivatePowerup(powerup);
            }
        }

        public List<PowerupBase> GetActivePowerups()
        {
            return new List<PowerupBase>(activePowerups);
        }

        protected override void OnDeinitialize()
        {
            base.OnDeinitialize();
            
            // Deactivate all powerups
            foreach (var powerup in activePowerups.ToArray())
            {
                DeactivatePowerup(powerup);
            }
            
            activePowerups.Clear();
            powerups.Clear();

            // Clean up powerups container
            if (powerupsContainer != null)
            {
                Destroy(powerupsContainer);
            }
        }
    }
}
