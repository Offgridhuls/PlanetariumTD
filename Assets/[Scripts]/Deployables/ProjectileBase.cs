using UnityEngine;

public abstract class ProjectileBase : MonoBehaviour
{
    [Header("Base Projectile Settings")]
    [SerializeField] protected float projectileSpeed = 20f;
    [SerializeField] protected float maxLifetime = 3f;
    [SerializeField] protected LayerMask targetLayers;
    [SerializeField] protected float effectLifetime = 2f;

    protected Vector3 targetPosition;
    protected GameObject targetEnemy;
    protected int damage;
    protected bool isInitialized;
    protected float aliveTime;

    public virtual void Initialize(int damage, Vector3 target, float speed)
    {
        this.damage = damage;
        this.targetPosition = target;
        this.projectileSpeed = speed;
        aliveTime = 0f;
    }

    public abstract void ShootProjectile(Vector3 target, GameObject enemy);

    protected virtual void Update()
    {
        if (!isInitialized) return;

        aliveTime += Time.deltaTime;
        if (aliveTime >= maxLifetime)
        {
            OnProjectileHit();
            Destroy(gameObject);
            return;
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!isInitialized) return;

        // Check if we hit something in our target layers
        if (((1 << other.gameObject.layer) & targetLayers) != 0)
        {
            HandleHit(other.gameObject);
        }
    }

    protected virtual void HandleHit(GameObject hitObject)
    {
        var damageable = hitObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            DamageData damageData = new DamageData
            {
                Damage = damage,
                Source = gameObject
            };
            damageable.ProcessDamage(damageData);
        }

        OnProjectileHit();
        Destroy(gameObject);
    }

    public virtual void OnProjectileHit()
    {
        // Override in derived classes for specific hit effects
    }

    protected virtual void OnDestroy()
    {
        // Cleanup any remaining effects
        OnProjectileHit();
    }
}
