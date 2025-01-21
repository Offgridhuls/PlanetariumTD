using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Properties")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private GameObject hitEffect;

    private Transform target;
    private Vector3 lastKnownPosition;

    public void Initialize(Transform target)
    {
        this.target = target;
        if (target != null)
        {
            lastKnownPosition = target.position;
        }
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (target == null)
        {
            MoveToLastKnownPosition();
            return;
        }

        lastKnownPosition = target.position;
        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
        transform.LookAt(target);
    }

    private void MoveToLastKnownPosition()
    {
        Vector3 direction = (lastKnownPosition - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
        
        if (Vector3.Distance(transform.position, lastKnownPosition) < 0.1f)
        {
            DestroyBullet();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            DestroyBullet();
        }
    }

    private void DestroyBullet()
    {
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}
