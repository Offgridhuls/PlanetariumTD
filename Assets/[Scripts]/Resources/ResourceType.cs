using UnityEngine;

namespace Planetarium
{
    [CreateAssetMenu(fileName = "New Resource", menuName = "PlanetariumTD/Resource Type")]
    public class ResourceType : ScriptableObject
    {
        [Header("Resource Settings")]
        public string resourceName;
        public string description;
        public Sprite icon;
        public GameObject pickupPrefab;
        public Color resourceColor = Color.white;

        [Header("Physics Settings")]
        [Tooltip("Force applied towards the planet")]
        public float gravitationSpeed = 20f;
        [Tooltip("Radius for player pickup detection")]
        public float pickupRadius = 1f;
    }
}
