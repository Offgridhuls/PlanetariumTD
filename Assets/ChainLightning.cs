using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainLightning : ProjectileBase
{
    [SerializeField] ParticleSystem ChainParticles;
    public override void ShootProjectile(Vector3 target, GameObject Enemy)
    {
        ChainParticles.Play();
        var emitParams = new ParticleSystem.EmitParams();

        emitParams.position = transform.position;
        ChainParticles.Emit(emitParams, 1);

        emitParams.position = Enemy.transform.position;
        ChainParticles.Emit(emitParams, 1);
    }
    public override void OnProjectileHit()
    {
       
    }
}
