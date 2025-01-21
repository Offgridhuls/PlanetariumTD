using UnityEngine;

[CreateAssetMenu(fileName = "EnemySpawnData", menuName = "PlanetariumTD/Enemy Spawn Data")]
public class EnemySpawnData : ScriptableObject
{
    [System.Serializable]
    public class SpawnParameters
    {
        public Vector3 spawnPosition;
        public float spawnHeight = 10f;
        public float spawnRadius = 20f;
        public bool randomizeRotation = true;
    }

    [Header("Prefab Reference")]
    public GameObject enemyPrefab;

    [Header("Base Stats")]
    public EnemyStats baseStats;
    
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float baseIntegrity = 1f;

    [Header("Reward Settings")]
    public int scoreValue = 10;
    public int resourceValue = 5;

    [Header("Visual Effects")]
    public GameObject spawnEffect;
    public GameObject deathEffect;
    public GameObject damageEffect;
    public GameObject healthBarPrefab;

    [Header("Additional Parameters")]
    public SpawnParameters spawnParams;

    [Header("Flying Enemy Parameters")]
    public bool isFlying = false;
    public float heightVariation = 2f;
    public float heightChangeSpeed = 1f;

    public Vector3 GetRandomSpawnPosition(Transform planetTransform, float planetRadius)
    {
        // Generate random spherical coordinates
        float theta = Random.Range(0f, Mathf.PI * 2); // Angle around the equator
        float phi = Mathf.Acos(Random.Range(-1f, 1f)); // Angle from the pole

        // Convert to Cartesian coordinates
        float x = Mathf.Sin(phi) * Mathf.Cos(theta);
        float y = Mathf.Sin(phi) * Mathf.Sin(theta);
        float z = Mathf.Cos(phi);

        Vector3 direction = new Vector3(x, y, z).normalized;
        float spawnDistance = planetRadius + spawnParams.spawnHeight;

        return planetTransform.position + direction * spawnDistance;
    }
}
