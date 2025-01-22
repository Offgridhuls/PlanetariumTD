using UnityEngine;

[CreateAssetMenu(fileName = "TurretStats", menuName = "PlanetariumTD/Turret Stats")]
public class TurretStats : ScriptableObject
{
    [Header("Basic Stats")]
    [SerializeField] private string turretName = "Default Turret";
    [SerializeField] private float healthPoints = 100f;

    [Header("Combat Stats")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float fireInterval = 1f;
    [SerializeField] private float agroRadius = 5f;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float projectileSpeed = 20f;
    [SerializeField] private float projectileLifetime = 3f;

    [Header("Costs")]
    [SerializeField] private int scrapCost = 10;
    [SerializeField] private int coinCost = 10;

    // Basic Stats
    public string GetName() => turretName;
    public float GetHealth() => healthPoints;

    // Combat Stats
    public int GetDamage() => damage;
    public float GetFireInterval() => fireInterval;
    public float GetAgroRadius() => agroRadius;
    public float GetRotationSpeed() => rotationSpeed;
    public float GetProjectileSpeed() => projectileSpeed;
    public float GetProjectileLifetime() => projectileLifetime;

    // Costs
    public int GetScrapCost() => scrapCost;
    public int GetCoinCost() => coinCost;
}