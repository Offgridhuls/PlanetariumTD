using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletProjectile : ProjectileBase
{
    public override void ShootProjectile(Vector3 target)
    {
        if (RB != null)
        {
            Vector3 targetDirection = (target - transform.position).normalized;
            RB.AddForce(targetDirection * ProjectileSpeed, ForceMode.Impulse);
        }
    }
}
