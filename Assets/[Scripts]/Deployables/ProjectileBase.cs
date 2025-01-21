using UnityEngine;

public abstract class ProjectileBase : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] protected float projectileSpeed = 20f;
    [SerializeField] protected float lifetime = 3f;
    [SerializeField] protected GameObject hitEffect;
    [SerializeField] protected LayerMask targetLayers;

    protected int damage;
    protected Vector3 targetPosition;
    protected bool isInitialized;
    protected float aliveTime;
    protected GameObject targetEnemy;
    protected Rigidbody rigidBody;

    protected virtual void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
    }

    public virtual void Initialize(int damage, Vector3 target)
    {
        this.damage = damage;
        this.targetPosition = target;
        this.isInitialized = true;
        this.aliveTime = 0f;
    }

    public abstract void ShootProjectile(Vector3 target, GameObject enemy);

    protected virtual void Update()
    {
        if (!isInitialized) return;

        aliveTime += Time.deltaTime;
        if (aliveTime >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        MoveProjectile();
        CheckCollisions();
    }

    protected virtual void MoveProjectile()
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position += direction * projectileSpeed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    protected virtual void CheckCollisions()
    {
        RaycastHit[] hits = Physics.SphereCastAll(
            transform.position,
            0.5f,
            transform.forward,
            projectileSpeed * Time.deltaTime,
            targetLayers
        );

        foreach (RaycastHit hit in hits)
        {
            HandleHit(hit);
        }
    }

    protected virtual void HandleHit(RaycastHit hit)
    {
        DealDamage(hit.collider.gameObject);
        if (hitEffect != null)
        {
            Instantiate(hitEffect, hit.point, Quaternion.LookRotation(hit.normal));
        }
        OnProjectileHit();
        Destroy(gameObject);
    }

    protected virtual void DealDamage(GameObject target)
    {
        var damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
    }

    public abstract void OnProjectileHit();

    public float GetProjectileSpeed()
    {
        return projectileSpeed;
    }

    public void SetProjectileSpeed(float speed)
    {
        projectileSpeed = speed;
    }

    public float GetLifetime()
    {
        return lifetime;
    }

    public void SetLifetime(float time)
    {
        lifetime = time;
    }

    public void SetDamage(int amount)
    {
        damage = amount;
    }
}
