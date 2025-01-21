using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStats", menuName = "PlanetariumTD/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    [Header("Basic Stats")]
    [SerializeField] private string enemyName = "Default Enemy";
    [SerializeField] private float healthPoints = 100f;
    [SerializeField] private float integrity = 1f;

    [Header("Movement Stats")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotSpeed = 180f;
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float deceleration = 4f;

    [Header("Combat Stats")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float attackSpeed = 1f;

    [Header("Rewards")]
    [SerializeField] private int scoreValue = 10;
    [SerializeField] private int resourceValue = 5;

    // Basic Stats
    public string GetName() => enemyName;
    public float GetHealth() => healthPoints;
    public float GetIntegrity() => integrity;

    // Movement Stats
    public float MoveSpeed => moveSpeed;
    public float RotSpeed => rotSpeed;
    public float GetAcceleration() => acceleration;
    public float GetDeceleration() => deceleration;

    // Combat Stats
    public int GetDamage() => damage;
    public float GetAttackRange() => attackRange;
    public float GetAttackSpeed() => attackSpeed;

    // Rewards
    public int GetScoreValue() => scoreValue;
    public int GetResourceValue() => resourceValue;
}
