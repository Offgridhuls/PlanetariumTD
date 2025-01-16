using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ProjectileBase : MonoBehaviour
{
    private ParticleSystem ProjectileFX;

    protected Rigidbody RB;

    [SerializeField]
    protected float ProjectileSpeed;
    void Awake()
    {
        RB = GetComponent<Rigidbody>();
        ProjectileFX = GetComponent<ParticleSystem>();
    }
    public virtual void Start()
    {
        
    }
    public virtual void Update()
    { 
        
    }
    public abstract void ShootProjectile(Vector3 target, GameObject Enemy);

    public float GetProjectileSpeed()
    {
        return ProjectileSpeed;
    }

}
