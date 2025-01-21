using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainLightning : ProjectileBase
{
    [SerializeField] private GameObject ChainLineRendererPrefab; // Prefab with LineRenderer
    [SerializeField] private int MaxAffectedEnemies = 5; // Maximum number of enemies the lightning can chain to
    [SerializeField] private float ChainRange = 10f; // Range to find the next enemy
    [SerializeField] private float DelayBetweenChains = 0.2f; // Delay between each chain

    private int AffectedEnemies = 0;
    private List<EnemyBase> EnemiesInChain = new List<EnemyBase>(); // Track already affected enemies
    private List<GameObject> SpawnedLineRenderers = new List<GameObject>(); // Track spawned line renderers

    public override void ShootProjectile(Vector3 target, GameObject targetObject)
    {
        if (targetObject != null && targetObject.TryGetComponent(out EnemyBase initialEnemy))
        {
            StartCoroutine(ChainReaction(initialEnemy));
        }
    }

    public override void OnProjectileHit()
    {
        // Optional: Logic for when the projectile initially hits
    }

    public override void Update()
    {
        // Optional: Logic for ongoing updates
    }

    private IEnumerator ChainReaction(EnemyBase currentEnemy)
    {
        while (AffectedEnemies < MaxAffectedEnemies)
        {
            if (currentEnemy == null) yield break;

            AffectedEnemies++;
            EnemiesInChain.Add(currentEnemy);

            // Find the next closest enemy
            EnemyBase nextEnemy = FindClosestEnemy(currentEnemy.transform.position);
            if (nextEnemy == null) yield break;

            // Spawn the LineRenderer for this segment
            GameObject lineRendererObject = Instantiate(ChainLineRendererPrefab);
            SpawnedLineRenderers.Add(lineRendererObject);

            var chainController = lineRendererObject.GetComponent<ChainLightningController>();
            if (chainController != null)
            {
                chainController.SetPosition(currentEnemy.transform.position, nextEnemy.transform.position);
            }

            yield return new WaitForSeconds(DelayBetweenChains);

            // Continue the chain to the next enemy
            currentEnemy = nextEnemy;
        }
    }

    private EnemyBase FindClosestEnemy(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, ChainRange); // Find objects in range
        EnemyBase closestEnemy = null;
        float closestDistanceSqr = float.MaxValue;

        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent(out EnemyBase enemy) && !EnemiesInChain.Contains(enemy))
            {
                float distanceSqr = (enemy.transform.position - position).sqrMagnitude;
                if (distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    closestEnemy = enemy;
                }
            }
        }

        return closestEnemy;
    }

    public void DestroyLines()
    {
        foreach (var line in SpawnedLineRenderers)
        {
            Destroy(line);
        }
        SpawnedLineRenderers.Clear();
        EnemiesInChain.Clear();
        AffectedEnemies = 0;
    }
}
