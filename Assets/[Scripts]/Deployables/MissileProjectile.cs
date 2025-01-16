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
        Vector3 direction = (target - transform.position).normalized;
        transform.LookAt(target);
    }
    public override void Update()
    {
        if (isLaunched)
        {
            Vector3 direction = (EnemyObject.transform.position - transform.position).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 1000f * Time.deltaTime);

            RB.velocity = direction * ProjectileSpeed;
            if (Vector3.Distance(transform.position, EnemyObject.transform.position) < 0.5f)
            {
                OnProjectileHit();
            }
        }
    }
    public override void OnProjectileHit()
    {
        var iFX = Instantiate(ImpactFX, transform.position, Quaternion.identity);
        Destroy(this.gameObject);
    }
}
