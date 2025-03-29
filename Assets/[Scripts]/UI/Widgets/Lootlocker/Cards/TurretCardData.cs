using UnityEngine;

namespace Planetarium.UI
{
    // Simple class to hold basic turret card data
    [System.Serializable]
    public class TurretCardData
    {
        public string Id;
        public string Name;
        public string Description;
        public float Damage;
        public float FireRate;
    }
}