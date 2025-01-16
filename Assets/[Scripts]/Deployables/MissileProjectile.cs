using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class MissileProjectile : ProjectileBase
{
    private Vector3 targetPosition;
    private bool isLaunched = false;
    private GameObject EnemyObject;
    // Start is called before the first frame update
    public override void ShootProjectile(Vector3 target, GameObject enemyObject)
    {
        targetPosition = enemyObject.transform.position;
        isLaunched = true;
        EnemyObject = enemyObject;
        // Ensure the missile is facing the target
        Vector3 direction = (target - transform.position).normalized;
        transform.LookAt(target);
    }
    public override void Update()
    {
        if (isLaunched)
        {
            // Move towards the target
            Vector3 direction = (EnemyObject.transform.position - transform.position).normalized;
            RB.velocity = direction * ProjectileSpeed;
            Vector3 lookAtTarget = (EnemyObject.transform.position - transform.position).normalized;
            transform.LookAt(lookAtTarget);
            // Optional: Destroy the missile when it reaches the target
            if (Vector3.Distance(transform.position, EnemyObject.transform.position) < 0.5f)
            {
                Explode();
            }
        }
    }
    private void Explode()
    {

    }
}
