using UnityEngine;

[CreateAssetMenu(fileName = "EnemySpawnData", menuName = "Game/Enemy Spawn Data")]
public class EnemySpawnData : ScriptableObject
{
    [Header("Prefab")]
    public GameObject enemyPrefab;

    [Header("Base Stats")]
    public float maxHealth = 100f;
    public float damage = 10f;
    public float integrity = 0.15f;
    
    [Header("Movement")]
    public float RotSpeed = 180f;
    public float MoveSpeed = 50f;

    [Header("Score and Resources")]
    public int scoreValue = 10;
    public int resourceValue = 5;

    
    [Header("Attacking")]
    public float attackSpeed = 2f ;
    public int attackDamage = 20;
    public int attackRange = 50;
    public int ProjectileSpeed = 80;
    
    [Header("Effects")]
    public GameObject spawnEffect;
    public GameObject deathEffect;
    public GameObject damageEffect;
    public GameObject healthBarPrefab;
}
